import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import {
  buildProposalRevisionFromBuyerPo,
  formatProposalChangeSummary,
} from "@/features/purchasing/po-proposal-revision";

export type PurchaseOrderActivityKind =
  | "created"
  | "submitted"
  | "supplier_accepted"
  | "supplier_declined"
  | "supplier_preparing"
  | "supplier_ready"
  | "changes_proposed"
  | "stock_reserved"
  | "proposal_reservation"
  | "reservation_confirmed"
  | "reservation_released"
  | "reservation_expired"
  | "withdrawn"
  | "cancelled"
  | "receipt"
  | "receipt_reversed"
  | "remaining_closed"
  | "awaiting_payment"
  | "payment_confirmed"
  | "no_payment_due"
  | "completed";

export type PurchaseOrderActivityReceiptLine = {
  productName: string;
  uom: string;
  goodQty: number;
  /** Cumulative received on PO line after this receipt when ordered known; else null. */
  orderedQty: number | null;
  damagedQty: number;
  cancelledRemainingQty: number;
};

export type PurchaseOrderActivityEvent = {
  id: string;
  kind: PurchaseOrderActivityKind;
  atUtc: string;
  actorId?: string | null;
  grnNumber?: string;
  receiptId?: string;
  receiptStatus?: string;
  lines?: PurchaseOrderActivityReceiptLine[];
  /** Partial vs fully received for this receipt relative to PO after posting. */
  receiptResult?: "partial" | "fully_received" | "reversed";
  /** Proposal revision details for changes_proposed / proposal_reservation. */
  proposalSummary?: {
    changedLines: Array<{ productName: string; detail: string }>;
    originalTotal: number | null;
    proposedTotal: number | null;
    reservationExpiresAtUtc: string | null;
  };
  /** Optional seller settlement remarks (payment confirmed). */
  note?: string | null;
};

function compareUtc(a: string, b: string): number {
  return a.localeCompare(b);
}

/**
 * Build chronological PO activity from authoritative PO + goods-receipt fields only.
 * Omits events that lack timestamps on the current DTO.
 */
export function buildPurchaseOrderActivityEvents(input: {
  po: PosPurchaseOrderDto;
  receipts: readonly PosGoodsReceiptDto[];
}): PurchaseOrderActivityEvent[] {
  const { po, receipts } = input;
  const events: PurchaseOrderActivityEvent[] = [];
  const orderedByProduct = new Map(
    po.lines
      .filter((line) => line.productId)
      .map((line) => [line.productId!, line.orderedQty] as const),
  );

  if (po.createdAtUtc?.trim()) {
    events.push({
      id: `created:${po.purchaseOrderId}`,
      kind: "created",
      atUtc: po.createdAtUtc,
    });
  }

  if (po.orderedAtUtc?.trim()) {
    events.push({
      id: `submitted:${po.purchaseOrderId}`,
      kind: "submitted",
      atUtc: po.orderedAtUtc,
      actorId: po.orderedBy ?? null,
    });
  }

  if (po.supplierAcceptedAtUtc?.trim()) {
    events.push({
      id: `accepted:${po.purchaseOrderId}`,
      kind: "supplier_accepted",
      atUtc: po.supplierAcceptedAtUtc,
    });
  }

  if (po.supplierDeclinedAtUtc?.trim()) {
    events.push({
      id: `declined:${po.purchaseOrderId}`,
      kind: "supplier_declined",
      atUtc: po.supplierDeclinedAtUtc,
    });
  }

  if (po.supplierPreparingAtUtc?.trim()) {
    events.push({
      id: `preparing:${po.purchaseOrderId}:${po.supplierPreparingAtUtc}`,
      kind: "supplier_preparing",
      atUtc: po.supplierPreparingAtUtc,
    });
  }

  if (po.supplierFulfilledAtUtc?.trim()) {
    events.push({
      id: `fulfilled:${po.purchaseOrderId}:${po.supplierFulfilledAtUtc}`,
      kind: "supplier_ready",
      atUtc: po.supplierFulfilledAtUtc,
    });
  }

  if (po.changesProposedAtUtc?.trim()) {
    const revision = buildProposalRevisionFromBuyerPo(po);
    const changedLines =
      revision?.lines
        .filter((line) => line.changed)
        .map((line) => ({
          productName: line.productName,
          detail: formatProposalChangeSummary(line),
        })) ?? [];
    events.push({
      id: `changes:${po.purchaseOrderId}`,
      kind: "changes_proposed",
      atUtc: po.changesProposedAtUtc,
      proposalSummary: {
        changedLines,
        originalTotal: revision?.originalTotal ?? null,
        proposedTotal: revision?.proposedTotal ?? po.proposedTotalAmount ?? null,
        reservationExpiresAtUtc: po.inventoryReservationExpiresAtUtc?.trim() || null,
      },
    });
  }

  const reservationState = po.inventoryReservationState ?? null;
  const reservationExpires = po.inventoryReservationExpiresAtUtc?.trim() || null;
  if (reservationState === "TemporaryProposal" && (po.changesProposedAtUtc?.trim() || reservationExpires)) {
    events.push({
      id: `proposal-reservation:${po.purchaseOrderId}`,
      kind: "proposal_reservation",
      atUtc: po.changesProposedAtUtc?.trim() || reservationExpires!,
    });
  }
  if (reservationState === "Confirmed" && po.supplierAcceptedAtUtc?.trim()) {
    events.push({
      id: `stock-reserved:${po.purchaseOrderId}`,
      kind: "stock_reserved",
      atUtc: po.supplierAcceptedAtUtc,
    });
    if (po.changesProposedAtUtc?.trim()) {
      events.push({
        id: `reservation-confirmed:${po.purchaseOrderId}`,
        kind: "reservation_confirmed",
        atUtc: po.supplierAcceptedAtUtc,
      });
    }
  }
  if (reservationState === "Released") {
    const releasedAt =
      po.supplierDeclinedAtUtc?.trim() ||
      po.withdrawnAtUtc?.trim() ||
      po.cancelledAtUtc?.trim() ||
      po.updatedAtUtc?.trim();
    if (releasedAt) {
      events.push({
        id: `reservation-released:${po.purchaseOrderId}`,
        kind: "reservation_released",
        atUtc: releasedAt,
      });
    }
  }
  if (
    reservationState === "Released" &&
    reservationExpires &&
    po.displayStatus !== "ChangesNeedApproval" &&
    !po.supplierDeclinedAtUtc &&
    !po.withdrawnAtUtc
  ) {
    events.push({
      id: `reservation-expired:${po.purchaseOrderId}`,
      kind: "reservation_expired",
      atUtc: reservationExpires,
    });
  }

  if (po.withdrawnAtUtc?.trim()) {
    events.push({
      id: `withdrawn:${po.purchaseOrderId}`,
      kind: "withdrawn",
      atUtc: po.withdrawnAtUtc,
      actorId: po.cancelledByUserId ?? null,
    });
  } else if (po.cancelledAtUtc?.trim()) {
    // Local/draft cancel — distinct from connected buyer withdrawal.
    events.push({
      id: `cancelled:${po.purchaseOrderId}`,
      kind: "cancelled",
      atUtc: po.cancelledAtUtc,
      actorId: po.cancelledByUserId ?? null,
    });
  }

  const postedReceipts = [...receipts].sort((a, b) =>
    compareUtc(a.receivedAtUtc, b.receivedAtUtc),
  );

  for (const receipt of postedReceipts) {
    const lines: PurchaseOrderActivityReceiptLine[] = receipt.lines.map((line) => ({
      productName: line.nameSnapshot,
      uom: line.uomSnapshot,
      goodQty: line.quantityReceived ?? line.receivedQty ?? 0,
      orderedQty: orderedByProduct.get(line.productId) ?? null,
      damagedQty: line.damagedQty ?? 0,
      cancelledRemainingQty: line.shortClosedQty ?? 0,
    }));

    const isVoided = (receipt.status ?? "Posted") === "Voided";
    events.push({
      id: `receipt:${receipt.goodsReceiptId}`,
      kind: "receipt",
      atUtc: receipt.receivedAtUtc,
      actorId: receipt.receivedBy,
      grnNumber: receipt.grnNumber,
      receiptId: receipt.goodsReceiptId,
      receiptStatus: receipt.status,
      lines,
      receiptResult: isVoided ? "reversed" : undefined,
    });

    if (isVoided && receipt.voidedAtUtc?.trim()) {
      events.push({
        id: `receipt-reversed:${receipt.goodsReceiptId}`,
        kind: "receipt_reversed",
        atUtc: receipt.voidedAtUtc,
        actorId: receipt.voidedByUserId ?? null,
        grnNumber: receipt.grnNumber,
        receiptId: receipt.goodsReceiptId,
        receiptStatus: receipt.status,
      });
    }
  }

  const lastPostedReceiptAt = [...postedReceipts]
    .filter((r) => (r.status ?? "Posted") === "Posted")
    .sort((a, b) => compareUtc(a.receivedAtUtc, b.receivedAtUtc))
    .at(-1)?.receivedAtUtc;

  // Settlement gate for pay-on-delivery/receipt: goods received but payment still outstanding.
  const settlement = po.financialSettlementStatus ?? "NotRequired";
  if (po.status === "Received" && settlement !== "NotRequired") {
    const awaitingAt = lastPostedReceiptAt?.trim() || po.remainingClosedAtUtc?.trim();
    if (awaitingAt) {
      events.push({
        id: `awaiting-payment:${po.purchaseOrderId}`,
        kind: "awaiting_payment",
        atUtc: awaitingAt,
      });
    }
    if (settlement === "Settled" && po.financiallySettledAtUtc?.trim()) {
      events.push({
        id: `settled:${po.purchaseOrderId}`,
        kind: (po.amountPaidSnapshot ?? 0) > 0 ? "payment_confirmed" : "no_payment_due",
        atUtc: po.financiallySettledAtUtc,
        note: po.sellerSettlementRemarks?.trim() || null,
      });
    }
  }

  // Completion: only when PO is fully received; timestamp = last posted (non-void) receipt.
  if (po.remainingClosedAtUtc?.trim()) {
    events.push({
      id: `remaining-closed:${po.purchaseOrderId}`,
      kind: "remaining_closed",
      atUtc: po.remainingClosedAtUtc,
      actorId: po.remainingClosedByUserId ?? null,
    });
  } else if (po.status === "Received" && settlement !== "AwaitingPayment") {
    const lastPosted = [...postedReceipts]
      .filter((r) => (r.status ?? "Posted") === "Posted")
      .sort((a, b) => compareUtc(a.receivedAtUtc, b.receivedAtUtc))
      .at(-1);
    if (lastPosted?.receivedAtUtc?.trim()) {
      events.push({
        id: `completed:${po.purchaseOrderId}`,
        kind: "completed",
        atUtc: lastPosted.receivedAtUtc,
        actorId: lastPosted.receivedBy,
      });
    }
  }

  // Annotate receiptResult for posted receipts using PO line outstanding after all posted GRs
  // is expensive; instead: if PO currently Received and this is last posted receipt → fully,
  // else if any line good < ordered on that receipt snapshot → partial when outstanding remains.
  const lastPostedId = [...postedReceipts]
    .filter((r) => (r.status ?? "Posted") === "Posted")
    .sort((a, b) => compareUtc(a.receivedAtUtc, b.receivedAtUtc))
    .at(-1)?.goodsReceiptId;

  for (const event of events) {
    if (event.kind !== "receipt" || event.receiptResult === "reversed") {
      continue;
    }
    if (po.status === "Received" && event.receiptId === lastPostedId) {
      event.receiptResult = "fully_received";
    } else {
      event.receiptResult = "partial";
    }
  }

  return events.sort((a, b) => {
    const byTime = compareUtc(a.atUtc, b.atUtc);
    if (byTime !== 0) {
      return byTime;
    }
    // Stable tie-break: created before submitted before receipts before completed.
    const order: Record<PurchaseOrderActivityKind, number> = {
      created: 0,
      submitted: 1,
      supplier_accepted: 2,
      supplier_declined: 2,
      supplier_preparing: 3,
      supplier_ready: 4,
      changes_proposed: 2,
      stock_reserved: 2,
      proposal_reservation: 2,
      reservation_confirmed: 2,
      reservation_released: 2,
      reservation_expired: 2,
      withdrawn: 2,
      cancelled: 2,
      receipt: 3,
      receipt_reversed: 4,
      remaining_closed: 5,
      awaiting_payment: 5,
      payment_confirmed: 6,
      no_payment_due: 6,
      completed: 7,
    };
    return order[a.kind] - order[b.kind];
  });
}

export function formatActivityDateTime(
  isoUtc: string,
  locale = "en-PH",
): { date: string; time: string } {
  const d = new Date(isoUtc);
  if (Number.isNaN(d.getTime())) {
    return { date: isoUtc, time: "" };
  }
  return {
    date: new Intl.DateTimeFormat(locale, {
      month: "short",
      day: "numeric",
      year: "numeric",
    }).format(d),
    time: new Intl.DateTimeFormat(locale, {
      hour: "numeric",
      minute: "2-digit",
    }).format(d),
  };
}

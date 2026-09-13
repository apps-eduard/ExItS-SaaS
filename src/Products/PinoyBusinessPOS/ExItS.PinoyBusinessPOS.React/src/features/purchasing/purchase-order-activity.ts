import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";

export type PurchaseOrderActivityKind =
  | "created"
  | "submitted"
  | "supplier_accepted"
  | "supplier_declined"
  | "changes_proposed"
  | "withdrawn"
  | "receipt"
  | "receipt_reversed"
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

  if (po.changesProposedAtUtc?.trim()) {
    events.push({
      id: `changes:${po.purchaseOrderId}`,
      kind: "changes_proposed",
      atUtc: po.changesProposedAtUtc,
    });
  }

  if (po.withdrawnAtUtc?.trim()) {
    events.push({
      id: `withdrawn:${po.purchaseOrderId}`,
      kind: "withdrawn",
      atUtc: po.withdrawnAtUtc,
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

  // Completion: only when PO is fully received; timestamp = last posted (non-void) receipt.
  if (po.status === "Received") {
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
      changes_proposed: 2,
      withdrawn: 2,
      receipt: 3,
      receipt_reversed: 4,
      completed: 5,
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

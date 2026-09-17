import { roundMoneyAmount } from "@/lib/money-input";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import type { ConnectedPurchaseOrder, ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import type { PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";

export type ProposalRevisionLine = {
  productId: string;
  productName: string;
  sku: string | null;
  unitOfMeasureCode: string;
  requestedQty: number;
  proposedQty: number;
  originalUnitCost: number;
  proposedUnitCost: number;
  originalLineTotal: number;
  proposedLineTotal: number;
  qtyChanged: boolean;
  priceChanged: boolean;
  unavailable: boolean;
  changed: boolean;
};

export type ProposalRevisionView = {
  lines: ProposalRevisionLine[];
  originalTotal: number;
  proposedTotal: number;
  difference: number;
  reservationExpiresAtUtc: string | null;
  changesProposedAtUtc: string | null;
};

function lineFromConnected(line: ConnectedPurchaseOrderLine): ProposalRevisionLine {
  const unavailable =
    (line.availability ?? "").toLowerCase() === "unavailable" ||
    (line.proposedQty != null && line.proposedQty <= 0);
  const requestedQty = line.qty;
  const proposedQty = unavailable
    ? 0
    : (line.proposedQty ?? line.qty);
  const originalUnitCost = line.unitPriceSnapshot;
  const proposedUnitCost = line.proposedUnitPrice ?? line.unitPriceSnapshot;
  const originalLineTotal = line.lineTotal;
  const proposedLineTotal =
    line.proposedLineTotal != null
      ? line.proposedLineTotal
      : roundMoneyAmount(proposedQty * proposedUnitCost);
  const qtyChanged = unavailable || proposedQty !== requestedQty;
  const priceChanged =
    line.proposedUnitPrice != null && line.proposedUnitPrice !== line.unitPriceSnapshot;

  return {
    productId: line.productId,
    productName: line.nameSnapshot,
    sku: line.skuSnapshot?.trim() || null,
    unitOfMeasureCode: line.unitOfMeasureCode,
    requestedQty,
    proposedQty,
    originalUnitCost,
    proposedUnitCost,
    originalLineTotal,
    proposedLineTotal,
    qtyChanged,
    priceChanged,
    unavailable,
    changed: qtyChanged || priceChanged,
  };
}

export function buildProposalRevisionFromConnectedOrder(
  order: ConnectedPurchaseOrder,
): ProposalRevisionView {
  const lines = order.lines.map(lineFromConnected);
  const originalTotal = order.totalAmount;
  const proposedTotal =
    order.proposedTotalAmount != null
      ? order.proposedTotalAmount
      : roundMoneyAmount(lines.reduce((sum, line) => sum + line.proposedLineTotal, 0));
  return {
    lines,
    originalTotal,
    proposedTotal,
    difference: roundMoneyAmount(proposedTotal - originalTotal),
    reservationExpiresAtUtc: order.inventoryReservationExpiresAtUtc?.trim() || null,
    changesProposedAtUtc: order.changesProposedAtUtc?.trim() || null,
  };
}

export function buildProposalRevisionFromBuyerPo(po: PosPurchaseOrderDto): ProposalRevisionView | null {
  const connected = po.connectedLines;
  if (!connected || connected.length === 0) {
    return null;
  }

  const lines: ProposalRevisionLine[] = connected.map((raw) => {
    const productId = String(raw.productId ?? "");
    const buyerLine = po.lines.find((l) => l.productId === productId);
    const requestedQty = Number(raw.qty ?? raw.orderedQty ?? buyerLine?.orderedQty ?? 0);
    const proposedQtyRaw = raw.proposedQty ?? raw.proposedOrderedQty;
    const availability = String(raw.availability ?? "");
    const unavailable =
      availability.toLowerCase() === "unavailable" ||
      (proposedQtyRaw != null && Number(proposedQtyRaw) <= 0);
    const proposedQty = unavailable ? 0 : Number(proposedQtyRaw ?? requestedQty);
    const originalUnitCost = Number(
      raw.unitPriceSnapshot ?? raw.unitPurchaseCost ?? buyerLine?.unitPurchaseCost ?? 0,
    );
    const proposedUnitCostRaw = raw.proposedUnitPrice ?? raw.proposedUnitPurchaseCost;
    const proposedUnitCost =
      proposedUnitCostRaw != null ? Number(proposedUnitCostRaw) : originalUnitCost;
    const originalLineTotal = Number(
      raw.lineTotal ?? roundMoneyAmount(requestedQty * originalUnitCost),
    );
    const proposedLineTotal = Number(
      raw.proposedLineTotal != null
        ? raw.proposedLineTotal
        : roundMoneyAmount(proposedQty * proposedUnitCost),
    );
    const qtyChanged = unavailable || proposedQty !== requestedQty;
    const priceChanged =
      proposedUnitCostRaw != null && Number(proposedUnitCostRaw) !== originalUnitCost;
    const uom =
      String(raw.unitOfMeasureCode ?? buyerLine?.uomSnapshot ?? "").trim() || "Piece";

    return {
      productId,
      productName: String(raw.nameSnapshot ?? buyerLine?.nameSnapshot ?? "Product"),
      sku: (raw.skuSnapshot ?? buyerLine?.skuSnapshot)?.toString().trim() || null,
      unitOfMeasureCode: uom,
      requestedQty,
      proposedQty,
      originalUnitCost,
      proposedUnitCost,
      originalLineTotal,
      proposedLineTotal,
      qtyChanged,
      priceChanged,
      unavailable,
      changed: qtyChanged || priceChanged,
    };
  });

  const originalTotal = roundMoneyAmount(
    lines.reduce((sum, line) => sum + line.originalLineTotal, 0),
  );
  const proposedTotal =
    po.proposedTotalAmount != null
      ? po.proposedTotalAmount
      : roundMoneyAmount(lines.reduce((sum, line) => sum + line.proposedLineTotal, 0));

  return {
    lines,
    originalTotal,
    proposedTotal,
    difference: roundMoneyAmount(proposedTotal - originalTotal),
    reservationExpiresAtUtc: po.inventoryReservationExpiresAtUtc?.trim() || null,
    changesProposedAtUtc: po.changesProposedAtUtc?.trim() || null,
  };
}

export function formatProposalQty(qty: number, unitOfMeasureCode: string): string {
  return formatStockQtyLabel(qty, unitOfMeasureCode);
}

export function formatProposalChangeSummary(line: ProposalRevisionLine): string {
  if (line.unavailable) {
    return `${formatProposalQty(line.requestedQty, line.unitOfMeasureCode)} → unavailable`;
  }
  const parts: string[] = [];
  if (line.qtyChanged) {
    parts.push(
      `${formatProposalQty(line.requestedQty, line.unitOfMeasureCode)} → ${formatProposalQty(line.proposedQty, line.unitOfMeasureCode)}`,
    );
  }
  if (line.priceChanged) {
    parts.push(`₱${line.originalUnitCost.toFixed(2)} → ₱${line.proposedUnitCost.toFixed(2)}`);
  }
  return parts.join(", ");
}

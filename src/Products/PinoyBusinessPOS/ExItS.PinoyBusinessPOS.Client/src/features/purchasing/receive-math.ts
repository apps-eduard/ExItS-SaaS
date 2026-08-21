/**
 * Partial receive math / over-receipt denial — UI helpers only.
 * Server remains authoritative for inventory quantities.
 */

export type ReceiveLineInput = {
  productId: string;
  outstandingQty: number;
  goodQty: number;
  damagedQty: number;
  closeRemaining: boolean;
};

export type ReceiveLinePlan = {
  productId: string;
  receiveQty: number;
  damagedQty: number;
  shortClosedQty: number;
  remainingAfter: number;
  discrepancyKind: "Damaged" | "Short" | null;
};

export type BuildReceivePlanResult =
  | { ok: true; lines: ReceiveLinePlan[] }
  | { ok: false; error: "invalid_qty" | "over_receive" | "no_activity" };

export function parseNonNegativeQty(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return 0;
  }
  const n = Number(trimmed);
  if (!Number.isFinite(n) || n < 0) {
    return null;
  }
  return n;
}

export function outstandingAfterPrior(orderedQty: number, receivedQty: number): number {
  return Math.max(0, orderedQty - receivedQty);
}

export function buildReceivePlan(lines: ReceiveLineInput[]): BuildReceivePlanResult {
  const planned: ReceiveLinePlan[] = [];

  for (const line of lines) {
    if (
      !Number.isFinite(line.goodQty) ||
      !Number.isFinite(line.damagedQty) ||
      line.goodQty < 0 ||
      line.damagedQty < 0
    ) {
      return { ok: false, error: "invalid_qty" };
    }

    if (line.goodQty + line.damagedQty > line.outstandingQty + 1e-9) {
      return { ok: false, error: "over_receive" };
    }

    const remaining = line.outstandingQty - line.goodQty - line.damagedQty;
    const shortClosed = line.closeRemaining ? remaining : 0;
    if (line.goodQty + line.damagedQty + shortClosed <= 0) {
      continue;
    }

    const discrepancyKind = line.damagedQty > 0 ? "Damaged" : shortClosed > 0 ? "Short" : null;

    planned.push({
      productId: line.productId,
      receiveQty: line.goodQty,
      damagedQty: line.damagedQty,
      shortClosedQty: shortClosed,
      remainingAfter: remaining - shortClosed,
      discrepancyKind,
    });
  }

  if (planned.length === 0) {
    return { ok: false, error: "no_activity" };
  }

  return { ok: true, lines: planned };
}

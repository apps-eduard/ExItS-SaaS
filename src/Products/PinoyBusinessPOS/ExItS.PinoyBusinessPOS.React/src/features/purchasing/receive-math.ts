/**
 * Partial receive math — UI helpers only. Server remains authoritative.
 *
 * Discrepancy = Outstanding − Good received.
 * Damaged + Not delivered must equal Discrepancy.
 * Cancel remaining short-closes the full discrepancy; deliver later leaves it outstanding.
 * Damaged / not-delivered do not reduce outstanding by themselves.
 */

export type RemainingDisposition = "deliver_later" | "cancel_remaining";

export type ReceiveLineInput = {
  productId: string;
  outstandingQty: number;
  goodQty: number;
  damagedQty: number;
  /** Not delivered / missing qty (maps to RejectedQty). */
  notDeliveredQty: number;
  /** When discrepancy > 0: cancel maps to shortClosedQty = discrepancy. */
  cancelRemaining: boolean;
};

export type ReceiveLinePlan = {
  productId: string;
  receiveQty: number;
  damagedQty: number;
  rejectedQty: number;
  shortClosedQty: number;
  discrepancyQty: number;
  remainingAfter: number;
  remainingAction: RemainingDisposition | null;
  discrepancyKind: "Damaged" | "Short" | "Other" | null;
};

export type BuildReceivePlanResult =
  | { ok: true; lines: ReceiveLinePlan[] }
  | {
      ok: false;
      error:
        | "invalid_qty"
        | "over_receive"
        | "no_activity"
        | "classification_incomplete"
        | "classification_mismatch";
    };

const QTY_EPS = 1e-9;

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

/** Discrepancy when good received is below outstanding. */
export function receiveDiscrepancyQty(outstandingQty: number, goodQty: number): number {
  return Math.max(0, outstandingQty - goodQty);
}

export function classificationRemaining(
  discrepancyQty: number,
  damagedQty: number,
  notDeliveredQty: number,
): number {
  return Math.max(0, discrepancyQty - damagedQty - notDeliveredQty);
}

export function isClassificationComplete(
  discrepancyQty: number,
  damagedQty: number,
  notDeliveredQty: number,
): boolean {
  if (discrepancyQty <= QTY_EPS) {
    return damagedQty <= QTY_EPS && notDeliveredQty <= QTY_EPS;
  }
  return Math.abs(damagedQty + notDeliveredQty - discrepancyQty) <= QTY_EPS;
}

export function resolveDiscrepancyKind(
  damagedQty: number,
  notDeliveredQty: number,
): "Damaged" | "Short" | "Other" | null {
  if (damagedQty > QTY_EPS && notDeliveredQty > QTY_EPS) {
    return "Other";
  }
  if (damagedQty > QTY_EPS) {
    return "Damaged";
  }
  if (notDeliveredQty > QTY_EPS) {
    return "Short";
  }
  return null;
}

export function buildReceivePlan(lines: ReceiveLineInput[]): BuildReceivePlanResult {
  const planned: ReceiveLinePlan[] = [];

  for (const line of lines) {
    if (
      !Number.isFinite(line.goodQty) ||
      !Number.isFinite(line.damagedQty) ||
      !Number.isFinite(line.notDeliveredQty) ||
      line.goodQty < 0 ||
      line.damagedQty < 0 ||
      line.notDeliveredQty < 0
    ) {
      return { ok: false, error: "invalid_qty" };
    }

    if (line.goodQty > line.outstandingQty + QTY_EPS) {
      return { ok: false, error: "over_receive" };
    }

    const discrepancy = receiveDiscrepancyQty(line.outstandingQty, line.goodQty);

    if (discrepancy <= QTY_EPS) {
      if (line.damagedQty > QTY_EPS || line.notDeliveredQty > QTY_EPS) {
        return { ok: false, error: "classification_mismatch" };
      }
      if (line.goodQty <= QTY_EPS) {
        continue;
      }
      planned.push({
        productId: line.productId,
        receiveQty: line.goodQty,
        damagedQty: 0,
        rejectedQty: 0,
        shortClosedQty: 0,
        discrepancyQty: 0,
        remainingAfter: 0,
        remainingAction: null,
        discrepancyKind: null,
      });
      continue;
    }

    if (!isClassificationComplete(discrepancy, line.damagedQty, line.notDeliveredQty)) {
      return { ok: false, error: "classification_incomplete" };
    }

    const classified = line.damagedQty + line.notDeliveredQty;
    if (Math.abs(classified - discrepancy) > QTY_EPS) {
      return { ok: false, error: "classification_mismatch" };
    }

    const shortClosed = line.cancelRemaining ? discrepancy : 0;
    const remainingAfter = discrepancy - shortClosed;

    planned.push({
      productId: line.productId,
      receiveQty: line.goodQty,
      damagedQty: line.damagedQty,
      rejectedQty: line.notDeliveredQty,
      shortClosedQty: shortClosed,
      discrepancyQty: discrepancy,
      remainingAfter,
      remainingAction: line.cancelRemaining ? "cancel_remaining" : "deliver_later",
      discrepancyKind: resolveDiscrepancyKind(line.damagedQty, line.notDeliveredQty),
    });
  }

  if (planned.length === 0) {
    return { ok: false, error: "no_activity" };
  }

  return { ok: true, lines: planned };
}

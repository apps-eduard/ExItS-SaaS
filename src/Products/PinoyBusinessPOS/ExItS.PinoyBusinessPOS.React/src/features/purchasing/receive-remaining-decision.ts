import { parseNonNegativeQty, receiveDiscrepancyQty } from "@/features/purchasing/receive-math";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";

/** UI decision for outstanding qty after this receipt (maps to cancelRemaining / deliver_later). */
export type RemainingDecisionAction = "replace_later" | "cancel_remaining";

export type RemainingDecisionLineInput = {
  productId: string;
  name: string;
  sku: string;
  uom: string;
  outstandingQty: number;
  goodText: string;
  damagedText: string;
  notDeliveredText: string;
  remarksText: string;
  remainingAction: RemainingDecisionAction | null;
};

export type RemainingDecisionRow = {
  productId: string;
  name: string;
  sku: string;
  remainingQty: number;
  remainingLabel: string;
  issueLabel: string;
  remark: string | null;
  remainingAction: RemainingDecisionAction | null;
};

export function lineRemainingQty(line: {
  outstandingQty: number;
  goodText: string;
}): number {
  const good = parseNonNegativeQty(line.goodText);
  if (good === null) {
    return 0;
  }
  return receiveDiscrepancyQty(line.outstandingQty, good);
}

export function formatRemainingIssueLabel(
  line: {
    outstandingQty: number;
    goodText: string;
    damagedText: string;
    notDeliveredText: string;
    uom: string;
  },
  labels: { damaged: string; notDelivered: string },
): string {
  const damaged = parseNonNegativeQty(line.damagedText) ?? 0;
  const notDelivered = parseNonNegativeQty(line.notDeliveredText) ?? 0;
  const parts: string[] = [];
  if (damaged > 1e-9) {
    parts.push(labels.damaged);
  }
  if (notDelivered > 1e-9) {
    parts.push(labels.notDelivered);
  }
  if (parts.length === 0) {
    return labels.notDelivered;
  }
  return parts.join(" · ");
}

export function buildRemainingDecisionRows(
  lines: readonly RemainingDecisionLineInput[],
  labels: { damaged: string; notDelivered: string },
): RemainingDecisionRow[] {
  return lines
    .map((line) => {
      const remainingQty = lineRemainingQty(line);
      if (remainingQty <= 1e-9) {
        return null;
      }
      return {
        productId: line.productId,
        name: line.name,
        sku: line.sku.trim(),
        remainingQty,
        remainingLabel: formatStockQtyLabel(remainingQty, line.uom),
        issueLabel: formatRemainingIssueLabel(line, labels),
        remark: line.remarksText.trim() || null,
        remainingAction: line.remainingAction,
      } satisfies RemainingDecisionRow;
    })
    .filter((row): row is RemainingDecisionRow => row != null);
}

export function countUnresolvedRemaining(
  rows: readonly RemainingDecisionRow[],
): number {
  return rows.filter((row) => row.remainingAction == null).length;
}

export function sumRemainingUnits(rows: readonly RemainingDecisionRow[]): number {
  return rows.reduce((sum, row) => sum + row.remainingQty, 0);
}

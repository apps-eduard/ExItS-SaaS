import {
  isClassificationComplete,
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";

export type ReceiveDiscrepancyDraftLine = {
  outstandingQty: number;
  goodText: string;
  damagedText: string;
  notDeliveredText: string;
  remarksText: string;
  uom: string;
};

/** True when Good &lt; Outstanding. */
export function lineHasReceiveDiscrepancy(line: ReceiveDiscrepancyDraftLine): boolean {
  const good = parseNonNegativeQty(line.goodText);
  if (good === null) {
    return false;
  }
  return receiveDiscrepancyQty(line.outstandingQty, good) > 1e-9;
}

/** Qty split + note are complete for the current shortfall. */
export function isReceiveDiscrepancyClassified(line: ReceiveDiscrepancyDraftLine): boolean {
  const good = parseNonNegativeQty(line.goodText);
  const damaged = parseNonNegativeQty(line.damagedText);
  const notDelivered = parseNonNegativeQty(line.notDeliveredText);
  if (good === null || damaged === null || notDelivered === null) {
    return false;
  }
  const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
  if (discrepancy <= 1e-9) {
    return true;
  }
  if (!line.remarksText.trim()) {
    return false;
  }
  return isClassificationComplete(discrepancy, damaged, notDelivered);
}

export function lineNeedsReceiveDiscrepancyClassification(
  line: ReceiveDiscrepancyDraftLine,
): boolean {
  return lineHasReceiveDiscrepancy(line) && !isReceiveDiscrepancyClassified(line);
}

/**
 * Compact summary for classified shortfalls.
 * Examples: `1 Kg not delivered`, `0.5 Kg damaged`, `0.5 damaged · 0.5 not delivered`
 */
export function formatReceiveDiscrepancySummary(
  line: ReceiveDiscrepancyDraftLine,
  labels: { damaged: string; notDelivered: string },
): string | null {
  const good = parseNonNegativeQty(line.goodText);
  const damaged = parseNonNegativeQty(line.damagedText);
  const notDelivered = parseNonNegativeQty(line.notDeliveredText);
  if (good === null || damaged === null || notDelivered === null) {
    return null;
  }
  const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
  if (discrepancy <= 1e-9) {
    return null;
  }
  if (!line.remarksText.trim()) {
    return null;
  }
  if (!isClassificationComplete(discrepancy, damaged, notDelivered)) {
    return null;
  }

  const damagedPart =
    damaged > 1e-9 ? `${formatStockQtyLabel(damaged, line.uom)} ${labels.damaged.toLowerCase()}` : null;
  const notDeliveredPart =
    notDelivered > 1e-9
      ? `${formatStockQtyLabel(notDelivered, line.uom)} ${labels.notDelivered.toLowerCase()}`
      : null;

  if (damagedPart && notDeliveredPart) {
    // Mixed: drop repeated UOM word from labels for compactness — qty already includes UOM.
    const damagedCompact = `${formatStockQtyLabel(damaged, line.uom).replace(/\s+\S+$/, "")} ${labels.damaged.toLowerCase()}`;
    const notDeliveredCompact = `${formatStockQtyLabel(notDelivered, line.uom).replace(/\s+\S+$/, "")} ${labels.notDelivered.toLowerCase()}`;
    return `${damagedCompact} · ${notDeliveredCompact}`;
  }
  return damagedPart ?? notDeliveredPart;
}

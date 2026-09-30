import {
  isClassificationComplete,
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
import { requiresActualProduct, isActualProductSameAsExpected } from "@/features/inventory/transfer-exception-custody-policy";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";

export type ReceiveDiscrepancyDraftLine = {
  productId?: string;
  outstandingQty: number;
  goodText: string;
  damagedText: string;
  notDeliveredText: string;
  otherText?: string;
  otherReasonCode?: string;
  otherReasonText?: string;
  actualReceivedProductId?: string | null;
  actualReceivedProductName?: string | null;
  remarksText: string;
  uom: string;
};

export type ReceiveDiscrepancySummaryLabels = {
  damaged: string;
  notDelivered: string;
  /** Map other reason code → short label for summaries (e.g. WrongItem → "wrong item"). */
  otherReasons?: Record<string, string>;
  otherFallback?: string;
};

function otherSummaryLabel(
  code: string | undefined,
  labels: ReceiveDiscrepancySummaryLabels,
): string {
  if (code && labels.otherReasons?.[code]) {
    return labels.otherReasons[code].toLowerCase();
  }
  return (labels.otherFallback ?? "other").toLowerCase();
}

/** True when Good &lt; Outstanding. */
export function lineHasReceiveDiscrepancy(line: ReceiveDiscrepancyDraftLine): boolean {
  const good = parseNonNegativeQty(line.goodText);
  if (good === null) {
    return false;
  }
  return receiveDiscrepancyQty(line.outstandingQty, good) > 1e-9;
}

function otherReasonValid(line: ReceiveDiscrepancyDraftLine, other: number): boolean {
  if (other <= 1e-9) {
    return true;
  }
  const code = line.otherReasonCode?.trim() ?? "";
  if (!code) {
    return false;
  }
  if (code === "Other" && !(line.otherReasonText?.trim())) {
    return false;
  }
  if (requiresActualProduct(code) && !line.actualReceivedProductId?.trim()) {
    return false;
  }
  if (isActualProductSameAsExpected(code, line.productId, line.actualReceivedProductId)) {
    return false;
  }
  return true;
}

/** Qty split + note are complete for the current shortfall. */
export function isReceiveDiscrepancyClassified(line: ReceiveDiscrepancyDraftLine): boolean {
  const good = parseNonNegativeQty(line.goodText);
  const damaged = parseNonNegativeQty(line.damagedText);
  const notDelivered = parseNonNegativeQty(line.notDeliveredText);
  const other = parseNonNegativeQty(line.otherText ?? "0");
  if (good === null || damaged === null || notDelivered === null || other === null) {
    return false;
  }
  const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
  if (discrepancy <= 1e-9) {
    return true;
  }
  if (!line.remarksText.trim()) {
    return false;
  }
  if (!otherReasonValid(line, other)) {
    return false;
  }
  return isClassificationComplete(discrepancy, damaged, notDelivered, other);
}

export function lineNeedsReceiveDiscrepancyClassification(
  line: ReceiveDiscrepancyDraftLine,
): boolean {
  return lineHasReceiveDiscrepancy(line) && !isReceiveDiscrepancyClassified(line);
}

/**
 * Compact summary for classified shortfalls.
 * Examples: `1 Kg not delivered`, `0.5 damaged · 1 wrong item`
 */
export function formatReceiveDiscrepancySummary(
  line: ReceiveDiscrepancyDraftLine,
  labels: ReceiveDiscrepancySummaryLabels,
): string | null {
  const good = parseNonNegativeQty(line.goodText);
  const damaged = parseNonNegativeQty(line.damagedText);
  const notDelivered = parseNonNegativeQty(line.notDeliveredText);
  const other = parseNonNegativeQty(line.otherText ?? "0");
  if (good === null || damaged === null || notDelivered === null || other === null) {
    return null;
  }
  const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
  if (discrepancy <= 1e-9) {
    return null;
  }
  if (!line.remarksText.trim()) {
    return null;
  }
  if (!isClassificationComplete(discrepancy, damaged, notDelivered, other)) {
    return null;
  }

  const parts: string[] = [];
  if (damaged > 1e-9) {
    parts.push(
      `${formatStockQtyLabel(damaged, line.uom).replace(/\s+\S+$/, "")} ${labels.damaged.toLowerCase()}`,
    );
  }
  if (notDelivered > 1e-9) {
    parts.push(
      `${formatStockQtyLabel(notDelivered, line.uom).replace(/\s+\S+$/, "")} ${labels.notDelivered.toLowerCase()}`,
    );
  }
  if (other > 1e-9) {
    const otherLabel = otherSummaryLabel(line.otherReasonCode, labels);
    const qtyPart = formatStockQtyLabel(other, line.uom).replace(/\s+\S+$/, "");
    const actualName = line.actualReceivedProductName?.trim();
    if (actualName && requiresActualProduct(line.otherReasonCode?.trim() ?? "")) {
      parts.push(`${qtyPart} ${otherLabel} (${actualName})`);
    } else {
      parts.push(`${qtyPart} ${otherLabel}`);
    }
  }

  return parts.length > 0 ? parts.join(" · ") : null;
}

import { parseNonNegativeQty } from "@/features/purchasing/receive-math";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import type { TransferReceiveLineEdit } from "@/features/inventory/inventory-transfer-receive-helpers";
import { requiresActualProduct } from "@/features/inventory/transfer-exception-custody-policy";

export type TransferMissingFollowUp = "wait_original" | "request_replacement" | "accept_shortage";
export type TransferDamagedOtherFollowUp = "request_replacement" | "accept_shortage";

export type TransferFollowUpIssueKind = "missing" | "damaged" | "other";

export type TransferFollowUpRow = {
  rowKey: string;
  productId: string;
  name: string;
  sku: string;
  issueKind: TransferFollowUpIssueKind;
  qty: number;
  qtyLabel: string;
  issueLabel: string;
  otherReasonCode?: string;
  /** Secondary line under Issue — remarks, or actual product for wrong item/variant. */
  remark: string | null;
  actualReceivedProductName?: string | null;
  action: TransferMissingFollowUp | TransferDamagedOtherFollowUp | null;
};

export function missingDispositionFromFollowUp(
  followUp: TransferMissingFollowUp | null,
): "ExpectedLater" | "CloseMissing" | "AcceptShortage" | null {
  if (followUp === "wait_original") {
    return "ExpectedLater";
  }
  if (followUp === "request_replacement") {
    return "CloseMissing";
  }
  if (followUp === "accept_shortage") {
    return "AcceptShortage";
  }
  return null;
}

export function damagedOtherFollowUpToApi(
  followUp: TransferDamagedOtherFollowUp | null,
): "RequestReplacement" | "AcceptShortage" | undefined {
  if (followUp === "request_replacement") {
    return "RequestReplacement";
  }
  if (followUp === "accept_shortage") {
    return "AcceptShortage";
  }
  return undefined;
}

export function buildTransferFollowUpRows(
  lines: readonly TransferReceiveLineEdit[],
  labels: {
    damaged: string;
    notDelivered: string;
    otherReasons?: Record<string, string>;
    otherFallback?: string;
  },
): TransferFollowUpRow[] {
  const rows: TransferFollowUpRow[] = [];
  for (const line of lines) {
    const missing = parseNonNegativeQty(line.notDeliveredText) ?? 0;
    const damaged = parseNonNegativeQty(line.damagedText) ?? 0;
    const other = parseNonNegativeQty(line.otherText ?? "0") ?? 0;
    const remark = line.remarksText.trim() || null;

    if (missing > 1e-9) {
      rows.push({
        rowKey: `${line.productId}-missing`,
        productId: line.productId,
        name: line.name,
        sku: line.sku.trim(),
        issueKind: "missing",
        qty: missing,
        qtyLabel: formatStockQtyLabel(missing, line.uom),
        issueLabel: labels.notDelivered,
        remark,
        action: line.missingFollowUp,
      });
    }
    if (damaged > 1e-9) {
      rows.push({
        rowKey: `${line.productId}-damaged`,
        productId: line.productId,
        name: line.name,
        sku: line.sku.trim(),
        issueKind: "damaged",
        qty: damaged,
        qtyLabel: formatStockQtyLabel(damaged, line.uom),
        issueLabel: labels.damaged,
        remark,
        action: line.damagedFollowUp,
      });
    }
    if (other > 1e-9) {
      const code = line.otherReasonCode?.trim() ?? "";
      const actualName = line.actualReceivedProductName?.trim() || null;
      const showActualInsteadOfRemark = requiresActualProduct(code) && Boolean(actualName);
      rows.push({
        rowKey: `${line.productId}-other`,
        productId: line.productId,
        name: line.name,
        sku: line.sku.trim(),
        issueKind: "other",
        qty: other,
        qtyLabel: formatStockQtyLabel(other, line.uom),
        issueLabel: (code && labels.otherReasons?.[code]) || labels.otherFallback || "Other",
        otherReasonCode: code || undefined,
        remark: showActualInsteadOfRemark ? null : remark,
        actualReceivedProductName: showActualInsteadOfRemark ? actualName : null,
        action: line.otherFollowUp,
      });
    }
  }
  return rows;
}

export function countUnresolvedTransferFollowUp(rows: readonly TransferFollowUpRow[]): number {
  return rows.filter((row) => row.action == null).length;
}

export function sumTransferFollowUpUnits(rows: readonly TransferFollowUpRow[]): number {
  return rows.reduce((sum, row) => sum + row.qty, 0);
}

export function defaultTransferFollowUps(_linkedStockRequest: boolean): {
  missingFollowUp: TransferMissingFollowUp;
  damagedFollowUp: TransferDamagedOtherFollowUp;
  otherFollowUp: TransferDamagedOtherFollowUp;
} {
  return {
    // Wait / Request replacement both create Needs fulfillment; default Request replacement.
    missingFollowUp: "request_replacement",
    // Damaged / other: Request replacement or Accept shortage (always available).
    damagedFollowUp: "request_replacement",
    otherFollowUp: "request_replacement",
  };
}

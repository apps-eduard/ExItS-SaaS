import type { InventoryTransferReceiveLineRequest } from "@/api/pos/pos-inventory-transfer-client";
import type { TransferReceiveLineEdit } from "@/features/inventory/inventory-transfer-receive-helpers";
import {
  damagedOtherFollowUpToApi,
  missingDispositionFromFollowUp,
} from "@/features/inventory/transfer-receive-follow-up";
import {
  buildReceivePlan,
  parseNonNegativeQty,
  type BuildReceivePlanResult,
} from "@/features/purchasing/receive-math";
import {
  forcesReturnToSource,
  isActualProductSameAsExpected,
  requiresActualProduct,
  resolveDefaultOtherCustody,
} from "@/features/inventory/transfer-exception-custody-policy";

export const TRANSFER_MISSING_DISPOSITIONS = [
  "ExpectedLater",
  "CloseMissing",
  "AcceptShortage",
] as const;
export type TransferMissingDispositionCode = (typeof TRANSFER_MISSING_DISPOSITIONS)[number];

export type BuildTransferReceivePayloadError =
  | NonNullable<BuildReceivePlanResult extends { ok: false } ? BuildReceivePlanResult["error"] : never>
  | "other_actual_product_required"
  | "other_actual_product_same_as_expected"
  | "other_custody_required";

export type BuildTransferReceivePayloadResult =
  | { ok: true; lines: InventoryTransferReceiveLineRequest[] }
  | { ok: false; error: BuildTransferReceivePayloadError };

export function buildTransferReceivePayload(
  edits: readonly TransferReceiveLineEdit[],
): BuildTransferReceivePayloadResult {
  const inputs = edits.map((line) => {
    const good = parseNonNegativeQty(line.goodText);
    const damaged = parseNonNegativeQty(line.damagedText);
    const notDelivered = parseNonNegativeQty(line.notDeliveredText);
    const other = parseNonNegativeQty(line.otherText ?? "0");
    return { line, good, damaged, notDelivered, other };
  });

  if (inputs.some((p) => p.good === null || p.damaged === null || p.notDelivered === null || p.other === null)) {
    return { ok: false, error: "invalid_qty" };
  }

  const plan = buildReceivePlan(
    inputs.map(({ line, good, damaged, notDelivered, other }) => ({
      productId: line.productId,
      outstandingQty: line.outstandingQty,
      goodQty: good!,
      damagedQty: damaged!,
      notDeliveredQty: notDelivered!,
      otherQty: other!,
      cancelRemaining: false,
    })),
  );

  if (!plan.ok) {
    return { ok: false, error: plan.error };
  }

  const editByProduct = new Map(edits.map((line) => [line.productId, line]));
  const apiLines: InventoryTransferReceiveLineRequest[] = [];

  for (const planned of plan.lines) {
    const edit = editByProduct.get(planned.productId);
    if (!edit) {
      continue;
    }
    const missingQty = planned.rejectedQty;
    const missingDisposition = missingDispositionFromFollowUp(edit.missingFollowUp);
    const note = edit.remarksText.trim() || null;
    const otherReasonCode = edit.otherReasonCode?.trim() || null;
    const otherReasonNote =
      otherReasonCode === "Other" ? edit.otherReasonText?.trim() || null : null;

    const entry: InventoryTransferReceiveLineRequest = {
      lineId: edit.lineId,
      productId: edit.productId,
      goodQty: planned.receiveQty,
      receivedQty: planned.receiveQty,
      damagedQty: planned.damagedQty,
      missingQty,
      discrepancyNote: note,
    };
    if (missingQty > 1e-9 && missingDisposition) {
      entry.missingDisposition = missingDisposition;
    }
    if (planned.damagedQty > 1e-9) {
      entry.damagedFollowUp = damagedOtherFollowUpToApi(edit.damagedFollowUp);
      entry.damagedCustodyDecision = edit.damagedCustodyDecision ?? "KeepAtDestination";
    }
    if (planned.otherQty > 1e-9) {
      if (!otherReasonCode) {
        return { ok: false, error: "classification_incomplete" };
      }
      if (requiresActualProduct(otherReasonCode) && !edit.actualReceivedProductId?.trim()) {
        return { ok: false, error: "other_actual_product_required" };
      }
      if (
        isActualProductSameAsExpected(
          otherReasonCode,
          edit.productId,
          edit.actualReceivedProductId,
        )
      ) {
        return { ok: false, error: "other_actual_product_same_as_expected" };
      }
      const otherCustody = forcesReturnToSource(otherReasonCode)
        ? resolveDefaultOtherCustody(otherReasonCode)
        : edit.otherCustodyDecision ?? resolveDefaultOtherCustody(otherReasonCode);
      if (!otherCustody) {
        return { ok: false, error: "other_custody_required" };
      }
      entry.otherQty = planned.otherQty;
      entry.otherFollowUp = damagedOtherFollowUpToApi(edit.otherFollowUp);
      entry.otherCustodyDecision = otherCustody;
      if (otherReasonCode) {
        entry.otherReasonCode = otherReasonCode;
      }
      if (otherReasonNote) {
        entry.otherReasonNote = otherReasonNote;
      }
      const actualId = edit.actualReceivedProductId?.trim();
      if (actualId) {
        entry.actualReceivedProductId = actualId;
      }
    }
    apiLines.push(entry);
  }

  return { ok: true, lines: apiLines };
}

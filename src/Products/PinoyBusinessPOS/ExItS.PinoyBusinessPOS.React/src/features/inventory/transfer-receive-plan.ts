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

export const TRANSFER_MISSING_DISPOSITIONS = [
  "ExpectedLater",
  "CloseMissing",
  "AcceptShortage",
] as const;
export type TransferMissingDispositionCode = (typeof TRANSFER_MISSING_DISPOSITIONS)[number];

export type BuildTransferReceivePayloadResult =
  | { ok: true; lines: InventoryTransferReceiveLineRequest[] }
  | { ok: false; error: NonNullable<BuildReceivePlanResult extends { ok: false } ? BuildReceivePlanResult["error"] : never> };

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
    }
    if (planned.otherQty > 1e-9) {
      entry.otherQty = planned.otherQty;
      entry.otherFollowUp = damagedOtherFollowUpToApi(edit.otherFollowUp);
      if (otherReasonCode) {
        entry.otherReasonCode = otherReasonCode;
      }
      if (otherReasonNote) {
        entry.otherReasonNote = otherReasonNote;
      }
    }
    apiLines.push(entry);
  }

  return { ok: true, lines: apiLines };
}

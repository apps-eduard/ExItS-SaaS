import type { InventoryTransferReceiveLineRequest } from "@/api/pos/pos-inventory-transfer-client";
import type { TransferReceiveLineEdit } from "@/features/inventory/inventory-transfer-receive-helpers";
import {
  buildReceivePlan,
  parseNonNegativeQty,
  type BuildReceivePlanResult,
} from "@/features/purchasing/receive-math";
import type { RemainingDecisionAction } from "@/features/purchasing/receive-remaining-decision";

export const TRANSFER_MISSING_DISPOSITIONS = ["ExpectedLater", "CloseMissing"] as const;
export type TransferMissingDispositionCode = (typeof TRANSFER_MISSING_DISPOSITIONS)[number];

export type BuildTransferReceivePayloadResult =
  | { ok: true; lines: InventoryTransferReceiveLineRequest[] }
  | { ok: false; error: NonNullable<BuildReceivePlanResult extends { ok: false } ? BuildReceivePlanResult["error"] : never> };

export function missingDispositionFromRemainingAction(
  missingQty: number,
  action: RemainingDecisionAction | null,
): TransferMissingDispositionCode | null {
  if (missingQty <= 1e-9) {
    return null;
  }
  if (action === "cancel_remaining") {
    return "CloseMissing";
  }
  if (action === "replace_later") {
    return "ExpectedLater";
  }
  return null;
}

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
      cancelRemaining: line.remainingAction === "cancel_remaining",
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
    const missingDisposition = missingDispositionFromRemainingAction(
      missingQty,
      edit.remainingAction,
    );
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
      missingDisposition: missingDisposition ?? undefined,
      discrepancyNote: note,
    };
    if (planned.otherQty > 1e-9) {
      entry.otherQty = planned.otherQty;
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

import type {
  InventoryTransferDto,
  InventoryTransferLineDto,
} from "@/api/pos/pos-inventory-transfer-client";
import type { MessageKey } from "@/i18n/messages";
import { inventoryTransferDiscrepancyLabelKey } from "@/features/inventory/inventory-transfer-labels";
import { lineDamagedQty } from "@/features/inventory/inventory-transfer-receive-helpers";

export type ThisShipmentTotals = {
  sent: number;
  goodReceived: number;
  damaged: number;
  missing: number;
  other: number;
};

export type ReceivingDecisionView = {
  hasDiscrepancy: boolean;
  damagedQty: number;
  damagedFollowUp: string | null;
  custodyDecision: string | null;
  custodyStatus: string | null;
  missingQty: number;
  missingDisposition: string | null;
  otherQty: number;
  otherReasonCode: string | null;
  otherReasonNote: string | null;
  otherFollowUp: string | null;
};

function sumFinite(values: number[]): number {
  return values.reduce((sum, v) => sum + (Number.isFinite(v) ? v : 0), 0);
}

/** Physical classification for the transfer currently on screen — never family totals. */
export function computeThisShipmentTotals(
  transfer: Pick<InventoryTransferDto, "totalSentQty" | "totalReceivedQty" | "receipts">,
): ThisShipmentTotals {
  const receipts = transfer.receipts ?? [];
  if (receipts.length === 0) {
    return {
      sent: transfer.totalSentQty,
      goodReceived: transfer.totalReceivedQty,
      damaged: 0,
      missing: 0,
      other: 0,
    };
  }

  let good = 0;
  let damaged = 0;
  let missing = 0;
  let other = 0;
  for (const receipt of receipts) {
    for (const line of receipt.lines ?? []) {
      good += line.quantityReceived ?? 0;
      damaged += line.quantityDamaged ?? 0;
      missing += line.quantityMissing ?? 0;
      other += line.quantityOther ?? 0;
    }
  }
  return {
    sent: transfer.totalSentQty,
    goodReceived: good,
    damaged,
    missing,
    other,
  };
}

export function transferHasReceivingDiscrepancy(
  transfer: Pick<InventoryTransferDto, "totalSentQty" | "totalReceivedQty" | "receipts">,
): boolean {
  const totals = computeThisShipmentTotals(transfer);
  return totals.damaged > 1e-9 || totals.missing > 1e-9 || totals.other > 1e-9;
}

/**
 * Persisted receiving decisions for THIS transfer only.
 * Prefer damage-custody fields when present; otherwise receipt-line follow-ups.
 */
export function buildReceivingDecisionView(
  transfer: Pick<InventoryTransferDto, "transferId" | "receipts" | "damageCustodies">,
): ReceivingDecisionView {
  const receipts = transfer.receipts ?? [];
  let damagedQty = 0;
  let missingQty = 0;
  let otherQty = 0;
  let damagedFollowUp: string | null = null;
  let missingDisposition: string | null = null;
  let otherFollowUp: string | null = null;
  let otherReasonCode: string | null = null;
  let otherReasonNote: string | null = null;

  for (const receipt of receipts) {
    for (const line of receipt.lines ?? []) {
      const d = line.quantityDamaged ?? 0;
      const m = line.quantityMissing ?? 0;
      const o = line.quantityOther ?? 0;
      damagedQty += d;
      missingQty += m;
      otherQty += o;
      if (d > 1e-9 && line.damagedFollowUp && !damagedFollowUp) {
        damagedFollowUp = line.damagedFollowUp;
      }
      if (m > 1e-9 && line.missingDisposition && !missingDisposition) {
        missingDisposition = line.missingDisposition;
      }
      if (o > 1e-9) {
        if (line.otherFollowUp && !otherFollowUp) {
          otherFollowUp = line.otherFollowUp;
        }
        if (line.otherReasonCode && !otherReasonCode) {
          otherReasonCode = line.otherReasonCode;
        }
        if (line.otherReasonNote && !otherReasonNote) {
          otherReasonNote = line.otherReasonNote;
        }
      }
    }
  }

  const custodies = (transfer.damageCustodies ?? []).filter(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
  let custodyDecision: string | null = null;
  let custodyStatus: string | null = null;
  if (custodies.length > 0) {
    const primary = custodies[0]!;
    custodyDecision = primary.decision;
    custodyStatus = primary.status;
    if (primary.followUpIntent) {
      damagedFollowUp = primary.followUpIntent;
    }
    if (damagedQty < 1e-9) {
      damagedQty = sumFinite(custodies.map((c) => c.quantity));
    }
  }

  return {
    hasDiscrepancy: damagedQty > 1e-9 || missingQty > 1e-9 || otherQty > 1e-9,
    damagedQty,
    damagedFollowUp,
    custodyDecision,
    custodyStatus,
    missingQty,
    missingDisposition,
    otherQty,
    otherReasonCode,
    otherReasonNote,
    otherFollowUp,
  };
}

export function transferDiscrepancyFollowUpLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "RequestReplacement":
      return "transfer.decision.replacementRequested";
    case "AcceptShortage":
      return "transfer.decision.acceptedNoReplacement";
    default:
      return null;
  }
}

export function transferMissingDispositionLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "ExpectedLater":
      return "transfer.followUp.waitOriginal";
    case "CloseMissing":
      return "transfer.decision.replacementRequested";
    case "AcceptShortage":
      return "transfer.decision.acceptedShortage";
    default:
      return null;
  }
}

export function transferCustodyDecisionLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "KeepAtDestination":
      return "transfer.custody.keepAtDestination";
    case "ReturnToSource":
      return "transfer.custody.returnToSource";
    default:
      return null;
  }
}

export function transferCustodyStatusLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "HeldAtDestination":
      return "transfer.custody.heldAtDestination";
    case "AwaitingReturn":
      return "transfer.custody.awaitingReturn";
    case "ReturnInTransit":
      return "transfer.custody.returnInTransit";
    case "ReceivedAtSource":
      return "transfer.custody.receivedAtSource";
    case "AwaitingInspection":
      return "transfer.custody.awaitingInspection";
    default:
      return null;
  }
}

export function isKeepAtDestinationCustody(decision: string | null | undefined): boolean {
  return decision === "KeepAtDestination";
}

export function isReturnToSourceCustody(decision: string | null | undefined): boolean {
  return decision === "ReturnToSource";
}

export function lineMissingQty(
  transfer: Pick<InventoryTransferDto, "receipts">,
  lineId: string,
): number {
  let total = 0;
  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId === lineId) {
        total += rl.quantityMissing ?? 0;
      }
    }
  }
  return total;
}

export function lineOtherQty(
  transfer: Pick<InventoryTransferDto, "receipts">,
  lineId: string,
): number {
  let total = 0;
  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId === lineId) {
        total += rl.quantityOther ?? 0;
      }
    }
  }
  return total;
}

export type LineFollowUpDisplay = {
  labelKey: MessageKey;
  qty?: number;
} | null;

/** Compact follow-up cell for THIS transfer's line from persisted receipt decisions. */
export function lineFollowUpDisplay(
  transfer: Pick<InventoryTransferDto, "receipts" | "damageCustodies" | "transferId">,
  line: Pick<InventoryTransferLineDto, "lineId">,
): LineFollowUpDisplay {
  const damaged = lineDamagedQty(transfer, line.lineId);
  const missing = lineMissingQty(transfer, line.lineId);
  const other = lineOtherQty(transfer, line.lineId);

  let damagedFollowUp: string | null = null;
  let missingDisposition: string | null = null;
  let otherFollowUp: string | null = null;

  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId !== line.lineId) {
        continue;
      }
      if ((rl.quantityDamaged ?? 0) > 1e-9 && rl.damagedFollowUp) {
        damagedFollowUp = rl.damagedFollowUp;
      }
      if ((rl.quantityMissing ?? 0) > 1e-9 && rl.missingDisposition) {
        missingDisposition = rl.missingDisposition;
      }
      if ((rl.quantityOther ?? 0) > 1e-9 && rl.otherFollowUp) {
        otherFollowUp = rl.otherFollowUp;
      }
    }
  }

  const custody = (transfer.damageCustodies ?? []).find(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
  if (custody?.followUpIntent && damaged > 1e-9) {
    damagedFollowUp = custody.followUpIntent;
  }

  if (damaged > 1e-9 && damagedFollowUp === "RequestReplacement") {
    return { labelKey: "transfer.followUp.replaceQty", qty: damaged };
  }
  if (damaged > 1e-9 && damagedFollowUp === "AcceptShortage") {
    return { labelKey: "transfer.decision.acceptedNoReplacement" };
  }
  if (missing > 1e-9 && missingDisposition === "ExpectedLater") {
    return { labelKey: "transfer.followUp.waitOriginal" };
  }
  if (missing > 1e-9 && missingDisposition === "CloseMissing") {
    return { labelKey: "transfer.followUp.replaceQty", qty: missing };
  }
  if (missing > 1e-9 && missingDisposition === "AcceptShortage") {
    return { labelKey: "transfer.decision.acceptedShortage" };
  }
  if (other > 1e-9 && otherFollowUp === "RequestReplacement") {
    return { labelKey: "transfer.followUp.replaceQty", qty: other };
  }
  if (other > 1e-9 && otherFollowUp === "AcceptShortage") {
    return { labelKey: "transfer.decision.acceptedNoReplacement" };
  }
  return null;
}

export function otherReasonLabelKey(code: string | null | undefined): MessageKey {
  if (!code) {
    return "transfer.discrepancy.other";
  }
  return inventoryTransferDiscrepancyLabelKey(code);
}

export function familyFulfillmentTargetQty(
  transfer: Pick<InventoryTransferDto, "totalSentQty" | "familyMembers" | "rootTransferId">,
): number {
  const members = transfer.familyMembers ?? [];
  const root = members.find((m) => m.isRoot);
  if (root) {
    return root.totalSentQty;
  }
  if (!transfer.rootTransferId) {
    return transfer.totalSentQty;
  }
  return transfer.totalSentQty;
}

export function familyMemberDamagedQty(member: {
  totalDamagedQty?: number;
  totalSentQty: number;
  totalReceivedQty: number;
  totalOutstandingQty: number;
}): number {
  if (member.totalDamagedQty != null && Number.isFinite(member.totalDamagedQty)) {
    return member.totalDamagedQty;
  }
  return 0;
}

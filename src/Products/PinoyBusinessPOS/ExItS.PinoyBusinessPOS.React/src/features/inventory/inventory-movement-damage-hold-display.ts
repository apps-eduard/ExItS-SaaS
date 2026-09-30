import type { MessageKey } from "@/i18n/messages";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import {
  transferCustodyDecisionLabelKey,
  transferDiscrepancyFollowUpLabelKey,
} from "@/features/inventory/inventory-transfer-summary-presentation";

export type DamageHoldDecisionDisplay = {
  custodyLabelKey: MessageKey;
  followUpLabelKey: MessageKey;
};

/**
 * Persisted TransferDamageHold reason tail after transfer number:
 * "Transfer damage hold TR-… · Return to source · Replacement requested"
 */
export function parseDamageHoldDecisionFromReason(
  reason: string | null | undefined,
): DamageHoldDecisionDisplay | null {
  const text = reason?.trim() ?? "";
  if (!text) {
    return null;
  }
  const parts = text.split(" · ").map((p) => p.trim()).filter(Boolean);
  // [prefix+number, custody, follow-up]
  if (parts.length < 3) {
    return null;
  }
  const custodyPart = parts[parts.length - 2]!;
  const followUpPart = parts[parts.length - 1]!;

  let decisionCode: string | null = null;
  if (/return to source/i.test(custodyPart)) {
    decisionCode = "ReturnToSource";
  } else if (/keep at destination/i.test(custodyPart)) {
    decisionCode = "KeepAtDestination";
  }

  let followUpCode: string | null = null;
  if (/replacement requested/i.test(followUpPart)) {
    followUpCode = "RequestReplacement";
  } else if (/accepted|no replacement/i.test(followUpPart)) {
    followUpCode = "AcceptShortage";
  }

  const custodyLabelKey = transferCustodyDecisionLabelKey(decisionCode);
  const followUpLabelKey = transferDiscrepancyFollowUpLabelKey(followUpCode);
  if (!custodyLabelKey || !followUpLabelKey) {
    return null;
  }
  return { custodyLabelKey, followUpLabelKey };
}

export function damageHoldDecisionFromCustody(custody: {
  decision: string;
  followUpIntent: string;
} | null | undefined): DamageHoldDecisionDisplay | null {
  if (!custody) {
    return null;
  }
  const custodyLabelKey = transferCustodyDecisionLabelKey(custody.decision);
  const followUpLabelKey = transferDiscrepancyFollowUpLabelKey(custody.followUpIntent);
  if (!custodyLabelKey || !followUpLabelKey) {
    return null;
  }
  return { custodyLabelKey, followUpLabelKey };
}

/** Prefer live custody (drawer); fall back to persisted reason (history list). */
export function resolveDamageHoldDecisionDisplay(
  movement: Pick<PosStockMovementDto, "movementType" | "reason" | "sourceId">,
  custody?: { decision: string; followUpIntent: string; receiptLineId?: string } | null,
): DamageHoldDecisionDisplay | null {
  if (movement.movementType !== "TransferDamageHold") {
    return null;
  }
  if (
    custody &&
    (!movement.sourceId ||
      !custody.receiptLineId ||
      custody.receiptLineId.toLowerCase() === movement.sourceId.toLowerCase())
  ) {
    const fromCustody = damageHoldDecisionFromCustody(custody);
    if (fromCustody) {
      return fromCustody;
    }
  }
  return parseDamageHoldDecisionFromReason(movement.reason);
}

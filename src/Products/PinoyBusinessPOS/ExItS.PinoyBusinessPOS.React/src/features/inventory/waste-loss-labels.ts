import type { MessageKey } from "@/i18n/messages";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import type { WasteLossReasonCode } from "@/api/pos/pos-waste-loss-client";
import { WASTE_LOSS_REASONS } from "@/api/pos/pos-waste-loss-client";
import { sortLotsByExpiry } from "@/features/inventory/inventory-detail-helpers";
import { resolveLotExpiryLabel } from "@/features/inventory/inventory-lot-status";

export function isWasteLossReasonCode(value: string): value is WasteLossReasonCode {
  return (WASTE_LOSS_REASONS as readonly string[]).includes(value);
}

/** i18n key for a waste/loss reason code (falls back to Other). */
export function wasteLossReasonLabelKey(reason: string): MessageKey {
  switch (reason) {
    case "Spoiled":
      return "wasteLoss.reason.spoiled";
    case "Expired":
      return "wasteLoss.reason.expired";
    case "Damaged":
      return "wasteLoss.reason.damaged";
    case "Broken":
      return "wasteLoss.reason.broken";
    case "Spillage":
      return "wasteLoss.reason.spillage";
    case "MissingOrShrinkage":
      return "wasteLoss.reason.missingOrShrinkage";
    case "Other":
      return "wasteLoss.reason.other";
    default:
      return "wasteLoss.reason.other";
  }
}

export function wasteLossStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Voided":
      return "wasteLoss.status.voided";
    case "Posted":
    default:
      return "wasteLoss.status.posted";
  }
}

export function wasteLossCostStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Complete":
      return "wasteLoss.costComplete";
    case "Partial":
      return "wasteLoss.costPartial";
    case "Unavailable":
    default:
      return "wasteLoss.costUnavailable";
  }
}

export function formatWasteLossOccurredDate(occurredAtUtc: string): string {
  const parsed = new Date(occurredAtUtc);
  if (Number.isNaN(parsed.getTime())) {
    return occurredAtUtc;
  }
  return parsed.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

/** Sum line cost snapshots when every line has an authoritative cost; otherwise null. */
export function sumWasteLossLineCosts(
  lines: Array<{ lineCostSnapshot?: number | null }>,
): number | null {
  if (lines.length === 0) {
    return null;
  }
  let total = 0;
  for (const line of lines) {
    if (line.lineCostSnapshot == null || !Number.isFinite(line.lineCostSnapshot)) {
      return null;
    }
    total += line.lineCostSnapshot;
  }
  return total;
}

/**
 * Sort lots for waste/loss lot picker. When reason is Expired, expired lots appear first
 * (still sorted by expiry date within each group).
 */
export function sortLotsForWasteLoss(
  lots: PosInventoryLotDto[],
  prioritizeExpired: boolean,
): PosInventoryLotDto[] {
  const sorted = sortLotsByExpiry(lots);
  if (!prioritizeExpired) {
    return sorted;
  }
  const expired: PosInventoryLotDto[] = [];
  const other: PosInventoryLotDto[] = [];
  for (const lot of sorted) {
    const label = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
    if (label.kind === "expired") {
      expired.push(lot);
    } else {
      other.push(lot);
    }
  }
  return [...expired, ...other];
}

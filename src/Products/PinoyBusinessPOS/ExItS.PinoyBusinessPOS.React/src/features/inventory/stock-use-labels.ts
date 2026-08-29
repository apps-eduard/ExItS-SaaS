import type { MessageKey } from "@/i18n/messages";
import type { StockUseReasonCode } from "@/api/pos/pos-stock-use-client";
import { STOCK_USE_REASONS } from "@/api/pos/pos-stock-use-client";

export function isStockUseReasonCode(value: string): value is StockUseReasonCode {
  return (STOCK_USE_REASONS as readonly string[]).includes(value);
}

/** i18n key for a stock-use reason code (falls back to Other). */
export function stockUseReasonLabelKey(reason: string): MessageKey {
  switch (reason) {
    case "InternalOperations":
      return "stockUse.reason.internalOperations";
    case "StaffUse":
      return "stockUse.reason.staffUse";
    case "SampleOrTesting":
      return "stockUse.reason.sampleOrTesting";
    case "Other":
      return "stockUse.reason.other";
    default:
      return "stockUse.reason.other";
  }
}

export function stockUseStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Voided":
      return "stockUse.status.voided";
    case "Posted":
    default:
      return "stockUse.status.posted";
  }
}

export function formatStockUseOccurredDate(occurredAtUtc: string): string {
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
export function sumStockUseLineCosts(
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

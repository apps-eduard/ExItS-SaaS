import type { MessageKey } from "@/i18n/messages";
import type { StockCountLineDto } from "@/api/pos/pos-stock-count-client";

export function stockCountStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Draft":
      return "stockCount.status.draft";
    case "InProgress":
      return "stockCount.status.inProgress";
    case "Completed":
      return "stockCount.status.completed";
    case "Cancelled":
      return "stockCount.status.cancelled";
    default:
      return "stockCount.status.draft";
  }
}

export function stockCountStatusTone(
  status: string,
): "info" | "success" | "warning" | "danger" {
  switch (status) {
    case "Completed":
      return "success";
    case "InProgress":
      return "warning";
    case "Cancelled":
      return "danger";
    case "Draft":
    default:
      return "info";
  }
}

export function formatStockCountDate(countDate: string): string {
  // API DateOnly serializes as "yyyy-MM-dd"
  const parsed = /^\d{4}-\d{2}-\d{2}/.test(countDate)
    ? new Date(`${countDate.slice(0, 10)}T12:00:00`)
    : new Date(countDate);
  if (Number.isNaN(parsed.getTime())) {
    return countDate;
  }
  return parsed.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function formatStockCountTimestamp(utc: string | null | undefined): string {
  if (!utc) {
    return "—";
  }
  const parsed = new Date(utc);
  if (Number.isNaN(parsed.getTime())) {
    return utc;
  }
  return parsed.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

/** Authoritative variance when present; otherwise live preview from counted − system. */
export function formatVariance(variance: number | null | undefined): string {
  if (variance == null || !Number.isFinite(variance)) {
    return "—";
  }
  if (variance === 0) {
    return "0";
  }
  const abs = Math.abs(variance);
  const body = Number.isInteger(abs) ? String(abs) : String(abs);
  return variance > 0 ? `+${body}` : `-${body}`;
}

export function previewVariance(
  systemQty: number | null | undefined,
  countedText: string,
): number | null {
  if (systemQty == null || !Number.isFinite(systemQty)) {
    return null;
  }
  const trimmed = countedText.trim();
  if (trimmed === "") {
    return null;
  }
  const counted = Number(trimmed);
  if (!Number.isFinite(counted)) {
    return null;
  }
  return counted - systemQty;
}

export type StockCountLineFilter = "all" | "notCounted" | "hasDifference" | "matched";

export function lineMatchesFilter(
  line: StockCountLineDto,
  countedText: string | undefined,
  filter: StockCountLineFilter,
): boolean {
  const hasCounted =
    countedText !== undefined
      ? countedText.trim() !== ""
      : line.countedQuantity != null;
  const variance =
    countedText !== undefined
      ? previewVariance(line.systemOnHandSnapshot, countedText)
      : line.variance ?? null;

  switch (filter) {
    case "notCounted":
      return !hasCounted;
    case "hasDifference":
      return hasCounted && variance != null && variance !== 0;
    case "matched":
      return hasCounted && variance === 0;
    case "all":
    default:
      return true;
  }
}

export function summarizeCountLines(
  lines: StockCountLineDto[],
  localCounted?: Record<string, string>,
): {
  total: number;
  counted: number;
  remaining: number;
  matched: number;
  lower: number;
  higher: number;
} {
  let counted = 0;
  let matched = 0;
  let lower = 0;
  let higher = 0;
  for (const line of lines) {
    const text = localCounted?.[line.productId];
    const hasCounted =
      text !== undefined ? text.trim() !== "" : line.countedQuantity != null;
    if (!hasCounted) {
      continue;
    }
    counted += 1;
    const variance =
      text !== undefined
        ? previewVariance(line.systemOnHandSnapshot, text)
        : (line.variance ?? null);
    if (variance == null) {
      continue;
    }
    if (variance === 0) {
      matched += 1;
    } else if (variance < 0) {
      lower += 1;
    } else {
      higher += 1;
    }
  }
  return {
    total: lines.length,
    counted,
    remaining: lines.length - counted,
    matched,
    lower,
    higher,
  };
}

export function differenceProductCount(lines: StockCountLineDto[]): number {
  return lines.filter((line) => line.variance != null && line.variance !== 0).length;
}

export function todayDateOnly(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export function parseCountedQuantity(text: string): number | null | "invalid" {
  const trimmed = text.trim();
  if (trimmed === "") {
    return null;
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0) {
    return "invalid";
  }
  return value;
}

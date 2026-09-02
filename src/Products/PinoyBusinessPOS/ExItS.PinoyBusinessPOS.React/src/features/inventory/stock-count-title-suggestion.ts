/**
 * UI-only stock count title suggestions from count period + date.
 * Period is not persisted; title remains an editable label only.
 */

export const STOCK_COUNT_PERIOD_TYPES = [
  "Weekly",
  "Monthly",
  "Quarterly",
  "Annual",
  "Custom",
] as const;

export type StockCountPeriodType = (typeof STOCK_COUNT_PERIOD_TYPES)[number];

const MONTH_NAMES = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
] as const;

export function parseCountDateParts(
  countDate: string,
): { year: number; month: number; day: number } | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(countDate.trim());
  if (!match) {
    return null;
  }
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (
    !Number.isInteger(year) ||
    !Number.isInteger(month) ||
    !Number.isInteger(day) ||
    month < 1 ||
    month > 12 ||
    day < 1 ||
    day > 31
  ) {
    return null;
  }
  return { year, month, day };
}

/** Calendar week-of-month: days 1–7 → Week 1, … (stable, non-fiscal). */
export function weekOfMonth(day: number): number {
  return Math.max(1, Math.ceil(day / 7));
}

export function suggestStockCountTitle(
  period: StockCountPeriodType,
  countDate: string,
): string {
  const parts = parseCountDateParts(countDate);
  if (!parts) {
    return "";
  }
  const { year, month, day } = parts;
  const monthName = MONTH_NAMES[month - 1];

  switch (period) {
    case "Weekly":
      return `${monthName} ${year} - Week ${weekOfMonth(day)}`;
    case "Monthly":
      return `${monthName} ${year}`;
    case "Quarterly":
      return `Q${Math.ceil(month / 3)} ${year}`;
    case "Annual":
      return `${year} Annual Stock Count`;
    case "Custom":
      return "";
    default:
      return "";
  }
}

/**
 * When period/date changes, refresh the suggestion only if the user has not
 * manually edited the title (dirty flag).
 */
export function nextTitleAfterSuggestionInputsChange(options: {
  period: StockCountPeriodType;
  countDate: string;
  currentTitle: string;
  titleDirty: boolean;
}): string {
  if (options.titleDirty) {
    return options.currentTitle;
  }
  return suggestStockCountTitle(options.period, options.countDate);
}

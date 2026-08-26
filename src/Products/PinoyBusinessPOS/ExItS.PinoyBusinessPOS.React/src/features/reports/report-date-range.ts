/** UTC calendar-date helpers matching server ReportDateRange / PosReportOptions. */

export type ReportDatePreset = "today" | "yesterday" | "thisWeek" | "thisMonth" | "custom";

export type ReportDateRangeValue = {
  fromDate: string;
  toDate: string;
};

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

export function formatUtcDateOnly(date: Date): string {
  const y = date.getUTCFullYear();
  const m = String(date.getUTCMonth() + 1).padStart(2, "0");
  const d = String(date.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export function parseUtcDateOnly(value: string): Date | null {
  if (!ISO_DATE.test(value)) {
    return null;
  }
  const [ys, ms, ds] = value.split("-");
  const y = Number(ys);
  const m = Number(ms);
  const d = Number(ds);
  const dt = new Date(Date.UTC(y, m - 1, d));
  if (dt.getUTCFullYear() !== y || dt.getUTCMonth() !== m - 1 || dt.getUTCDate() !== d) {
    return null;
  }
  return dt;
}

export function utcToday(now: Date = new Date()): Date {
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
}

function addUtcDays(date: Date, days: number): Date {
  const next = new Date(date.getTime());
  next.setUTCDate(next.getUTCDate() + days);
  return next;
}

/** Monday-start week in UTC (ISO-like). */
function startOfUtcWeek(date: Date): Date {
  const day = date.getUTCDay(); // 0 Sun … 6 Sat
  const mondayOffset = day === 0 ? -6 : 1 - day;
  return addUtcDays(date, mondayOffset);
}

function startOfUtcMonth(date: Date): Date {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1));
}

/**
 * Resolve a preset to explicit yyyy-MM-dd bounds.
 * Server membership uses the UTC calendar date of recorded timestamps.
 */
export function resolveReportDatePreset(
  preset: ReportDatePreset,
  now: Date = new Date(),
  custom?: ReportDateRangeValue | null,
): ReportDateRangeValue {
  const today = utcToday(now);

  switch (preset) {
    case "today":
      return { fromDate: formatUtcDateOnly(today), toDate: formatUtcDateOnly(today) };
    case "yesterday": {
      const y = addUtcDays(today, -1);
      return { fromDate: formatUtcDateOnly(y), toDate: formatUtcDateOnly(y) };
    }
    case "thisWeek": {
      const start = startOfUtcWeek(today);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "thisMonth": {
      const start = startOfUtcMonth(today);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "custom": {
      if (
        custom &&
        parseUtcDateOnly(custom.fromDate) &&
        parseUtcDateOnly(custom.toDate) &&
        custom.fromDate <= custom.toDate
      ) {
        return { fromDate: custom.fromDate, toDate: custom.toDate };
      }
      return { fromDate: formatUtcDateOnly(today), toDate: formatUtcDateOnly(today) };
    }
    default:
      return { fromDate: formatUtcDateOnly(today), toDate: formatUtcDateOnly(today) };
  }
}

/** Inclusive day span must stay within server MaxInclusiveDaySpan (366). */
export const MAX_REPORT_DAY_SPAN = 366;

export function isReportRangeValid(range: ReportDateRangeValue): boolean {
  const from = parseUtcDateOnly(range.fromDate);
  const to = parseUtcDateOnly(range.toDate);
  if (!from || !to || range.toDate < range.fromDate) {
    return false;
  }
  const span = Math.floor((to.getTime() - from.getTime()) / (24 * 60 * 60 * 1000)) + 1;
  return span <= MAX_REPORT_DAY_SPAN;
}

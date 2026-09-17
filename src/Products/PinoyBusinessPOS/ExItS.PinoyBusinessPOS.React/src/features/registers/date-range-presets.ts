import {
  formatUtcDateOnly,
  utcToday,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";

export type HistoryDatePreset = "today" | "last7Days" | "last30Days" | "thisWeek" | "thisMonth" | "custom";

function addUtcDays(date: Date, days: number): Date {
  const next = new Date(date.getTime());
  next.setUTCDate(next.getUTCDate() + days);
  return next;
}

/** Default history window: last 7 UTC calendar days inclusive of today. */
export function resolveHistoryDatePreset(
  preset: HistoryDatePreset,
  now: Date = new Date(),
  custom?: ReportDateRangeValue | null,
): ReportDateRangeValue {
  const today = utcToday(now);

  switch (preset) {
    case "today":
      return { fromDate: formatUtcDateOnly(today), toDate: formatUtcDateOnly(today) };
    case "last7Days": {
      const start = addUtcDays(today, -6);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "last30Days": {
      const start = addUtcDays(today, -29);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "thisWeek": {
      const day = today.getUTCDay();
      const mondayOffset = day === 0 ? -6 : 1 - day;
      const start = addUtcDays(today, mondayOffset);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "thisMonth": {
      const start = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "custom": {
      if (custom?.fromDate && custom?.toDate && custom.fromDate <= custom.toDate) {
        return { fromDate: custom.fromDate, toDate: custom.toDate };
      }
      return resolveHistoryDatePreset("last7Days", now);
    }
    default:
      return resolveHistoryDatePreset("last7Days", now);
  }
}

/** Inclusive UTC day bounds as ISO timestamps for register activity. */
export function dateOnlyRangeToUtcBounds(range: ReportDateRangeValue): {
  fromUtc: string;
  toUtc: string;
} {
  return {
    fromUtc: `${range.fromDate}T00:00:00.000Z`,
    toUtc: `${range.toDate}T23:59:59.999Z`,
  };
}

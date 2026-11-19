import {
  formatUtcDateOnly,
  parseUtcDateOnly,
  utcToday,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";

export type PersonalHomeKpiDatePreset =
  | "thisWeek"
  | "previousWeek"
  | "thisMonth"
  | "previousMonth"
  | "last3Months"
  | "last6Months"
  | "last12Months"
  | "custom";

export const PERSONAL_HOME_KPI_DATE_PRESETS: readonly PersonalHomeKpiDatePreset[] = [
  "thisWeek",
  "previousWeek",
  "thisMonth",
  "previousMonth",
  "last3Months",
  "last6Months",
  "last12Months",
  "custom",
] as const;

export const PERSONAL_HOME_KPI_DEFAULT_PRESET: PersonalHomeKpiDatePreset = "last3Months";

function addUtcDays(date: Date, days: number): Date {
  const next = new Date(date.getTime());
  next.setUTCDate(next.getUTCDate() + days);
  return next;
}

/** Monday-start week in UTC (ISO-like). */
function startOfUtcWeek(date: Date): Date {
  const day = date.getUTCDay();
  const mondayOffset = day === 0 ? -6 : 1 - day;
  return addUtcDays(date, mondayOffset);
}

function startOfUtcMonth(date: Date): Date {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1));
}

export function resolvePersonalHomeKpiDatePreset(
  preset: PersonalHomeKpiDatePreset,
  now: Date = new Date(),
  custom?: ReportDateRangeValue | null,
): ReportDateRangeValue {
  const today = utcToday(now);

  switch (preset) {
    case "thisWeek": {
      const start = startOfUtcWeek(today);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "previousWeek": {
      const thisWeekStart = startOfUtcWeek(today);
      const previousWeekEnd = addUtcDays(thisWeekStart, -1);
      const previousWeekStart = startOfUtcWeek(previousWeekEnd);
      return {
        fromDate: formatUtcDateOnly(previousWeekStart),
        toDate: formatUtcDateOnly(previousWeekEnd),
      };
    }
    case "thisMonth": {
      const start = startOfUtcMonth(today);
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "previousMonth": {
      const thisMonthStart = startOfUtcMonth(today);
      const previousMonthEnd = addUtcDays(thisMonthStart, -1);
      const previousMonthStart = startOfUtcMonth(previousMonthEnd);
      return {
        fromDate: formatUtcDateOnly(previousMonthStart),
        toDate: formatUtcDateOnly(previousMonthEnd),
      };
    }
    case "last3Months": {
      const start = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() - 2, 1));
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "last6Months": {
      const start = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() - 5, 1));
      return { fromDate: formatUtcDateOnly(start), toDate: formatUtcDateOnly(today) };
    }
    case "last12Months": {
      const start = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() - 11, 1));
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
      return resolvePersonalHomeKpiDatePreset("last3Months", now);
    }
    default:
      return resolvePersonalHomeKpiDatePreset("last3Months", now);
  }
}

export function enumerateUtcDateKeys(fromDate: string, toDate: string): string[] {
  const from = parseUtcDateOnly(fromDate);
  const to = parseUtcDateOnly(toDate);
  if (!from || !to || from.getTime() > to.getTime()) {
    return [];
  }
  const keys: string[] = [];
  let cursor = from;
  while (cursor.getTime() <= to.getTime()) {
    keys.push(formatUtcDateOnly(cursor));
    cursor = addUtcDays(cursor, 1);
  }
  return keys;
}

/** Inclusive end-of UTC calendar day in epoch ms. */
export function endOfUtcDateMs(dateKey: string): number {
  const day = parseUtcDateOnly(dateKey);
  if (!day) {
    return 0;
  }
  return day.getTime() + 24 * 60 * 60 * 1000 - 1;
}

/** Instant immediately before the UTC calendar day of `fromDate` (period opening snapshot). */
export function startOfUtcPeriodOpeningMs(fromDate: string): number | null {
  const day = parseUtcDateOnly(fromDate);
  if (!day) {
    return null;
  }
  return day.getTime() - 1;
}
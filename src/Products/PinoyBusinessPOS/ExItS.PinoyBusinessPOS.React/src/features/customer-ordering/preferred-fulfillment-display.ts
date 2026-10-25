import { addCalendarDays } from "@/features/customer-ordering/preferred-fulfillment-periods";
import { branchLocalClock } from "@/features/customer-ordering/pickup-time-bounds";

/** Authoritative Philippines zone for checkout fulfillment clock. */
export const PHILIPPINES_TIME_ZONE = "Asia/Manila";

const WEEKDAY_SHORT = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"] as const;
const MONTH_SHORT = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
] as const;

/** Calendar line, e.g. `Sun, Oct 25, 2026`. */
export function formatPreferredFulfillmentLongDate(isoDate: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate.trim());
  if (!match) return "";
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const utc = new Date(Date.UTC(year, month - 1, day));
  if (
    utc.getUTCFullYear() !== year
    || utc.getUTCMonth() !== month - 1
    || utc.getUTCDate() !== day
  ) {
    return "";
  }
  return `${WEEKDAY_SHORT[utc.getUTCDay()]}, ${MONTH_SHORT[utc.getUTCMonth()]} ${day}, ${year}`;
}

/** Live Philippines (Asia/Manila) date + 12-hour clock for checkout fulfillment title. */
export function philippinesFulfillmentClock(now = new Date()): {
  dateIso: string;
  dateLong: string;
  timeDisplay: string;
} {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: PHILIPPINES_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).formatToParts(now);
  const read = (type: Intl.DateTimeFormatPartTypes) =>
    parts.find((part) => part.type === type)?.value ?? "";
  const dateIso = `${read("year")}-${read("month")}-${read("day")}`;
  const dayPeriod = read("dayPeriod");
  const timeDisplay = `${read("hour")}:${read("minute")}${dayPeriod ? ` ${dayPeriod}` : ""}`;
  return {
    dateIso,
    dateLong: formatPreferredFulfillmentLongDate(dateIso),
    timeDisplay,
  };
}

/** Human label for preferred day + period without inventing an exact appointment time. */
export function formatPreferredFulfillmentLabel(input: {
  date?: string | null;
  period?: string | null;
  timeZoneId?: string | null;
  now?: Date;
  todayLabel?: string;
  tomorrowLabel?: string;
}): string | null {
  const date = input.date?.trim() ?? "";
  const period = input.period?.trim() ?? "";
  if (!date || !period) return null;

  const clock = branchLocalClock(input.now ?? new Date(), input.timeZoneId);
  const tomorrow = addCalendarDays(clock.date, 1);
  let dayLabel = date;
  if (date === clock.date) {
    dayLabel = input.todayLabel ?? "Today";
  } else if (date === tomorrow) {
    dayLabel = input.tomorrowLabel ?? "Tomorrow";
  }

  return `${dayLabel} — ${period}`;
}

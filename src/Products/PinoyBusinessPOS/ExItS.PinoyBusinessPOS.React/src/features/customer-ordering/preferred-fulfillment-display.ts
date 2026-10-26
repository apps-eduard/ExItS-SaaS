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

/**
 * Preferred day + period as calendar date — period (e.g. `Sun, Oct 26, 2026 — Afternoon`).
 * Does not invent an exact appointment time. Relative Today/Tomorrow labels are not used.
 */
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

  const dayLabel = formatPreferredFulfillmentLongDate(date) || date;
  return `${dayLabel} — ${period}`;
}

/**
 * Seller/customer fulfillment event timestamp (ready / pickup / delivered).
 * Calendar date + 12-hour clock, e.g. `Tue, Oct 27, 2026, 3:45 PM`.
 */
export function formatOrderEventDateTime(
  isoUtc: string,
  timeZoneId: string = PHILIPPINES_TIME_ZONE,
): string {
  const instant = new Date(isoUtc);
  if (Number.isNaN(instant.getTime())) return "";

  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: timeZoneId.trim() || PHILIPPINES_TIME_ZONE,
    weekday: "short",
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).formatToParts(instant);
  const read = (type: Intl.DateTimeFormatPartTypes) =>
    parts.find((part) => part.type === type)?.value ?? "";
  const weekday = read("weekday");
  const month = read("month");
  const day = read("day");
  const year = read("year");
  const hour = read("hour");
  const minute = read("minute");
  const dayPeriod = read("dayPeriod");
  if (!weekday || !month || !day || !year || !hour || !minute) return "";
  const time = `${hour}:${minute}${dayPeriod ? ` ${dayPeriod}` : ""}`;
  return `${weekday}, ${month} ${day}, ${year}, ${time}`;
}

/** True when the order has already been collected / delivered. */
export function isOrderFulfillmentCompleted(fulfillmentStatus: string): boolean {
  return (
    fulfillmentStatus.localeCompare("Collected", undefined, { sensitivity: "accent" }) === 0
    || fulfillmentStatus.localeCompare("Delivered", undefined, { sensitivity: "accent" }) === 0
  );
}

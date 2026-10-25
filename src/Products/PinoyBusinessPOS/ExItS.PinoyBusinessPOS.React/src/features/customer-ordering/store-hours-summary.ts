import type { CustomerStorefrontBranchDto } from "@/api/pos/pos-customer-orders-client";

type HoursDay = NonNullable<CustomerStorefrontBranchDto["operatingHours"]>[number];

const WEEKDAY_ORDER = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday",
] as const;

const SHORT: Record<string, string> = {
  Monday: "Mon",
  Tuesday: "Tue",
  Wednesday: "Wed",
  Thursday: "Thu",
  Friday: "Fri",
  Saturday: "Sat",
  Sunday: "Sun",
};

function normalizeDay(dayOfWeek: string): (typeof WEEKDAY_ORDER)[number] | null {
  const match = WEEKDAY_ORDER.find(
    (day) => day.localeCompare(dayOfWeek.trim(), undefined, { sensitivity: "accent" }) === 0,
  );
  return match ?? null;
}

/** Compact 12-hour clock: `8 AM`, `10:30 PM`. */
export function formatStoreClock(time: string | null | undefined): string | null {
  if (!time?.trim()) return null;
  const match = /^(\d{1,2}):(\d{2})/.exec(time.trim());
  if (!match) return null;
  let hour = Number(match[1]);
  const minute = Number(match[2]);
  if (!Number.isFinite(hour) || !Number.isFinite(minute) || hour > 23 || minute > 59) {
    return null;
  }
  const suffix = hour >= 12 ? "PM" : "AM";
  hour = hour % 12;
  if (hour === 0) hour = 12;
  return minute === 0 ? `${hour} ${suffix}` : `${hour}:${String(minute).padStart(2, "0")} ${suffix}`;
}

function dayKey(day: HoursDay): string {
  if (day.isClosed) return "closed";
  if (day.isOpen24Hours) return "24h";
  return `${day.openTime ?? ""}|${day.closeTime ?? ""}`;
}

function dayLabel(day: HoursDay): string {
  if (day.isClosed) return "Closed";
  if (day.isOpen24Hours) return "Open 24 hours";
  const open = formatStoreClock(day.openTime);
  const close = formatStoreClock(day.closeTime);
  if (!open || !close) return "Hours unavailable";
  return `${open} – ${close}`;
}

function rangeLabel(start: string, end: string): string {
  const a = SHORT[start] ?? start;
  const b = SHORT[end] ?? end;
  return start === end ? a : `${a}–${b}`;
}

/**
 * Customer-facing weekly hours, e.g. `Mon–Fri 8 AM – 10 PM`.
 * Groups consecutive weekdays that share the same open window.
 */
export function formatStoreHoursSummary(
  operatingHours: ReadonlyArray<HoursDay> | null | undefined,
): string | null {
  if (!operatingHours?.length) return null;

  const byDay = new Map<string, HoursDay>();
  for (const day of operatingHours) {
    const name = normalizeDay(day.dayOfWeek);
    if (name) byDay.set(name, day);
  }
  if (byDay.size === 0) return null;

  const ordered = WEEKDAY_ORDER.map((name) => byDay.get(name)).filter(
    (day): day is HoursDay => day != null,
  );
  if (ordered.length === 0) return null;

  const segments: string[] = [];
  let i = 0;
  while (i < ordered.length) {
    const start = ordered[i]!;
    const startName = normalizeDay(start.dayOfWeek)!;
    const key = dayKey(start);
    let j = i + 1;
    while (j < ordered.length && dayKey(ordered[j]!) === key) {
      j += 1;
    }
    const endName = normalizeDay(ordered[j - 1]!.dayOfWeek)!;
    if (key !== "closed") {
      segments.push(`${rangeLabel(startName, endName)} ${dayLabel(start)}`);
    }
    i = j;
  }

  if (segments.length === 0) {
    return "Closed every day";
  }
  return segments.join("; ");
}

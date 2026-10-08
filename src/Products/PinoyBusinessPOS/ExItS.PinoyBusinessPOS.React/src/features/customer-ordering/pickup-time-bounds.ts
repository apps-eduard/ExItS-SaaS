export type PickupHoursDay = {
  dayOfWeek: string;
  isClosed: boolean;
  isOpen24Hours: boolean;
  openTime?: string | null;
  closeTime?: string | null;
};

export type PickupTimeBounds = {
  min: string | null;
  max: string | null;
  closed: boolean;
};

const WEEKDAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"] as const;

export function weekdayOfIsoDate(isoDate: string): string | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));
  if (date.getUTCFullYear() !== year || date.getUTCMonth() !== month - 1 || date.getUTCDate() !== day) {
    return null;
  }
  return WEEKDAYS[date.getUTCDay()] ?? null;
}

export function branchLocalClock(now: Date, timeZoneId: string | null | undefined): { date: string; time: string } {
  const zone = timeZoneId?.trim() || undefined;
  try {
    const parts = new Intl.DateTimeFormat("en-US", {
      timeZone: zone,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hourCycle: "h23",
    }).formatToParts(now);
    const read = (type: Intl.DateTimeFormatPartTypes) => parts.find((part) => part.type === type)?.value ?? "";
    const hour = read("hour") === "24" ? "00" : read("hour");
    const minute = read("minute");
    const second = Number(read("second") || "0");
    const date = `${read("year")}-${read("month")}-${read("day")}`;
    const time = second > 0 ? addMinutes(`${hour}:${minute}`, 1) : `${hour}:${minute}`;
    return { date, time };
  } catch {
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, "0");
    const day = String(now.getDate()).padStart(2, "0");
    const hour = String(now.getHours()).padStart(2, "0");
    const minute = String(now.getMinutes()).padStart(2, "0");
    const time = now.getSeconds() > 0 ? addMinutes(`${hour}:${minute}`, 1) : `${hour}:${minute}`;
    return { date: `${year}-${month}-${day}`, time };
  }
}

export function pickupTimeBounds(input: {
  date: string;
  now: Date;
  timeZoneId?: string | null;
  operatingHours?: PickupHoursDay[] | null;
}): PickupTimeBounds {
  const local = branchLocalClock(input.now, input.timeZoneId);
  const weekday = weekdayOfIsoDate(input.date);
  const day = weekday
    ? (input.operatingHours ?? []).find((item) => item.dayOfWeek.toLowerCase() === weekday.toLowerCase())
    : undefined;
  if (day?.isClosed) {
    return { min: null, max: null, closed: true };
  }

  const close = day && !day.isOpen24Hours ? clockOrNull(day.closeTime) : null;
  const open = day && !day.isOpen24Hours ? clockOrNull(day.openTime) : null;
  const sameDayClose = close && open && close > open ? close : null;
  const min = input.date === local.date ? local.time : null;
  if (min && sameDayClose && min > sameDayClose) {
    return { min, max: sameDayClose, closed: true };
  }
  return { min, max: sameDayClose, closed: false };
}

export function clampPickupTime(value: string, bounds: PickupTimeBounds): string {
  const clock = clockOrNull(value);
  if (!clock || bounds.closed) return "";
  if (bounds.min && clock < bounds.min) return bounds.min;
  if (bounds.max && clock > bounds.max) return bounds.max;
  return clock;
}

function clockOrNull(value: string | null | undefined): string | null {
  const match = /^(\d{1,2}):(\d{2})/.exec((value ?? "").trim());
  if (!match) return null;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (hour > 23 || minute > 59) return null;
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function addMinutes(clock: string, minutes: number): string {
  const [hour, minute] = clock.split(":").map(Number);
  const total = (hour * 60 + minute + minutes) % (24 * 60);
  const nextHour = Math.floor(total / 60);
  const nextMinute = total % 60;
  return `${String(nextHour).padStart(2, "0")}:${String(nextMinute).padStart(2, "0")}`;
}

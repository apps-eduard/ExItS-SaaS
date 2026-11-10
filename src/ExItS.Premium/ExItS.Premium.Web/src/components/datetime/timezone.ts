/**
 * Named-timezone boundaries for a local calendar date plus a local `HH:mm`.
 *
 * This does not schedule anything and does not store a UTC time-of-day.
 * Resolution uses `Intl.DateTimeFormat` with an IANA zone. A local value is
 * kept as wall-clock text until this function is asked to find the matching instant.
 *
 * Policy:
 * - Nonexistent civil times (daylight-saving gaps) return `nonexistent`.
 *   They are not moved forward or backward.
 * - Ambiguous civil times (fall-back overlaps) return both instants.
 *   The default policy is `reject`. `earlier` and `later` choose the earlier
 *   or later UTC instant only when the caller sets that policy.
 * - If this runtime cannot resolve named zones, the result is `unsupported`.
 *   Callers must not describe that as full daylight-saving support.
 */

import { formatCalendarDate, parseCalendarDate } from "./date";
import { formatTimeValue, parseTimeValue } from "../time/time";

export type AmbiguityPolicy = "reject" | "earlier" | "later";

export type ZoneInstant = {
  utcMs: number;
  offsetMinutes: number;
};

export type ZoneResolution =
  | ({ status: "unique" } & ZoneInstant)
  | {
      status: "ambiguous";
      earlierUtcMs: number;
      laterUtcMs: number;
      earlierOffsetMinutes: number;
      laterOffsetMinutes: number;
    }
  | { status: "nonexistent" }
  | { status: "invalid-timezone" }
  | { status: "unsupported" };

export type WallClock = {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
};

export function namedTimeZoneSupported(): boolean {
  try {
    new Intl.DateTimeFormat("en-US", { timeZone: "UTC" }).format(new Date(0));
    return typeof Intl.DateTimeFormat.prototype.formatToParts === "function";
  } catch {
    return false;
  }
}

export function isValidTimeZone(timeZone: string): boolean {
  if (!namedTimeZoneSupported()) return false;
  try {
    new Intl.DateTimeFormat("en-US", { timeZone }).format(new Date(0));
    return true;
  } catch {
    return false;
  }
}

export function readWallClock(utcMs: number, timeZone: string): WallClock {
  const formatter = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
  const bag: Record<string, string> = {};
  for (const part of formatter.formatToParts(new Date(utcMs))) {
    if (part.type !== "literal") bag[part.type] = part.value;
  }
  let year = Number(bag.year);
  let month = Number(bag.month);
  let day = Number(bag.day);
  let hour = Number(bag.hour);
  const minute = Number(bag.minute);
  if (hour === 24) {
    hour = 0;
    const next = new Date(Date.UTC(year, month - 1, day));
    next.setUTCDate(next.getUTCDate() + 1);
    year = next.getUTCFullYear();
    month = next.getUTCMonth() + 1;
    day = next.getUTCDate();
  }
  return { year, month, day, hour, minute };
}

export function offsetMinutesAt(utcMs: number, timeZone: string): number {
  const wall = readWallClock(utcMs, timeZone);
  const wallAsUtc = Date.UTC(wall.year, wall.month - 1, wall.day, wall.hour, wall.minute);
  return Math.round((wallAsUtc - utcMs) / 60000);
}

export function formatOffsetMinutes(offsetMinutes: number): string {
  const sign = offsetMinutes >= 0 ? "+" : "-";
  const absolute = Math.abs(offsetMinutes);
  const hours = Math.floor(absolute / 60);
  const minutes = absolute % 60;
  return `UTC${sign}${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
}

function sameWall(wall: WallClock, desired: WallClock): boolean {
  return (
    wall.year === desired.year &&
    wall.month === desired.month &&
    wall.day === desired.day &&
    wall.hour === desired.hour &&
    wall.minute === desired.minute
  );
}

export function resolveZonedDateTime(date: string, time: string, timeZone: string): ZoneResolution {
  if (!namedTimeZoneSupported()) return { status: "unsupported" };
  if (!isValidTimeZone(timeZone)) return { status: "invalid-timezone" };
  const calendar = parseCalendarDate(date);
  const clock = parseTimeValue(time);
  if (!calendar || !clock) return { status: "nonexistent" };

  const desired: WallClock = { ...calendar, hour: clock.hour, minute: clock.minute };
  const naiveUtc = Date.UTC(calendar.year, calendar.month - 1, calendar.day, clock.hour, clock.minute);
  const offsets = new Set<number>();
  for (let hour = -18; hour <= 18; hour += 1) {
    offsets.add(offsetMinutesAt(naiveUtc + hour * 60 * 60 * 1000, timeZone));
  }

  const matches: ZoneInstant[] = [];
  for (const offsetMinutes of offsets) {
    const utcMs = naiveUtc - offsetMinutes * 60000;
    if (matches.some((match) => match.utcMs === utcMs)) continue;
    if (sameWall(readWallClock(utcMs, timeZone), desired)) matches.push({ utcMs, offsetMinutes });
  }
  matches.sort((a, b) => a.utcMs - b.utcMs);
  if (matches.length === 0) return { status: "nonexistent" };
  if (matches.length === 1) return { status: "unique", utcMs: matches[0].utcMs, offsetMinutes: matches[0].offsetMinutes };
  const earlier = matches[0];
  const later = matches[matches.length - 1];
  return {
    status: "ambiguous",
    earlierUtcMs: earlier.utcMs,
    laterUtcMs: later.utcMs,
    earlierOffsetMinutes: earlier.offsetMinutes,
    laterOffsetMinutes: later.offsetMinutes,
  };
}

export function applyAmbiguity(resolution: ZoneResolution, policy: AmbiguityPolicy = "reject"): ZoneInstant | Exclude<ZoneResolution, { status: "unique" | "ambiguous" }> | { status: "ambiguous" } {
  if (resolution.status === "unique") return { utcMs: resolution.utcMs, offsetMinutes: resolution.offsetMinutes };
  if (resolution.status !== "ambiguous") return resolution;
  if (policy === "reject") return { status: "ambiguous" };
  if (policy === "earlier") return { utcMs: resolution.earlierUtcMs, offsetMinutes: resolution.earlierOffsetMinutes };
  return { utcMs: resolution.laterUtcMs, offsetMinutes: resolution.laterOffsetMinutes };
}

export function currentWallClock(now: Date, timeZone?: string): { date: string; time: string } | { error: "invalid-timezone" | "unsupported" } {
  if (!timeZone) {
    return {
      date: formatCalendarDate({ year: now.getFullYear(), month: now.getMonth() + 1, day: now.getDate() }),
      time: formatTimeValue({ hour: now.getHours(), minute: now.getMinutes() }),
    };
  }
  if (!namedTimeZoneSupported()) return { error: "unsupported" };
  if (!isValidTimeZone(timeZone)) return { error: "invalid-timezone" };
  const wall = readWallClock(now.getTime(), timeZone);
  return {
    date: formatCalendarDate(wall),
    time: formatTimeValue({ hour: wall.hour, minute: wall.minute }),
  };
}

import { addCalendarDays } from "@/features/customer-ordering/preferred-fulfillment-periods";
import {
  branchLocalClock,
  weekdayOfIsoDate,
  type PickupHoursDay,
} from "@/features/customer-ordering/pickup-time-bounds";

type ClockInterval = { startMinutes: number; endMinutes: number };

export type StoreOpenStatus =
  | { kind: "unknown" }
  | { kind: "open24" }
  /** `closeTime` is branch-local `HH:mm` for the end of the current open window. */
  | { kind: "open"; minutesUntilClose: number; closeTime: string }
  | { kind: "closed"; minutesUntilOpen: number | null };

/** Current open / closed status from branch hours + timezone. */
export function resolveStoreOpenStatus(input: {
  now?: Date;
  timeZoneId?: string | null;
  operatingHours?: PickupHoursDay[] | null;
}): StoreOpenStatus {
  const hours = input.operatingHours ?? [];
  if (hours.length === 0) return { kind: "unknown" };

  const now = input.now ?? new Date();
  const local = branchLocalClock(now, input.timeZoneId);
  const nowMinutes = clockToMinutes(local.time);
  if (nowMinutes == null) return { kind: "unknown" };

  const todayWeekday = weekdayOfIsoDate(local.date);
  const todayHours = todayWeekday
    ? hours.find((item) => item.dayOfWeek.toLowerCase() === todayWeekday.toLowerCase())
    : undefined;
  if (todayHours && !todayHours.isClosed && todayHours.isOpen24Hours) {
    return { kind: "open24" };
  }

  const todayIntervals = openIntervalsForDay(local.date, hours);
  const current = todayIntervals.find(
    (interval) => nowMinutes >= interval.startMinutes && nowMinutes < interval.endMinutes,
  );
  if (current) {
    let untilClose = current.endMinutes - nowMinutes;
    let closeMinutes = current.endMinutes;
    if (current.endMinutes >= 24 * 60) {
      const tomorrow = addCalendarDays(local.date, 1);
      const nextIntervals = openIntervalsForDay(tomorrow, hours);
      const continues = nextIntervals.find((interval) => interval.startMinutes === 0);
      if (continues) {
        untilClose += continues.endMinutes;
        closeMinutes = continues.endMinutes;
      } else {
        closeMinutes = 0;
      }
    }
    if (untilClose >= 24 * 60) {
      return { kind: "open24" };
    }
    return {
      kind: "open",
      minutesUntilClose: Math.max(0, untilClose),
      closeTime: minutesToClock(closeMinutes),
    };
  }

  const untilOpen = minutesUntilNextOpen(local.date, nowMinutes, hours);
  return { kind: "closed", minutesUntilOpen: untilOpen };
}

/** Human remaining span: `45m`, `2h`, `2h 15m`, `1d 3h`. */
export function formatRemainingDuration(totalMinutes: number): string {
  const minutes = Math.max(0, Math.round(totalMinutes));
  if (minutes < 1) return "< 1m";
  const days = Math.floor(minutes / (24 * 60));
  const hours = Math.floor((minutes % (24 * 60)) / 60);
  const mins = minutes % 60;
  if (days > 0) {
    return hours > 0 ? `${days}d ${hours}h` : `${days}d`;
  }
  if (hours > 0) {
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }
  return `${mins}m`;
}

function minutesUntilNextOpen(
  todayIso: string,
  nowMinutes: number,
  hours: PickupHoursDay[],
): number | null {
  const todayIntervals = openIntervalsForDay(todayIso, hours);
  const laterToday = todayIntervals.find((interval) => interval.startMinutes > nowMinutes);
  if (laterToday) {
    return laterToday.startMinutes - nowMinutes;
  }

  for (let dayOffset = 1; dayOffset <= 14; dayOffset += 1) {
    const iso = addCalendarDays(todayIso, dayOffset);
    const intervals = openIntervalsForDay(iso, hours);
    if (intervals.length === 0) continue;
    const first = intervals[0]!;
    return dayOffset * 24 * 60 - nowMinutes + first.startMinutes;
  }
  return null;
}

function openIntervalsForDay(
  isoDate: string,
  operatingHours: PickupHoursDay[],
): ClockInterval[] {
  if (operatingHours.length === 0) return [];

  const weekday = weekdayOfIsoDate(isoDate);
  if (!weekday) return [];
  const day = operatingHours.find((item) => item.dayOfWeek.toLowerCase() === weekday.toLowerCase());
  const intervals: ClockInterval[] = [];

  if (day && !day.isClosed) {
    if (day.isOpen24Hours) {
      intervals.push({ startMinutes: 0, endMinutes: 24 * 60 });
    } else {
      const open = clockToMinutes(day.openTime);
      const close = clockToMinutes(day.closeTime);
      if (open != null && close != null) {
        if (close > open) {
          intervals.push({ startMinutes: open, endMinutes: close });
        } else if (close < open) {
          intervals.push({ startMinutes: open, endMinutes: 24 * 60 });
        }
      }
    }
  }

  const previousIso = addCalendarDays(isoDate, -1);
  const previousWeekday = weekdayOfIsoDate(previousIso);
  const previous = previousWeekday
    ? operatingHours.find((item) => item.dayOfWeek.toLowerCase() === previousWeekday.toLowerCase())
    : undefined;
  if (previous && !previous.isClosed && !previous.isOpen24Hours) {
    const open = clockToMinutes(previous.openTime);
    const close = clockToMinutes(previous.closeTime);
    if (open != null && close != null && close < open && close > 0) {
      intervals.push({ startMinutes: 0, endMinutes: close });
    }
  }

  return mergeIntervals(intervals);
}

function mergeIntervals(intervals: ClockInterval[]): ClockInterval[] {
  if (intervals.length <= 1) return intervals;
  const ordered = [...intervals].sort((a, b) => a.startMinutes - b.startMinutes);
  const merged: ClockInterval[] = [{ ...ordered[0]! }];
  for (let i = 1; i < ordered.length; i += 1) {
    const next = ordered[i]!;
    const last = merged[merged.length - 1]!;
    if (next.startMinutes <= last.endMinutes) {
      last.endMinutes = Math.max(last.endMinutes, next.endMinutes);
    } else {
      merged.push({ ...next });
    }
  }
  return merged;
}

function clockToMinutes(value: string | null | undefined): number | null {
  const match = /^(\d{1,2}):(\d{2})/.exec((value ?? "").trim());
  if (!match) return null;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (hour > 23 || minute > 59) return null;
  return hour * 60 + minute;
}

/** Wall-clock `HH:mm` from minutes past midnight (wraps at 24h). */
function minutesToClock(totalMinutes: number): string {
  const wrapped = ((Math.round(totalMinutes) % (24 * 60)) + 24 * 60) % (24 * 60);
  const hour = Math.floor(wrapped / 60);
  const minute = wrapped % 60;
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

import {
  branchLocalClock,
  pickupMaxScheduleDate,
  weekdayOfIsoDate,
  type PickupHoursDay,
} from "./pickup-time-bounds";

export type PreferredFulfillmentPeriod = "Morning" | "Afternoon" | "Evening";

export const PREFERRED_FULFILLMENT_PERIODS: readonly PreferredFulfillmentPeriod[] = [
  "Morning",
  "Afternoon",
  "Evening",
] as const;

/** Minimum usable intersection after lead-time clipping (minutes). Matches backend. */
export const PREFERRED_FULFILLMENT_MIN_USABLE_MINUTES = 30;

type ClockInterval = { startMinutes: number; endMinutes: number };

const PERIOD_WINDOWS: Record<PreferredFulfillmentPeriod, ClockInterval> = {
  Morning: { startMinutes: 0, endMinutes: 12 * 60 },
  Afternoon: { startMinutes: 12 * 60, endMinutes: 18 * 60 },
  Evening: { startMinutes: 18 * 60, endMinutes: 24 * 60 },
};

export function addCalendarDays(isoDate: string, days: number): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
  if (!match) return isoDate;
  const utc = new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
  utc.setUTCDate(utc.getUTCDate() + days);
  const y = utc.getUTCFullYear();
  const m = String(utc.getUTCMonth() + 1).padStart(2, "0");
  const d = String(utc.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export function isDayClosed(
  isoDate: string,
  operatingHours: PickupHoursDay[] | null | undefined,
): boolean {
  const intervals = openIntervalsForDay(isoDate, operatingHours);
  return intervals.length === 0;
}

export function isPreferredDateSelectable(input: {
  date: string;
  now: Date;
  timeZoneId?: string | null;
  operatingHours?: PickupHoursDay[] | null;
  maxAdvanceScheduleDays?: number;
}): boolean {
  const today = branchLocalClock(input.now, input.timeZoneId).date;
  if (input.date < today) return false;
  const max = pickupMaxScheduleDate({
    now: input.now,
    timeZoneId: input.timeZoneId,
    maxAdvanceScheduleDays: input.maxAdvanceScheduleDays,
  });
  if (max && input.date > max) return false;
  return !isDayClosed(input.date, input.operatingHours);
}

export function availablePreferredPeriods(input: {
  date: string;
  now: Date;
  timeZoneId?: string | null;
  operatingHours?: PickupHoursDay[] | null;
  minPreparationLeadMinutes?: number;
}): PreferredFulfillmentPeriod[] {
  const open = openIntervalsForDay(input.date, input.operatingHours);
  if (open.length === 0) return [];

  const local = branchLocalClock(input.now, input.timeZoneId);
  const lead = Math.max(0, input.minPreparationLeadMinutes ?? 0);
  const localMinutes = clockToMinutes(local.time) ?? 0;
  const earliest = input.date === local.date ? localMinutes + lead : 0;

  const available: PreferredFulfillmentPeriod[] = [];
  for (const period of PREFERRED_FULFILLMENT_PERIODS) {
    const periodWindow = PERIOD_WINDOWS[period];
    const intersections = intersectIntervals(open, periodWindow)
      .map((interval) => ({
        startMinutes: Math.max(interval.startMinutes, earliest),
        endMinutes: interval.endMinutes,
      }))
      .filter((interval) => interval.endMinutes > interval.startMinutes);
    const usable = intersections.some(
      (interval) =>
        interval.endMinutes - interval.startMinutes >= PREFERRED_FULFILLMENT_MIN_USABLE_MINUTES,
    );
    if (usable) available.push(period);
  }
  return available;
}

export function periodCaption(period: PreferredFulfillmentPeriod): string {
  switch (period) {
    case "Morning":
      return "Before 12:00 PM";
    case "Afternoon":
      return "12:00 PM to 6:00 PM";
    case "Evening":
      return "After 6:00 PM";
  }
}

function openIntervalsForDay(
  isoDate: string,
  operatingHours: PickupHoursDay[] | null | undefined,
): ClockInterval[] {
  const hours = operatingHours ?? [];
  if (hours.length === 0) return [];

  const weekday = weekdayOfIsoDate(isoDate);
  if (!weekday) return [];
  const day = hours.find((item) => item.dayOfWeek.toLowerCase() === weekday.toLowerCase());
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
    ? hours.find((item) => item.dayOfWeek.toLowerCase() === previousWeekday.toLowerCase())
    : undefined;
  if (
    previous
    && !previous.isClosed
    && !previous.isOpen24Hours
  ) {
    const open = clockToMinutes(previous.openTime);
    const close = clockToMinutes(previous.closeTime);
    if (open != null && close != null && close < open && close > 0) {
      intervals.push({ startMinutes: 0, endMinutes: close });
    }
  }

  return mergeIntervals(intervals);
}

function intersectIntervals(open: ClockInterval[], period: ClockInterval): ClockInterval[] {
  const result: ClockInterval[] = [];
  for (const interval of open) {
    const start = Math.max(interval.startMinutes, period.startMinutes);
    const end = Math.min(interval.endMinutes, period.endMinutes);
    if (end > start) result.push({ startMinutes: start, endMinutes: end });
  }
  return result;
}

function mergeIntervals(intervals: ClockInterval[]): ClockInterval[] {
  if (intervals.length <= 1) return intervals;
  const ordered = [...intervals].sort((a, b) => a.startMinutes - b.startMinutes);
  const merged: ClockInterval[] = [ordered[0]!];
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

import type { BranchOperatingHoursDayDto } from "@/api/platform/branch-fulfillment-client";

export const ORDERED_WEEKDAYS = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday",
] as const;

export type Weekday = (typeof ORDERED_WEEKDAYS)[number];

export type HoursDayDraft = {
  dayOfWeek: Weekday;
  isClosed: boolean;
  isOpen24Hours: boolean;
  openTime: string;
  closeTime: string;
};

export function defaultHoursSchedule(): HoursDayDraft[] {
  return ORDERED_WEEKDAYS.map((day) => ({
    dayOfWeek: day,
    isClosed: true,
    isOpen24Hours: false,
    openTime: "08:00",
    closeTime: "21:00",
  }));
}

export function hoursFromDto(days: BranchOperatingHoursDayDto[]): HoursDayDraft[] {
  const byDay = new Map(days.map((d) => [d.dayOfWeek.toLowerCase(), d]));
  return ORDERED_WEEKDAYS.map((day) => {
    const dto = byDay.get(day.toLowerCase());
    if (!dto) {
      return {
        dayOfWeek: day,
        isClosed: true,
        isOpen24Hours: false,
        openTime: "08:00",
        closeTime: "21:00",
      };
    }
    return {
      dayOfWeek: day,
      isClosed: dto.isClosed,
      isOpen24Hours: dto.isOpen24Hours,
      openTime: normalizeTime(dto.openTime) ?? "08:00",
      closeTime: normalizeTime(dto.closeTime) ?? "21:00",
    };
  });
}

export function hoursToRequest(days: HoursDayDraft[]): BranchOperatingHoursDayDto[] {
  return days.map((day) => ({
    dayOfWeek: day.dayOfWeek,
    isClosed: day.isClosed,
    isOpen24Hours: day.isOpen24Hours && !day.isClosed,
    openTime: day.isClosed || day.isOpen24Hours ? null : day.openTime,
    closeTime: day.isClosed || day.isOpen24Hours ? null : day.closeTime,
  }));
}

export function hasConfiguredHours(days: HoursDayDraft[]): boolean {
  return days.some((d) => !d.isClosed || d.isOpen24Hours);
}

function normalizeTime(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const trimmed = value.trim();
  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(trimmed);
  if (!match) {
    return trimmed.slice(0, 5);
  }
  const hour = Number(match[1]).toString().padStart(2, "0");
  return `${hour}:${match[2]}`;
}

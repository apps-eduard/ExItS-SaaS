/**
 * Local date + local time while editing: `{ date: "YYYY-MM-DD", time: "HH:mm" }`.
 * Neither field is a UTC timestamp. Optional IANA resolution is explicit via
 * `resolveZonedDateTime` and does not rewrite the stored value.
 */

import { calendarDateMessage, compareCalendarDate, formatCalendarDisplay, parseCalendarDate } from "./date";
import {
  applyAmbiguity,
  currentWallClock,
  formatOffsetMinutes,
  resolveZonedDateTime,
  type AmbiguityPolicy,
  type ZoneResolution,
} from "./timezone";
import {
  floorMinute,
  formatTimeDisplay,
  formatTimeValue,
  isMinuteStep,
  parseTimeValue,
  validateTime,
  type HourCycle,
  type MinuteStep,
  type TimeValidation,
} from "../time/time";

export type DateTimeValue = {
  /** Local calendar date `YYYY-MM-DD`. */
  date: string;
  /** Local time `HH:mm`. */
  time: string;
};

export type DateTimeValidationCode =
  | "required"
  | "invalid-date"
  | "invalid-time"
  | "min"
  | "max"
  | "range"
  | "step"
  | "disabled"
  | "nonexistent"
  | "ambiguous"
  | "invalid-timezone"
  | "unsupported";

export type DateTimeValidation = {
  valid: boolean;
  code?: DateTimeValidationCode;
  message?: string;
  resolution?: ZoneResolution;
  instantUtcMs?: number;
  offsetMinutes?: number;
};

export function compareDateTime(a: DateTimeValue, b: DateTimeValue): number {
  const byDate = compareCalendarDate(a.date, b.date);
  if (byDate !== 0) return byDate;
  if (a.time === b.time) return 0;
  return a.time < b.time ? -1 : 1;
}

export function formatDateTimeDisplay(value: DateTimeValue, hourCycle: HourCycle, locale?: string): string | null {
  const date = formatCalendarDisplay(value.date, locale);
  const time = formatTimeDisplay(value.time, hourCycle, locale);
  if (!date || !time) return null;
  return `${date}, ${time}`;
}

export function validateDateTime(input: {
  value: DateTimeValue | null;
  min?: DateTimeValue;
  max?: DateTimeValue;
  minuteStep?: MinuteStep;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
  timeZone?: string;
  ambiguity?: AmbiguityPolicy;
  required?: boolean;
  locale?: string;
  hourCycle?: HourCycle;
}): DateTimeValidation {
  if (!input.value) {
    if (input.required) return { valid: false, code: "required", message: "Choose a date and time." };
    return { valid: true };
  }
  if (!parseCalendarDate(input.value.date)) {
    return { valid: false, code: "invalid-date", message: calendarDateMessage(input.value.date) };
  }
  const timeResult: TimeValidation = validateTime({
    value: input.value.time,
    minuteStep: input.minuteStep,
    disabledTimes: input.disabledTimes,
    isTimeDisabled: input.isTimeDisabled,
    locale: input.locale,
  });
  if (!timeResult.valid) {
    const code = timeResult.code === "invalid" || timeResult.code === "required" ? "invalid-time" : timeResult.code;
    return { valid: false, code: code ?? "invalid-time", message: timeResult.message };
  }
  if (input.minuteStep !== undefined && !isMinuteStep(input.minuteStep)) {
    return { valid: false, code: "step", message: "Choose a minute step of 1, 5, 10, 15, or 30." };
  }
  if (input.min && (!parseCalendarDate(input.min.date) || !parseTimeValue(input.min.time))) {
    return { valid: false, code: "range", message: "The earliest date and time is not valid." };
  }
  if (input.max && (!parseCalendarDate(input.max.date) || !parseTimeValue(input.max.time))) {
    return { valid: false, code: "range", message: "The latest date and time is not valid." };
  }
  if (input.min && input.max && compareDateTime(input.min, input.max) > 0) {
    return { valid: false, code: "range", message: "The earliest date and time is after the latest." };
  }
  const cycle = input.hourCycle ?? "h12";
  const label = (value: DateTimeValue) => formatDateTimeDisplay(value, cycle, input.locale) ?? `${value.date} ${value.time}`;
  if (input.min && compareDateTime(input.value, input.min) < 0) {
    return { valid: false, code: "min", message: `Choose ${label(input.min)} or later.` };
  }
  if (input.max && compareDateTime(input.value, input.max) > 0) {
    return { valid: false, code: "max", message: `Choose ${label(input.max)} or earlier.` };
  }
  if (!input.timeZone) return { valid: true };

  const resolution = resolveZonedDateTime(input.value.date, input.value.time, input.timeZone);
  if (resolution.status === "unsupported") {
    return {
      valid: false,
      code: "unsupported",
      resolution,
      message: "This browser cannot resolve named time zones. Choose a date and time without a time zone, or use a runtime with IANA time zone data.",
    };
  }
  if (resolution.status === "invalid-timezone") {
    return { valid: false, code: "invalid-timezone", resolution, message: `"${input.timeZone}" is not a valid IANA time zone.` };
  }
  if (resolution.status === "nonexistent") {
    return {
      valid: false,
      code: "nonexistent",
      resolution,
      message: `This clock time does not exist in ${input.timeZone} because of a daylight-saving change. Choose a different time. It was not moved.`,
    };
  }
  const chosen = applyAmbiguity(resolution, input.ambiguity ?? "reject");
  if ("status" in chosen && chosen.status === "ambiguous") {
    return {
      valid: false,
      code: "ambiguous",
      resolution,
      message: `This clock time happens twice in ${input.timeZone}. It was not chosen for you. Set the ambiguity policy to earlier or later to select the earlier or later instant.`,
    };
  }
  if (!("utcMs" in chosen)) return { valid: false, code: "invalid-time", resolution, message: "This date and time could not be resolved." };
  return { valid: true, resolution, instantUtcMs: chosen.utcMs, offsetMinutes: chosen.offsetMinutes };
}

export function zonedNow(
  now: Date,
  minuteStep: MinuteStep,
  timeZone?: string,
): { value: DateTimeValue } | { error: "invalid-timezone" | "unsupported" } {
  const wall = currentWallClock(now, timeZone);
  if ("error" in wall) return wall;
  const parts = parseTimeValue(wall.time);
  if (!parts) return { error: "invalid-timezone" };
  return {
    value: {
      date: wall.date,
      time: formatTimeValue({ hour: parts.hour, minute: floorMinute(parts.minute, minuteStep) }),
    },
  };
}

export function zonePreviewSuffix(validation: DateTimeValidation, timeZone?: string): string {
  if (!timeZone || validation.offsetMinutes == null) return "";
  return ` (${timeZone}, ${formatOffsetMinutes(validation.offsetMinutes)})`;
}

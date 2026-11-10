/**
 * Local time-of-day values.
 *
 * The stable representation is `HH:mm` in 24-hour form (`00:00`–`23:59`).
 * It is a clock time, not a UTC timestamp and not an ISO instant.
 * Do not parse these strings with `Date.parse` or `new Date(value)`.
 */

export const MINUTE_STEPS = [1, 5, 10, 15, 30] as const;

export type MinuteStep = (typeof MINUTE_STEPS)[number];
export type HourCycle = "h12" | "h24";
export type DayPeriod = "AM" | "PM";

export type TimeParts = {
  /** 0–23 */
  hour: number;
  /** 0–59 */
  minute: number;
};

export type TimeConstraints = {
  minuteStep: MinuteStep;
  min?: string;
  max?: string;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
};

export type TimeValidationCode = "invalid" | "step" | "min" | "max" | "disabled" | "range" | "required";

export type TimeValidation = {
  valid: boolean;
  code?: TimeValidationCode;
  message?: string;
};

const TIME_VALUE = /^([01]\d|2[0-3]):([0-5]\d)$/;

export function isMinuteStep(value: number): value is MinuteStep {
  return (MINUTE_STEPS as readonly number[]).includes(value);
}

export function parseTimeValue(value: string | null | undefined): TimeParts | null {
  if (!value) return null;
  const match = TIME_VALUE.exec(value);
  if (!match) return null;
  return { hour: Number(match[1]), minute: Number(match[2]) };
}

export function formatTimeValue(parts: TimeParts): string {
  return `${String(parts.hour).padStart(2, "0")}:${String(parts.minute).padStart(2, "0")}`;
}

export function toDayPeriod(hour24: number): { hour12: number; period: DayPeriod } {
  if (!Number.isInteger(hour24) || hour24 < 0 || hour24 > 23) {
    throw new RangeError("hour24 must be an integer from 0 to 23");
  }
  return {
    hour12: hour24 % 12 === 0 ? 12 : hour24 % 12,
    period: hour24 >= 12 ? "PM" : "AM",
  };
}

/** 12 AM is 00:00. 12 PM is 12:00. */
export function fromDayPeriod(hour12: number, period: DayPeriod): number {
  if (!Number.isInteger(hour12) || hour12 < 1 || hour12 > 12) {
    throw new RangeError("hour12 must be an integer from 1 to 12");
  }
  if (period === "AM") return hour12 === 12 ? 0 : hour12;
  return hour12 === 12 ? 12 : hour12 + 12;
}

export function minuteAligned(minute: number, step: MinuteStep): boolean {
  return minute >= 0 && minute < 60 && minute % step === 0;
}

export function floorMinute(minute: number, step: MinuteStep): number {
  if (minute < 0) return 0;
  const floored = Math.floor(minute / step) * step;
  return Math.min(60 - step, floored);
}

export function minuteOptions(step: MinuteStep): number[] {
  const values: number[] = [];
  for (let minute = 0; minute < 60; minute += step) values.push(minute);
  return values;
}

export function compareTime(a: string, b: string): number {
  if (a === b) return 0;
  return a < b ? -1 : 1;
}

export function isAllowedTime(value: string, constraints: TimeConstraints): boolean {
  const parts = parseTimeValue(value);
  if (!parts || !minuteAligned(parts.minute, constraints.minuteStep)) return false;
  if (constraints.min && parseTimeValue(constraints.min) && compareTime(value, constraints.min) < 0) return false;
  if (constraints.max && parseTimeValue(constraints.max) && compareTime(value, constraints.max) > 0) return false;
  if (constraints.disabledTimes?.includes(value)) return false;
  if (constraints.isTimeDisabled?.(value)) return false;
  return true;
}

export function firstAllowedMinute(hour: number, constraints: TimeConstraints): number | null {
  for (const minute of minuteOptions(constraints.minuteStep)) {
    if (isAllowedTime(formatTimeValue({ hour, minute }), constraints)) return minute;
  }
  return null;
}

/**
 * Typed or pasted clock text.
 * AM/PM is explicit. Without a period, 24-hour text is accepted (`21:30`, `00:00`, `0930`).
 * A 1–12 hour without a period is accepted only in 24-hour mode, so 12-hour entry cannot guess AM/PM.
 */
export function parseTimeEntry(raw: string, hourCycle: HourCycle): TimeParts | null {
  const text = raw.trim().toLowerCase().replace(/\./g, "").replace(/\s+/g, " ");
  if (!text) return null;
  const periodMatch = /([ap])m?$/.exec(text);
  let period: DayPeriod | null = null;
  let clock = text;
  if (periodMatch?.[1] && periodMatch.index !== undefined) {
    const before = text[periodMatch.index - 1];
    if (periodMatch.index === 0 || before === " " || before === ":" || (before !== undefined && before >= "0" && before <= "9")) {
      period = periodMatch[1] === "a" ? "AM" : "PM";
      clock = text.slice(0, periodMatch.index).trim();
    }
  }
  const parsed = parseClock(clock);
  if (!parsed || parsed.minute > 59) return null;
  if (period) {
    if (parsed.hour < 1 || parsed.hour > 12) return null;
    return { hour: fromDayPeriod(parsed.hour, period), minute: parsed.minute };
  }
  if (parsed.hour > 23) return null;
  const explicit24 = parsed.hour === 0 || parsed.hour > 12 || /^\d{3,4}$/.test(clock);
  if (hourCycle === "h12" && !explicit24) return null;
  return parsed;
}

function parseClock(clock: string): TimeParts | null {
  if (/^\d{1,2}:\d{2}$/.test(clock)) {
    const [hour, minute] = clock.split(":");
    return { hour: Number(hour), minute: Number(minute) };
  }
  if (/^\d{3,4}$/.test(clock)) {
    const padded = clock.padStart(4, "0");
    return { hour: Number(padded.slice(0, 2)), minute: Number(padded.slice(2)) };
  }
  if (/^\d{1,2}$/.test(clock)) return { hour: Number(clock), minute: 0 };
  return null;
}

export function formatTimeDisplay(value: string, hourCycle: HourCycle, locale?: string): string | null {
  const parts = parseTimeValue(value);
  if (!parts) return null;
  const carrier = new Date(2000, 0, 1, parts.hour, parts.minute, 0, 0);
  return new Intl.DateTimeFormat(locale, {
    hour: "numeric",
    minute: "2-digit",
    hourCycle,
  }).format(carrier);
}

export function dayPeriodLabel(period: DayPeriod, locale?: string): string {
  const carrier = new Date(2000, 0, 1, period === "AM" ? 0 : 13, 0, 0, 0);
  const part = new Intl.DateTimeFormat(locale, { hour: "numeric", hourCycle: "h12" })
    .formatToParts(carrier)
    .find((item) => item.type === "dayPeriod");
  return part?.value ?? period;
}

export function formatHourLabel(hour: number, hourCycle: HourCycle, locale?: string): string {
  if (hourCycle === "h24") {
    return new Intl.NumberFormat(locale, { minimumIntegerDigits: 2, useGrouping: false }).format(hour);
  }
  return new Intl.NumberFormat(locale, { useGrouping: false }).format(hour);
}

export function formatMinuteLabel(minute: number, locale?: string): string {
  return new Intl.NumberFormat(locale, { minimumIntegerDigits: 2, useGrouping: false }).format(minute);
}

export function indexFromScroll(scrollTop: number, itemSize: number, count: number): number {
  if (count <= 0 || itemSize <= 0) return 0;
  const index = Math.round(scrollTop / itemSize);
  if (index < 0) return 0;
  if (index >= count) return count - 1;
  return index;
}

export function nearestEnabledIndex(start: number, disabled: readonly boolean[], direction: -1 | 0 | 1): number | null {
  if (start < 0 || start >= disabled.length) return null;
  if (!disabled[start]) return start;
  if (direction === 0) {
    for (let distance = 1; distance < disabled.length; distance += 1) {
      const lower = start - distance;
      const upper = start + distance;
      if (lower >= 0 && !disabled[lower]) return lower;
      if (upper < disabled.length && !disabled[upper]) return upper;
    }
    return null;
  }
  for (let index = start + direction; index >= 0 && index < disabled.length; index += direction) {
    if (!disabled[index]) return index;
  }
  return null;
}

export function validateTime(input: {
  value: string | null;
  minuteStep?: MinuteStep;
  min?: string;
  max?: string;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
  required?: boolean;
  locale?: string;
}): TimeValidation {
  if (input.value == null || input.value === "") {
    if (input.required) return { valid: false, code: "required", message: "Choose a time." };
    return { valid: true };
  }
  if (input.minuteStep !== undefined && !isMinuteStep(input.minuteStep)) {
    return { valid: false, code: "step", message: "Choose a minute step of 1, 5, 10, 15, or 30." };
  }
  const step = input.minuteStep ?? 1;
  const parts = parseTimeValue(input.value);
  if (!parts) return { valid: false, code: "invalid", message: "Enter a valid time, such as 9:30 AM or 21:30." };
  if (!minuteAligned(parts.minute, step)) {
    return { valid: false, code: "step", message: `Minutes must be in steps of ${step}.` };
  }
  if (input.min && !parseTimeValue(input.min)) {
    return { valid: false, code: "range", message: "The earliest time is not a valid HH:mm value." };
  }
  if (input.max && !parseTimeValue(input.max)) {
    return { valid: false, code: "range", message: "The latest time is not a valid HH:mm value." };
  }
  if (input.min && input.max && compareTime(input.min, input.max) > 0) {
    return { valid: false, code: "range", message: "The earliest time is after the latest time." };
  }
  const display = (value: string) => formatTimeDisplay(value, "h24", input.locale) ?? value;
  if (input.min && compareTime(input.value, input.min) < 0) {
    return { valid: false, code: "min", message: `Choose ${display(input.min)} or later.` };
  }
  if (input.max && compareTime(input.value, input.max) > 0) {
    return { valid: false, code: "max", message: `Choose ${display(input.max)} or earlier.` };
  }
  if (input.disabledTimes?.includes(input.value) || input.isTimeDisabled?.(input.value)) {
    return { valid: false, code: "disabled", message: `${display(input.value)} is unavailable.` };
  }
  return { valid: true };
}

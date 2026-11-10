/** Local calendar dates. Values are `YYYY-MM-DD` and never pass through UTC ISO strings. */

export type CalendarDate = string;

export type WeekStartsOn = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export type CalendarParts = {
  year: number;
  month: number;
  day: number;
};

export type CalendarView = {
  year: number;
  month: number;
};

export type CalendarCell = {
  date: CalendarDate;
  inMonth: boolean;
};

export type DateConstraints = {
  min?: CalendarDate;
  max?: CalendarDate;
  disabledDates?: readonly CalendarDate[];
  isDateDisabled?: (date: CalendarDate) => boolean;
};

export type DateRange = {
  start: CalendarDate;
  end: CalendarDate;
};

export const RANGE_PRESET_IDS = ["today", "yesterday", "last7", "last30", "thisWeek", "thisMonth", "lastMonth", "custom"] as const;

export type RangePresetId = (typeof RANGE_PRESET_IDS)[number];

export type CalculatedPresetId = Exclude<RangePresetId, "custom">;

export const RANGE_PRESET_LABELS: Record<RangePresetId, string> = {
  today: "Today",
  yesterday: "Yesterday",
  last7: "Last 7 Days",
  last30: "Last 30 Days",
  thisWeek: "This Week",
  thisMonth: "This Month",
  lastMonth: "Last Month",
  custom: "Custom",
};

export const CALENDAR_MIN_YEAR = 1;
export const CALENDAR_MAX_YEAR = 9999;

const DATE_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

export function isLeapYear(year: number) {
  return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
}

export function daysInMonth(year: number, month: number) {
  if (month === 2) return isLeapYear(year) ? 29 : 28;
  if (month === 4 || month === 6 || month === 9 || month === 11) return 30;
  return 31;
}

export function parseCalendarDate(value: string): CalendarParts | null {
  const match = DATE_PATTERN.exec(value);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (year < CALENDAR_MIN_YEAR || year > CALENDAR_MAX_YEAR) return null;
  if (month < 1 || month > 12) return null;
  if (day < 1 || day > daysInMonth(year, month)) return null;
  return { year, month, day };
}

export function formatCalendarDate(parts: CalendarParts): CalendarDate {
  const year = String(parts.year).padStart(4, "0");
  const month = String(parts.month).padStart(2, "0");
  const day = String(parts.day).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function calendarDateFromLocalDate(date: Date): CalendarDate {
  return formatCalendarDate({
    year: date.getFullYear(),
    month: date.getMonth() + 1,
    day: date.getDate(),
  });
}

export function todayCalendarDate(now: Date = new Date()) {
  return calendarDateFromLocalDate(now);
}

export function toLocalDate(value: CalendarDate) {
  const parts = parseCalendarDate(value);
  if (!parts) return null;
  return new Date(parts.year, parts.month - 1, parts.day);
}

export function compareCalendarDates(left: CalendarDate, right: CalendarDate) {
  if (left === right) return 0;
  return left < right ? -1 : 1;
}

function requireParts(value: CalendarDate) {
  const parts = parseCalendarDate(value);
  if (!parts) throw new Error(`Invalid calendar date: ${value}`);
  return parts;
}

export function addDays(value: CalendarDate, amount: number) {
  const parts = requireParts(value);
  const date = new Date(parts.year, parts.month - 1, parts.day);
  date.setDate(date.getDate() + amount);
  return calendarDateFromLocalDate(date);
}

export function shiftMonth(year: number, month: number, amount: number): CalendarView {
  const index = year * 12 + (month - 1) + amount;
  const nextYear = Math.floor(index / 12);
  const nextMonth = index - nextYear * 12 + 1;
  return { year: nextYear, month: nextMonth };
}

export function addMonths(value: CalendarDate, amount: number) {
  const parts = requireParts(value);
  const next = shiftMonth(parts.year, parts.month, amount);
  const day = Math.min(parts.day, daysInMonth(next.year, next.month));
  return formatCalendarDate({ year: next.year, month: next.month, day });
}

export function startOfMonth(value: CalendarDate) {
  const parts = requireParts(value);
  return formatCalendarDate({ year: parts.year, month: parts.month, day: 1 });
}

export function endOfMonth(value: CalendarDate) {
  const parts = requireParts(value);
  return formatCalendarDate({
    year: parts.year,
    month: parts.month,
    day: daysInMonth(parts.year, parts.month),
  });
}

export function weekdayIndex(value: CalendarDate): WeekStartsOn {
  const date = toLocalDate(value);
  if (!date) throw new Error(`Invalid calendar date: ${value}`);
  return date.getDay() as WeekStartsOn;
}

export function startOfWeek(value: CalendarDate, weekStartsOn: WeekStartsOn) {
  const delta = (weekdayIndex(value) - weekStartsOn + 7) % 7;
  return addDays(value, -delta);
}

export function endOfWeek(value: CalendarDate, weekStartsOn: WeekStartsOn) {
  return addDays(startOfWeek(value, weekStartsOn), 6);
}

export function viewOf(value: CalendarDate): CalendarView {
  const parts = parseCalendarDate(value) ?? parseCalendarDate(todayCalendarDate());
  return { year: parts?.year ?? 1970, month: parts?.month ?? 1 };
}

export function buildMonthGrid(year: number, month: number, weekStartsOn: WeekStartsOn): CalendarCell[] {
  const first = formatCalendarDate({ year, month, day: 1 });
  let cursor = startOfWeek(first, weekStartsOn);
  const cells: CalendarCell[] = [];
  for (let index = 0; index < 42; index += 1) {
    const parts = requireParts(cursor);
    cells.push({
      date: cursor,
      inMonth: parts.year === year && parts.month === month,
    });
    cursor = addDays(cursor, 1);
  }
  return cells;
}

export function monthLabel(year: number, month: number, locale: string) {
  return new Intl.DateTimeFormat(locale, { month: "long", year: "numeric" }).format(new Date(year, month - 1, 1));
}

export function monthNames(locale: string, width: "long" | "short" = "short") {
  return Array.from({ length: 12 }, (_, index) => new Intl.DateTimeFormat(locale, { month: width }).format(new Date(2024, index, 1)));
}

export function weekdayLabels(locale: string, weekStartsOn: WeekStartsOn, width: "narrow" | "short" | "long" = "narrow") {
  const labels: string[] = [];
  for (let index = 0; index < 7; index += 1) {
    const day = new Date(2024, 0, 7 + ((weekStartsOn + index) % 7));
    labels.push(new Intl.DateTimeFormat(locale, { weekday: width }).format(day));
  }
  return labels;
}

export function formatDisplayDate(value: CalendarDate, locale: string) {
  const parts = parseCalendarDate(value);
  if (!parts) return value;
  return new Intl.DateTimeFormat(locale, { month: "short", day: "numeric", year: "numeric" }).format(new Date(parts.year, parts.month - 1, parts.day));
}

export function dayAriaLabel(value: CalendarDate, locale: string, today: CalendarDate) {
  const display = formatDisplayDate(value, locale);
  return value === today ? `Today, ${display}` : display;
}

export function formatRangeLabel(range: DateRange, locale: string) {
  if (range.start === range.end) return formatDisplayDate(range.start, locale);
  return `${formatDisplayDate(range.start, locale)} – ${formatDisplayDate(range.end, locale)}`;
}

type WeekInfoLike = { firstDay?: number };

export function localeWeekStart(locale: string): WeekStartsOn {
  if (typeof Intl.Locale !== "function") return 0;
  try {
    const value = new Intl.Locale(locale) as Intl.Locale & {
      weekInfo?: WeekInfoLike;
      getWeekInfo?: () => WeekInfoLike;
    };
    const info = value.weekInfo ?? value.getWeekInfo?.();
    const first = info?.firstDay;
    if (first === 7) return 0;
    if (first === 1 || first === 2 || first === 3 || first === 4 || first === 5 || first === 6) return first;
  } catch {
    return 0;
  }
  return 0;
}

function usableBound(value: CalendarDate | undefined) {
  if (!value || !parseCalendarDate(value)) return undefined;
  return value;
}

export function isDateUnavailable(date: CalendarDate, constraints: DateConstraints = {}) {
  if (!parseCalendarDate(date)) return true;
  const min = usableBound(constraints.min);
  const max = usableBound(constraints.max);
  if (min && compareCalendarDates(date, min) < 0) return true;
  if (max && compareCalendarDates(date, max) > 0) return true;
  if (constraints.disabledDates?.includes(date)) return true;
  if (constraints.isDateDisabled?.(date)) return true;
  return false;
}

export function rangeIsValid(start: CalendarDate, end: CalendarDate, constraints: DateConstraints = {}) {
  if (!parseCalendarDate(start) || !parseCalendarDate(end)) return false;
  if (compareCalendarDates(start, end) > 0) return false;
  if (isDateUnavailable(start, constraints) || isDateUnavailable(end, constraints)) return false;
  if (constraints.disabledDates) {
    for (const date of constraints.disabledDates) {
      if (!parseCalendarDate(date)) continue;
      if (compareCalendarDates(date, start) >= 0 && compareCalendarDates(date, end) <= 0) return false;
    }
  }
  if (!constraints.isDateDisabled) return true;
  const last = toLocalDate(end);
  const cursor = toLocalDate(start);
  if (!last || !cursor) return false;
  cursor.setDate(cursor.getDate() + 1);
  while (cursor < last) {
    if (constraints.isDateDisabled(calendarDateFromLocalDate(cursor))) return false;
    cursor.setDate(cursor.getDate() + 1);
  }
  return true;
}

export function isInsideRange(date: CalendarDate, start: CalendarDate | null, end: CalendarDate | null) {
  if (!start || !end) return false;
  const [from, to] = compareCalendarDates(start, end) <= 0 ? [start, end] : [end, start];
  return compareCalendarDates(date, from) >= 0 && compareCalendarDates(date, to) <= 0;
}

export function orderRange(start: CalendarDate, end: CalendarDate): DateRange {
  if (compareCalendarDates(start, end) <= 0) return { start, end };
  return { start: end, end: start };
}

export function presetRange(id: CalculatedPresetId, today: CalendarDate, weekStartsOn: WeekStartsOn): DateRange {
  if (id === "today") return { start: today, end: today };
  if (id === "yesterday") {
    const day = addDays(today, -1);
    return { start: day, end: day };
  }
  if (id === "last7") return { start: addDays(today, -6), end: today };
  if (id === "last30") return { start: addDays(today, -29), end: today };
  if (id === "thisWeek") return { start: startOfWeek(today, weekStartsOn), end: endOfWeek(today, weekStartsOn) };
  if (id === "thisMonth") return { start: startOfMonth(today), end: endOfMonth(today) };
  const previous = addMonths(startOfMonth(today), -1);
  return { start: startOfMonth(previous), end: endOfMonth(previous) };
}

/** Clips a preset to min/max, then rejects it when the result is empty or contains a disabled date. */
export function resolvePreset(id: CalculatedPresetId, today: CalendarDate, weekStartsOn: WeekStartsOn, constraints: DateConstraints = {}): DateRange | null {
  const natural = presetRange(id, today, weekStartsOn);
  let start = natural.start;
  let end = natural.end;
  const min = usableBound(constraints.min);
  const max = usableBound(constraints.max);
  if (min && compareCalendarDates(start, min) < 0) start = min;
  if (max && compareCalendarDates(end, max) > 0) end = max;
  if (compareCalendarDates(start, end) > 0) return null;
  if (!rangeIsValid(start, end, constraints)) return null;
  return { start, end };
}

export function matchingPreset(range: DateRange, today: CalendarDate, weekStartsOn: WeekStartsOn, constraints: DateConstraints = {}): RangePresetId {
  const ids: CalculatedPresetId[] = ["today", "yesterday", "last7", "last30", "thisWeek", "thisMonth", "lastMonth"];
  for (const id of ids) {
    const resolved = resolvePreset(id, today, weekStartsOn, constraints);
    if (resolved && resolved.start === range.start && resolved.end === range.end) return id;
  }
  return "custom";
}

export function yearPage(anchorYear: number) {
  const rawStart = Math.floor(anchorYear / 12) * 12;
  const start = Math.min(Math.max(rawStart, CALENDAR_MIN_YEAR), CALENDAR_MAX_YEAR - 11);
  return Array.from({ length: 12 }, (_, index) => start + index);
}

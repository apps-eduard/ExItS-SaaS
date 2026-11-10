/**
 * Local calendar dates.
 *
 * The stable representation is `YYYY-MM-DD`. It is a calendar date, not a UTC
 * instant. Never parse it with `new Date("YYYY-MM-DD")` — that form is UTC and
 * can shift the day. Build a local carrier only when formatting for display:
 * `new Date(year, monthIndex, day)`.
 */

export type CalendarParts = {
  year: number;
  month: number;
  day: number;
};

const CALENDAR_DATE = /^(\d{4})-(\d{2})-(\d{2})$/;

export function isLeapYear(year: number): boolean {
  return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
}

export function daysInMonth(year: number, month: number): number {
  if (month < 1 || month > 12) return 0;
  if (month === 2) return isLeapYear(year) ? 29 : 28;
  if (month === 4 || month === 6 || month === 9 || month === 11) return 30;
  return 31;
}

export function parseCalendarDate(value: string | null | undefined): CalendarParts | null {
  if (!value) return null;
  const match = CALENDAR_DATE.exec(value);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (year < 1 || year > 9999) return null;
  if (month < 1 || month > 12) return null;
  if (day < 1 || day > daysInMonth(year, month)) return null;
  return { year, month, day };
}

export function formatCalendarDate(parts: CalendarParts): string {
  return `${String(parts.year).padStart(4, "0")}-${String(parts.month).padStart(2, "0")}-${String(parts.day).padStart(2, "0")}`;
}

export function compareCalendarDate(a: string, b: string): number {
  if (a === b) return 0;
  return a < b ? -1 : 1;
}

/** Today's date in the runtime's local calendar. */
export function todayCalendarDate(now = new Date()): string {
  return formatCalendarDate({
    year: now.getFullYear(),
    month: now.getMonth() + 1,
    day: now.getDate(),
  });
}

export function formatCalendarDisplay(value: string, locale?: string): string | null {
  const parts = parseCalendarDate(value);
  if (!parts) return null;
  const carrier = new Date(parts.year, parts.month - 1, parts.day);
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium" }).format(carrier);
}

export function calendarDateMessage(value: string): string {
  const match = CALENDAR_DATE.exec(value);
  if (!match) return "Enter a valid date.";
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (month === 2 && day === 29 && !isLeapYear(year)) return `${year} is not a leap year, so February 29 is not a date.`;
  if (month === 2 && day > 29) return "February does not have that day.";
  if (month >= 1 && month <= 12 && day > daysInMonth(year, month)) return "That day does not exist in this month.";
  return "Enter a valid date.";
}

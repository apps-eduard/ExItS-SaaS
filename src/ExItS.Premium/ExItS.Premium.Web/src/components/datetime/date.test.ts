import { describe, expect, it } from "vitest";
import { calendarDateMessage, daysInMonth, isLeapYear, parseCalendarDate, todayCalendarDate } from "./date";

describe("calendar dates", () => {
  it("keeps a date-only value on the same calendar day", () => {
    expect(parseCalendarDate("2024-03-10")).toEqual({ year: 2024, month: 3, day: 10 });
    expect(parseCalendarDate("2024-03-10T00:00:00.000Z")).toBeNull();
  });

  it("accepts leap-year February 29 and rejects the other years", () => {
    expect(isLeapYear(2024)).toBe(true);
    expect(isLeapYear(2000)).toBe(true);
    expect(isLeapYear(1900)).toBe(false);
    expect(isLeapYear(2023)).toBe(false);
    expect(daysInMonth(2024, 2)).toBe(29);
    expect(parseCalendarDate("2024-02-29")).toEqual({ year: 2024, month: 2, day: 29 });
    expect(parseCalendarDate("2023-02-29")).toBeNull();
    expect(parseCalendarDate("1900-02-29")).toBeNull();
    expect(calendarDateMessage("2023-02-29")).toMatch(/not a leap year/);
    expect(parseCalendarDate("2024-04-31")).toBeNull();
    expect(parseCalendarDate("2024-13-01")).toBeNull();
  });

  it("reads today from local calendar fields", () => {
    expect(todayCalendarDate(new Date(2024, 4, 2, 23, 30))).toBe("2024-05-02");
  });
});

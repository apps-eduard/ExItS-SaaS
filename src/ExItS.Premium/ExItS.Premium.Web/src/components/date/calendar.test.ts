import { describe, expect, it } from "vitest";
import {
  addDays,
  addMonths,
  buildMonthGrid,
  calendarDateFromLocalDate,
  compareCalendarDates,
  daysInMonth,
  formatCalendarDate,
  isLeapYear,
  localeWeekStart,
  matchingPreset,
  monthNames,
  parseCalendarDate,
  presetRange,
  rangeIsValid,
  resolvePreset,
  startOfWeek,
  weekdayLabels,
  yearPage,
} from "./calendar";

describe("calendar dates", () => {
  it("accepts leap days and rejects month lengths that do not exist", () => {
    expect(isLeapYear(2024)).toBe(true);
    expect(isLeapYear(2000)).toBe(true);
    expect(isLeapYear(1900)).toBe(false);
    expect(isLeapYear(2023)).toBe(false);
    expect(daysInMonth(2024, 2)).toBe(29);
    expect(daysInMonth(2023, 2)).toBe(28);
    expect(daysInMonth(2024, 4)).toBe(30);
    expect(daysInMonth(2024, 1)).toBe(31);
    expect(parseCalendarDate("2024-02-29")).toEqual({ year: 2024, month: 2, day: 29 });
    expect(parseCalendarDate("2023-02-29")).toBeNull();
    expect(parseCalendarDate("1900-02-29")).toBeNull();
    expect(parseCalendarDate("2000-02-29")).toEqual({ year: 2000, month: 2, day: 29 });
    expect(parseCalendarDate("2024-04-31")).toBeNull();
    expect(parseCalendarDate("2024-13-01")).toBeNull();
    expect(parseCalendarDate("2024-2-01")).toBeNull();
  });

  it("keeps the local calendar day instead of a UTC timestamp", () => {
    expect(calendarDateFromLocalDate(new Date(2026, 10, 10, 0, 5))).toBe("2026-11-10");
    expect(calendarDateFromLocalDate(new Date(2026, 10, 10, 23, 5))).toBe("2026-11-10");
    expect(formatCalendarDate({ year: 2026, month: 11, day: 10 })).toBe("2026-11-10");
    expect(addDays("2026-03-08", 1)).toBe("2026-03-09");
    expect(addDays("2026-11-01", 1)).toBe("2026-11-02");
    expect(addDays("2024-02-28", 1)).toBe("2024-02-29");
    expect(addDays("2024-02-29", 1)).toBe("2024-03-01");
    expect(addDays("2023-02-28", 1)).toBe("2023-03-01");
  });

  it("clamps month navigation onto real month lengths", () => {
    expect(addMonths("2024-01-31", 1)).toBe("2024-02-29");
    expect(addMonths("2023-01-31", 1)).toBe("2023-02-28");
    expect(addMonths("2024-03-31", -1)).toBe("2024-02-29");
    expect(addMonths("2024-01-15", 12)).toBe("2025-01-15");
    expect(compareCalendarDates("2024-02-28", "2024-02-29")).toBe(-1);
  });

  it("aligns the month grid to the configured first weekday", () => {
    const sunday = buildMonthGrid(2024, 6, 0);
    expect(sunday).toHaveLength(42);
    expect(sunday[0]?.date).toBe("2024-05-26");
    expect(sunday.filter((cell) => cell.inMonth)).toHaveLength(30);
    const monday = buildMonthGrid(2024, 3, 1);
    expect(monday[0]?.date).toBe("2024-02-26");
    expect(startOfWeek("2024-03-01", 1)).toBe("2024-02-26");
    expect(buildMonthGrid(2024, 2, 0).filter((cell) => cell.inMonth)).toHaveLength(29);
    expect(buildMonthGrid(1900, 2, 0).filter((cell) => cell.inMonth)).toHaveLength(28);
  });

  it("uses locale month and weekday names", () => {
    expect(monthNames("en-US", "long")[0]).toBe("January");
    expect(monthNames("fr-FR", "long")[0]?.toLocaleLowerCase("fr-FR")).toBe("janvier");
    expect(weekdayLabels("en-US", 1, "long")[0]).toBe("Monday");
    expect(weekdayLabels("en-US", 1, "long")[6]).toBe("Sunday");
    expect(weekdayLabels("en-US", 0, "long")[0]).toBe("Sunday");
    expect(localeWeekStart("en-US")).toBeGreaterThanOrEqual(0);
    expect(localeWeekStart("en-US")).toBeLessThanOrEqual(6);
  });

  it("pages years without dropping the anchor year", () => {
    const page = yearPage(2026);
    expect(page).toHaveLength(12);
    expect(page).toContain(2026);
    expect(yearPage(1)[0]).toBe(1);
    expect(yearPage(9999).at(-1)).toBe(9999);
  });
});

describe("date constraints and presets", () => {
  const today = "2024-06-15";

  it("rejects disabled dates and dates outside min and max", () => {
    const constraints = {
      min: "2024-06-10",
      max: "2024-06-20",
      disabledDates: ["2024-06-12"],
      isDateDisabled: (date: string) => date === "2024-06-18",
    };
    expect(rangeIsValid("2024-06-10", "2024-06-11", constraints)).toBe(true);
    expect(rangeIsValid("2024-06-11", "2024-06-13", constraints)).toBe(false);
    expect(rangeIsValid("2024-06-16", "2024-06-19", constraints)).toBe(false);
    expect(rangeIsValid("2024-06-09", "2024-06-11", constraints)).toBe(false);
    expect(rangeIsValid("2024-06-19", "2024-06-21", constraints)).toBe(false);
    expect(rangeIsValid("2024-06-16", "2024-06-10", constraints)).toBe(false);
  });

  it("calculates presets from local calendar boundaries", () => {
    expect(presetRange("today", today, 0)).toEqual({ start: today, end: today });
    expect(presetRange("yesterday", today, 0)).toEqual({ start: "2024-06-14", end: "2024-06-14" });
    expect(presetRange("last7", today, 0)).toEqual({ start: "2024-06-09", end: today });
    expect(presetRange("last30", today, 0)).toEqual({ start: "2024-05-17", end: today });
    expect(presetRange("thisWeek", today, 0)).toEqual({ start: "2024-06-09", end: "2024-06-15" });
    expect(presetRange("thisWeek", today, 1)).toEqual({ start: "2024-06-10", end: "2024-06-16" });
    expect(presetRange("thisMonth", today, 0)).toEqual({ start: "2024-06-01", end: "2024-06-30" });
    expect(presetRange("lastMonth", today, 0)).toEqual({ start: "2024-05-01", end: "2024-05-31" });
    expect(presetRange("last7", "2024-03-01", 0)).toEqual({ start: "2024-02-24", end: "2024-03-01" });
  });

  it("clips presets to constraints and rejects ranges that include disabled dates", () => {
    expect(resolvePreset("last7", today, 0, { min: "2024-06-12" })).toEqual({ start: "2024-06-12", end: today });
    expect(resolvePreset("last7", today, 0, { min: "2024-06-20" })).toBeNull();
    expect(resolvePreset("yesterday", today, 0, { min: today })).toBeNull();
    expect(resolvePreset("last7", today, 0, { disabledDates: ["2024-06-12"] })).toBeNull();
    expect(resolvePreset("thisMonth", today, 0, { isDateDisabled: (date) => date === "2024-06-02" })).toBeNull();
    expect(matchingPreset({ start: today, end: today }, today, 0)).toBe("today");
    expect(matchingPreset({ start: "2024-06-01", end: "2024-06-03" }, today, 0)).toBe("custom");
  });
});

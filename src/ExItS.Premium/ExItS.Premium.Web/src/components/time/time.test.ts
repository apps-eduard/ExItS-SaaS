import { describe, expect, it } from "vitest";
import {
  compareTime,
  formatTimeDisplay,
  formatTimeValue,
  fromDayPeriod,
  indexFromScroll,
  isAllowedTime,
  minuteOptions,
  nearestEnabledIndex,
  parseTimeEntry,
  parseTimeValue,
  toDayPeriod,
  validateTime,
} from "./time";

describe("time values", () => {
  it("keeps HH:mm independent of display format", () => {
    expect(parseTimeValue("00:00")).toEqual({ hour: 0, minute: 0 });
    expect(parseTimeValue("12:00")).toEqual({ hour: 12, minute: 0 });
    expect(parseTimeValue("23:59")).toEqual({ hour: 23, minute: 59 });
    expect(parseTimeValue("24:00")).toBeNull();
    expect(parseTimeValue("9:30")).toBeNull();
    expect(formatTimeValue({ hour: 0, minute: 5 })).toBe("00:05");
  });

  it("converts 12 AM and 12 PM without shifting the clock", () => {
    expect(fromDayPeriod(12, "AM")).toBe(0);
    expect(fromDayPeriod(12, "PM")).toBe(12);
    expect(fromDayPeriod(1, "AM")).toBe(1);
    expect(fromDayPeriod(1, "PM")).toBe(13);
    expect(toDayPeriod(0)).toEqual({ hour12: 12, period: "AM" });
    expect(toDayPeriod(12)).toEqual({ hour12: 12, period: "PM" });
    expect(toDayPeriod(15)).toEqual({ hour12: 3, period: "PM" });
    expect(parseTimeEntry("12:00 AM", "h24")).toEqual({ hour: 0, minute: 0 });
    expect(parseTimeEntry("12:00 PM", "h12")).toEqual({ hour: 12, minute: 0 });
    expect(parseTimeEntry("12am", "h12")).toEqual({ hour: 0, minute: 0 });
    expect(parseTimeEntry("12pm", "h12")).toEqual({ hour: 12, minute: 0 });
  });

  it("accepts 24-hour entry and refuses an ambiguous 12-hour entry", () => {
    expect(parseTimeEntry("21:30", "h24")).toEqual({ hour: 21, minute: 30 });
    expect(parseTimeEntry("0930", "h12")).toEqual({ hour: 9, minute: 30 });
    expect(parseTimeEntry("9:30", "h12")).toBeNull();
    expect(parseTimeEntry("9:30 PM", "h24")).toEqual({ hour: 21, minute: 30 });
    expect(formatTimeDisplay("00:00", "h12", "en-US")).toMatch(/12:00\sAM/);
    expect(formatTimeDisplay("12:00", "h12", "en-US")).toMatch(/12:00\sPM/);
    expect(formatTimeDisplay("15:05", "h24", "en-US")).toMatch(/15:05/);
  });

  it("builds minute steps and rejects misaligned times", () => {
    expect(minuteOptions(15)).toEqual([0, 15, 30, 45]);
    expect(validateTime({ value: "10:10", minuteStep: 15 }).code).toBe("step");
    expect(validateTime({ value: "10:15", minuteStep: 15 }).valid).toBe(true);
    expect(validateTime({ value: "10:07", minuteStep: 1 }).valid).toBe(true);
  });

  it("enforces min, max, and disabled times", () => {
    expect(validateTime({ value: "08:59", min: "09:00" }).code).toBe("min");
    expect(validateTime({ value: "17:01", max: "17:00" }).code).toBe("max");
    expect(validateTime({ value: "09:00", min: "09:00", max: "17:00" }).valid).toBe(true);
    expect(validateTime({ value: "10:00", disabledTimes: ["10:00"] }).code).toBe("disabled");
    expect(validateTime({ value: "10:15", isTimeDisabled: (time) => time === "10:15" }).code).toBe("disabled");
    expect(validateTime({ value: "12:00", min: "18:00", max: "09:00" }).code).toBe("range");
    expect(isAllowedTime("09:30", { minuteStep: 15, min: "09:00", max: "10:00", disabledTimes: ["09:30"] })).toBe(false);
    expect(compareTime("09:00", "09:00")).toBe(0);
  });

  it("rejects invalid typed times and empty required values", () => {
    expect(validateTime({ value: "25:00" }).code).toBe("invalid");
    expect(validateTime({ value: null, required: true }).code).toBe("required");
    expect(validateTime({ value: null }).valid).toBe(true);
  });

  it("maps wheel scroll offsets onto the selected index", () => {
    expect(indexFromScroll(0, 44, 12)).toBe(0);
    expect(indexFromScroll(44, 44, 12)).toBe(1);
    expect(indexFromScroll(90, 44, 12)).toBe(2);
    expect(indexFromScroll(999, 44, 4)).toBe(3);
    expect(nearestEnabledIndex(2, [false, false, true, false], 1)).toBe(3);
    expect(nearestEnabledIndex(1, [true, true, true], 0)).toBeNull();
    const minutes = minuteOptions(5);
    const index = indexFromScroll(6 * 44, 44, minutes.length);
    expect(minutes[index]).toBe(30);
  });
});

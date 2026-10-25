import { describe, expect, it } from "vitest";
import {
  addCalendarDays,
  availablePreferredPeriods,
  isDayClosed,
  isPreferredDateSelectable,
  PREFERRED_FULFILLMENT_MIN_USABLE_MINUTES,
} from "./preferred-fulfillment-periods";
import { formatPreferredFulfillmentLabel } from "./preferred-fulfillment-display";

const weekdayHours = [
  { dayOfWeek: "Thursday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Friday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "22:00" },
  { dayOfWeek: "Saturday", isClosed: false, isOpen24Hours: false, openTime: "18:00", closeTime: "22:00" },
  { dayOfWeek: "Sunday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
  { dayOfWeek: "Monday", isClosed: false, isOpen24Hours: false, openTime: "22:00", closeTime: "02:00" },
  { dayOfWeek: "Tuesday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Wednesday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
];

describe("preferred fulfillment periods", () => {
  it("exposes a documented minimum usable window", () => {
    expect(PREFERRED_FULFILLMENT_MIN_USABLE_MINUTES).toBe(30);
  });

  it("offers morning and afternoon when the store closes at 5 PM", () => {
    const now = new Date("2026-10-08T01:00:00.000Z"); // 09:00 Manila
    const periods = availablePreferredPeriods({
      date: "2026-10-08",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: weekdayHours,
    });
    expect(periods).toEqual(["Morning", "Afternoon"]);
  });

  it("offers evening only when the store opens at 6 PM", () => {
    const now = new Date("2026-10-08T01:00:00.000Z");
    const periods = availablePreferredPeriods({
      date: "2026-10-10",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: weekdayHours,
    });
    expect(periods).toEqual(["Evening"]);
  });

  it("hides morning after noon with preparation lead time", () => {
    const now = new Date("2026-10-08T04:00:00.000Z"); // 12:00 Manila
    const periods = availablePreferredPeriods({
      date: "2026-10-08",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: weekdayHours,
      minPreparationLeadMinutes: 30,
    });
    expect(periods).not.toContain("Morning");
    expect(periods).toContain("Afternoon");
  });

  it("disables closed days and past dates", () => {
    const now = new Date("2026-10-08T04:00:00.000Z");
    expect(isDayClosed("2026-10-11", weekdayHours)).toBe(true);
    expect(
      isPreferredDateSelectable({
        date: "2026-10-07",
        now,
        timeZoneId: "Asia/Manila",
        operatingHours: weekdayHours,
        maxAdvanceScheduleDays: 14,
      }),
    ).toBe(false);
    expect(
      isPreferredDateSelectable({
        date: "2026-10-30",
        now,
        timeZoneId: "Asia/Manila",
        operatingHours: weekdayHours,
        maxAdvanceScheduleDays: 7,
      }),
    ).toBe(false);
  });

  it("handles overnight open windows across midnight", () => {
    const now = new Date("2026-10-08T04:00:00.000Z");
    const mondayEvening = availablePreferredPeriods({
      date: "2026-10-12",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: weekdayHours,
    });
    const tuesdayMorning = availablePreferredPeriods({
      date: "2026-10-13",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: weekdayHours,
    });
    expect(mondayEvening).toEqual(["Evening"]);
    expect(tuesdayMorning).toContain("Morning");
  });

  it("formats today and tomorrow labels without inventing an exact time", () => {
    const now = new Date("2026-10-08T04:00:00.000Z");
    expect(
      formatPreferredFulfillmentLabel({
        date: "2026-10-08",
        period: "Afternoon",
        timeZoneId: "Asia/Manila",
        now,
        todayLabel: "Today",
        tomorrowLabel: "Tomorrow",
      }),
    ).toBe("Today — Afternoon");
    expect(
      formatPreferredFulfillmentLabel({
        date: addCalendarDays("2026-10-08", 1),
        period: "Morning",
        timeZoneId: "Asia/Manila",
        now,
        todayLabel: "Today",
        tomorrowLabel: "Tomorrow",
      }),
    ).toBe("Tomorrow — Morning");
  });
});

import { describe, expect, it } from "vitest";
import {
  formatRemainingDuration,
  resolveStoreOpenStatus,
} from "@/features/customer-ordering/store-open-status";
import type { PickupHoursDay } from "@/features/customer-ordering/pickup-time-bounds";

const week: PickupHoursDay[] = [
  { dayOfWeek: "Monday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Tuesday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Wednesday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Thursday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Friday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "22:00" },
  { dayOfWeek: "Saturday", isClosed: false, isOpen24Hours: false, openTime: "18:00", closeTime: "22:00" },
  { dayOfWeek: "Sunday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
];

const overnight: PickupHoursDay[] = [
  { dayOfWeek: "Monday", isClosed: false, isOpen24Hours: false, openTime: "22:00", closeTime: "02:00" },
  { dayOfWeek: "Tuesday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "17:00" },
  { dayOfWeek: "Wednesday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
  { dayOfWeek: "Thursday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
  { dayOfWeek: "Friday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
  { dayOfWeek: "Saturday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
  { dayOfWeek: "Sunday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
];

/** Fixed UTC instant that is the given wall clock in Asia/Manila (UTC+8). */
function manila(isoLocal: string): Date {
  return new Date(`${isoLocal}+08:00`);
}

describe("formatRemainingDuration", () => {
  it("formats minutes, hours, and days", () => {
    expect(formatRemainingDuration(0)).toBe("< 1m");
    expect(formatRemainingDuration(45)).toBe("45m");
    expect(formatRemainingDuration(120)).toBe("2h");
    expect(formatRemainingDuration(135)).toBe("2h 15m");
    expect(formatRemainingDuration(24 * 60 + 180)).toBe("1d 3h");
  });
});

describe("resolveStoreOpenStatus", () => {
  it("returns open with minutes until close", () => {
    // Thursday 2026-10-22 10:00 Manila
    const status = resolveStoreOpenStatus({
      now: manila("2026-10-22T10:00:00"),
      timeZoneId: "Asia/Manila",
      operatingHours: week,
    });
    expect(status).toEqual({ kind: "open", minutesUntilClose: 7 * 60, closeTime: "17:00" });
  });

  it("returns closed with minutes until open", () => {
    // Thursday 2026-10-22 06:00 Manila → opens 08:00
    const status = resolveStoreOpenStatus({
      now: manila("2026-10-22T06:00:00"),
      timeZoneId: "Asia/Manila",
      operatingHours: week,
    });
    expect(status).toEqual({ kind: "closed", minutesUntilOpen: 2 * 60 });
  });

  it("returns open24 when the day is 24 hours", () => {
    const hours = week.map((day) =>
      day.dayOfWeek === "Thursday"
        ? { ...day, isOpen24Hours: true, openTime: null, closeTime: null }
        : day,
    );
    const status = resolveStoreOpenStatus({
      now: manila("2026-10-22T15:30:00"),
      timeZoneId: "Asia/Manila",
      operatingHours: hours,
    });
    expect(status).toEqual({ kind: "open24" });
  });

  it("includes overnight continuation in minutes until close", () => {
    // Monday 2026-10-19 23:00 Manila → close Tuesday 02:00 = 3h
    const status = resolveStoreOpenStatus({
      now: manila("2026-10-19T23:00:00"),
      timeZoneId: "Asia/Manila",
      operatingHours: overnight,
    });
    expect(status).toEqual({ kind: "open", minutesUntilClose: 3 * 60, closeTime: "02:00" });
  });

  it("returns unknown without hours", () => {
    expect(
      resolveStoreOpenStatus({
        now: manila("2026-10-22T10:00:00"),
        timeZoneId: "Asia/Manila",
        operatingHours: [],
      }),
    ).toEqual({ kind: "unknown" });
  });
});

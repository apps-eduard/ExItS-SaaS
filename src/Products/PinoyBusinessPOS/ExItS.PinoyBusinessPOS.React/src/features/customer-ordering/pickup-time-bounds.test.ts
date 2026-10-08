import { describe, expect, it } from "vitest";
import { clampPickupTime, pickupTimeBounds } from "@/features/customer-ordering/pickup-time-bounds";

const hours = [
  { dayOfWeek: "Thursday", isClosed: false, isOpen24Hours: false, openTime: "08:00", closeTime: "18:00" },
  { dayOfWeek: "Sunday", isClosed: true, isOpen24Hours: false, openTime: null, closeTime: null },
];

describe("pickupTimeBounds", () => {
  const now = new Date("2026-10-08T06:15:30Z");

  it("keeps today's pickup at or after the current branch time and at or before closing", () => {
    const bounds = pickupTimeBounds({
      date: "2026-10-08",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: hours,
    });

    expect(bounds.closed).toBe(false);
    expect(bounds.min).toBe("14:16");
    expect(bounds.max).toBe("18:00");
    expect(clampPickupTime("10:00", bounds)).toBe("14:16");
    expect(clampPickupTime("19:30", bounds)).toBe("18:00");
    expect(clampPickupTime("15:00", bounds)).toBe("15:00");
  });

  it("does not apply the current time on a later date", () => {
    const bounds = pickupTimeBounds({
      date: "2026-10-09",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: hours,
    });

    expect(bounds.min).toBeNull();
    expect(bounds.max).toBeNull();
    expect(clampPickupTime("07:00", bounds)).toBe("07:00");
  });

  it("treats today as closed once the current time is past closing", () => {
    const bounds = pickupTimeBounds({
      date: "2026-10-08",
      now: new Date("2026-10-08T11:00:00Z"),
      timeZoneId: "Asia/Manila",
      operatingHours: hours,
    });

    expect(bounds.closed).toBe(true);
  });

  it("rejects a closed day", () => {
    const bounds = pickupTimeBounds({
      date: "2026-10-11",
      now,
      timeZoneId: "Asia/Manila",
      operatingHours: hours,
    });

    expect(bounds.closed).toBe(true);
    expect(clampPickupTime("15:00", bounds)).toBe("");
  });
});

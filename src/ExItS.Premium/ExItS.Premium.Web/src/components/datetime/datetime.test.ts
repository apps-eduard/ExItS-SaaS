import { describe, expect, it } from "vitest";
import { validateDateTime, zonedNow } from "./datetime";
import { namedTimeZoneSupported } from "./timezone";

describe("date and time validation", () => {
  it("combines a local date and a local time", () => {
    const result = validateDateTime({
      value: { date: "2024-05-02", time: "14:30" },
      min: { date: "2024-05-02", time: "09:00" },
      max: { date: "2024-05-03", time: "00:00" },
    });
    expect(result.valid).toBe(true);
    expect(result.instantUtcMs).toBeUndefined();
  });

  it("rejects invalid dates, invalid times, and crossed bounds", () => {
    expect(validateDateTime({ value: { date: "2023-02-29", time: "10:00" } }).code).toBe("invalid-date");
    expect(validateDateTime({ value: { date: "2024-05-02", time: "25:00" } }).code).toBe("invalid-time");
    expect(validateDateTime({ value: { date: "2024-05-02", time: "08:59" }, min: { date: "2024-05-02", time: "09:00" } }).code).toBe("min");
    expect(validateDateTime({ value: { date: "2024-05-01", time: "23:30" }, min: { date: "2024-05-02", time: "09:00" } }).code).toBe("min");
    expect(validateDateTime({ value: { date: "2024-05-04", time: "00:00" }, max: { date: "2024-05-03", time: "18:00" } }).code).toBe("max");
    expect(validateDateTime({ value: null, required: true }).code).toBe("required");
  });

  it("floors Now to the minute step in the requested zone", () => {
    const local = zonedNow(new Date(2024, 4, 2, 14, 7, 40), 5);
    expect(local).toEqual({ value: { date: "2024-05-02", time: "14:05" } });
    if (!namedTimeZoneSupported()) return;
    const zoned = zonedNow(new Date(Date.UTC(2024, 0, 15, 17, 7)), 15, "America/New_York");
    expect(zoned).toEqual({ value: { date: "2024-01-15", time: "12:00" } });
  });

  it("keeps nonexistent and ambiguous civil times unresolved unless a policy is set", () => {
    if (!namedTimeZoneSupported()) return;
    const gap = validateDateTime({
      value: { date: "2024-03-10", time: "02:30" },
      timeZone: "America/New_York",
    });
    expect(gap.code).toBe("nonexistent");
    expect(gap.instantUtcMs).toBeUndefined();

    const overlap = validateDateTime({
      value: { date: "2024-11-03", time: "01:30" },
      timeZone: "America/New_York",
    });
    expect(overlap.code).toBe("ambiguous");
    expect(overlap.instantUtcMs).toBeUndefined();

    const later = validateDateTime({
      value: { date: "2024-11-03", time: "01:30" },
      timeZone: "America/New_York",
      ambiguity: "later",
    });
    expect(later.valid).toBe(true);
    expect(later.instantUtcMs).toBe(Date.UTC(2024, 10, 3, 6, 30));
    expect(validateDateTime({ value: { date: "2024-05-02", time: "09:00" }, timeZone: "Not/AZone" }).code).toBe("invalid-timezone");
  });
});

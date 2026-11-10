import { describe, expect, it } from "vitest";
import { applyAmbiguity, currentWallClock, namedTimeZoneSupported, resolveZonedDateTime } from "./timezone";

const zoneReady = namedTimeZoneSupported();

describe("named time zones", () => {
  it("resolves a unique offset without changing the wall clock", () => {
    if (!zoneReady) return;
    const winter = resolveZonedDateTime("2024-01-15", "12:00", "America/New_York");
    expect(winter.status).toBe("unique");
    if (winter.status !== "unique") return;
    expect(winter.offsetMinutes).toBe(-300);
    expect(winter.utcMs).toBe(Date.UTC(2024, 0, 15, 17, 0));

    const summer = resolveZonedDateTime("2024-07-15", "12:00", "America/New_York");
    expect(summer.status).toBe("unique");
    if (summer.status !== "unique") return;
    expect(summer.offsetMinutes).toBe(-240);
    expect(summer.utcMs).toBe(Date.UTC(2024, 6, 15, 16, 0));

    const india = resolveZonedDateTime("2024-01-15", "12:00", "Asia/Kolkata");
    expect(india.status).toBe("unique");
    if (india.status !== "unique") return;
    expect(india.offsetMinutes).toBe(330);
    expect(india.utcMs).toBe(Date.UTC(2024, 0, 15, 6, 30));
  });

  it("does not normalize a daylight-saving gap", () => {
    if (!zoneReady) return;
    const gap = resolveZonedDateTime("2024-03-10", "02:30", "America/New_York");
    expect(gap).toEqual({ status: "nonexistent" });
    expect("utcMs" in gap).toBe(false);
  });

  it("reports both instants for an ambiguous fall-back time", () => {
    if (!zoneReady) return;
    const overlap = resolveZonedDateTime("2024-11-03", "01:30", "America/New_York");
    expect(overlap.status).toBe("ambiguous");
    if (overlap.status !== "ambiguous") return;
    expect(overlap.earlierUtcMs).toBe(Date.UTC(2024, 10, 3, 5, 30));
    expect(overlap.laterUtcMs).toBe(Date.UTC(2024, 10, 3, 6, 30));
    expect(overlap.earlierOffsetMinutes).toBe(-240);
    expect(overlap.laterOffsetMinutes).toBe(-300);
    expect(applyAmbiguity(overlap, "reject")).toEqual({ status: "ambiguous" });
    expect(applyAmbiguity(overlap, "earlier")).toMatchObject({ utcMs: overlap.earlierUtcMs });
    expect(applyAmbiguity(overlap, "later")).toMatchObject({ utcMs: overlap.laterUtcMs });
  });

  it("rejects an unknown zone and reads the current wall clock in a named zone", () => {
    if (!zoneReady) return;
    expect(resolveZonedDateTime("2024-01-15", "12:00", "Not/AZone").status).toBe("invalid-timezone");
    const wall = currentWallClock(new Date(Date.UTC(2024, 0, 15, 17, 0)), "America/New_York");
    expect(wall).toEqual({ date: "2024-01-15", time: "12:00" });
  });
});

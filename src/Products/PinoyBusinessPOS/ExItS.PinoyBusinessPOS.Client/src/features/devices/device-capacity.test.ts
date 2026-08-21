import { describe, expect, it } from "vitest";
import { formatPosDeviceCapacity } from "@/features/devices/device-capacity";

describe("formatPosDeviceCapacity", () => {
  it("returns null when capacity is missing", () => {
    expect(formatPosDeviceCapacity(null)).toBeNull();
    expect(formatPosDeviceCapacity(undefined)).toBeNull();
  });

  it("formats finite capacity with available slots", () => {
    expect(formatPosDeviceCapacity({ used: 3, allowed: 5 })).toEqual({
      kind: "finite",
      used: 3,
      allowed: 5,
      available: 2,
      atLimit: false,
      progressRatio: 0.6,
    });
  });

  it("marks finite capacity at limit", () => {
    expect(formatPosDeviceCapacity({ used: 5, allowed: 5 })).toEqual({
      kind: "finite",
      used: 5,
      allowed: 5,
      available: 0,
      atLimit: true,
      progressRatio: 1,
    });
  });

  it("treats allowed >= 10000 as unlimited", () => {
    expect(formatPosDeviceCapacity({ used: 3, allowed: 10000 })).toEqual({
      kind: "unlimited",
      used: 3,
      progressRatio: null,
    });
  });
});

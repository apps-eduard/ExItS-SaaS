import { describe, expect, it } from "vitest";
import {
  PERSONAL_HOME_KPI_DEFAULT_PRESET,
  resolvePersonalHomeKpiDatePreset,
} from "@/features/personal/personal-home-kpi-date-range";

describe("personal-home-kpi-date-range", () => {
  it("defaults to last 3 months", () => {
    expect(PERSONAL_HOME_KPI_DEFAULT_PRESET).toBe("last3Months");
    const range = resolvePersonalHomeKpiDatePreset("last3Months", new Date("2026-03-15T12:00:00.000Z"));
    expect(range).toEqual({ fromDate: "2026-01-01", toDate: "2026-03-15" });
  });

  it("accepts valid custom from/to", () => {
    const range = resolvePersonalHomeKpiDatePreset(
      "custom",
      new Date("2026-03-15T12:00:00.000Z"),
      { fromDate: "2026-02-01", toDate: "2026-02-28" },
    );
    expect(range).toEqual({ fromDate: "2026-02-01", toDate: "2026-02-28" });
  });

  it("falls back to last 3 months when custom range is invalid", () => {
    const range = resolvePersonalHomeKpiDatePreset(
      "custom",
      new Date("2026-03-15T12:00:00.000Z"),
      { fromDate: "2026-03-10", toDate: "2026-03-01" },
    );
    expect(range).toEqual({ fromDate: "2026-01-01", toDate: "2026-03-15" });
  });
});
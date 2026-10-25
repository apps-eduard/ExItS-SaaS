import { describe, expect, it } from "vitest";
import {
  localDateTimeToOrgEhgDisplay,
  orgEhgDisplayToLocalDateTime,
} from "@/components/exits/ExitsDateGlassDateTimeField";

describe("ExitsDateGlassDateTimeField value bridge", () => {
  it("round-trips datetime-local through Org EHG display", () => {
    expect(localDateTimeToOrgEhgDisplay("2025-02-20T10:30")).toBe("02/20/2025  10:30 AM");
    expect(localDateTimeToOrgEhgDisplay("2025-02-20T22:05")).toBe("02/20/2025  10:05 PM");
    expect(localDateTimeToOrgEhgDisplay("")).toBe("");
    expect(orgEhgDisplayToLocalDateTime("02/20/2025  10:30 AM")).toBe("2025-02-20T10:30");
    expect(orgEhgDisplayToLocalDateTime("02/20/2025  10:05 PM")).toBe("2025-02-20T22:05");
    expect(orgEhgDisplayToLocalDateTime("")).toBe("");
  });
});

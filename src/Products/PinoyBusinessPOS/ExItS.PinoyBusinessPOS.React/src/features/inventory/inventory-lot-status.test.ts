import { describe, expect, it } from "vitest";
import {
  addLocalDays,
  calendarDaysBetween,
  daysUntilExpiration,
  formatLocalDateOnly,
  parseBusinessDate,
  requiresOpeningExpirationDate,
  resolveLotExpiryLabel,
} from "@/features/inventory/inventory-lot-status";

describe("inventory-lot-status", () => {
  it("parses yyyy-MM-dd business dates", () => {
    const parsed = parseBusinessDate("2026-08-21");
    expect(parsed?.getUTCFullYear()).toBe(2026);
    expect(parsed?.getUTCMonth()).toBe(7);
    expect(parsed?.getUTCDate()).toBe(21);
  });

  it("counts calendar days between dates", () => {
    const a = new Date(Date.UTC(2026, 7, 21));
    const b = new Date(Date.UTC(2026, 7, 28));
    expect(calendarDaysBetween(a, b)).toBe(7);
    expect(calendarDaysBetween(b, a)).toBe(-7);
  });

  it("resolves expired / today / near / ok labels", () => {
    const today = new Date(Date.UTC(2026, 7, 21));
    expect(resolveLotExpiryLabel("Expired", "2026-08-01", today)).toEqual({ kind: "expired" });
    expect(resolveLotExpiryLabel("ExpiresToday", "2026-08-21", today)).toEqual({
      kind: "expiresToday",
    });
    expect(resolveLotExpiryLabel("NearExpiry", "2026-08-28", today)).toEqual({
      kind: "expiresInDays",
      days: 7,
    });
    expect(resolveLotExpiryLabel("Ok", "2026-09-30", today)).toEqual({ kind: "ok" });
    expect(resolveLotExpiryLabel("Weird", "2026-09-30", today)).toEqual({
      kind: "unknown",
      status: "Weird",
    });
  });

  it("computes days until expiration", () => {
    const today = new Date(Date.UTC(2026, 7, 21));
    expect(daysUntilExpiration("2026-08-21", today)).toBe(0);
    expect(daysUntilExpiration("2026-08-24", today)).toBe(3);
  });

  it("requires opening expiration only when tracking and qty > 0", () => {
    expect(requiresOpeningExpirationDate(false, 10)).toBe(false);
    expect(requiresOpeningExpirationDate(true, 0)).toBe(false);
    expect(requiresOpeningExpirationDate(true, null)).toBe(false);
    expect(requiresOpeningExpirationDate(true, 5)).toBe(true);
  });

  it("formats and shifts local date-only values", () => {
    expect(formatLocalDateOnly(new Date(2026, 7, 24))).toBe("2026-08-24");
    expect(addLocalDays("2026-08-24", 30)).toBe("2026-09-23");
  });
});

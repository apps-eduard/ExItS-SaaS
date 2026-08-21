import { describe, expect, it } from "vitest";
import {
  classifyCashVariance,
  resolveClosingCashCounted,
  resolveHistoricalClosingMode,
  resolveHistoricalOpeningMode,
} from "@/features/shifts/shift-cash-history";

describe("shift-cash-history helpers", () => {
  it("uses effectiveOpeningCashCountMode when present", () => {
    expect(
      resolveHistoricalOpeningMode({
        effectiveOpeningCashCountMode: "Optional",
        effectiveCashCountMode: "Required",
      }),
    ).toBe("Optional");
  });

  it("falls back to legacy effectiveCashCountMode for opening", () => {
    expect(
      resolveHistoricalOpeningMode({
        effectiveOpeningCashCountMode: null,
        effectiveCashCountMode: "Required",
      }),
    ).toBe("Required");
  });

  it("uses effectiveClosingCashCountMode when present", () => {
    expect(
      resolveHistoricalClosingMode({
        effectiveClosingCashCountMode: "Optional",
        effectiveCashCountMode: "Required",
      }),
    ).toBe("Optional");
  });

  it("falls back to legacy effectiveCashCountMode for closing", () => {
    expect(
      resolveHistoricalClosingMode({
        effectiveClosingCashCountMode: "  ",
        effectiveCashCountMode: "Optional",
      }),
    ).toBe("Optional");
  });

  it("prefers closingCashCountState Counted", () => {
    expect(
      resolveClosingCashCounted({
        closingCashCountState: "Counted",
        closingCashAmount: 0,
      }),
    ).toBe(true);
  });

  it("prefers closingCashCountState NotPerformed as skipped", () => {
    expect(
      resolveClosingCashCounted({
        closingCashCountState: "NotPerformed",
        closingCashAmount: null,
      }),
    ).toBe(false);
  });

  it("treats null closing amount as not counted when state absent", () => {
    expect(
      resolveClosingCashCounted({
        closingCashCountState: null,
        closingCashAmount: null,
      }),
    ).toBe(false);
  });

  it("treats non-null closing amount as counted when state absent", () => {
    expect(
      resolveClosingCashCounted({
        closingCashCountState: null,
        closingCashAmount: 0,
      }),
    ).toBe(true);
  });

  it("classifies variance balanced / over / short", () => {
    expect(classifyCashVariance(0)).toBe("balanced");
    expect(classifyCashVariance(25)).toBe("over");
    expect(classifyCashVariance(-10)).toBe("short");
  });
});

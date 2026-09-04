import { describe, expect, it } from "vitest";
import {
  resolveCashCountRequired,
  resolveClosingCashCountMode,
  resolveOpeningCashCountMode,
  resolveOpeningCashVisible,
} from "@/api/pos/pos-operational-setup-client";

describe("cash count policy helpers", () => {
  it("defaults missing modes to required (both policies on)", () => {
    expect(resolveCashCountRequired(undefined)).toBe(true);
    expect(resolveCashCountRequired("")).toBe(true);
    expect(resolveCashCountRequired("Optional")).toBe(false);
    expect(resolveCashCountRequired("Required")).toBe(true);
    expect(resolveOpeningCashCountMode(undefined)).toBe("Required");
    expect(resolveClosingCashCountMode(undefined)).toBe("Required");
  });

  it("prefers opening/closing fields over legacy cashCountMode", () => {
    const setup = {
      organizationId: "o",
      storeDisplayName: "Store",
      currencyCode: "PHP",
      taxPricingMode: "TaxExclusive",
      taxRatePercent: 0,
      cashCountMode: "Required",
      openingCashCountMode: "Optional",
      closingCashCountMode: "Required",
      isComplete: true,
      createdAtUtc: "2026-01-01T00:00:00Z",
      createdBy: "a",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      updatedBy: "a",
    };
    expect(resolveOpeningCashCountMode(setup)).toBe("Optional");
    expect(resolveClosingCashCountMode(setup)).toBe("Required");
    expect(resolveOpeningCashVisible(resolveOpeningCashCountMode(setup))).toBe(true);
  });

  it("hides opening cash only for Off", () => {
    expect(resolveOpeningCashVisible("Off")).toBe(false);
    expect(resolveOpeningCashVisible("Optional")).toBe(true);
    expect(resolveOpeningCashVisible("Required")).toBe(true);
  });
});

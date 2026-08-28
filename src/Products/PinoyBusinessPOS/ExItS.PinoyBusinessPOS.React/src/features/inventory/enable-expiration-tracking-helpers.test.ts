import { describe, expect, it } from "vitest";
import {
  canSubmitExpirationAllocation,
  createExpirationLotDraft,
  isExpiryDateInPast,
  quantitiesMatchOnHand,
  remainingToAllocate,
  sumAllocatedQuantity,
  toExistingStockLotInputs,
} from "@/features/inventory/enable-expiration-tracking-helpers";

describe("enable-expiration-tracking-helpers", () => {
  it("sums allocated quantities and remaining", () => {
    expect(sumAllocatedQuantity([{ quantity: 10 }, { quantity: 5.5 }])).toBe(15.5);
    expect(remainingToAllocate(40, 15.5)).toBe(24.5);
  });

  it("matches on-hand with floating tolerance", () => {
    expect(quantitiesMatchOnHand(40, 40)).toBe(true);
    expect(quantitiesMatchOnHand(10.1 + 9.9, 20)).toBe(true);
    expect(quantitiesMatchOnHand(39, 40)).toBe(false);
  });

  it("allows submit only when allocated equals on-hand and rows are complete", () => {
    const exact = [
      { ...createExpirationLotDraft("25", "a"), expiryDate: "2027-01-01" },
      { ...createExpirationLotDraft("15", "b"), expiryDate: "2027-06-01" },
    ];
    expect(canSubmitExpirationAllocation(40, exact)).toBe(true);

    const under = [{ ...createExpirationLotDraft("30", "a"), expiryDate: "2027-01-01" }];
    expect(canSubmitExpirationAllocation(40, under)).toBe(false);

    const missingExpiry = [createExpirationLotDraft("40", "a")];
    expect(canSubmitExpirationAllocation(40, missingExpiry)).toBe(false);

    const zeroQty = [{ ...createExpirationLotDraft("0", "a"), expiryDate: "2027-01-01" }];
    expect(canSubmitExpirationAllocation(0, zeroQty)).toBe(false);

    expect(canSubmitExpirationAllocation(0, [])).toBe(true);
  });

  it("maps drafts to API lot inputs", () => {
    const drafts = [
      {
        ...createExpirationLotDraft("12", "a"),
        expiryDate: "2027-03-01",
        lotNumber: "LOT-1",
      },
      {
        ...createExpirationLotDraft("8", "b"),
        expiryDate: "2027-04-01",
        lotNumber: "  ",
      },
    ];
    expect(toExistingStockLotInputs(drafts)).toEqual([
      { quantity: 12, expiryDate: "2027-03-01", lotNumber: "LOT-1" },
      { quantity: 8, expiryDate: "2027-04-01", lotNumber: null },
    ]);
  });

  it("detects past expiry dates", () => {
    expect(isExpiryDateInPast("2020-01-01", "2026-08-28")).toBe(true);
    expect(isExpiryDateInPast("2026-08-28", "2026-08-28")).toBe(false);
    expect(isExpiryDateInPast("2027-01-01", "2026-08-28")).toBe(false);
    expect(isExpiryDateInPast("", "2026-08-28")).toBe(false);
  });
});

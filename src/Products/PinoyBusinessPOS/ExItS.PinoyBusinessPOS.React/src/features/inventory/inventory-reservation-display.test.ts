import { describe, expect, it } from "vitest";
import {
  formatInventoryQty,
  resolveAvailableQuantity,
  resolvePendingReturnQuantity,
  resolveReservedQuantity,
} from "@/features/inventory/inventory-reservation-display";

describe("inventory-reservation-display", () => {
  it("shows available as onHand - reserved (3 - 2 = 1)", () => {
    expect(
      resolveAvailableQuantity({
        isTracked: true,
        onHandQuantity: 3,
        reservedQuantity: 2,
        availableQuantity: 1,
      }),
    ).toBe(1);
    expect(resolveReservedQuantity({ reservedQuantity: 2 })).toBe(2);
  });

  it("prefers authoritative availableQuantity from API", () => {
    expect(
      resolveAvailableQuantity({
        isTracked: true,
        onHandQuantity: 3,
        reservedQuantity: 2,
        availableQuantity: 1,
      }),
    ).toBe(1);
  });

  it("hides reserved when zero", () => {
    expect(resolveReservedQuantity({ reservedQuantity: 0 })).toBe(0);
    expect(resolveReservedQuantity({})).toBe(0);
  });

  it("defaults pending-return quantity to zero", () => {
    expect(resolvePendingReturnQuantity({ pendingReturnQuantity: 0 })).toBe(0);
    expect(resolvePendingReturnQuantity({ pendingReturnQuantity: -2 })).toBe(0);
    expect(resolvePendingReturnQuantity({ pendingReturnQuantity: 1.25 })).toBe(1.25);
    expect(resolvePendingReturnQuantity({})).toBe(0);
  });

  it("excludes pending return from available when API availableQuantity is absent", () => {
    expect(
      resolveAvailableQuantity({
        isTracked: true,
        onHandQuantity: 10,
        reservedQuantity: 2,
        pendingReturnQuantity: 3,
      }),
    ).toBe(5);
  });

  it("excludes damaged and inspection hold from sellable fallback", () => {
    expect(
      resolveAvailableQuantity({
        isTracked: true,
        onHandQuantity: 20,
        reservedQuantity: 0,
        damagedQuantity: 10,
        inspectionHoldQuantity: 0,
      }),
    ).toBe(10);
  });

  it("formats weighted quantities with precision", () => {
    expect(formatInventoryQty(1.25)).toBe("1.25");
    expect(formatInventoryQty(2)).toBe("2");
  });

  it("adds thousand separators for inventory card quantities", () => {
    expect(formatInventoryQty(1000)).toBe("1,000");
    expect(formatInventoryQty(12500)).toBe("12,500");
    expect(formatInventoryQty(1000.5)).toBe("1,000.5");
  });
});

import { describe, expect, it } from "vitest";
import {
  formatInventoryQty,
  resolveAvailableQuantity,
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

  it("formats weighted quantities with precision", () => {
    expect(formatInventoryQty(1.25)).toBe("1.25");
    expect(formatInventoryQty(2)).toBe("2");
  });
});

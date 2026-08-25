import { describe, expect, it } from "vitest";
import {
  clampReturnQuantity,
  formatReturnQuantityDisplay,
  maxReturnQuantityDecimals,
  requiresWholeReturnQuantity,
} from "@/features/returns/return-quantity";

describe("return-quantity", () => {
  it("uses whole steps for PerItem piece/pack", () => {
    expect(requiresWholeReturnQuantity("Piece", "PerItem")).toBe(true);
    expect(requiresWholeReturnQuantity("Pack", "PerItem")).toBe(true);
    expect(maxReturnQuantityDecimals("Piece", "PerItem")).toBe(0);
    expect(clampReturnQuantity(2.7, 5, 0)).toBe(2);
  });

  it("allows decimals for ByWeight", () => {
    expect(requiresWholeReturnQuantity("Kilogram", "ByWeight")).toBe(false);
    expect(maxReturnQuantityDecimals("Kilogram", "ByWeight")).toBe(3);
    expect(clampReturnQuantity(0.75, 1.25, 3)).toBe(0.75);
    expect(formatReturnQuantityDisplay(0.75, "Kilogram", "ByWeight")).toContain("kg");
  });

  it("does not silently raise quantity above refundable max", () => {
    expect(clampReturnQuantity(9, 5, 0)).toBe(5);
    expect(clampReturnQuantity(-1, 5, 0)).toBe(0);
  });
});

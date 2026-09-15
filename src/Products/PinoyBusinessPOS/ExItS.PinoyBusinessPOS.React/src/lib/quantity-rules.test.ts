import { describe, expect, it } from "vitest";
import {
  formatQuantityValue,
  isValidQuantity,
  maxQuantityDecimals,
  minPositiveQuantity,
  parseQuantityTyping,
  quantityStepForPrecision,
  requiresWholeQuantity,
  stepQuantity,
} from "@/lib/quantity-rules";

describe("quantity-rules", () => {
  it("mirrors SaleMoney whole vs measured precision", () => {
    expect(maxQuantityDecimals("Piece", "PerItem")).toBe(0);
    expect(maxQuantityDecimals("Pack", "PerItem")).toBe(0);
    expect(maxQuantityDecimals("Kilogram", "PerItem")).toBe(3);
    expect(maxQuantityDecimals("Piece", "ByWeight")).toBe(3);
    expect(requiresWholeQuantity("Bottle", "PerItem")).toBe(true);
    expect(quantityStepForPrecision(0)).toBe(1);
    expect(quantityStepForPrecision(3)).toBe(0.001);
    expect(minPositiveQuantity(3)).toBe(0.001);
  });

  it("validates and formats quantities", () => {
    expect(isValidQuantity(1, "Piece", "PerItem")).toBe(true);
    expect(isValidQuantity(1.5, "Piece", "PerItem")).toBe(false);
    expect(isValidQuantity(0.5, "Kilogram", "ByWeight")).toBe(true);
    expect(isValidQuantity(1.2345, "Kilogram", "ByWeight")).toBe(false);
    expect(formatQuantityValue(1.25, 3)).toBe("1.25");
    expect(formatQuantityValue(2, 0)).toBe("2");
  });

  it("parses typing and steps with min protection", () => {
    expect(parseQuantityTyping("", 3)).toEqual({ kind: "empty" });
    expect(parseQuantityTyping("1.", 3)).toEqual({ kind: "incomplete" });
    expect(parseQuantityTyping("1.25", 3)).toEqual({ kind: "value", value: 1.25 });
    expect(parseQuantityTyping("1.2345", 3)).toEqual({ kind: "invalid" });
    expect(parseQuantityTyping("1.5", 0)).toEqual({ kind: "invalid" });
    expect(
      stepQuantity({ value: 1, direction: -1, step: 1, precision: 0, min: 1 }),
    ).toBe(1);
    expect(
      stepQuantity({ value: 0.5, direction: 1, step: 0.001, precision: 3, min: 0.001 }),
    ).toBe(0.501);
    expect(
      stepQuantity({ value: 1.004, direction: 1, step: 1, precision: 3, min: 0.001 }),
    ).toBe(2);
    expect(
      stepQuantity({ value: 1.004, direction: -1, step: 1, precision: 3, min: 0.001 }),
    ).toBe(1);
    expect(formatQuantityValue(2, 3)).toBe("2");
  });
});

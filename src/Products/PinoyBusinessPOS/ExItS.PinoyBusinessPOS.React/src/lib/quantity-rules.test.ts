import { describe, expect, it } from "vitest";
import {
  formatQuantityValue,
  isValidQuantity,
  maxQuantityDecimals,
  MEASURED_QUANTITY_DECIMALS,
  minPositiveQuantity,
  parseQuantityTyping,
  quantityInputMinimum,
  quantityStepForPrecision,
  requiresWholeQuantity,
  stepQuantity,
} from "@/lib/quantity-rules";

describe("quantity-rules", () => {
  it("mirrors SaleMoney whole vs measured precision", () => {
    expect(MEASURED_QUANTITY_DECIMALS).toBe(2);
    expect(maxQuantityDecimals("Piece", "PerItem")).toBe(0);
    expect(maxQuantityDecimals("Pack", "PerItem")).toBe(0);
    expect(maxQuantityDecimals("Kilogram", "PerItem")).toBe(2);
    expect(maxQuantityDecimals("Piece", "ByWeight")).toBe(2);
    expect(requiresWholeQuantity("Bottle", "PerItem")).toBe(true);
    expect(quantityStepForPrecision(0)).toBe(1);
    expect(quantityStepForPrecision(2)).toBe(0.01);
    expect(minPositiveQuantity(2)).toBe(0.01);
    expect(quantityInputMinimum("Piece", "PerItem")).toBe(1);
    expect(quantityInputMinimum("Kilogram", "PerItem")).toBe(0.01);
    expect(quantityInputMinimum("Pack", "PerItem")).toBe(1);
  });

  it("validates and formats quantities", () => {
    expect(isValidQuantity(1, "Piece", "PerItem")).toBe(true);
    expect(isValidQuantity(1.5, "Piece", "PerItem")).toBe(false);
    expect(isValidQuantity(0.5, "Kilogram", "ByWeight")).toBe(true);
    expect(isValidQuantity(1.25, "Kilogram", "ByWeight")).toBe(true);
    expect(isValidQuantity(1.255, "Kilogram", "ByWeight")).toBe(false);
    expect(isValidQuantity(2.999, "Kilogram", "ByWeight")).toBe(false);
    expect(formatQuantityValue(1.25, 2)).toBe("1.25");
    expect(formatQuantityValue(2, 0)).toBe("2");
    expect(formatQuantityValue(1000, 0)).toBe("1,000");
    expect(formatQuantityValue(1234.5, 2)).toBe("1,234.5");
  });

  it("parses typing and steps with remainder preserved", () => {
    expect(parseQuantityTyping("", 2)).toEqual({ kind: "empty" });
    expect(parseQuantityTyping("1.", 2)).toEqual({ kind: "incomplete" });
    expect(parseQuantityTyping("1.2", 2)).toEqual({ kind: "value", value: 1.2 });
    expect(parseQuantityTyping("1.25", 2)).toEqual({ kind: "value", value: 1.25 });
    expect(parseQuantityTyping("0.75", 2)).toEqual({ kind: "value", value: 0.75 });
    expect(parseQuantityTyping("1.255", 2)).toEqual({ kind: "invalid" });
    expect(parseQuantityTyping("2.999", 2)).toEqual({ kind: "invalid" });
    expect(parseQuantityTyping("1.5", 0)).toEqual({ kind: "invalid" });
    expect(parseQuantityTyping("1,000", 0)).toEqual({ kind: "value", value: 1000 });
    expect(parseQuantityTyping("1,234.5", 2)).toEqual({ kind: "value", value: 1234.5 });
    expect(
      stepQuantity({ value: 1, direction: -1, step: 1, precision: 0, min: 1 }),
    ).toBe(1);
    expect(
      stepQuantity({ value: 1, direction: 1, step: 1, precision: 2, min: 0.01 }),
    ).toBe(2);
    expect(
      stepQuantity({ value: 1.5, direction: 1, step: 1, precision: 2, min: 0.01 }),
    ).toBe(2.5);
    expect(
      stepQuantity({ value: 2.5, direction: -1, step: 1, precision: 2, min: 0.01 }),
    ).toBe(1.5);
    expect(
      stepQuantity({ value: 0.5, direction: 1, step: 0.01, precision: 2, min: 0.01 }),
    ).toBe(0.51);
    expect(formatQuantityValue(2, 2)).toBe("2");
    expect(isValidQuantity(0.75, "Kilogram", "PerItem")).toBe(true);
    expect(isValidQuantity(1.5, "Pack", "PerItem")).toBe(false);
  });
});

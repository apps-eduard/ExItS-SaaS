import { describe, expect, it } from "vitest";
import {
  canAddStockUseQuantity,
  convertDisplayBetweenWeightUnits,
  displayQtyFromCanonical,
  isStockUseWeightProduct,
  parseStockUseEntryQuantity,
  remainingAfterEntry,
} from "@/features/inventory/stock-use-quantity";

describe("stock-use-quantity", () => {
  it("detects weight products by UOM and selling mode", () => {
    expect(isStockUseWeightProduct({ unitOfMeasure: "Kilogram" })).toBe(true);
    expect(isStockUseWeightProduct({ unitOfMeasure: "kg" })).toBe(true);
    expect(isStockUseWeightProduct({ unitOfMeasure: "Piece" })).toBe(false);
    expect(isStockUseWeightProduct({ unitOfMeasure: "Piece", sellingMode: "ByWeight" })).toBe(
      true,
    );
  });

  it("rejects blank, zero, and invalid picker quantities", () => {
    expect(
      parseStockUseEntryQuantity({ raw: "", isWeight: false, weightUnit: "kg" }),
    ).toEqual({ ok: false, reason: "blank" });
    expect(
      parseStockUseEntryQuantity({ raw: "0", isWeight: false, weightUnit: "kg" }),
    ).toEqual({ ok: false, reason: "zero" });
    expect(
      parseStockUseEntryQuantity({ raw: "-1", isWeight: false, weightUnit: "kg" }),
    ).toEqual({ ok: false, reason: "zero" });
    expect(
      parseStockUseEntryQuantity({ raw: "abc", isWeight: false, weightUnit: "kg" }),
    ).toEqual({ ok: false, reason: "invalid" });
  });

  it("converts grams to kilograms for validation and storage", () => {
    expect(
      parseStockUseEntryQuantity({ raw: "250", isWeight: true, weightUnit: "g" }),
    ).toEqual({ ok: true, quantity: 0.25 });
    expect(
      parseStockUseEntryQuantity({ raw: "0.25", isWeight: true, weightUnit: "kg" }),
    ).toEqual({ ok: true, quantity: 0.25 });
  });

  it("enables add only when quantity is positive and within available stock", () => {
    expect(
      canAddStockUseQuantity({
        raw: "",
        isWeight: false,
        weightUnit: "kg",
        available: 5,
      }),
    ).toBe(false);
    expect(
      canAddStockUseQuantity({
        raw: "2",
        isWeight: false,
        weightUnit: "kg",
        available: 5,
      }),
    ).toBe(true);
    expect(
      canAddStockUseQuantity({
        raw: "6",
        isWeight: false,
        weightUnit: "kg",
        available: 5,
      }),
    ).toBe(false);
    expect(
      canAddStockUseQuantity({
        raw: "250",
        isWeight: true,
        weightUnit: "g",
        available: 0.2,
      }),
    ).toBe(false);
    expect(
      canAddStockUseQuantity({
        raw: "250",
        isWeight: true,
        weightUnit: "g",
        available: 0.25,
      }),
    ).toBe(true);
  });

  it("formats and converts display quantities between kg and g", () => {
    expect(
      displayQtyFromCanonical({ quantity: 0.25, isWeight: true, weightUnit: "g" }),
    ).toBe("250");
    expect(
      displayQtyFromCanonical({ quantity: 0.25, isWeight: true, weightUnit: "kg" }),
    ).toBe("0.25");
    expect(convertDisplayBetweenWeightUnits("0.25", "kg", "g")).toBe("250");
    expect(convertDisplayBetweenWeightUnits("250", "g", "kg")).toBe("0.25");
    expect(convertDisplayBetweenWeightUnits("", "kg", "g")).toBe("");
  });

  it("computes remaining stock after the entered quantity", () => {
    expect(
      remainingAfterEntry({
        available: 24,
        raw: "5",
        isWeight: false,
        weightUnit: "kg",
      }),
    ).toBe(19);
    expect(
      remainingAfterEntry({
        available: 0.5,
        raw: "250",
        isWeight: true,
        weightUnit: "g",
      }),
    ).toBe(0.25);
    expect(
      remainingAfterEntry({
        available: 24,
        raw: "",
        isWeight: false,
        weightUnit: "kg",
      }),
    ).toBe(24);
  });
});

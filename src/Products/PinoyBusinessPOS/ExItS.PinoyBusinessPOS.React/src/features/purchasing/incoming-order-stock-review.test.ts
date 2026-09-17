import { describe, expect, it } from "vitest";
import {
  countShortageLines,
  defaultConfirmQty,
  formatStockQtyLabel,
  hasMaterialProposalChanges,
  lineHasShortage,
} from "@/features/purchasing/incoming-order-stock-review";

describe("incoming-order-stock-review", () => {
  it("detects shortage when requested exceeds available stock", () => {
    expect(
      lineHasShortage({
        productId: "a",
        qty: 4,
        availableToPromise: 3,
        unitOfMeasureCode: "Kilogram",
      }),
    ).toBe(true);
    expect(
      lineHasShortage({
        productId: "b",
        qty: 2,
        availableToPromise: 5,
        unitOfMeasureCode: "Piece",
      }),
    ).toBe(false);
  });

  it("defaults confirm qty to available stock on shortage and requested when available", () => {
    expect(
      defaultConfirmQty({
        productId: "a",
        qty: 4,
        availableToPromise: 3,
        shortageWarning: true,
        unitOfMeasureCode: "Kilogram",
      }),
    ).toBe(3);
    expect(
      defaultConfirmQty({
        productId: "b",
        qty: 2,
        availableToPromise: 5,
        unitOfMeasureCode: "Piece",
      }),
    ).toBe(2);
  });

  it("preserves measured quantity precision on shortage default", () => {
    expect(
      defaultConfirmQty({
        productId: "a",
        qty: 4.25,
        availableToPromise: 3.5,
        shortageWarning: true,
        unitOfMeasureCode: "Kilogram",
      }),
    ).toBe(3.5);
  });

  it("counts shortage lines and material proposal changes", () => {
    const lines = [
      {
        productId: "short",
        qty: 4,
        availableToPromise: 3,
        unitOfMeasureCode: "Kilogram",
      },
      {
        productId: "ok",
        qty: 2,
        availableToPromise: 5,
        unitOfMeasureCode: "Piece",
      },
    ];
    expect(countShortageLines(lines)).toBe(1);
    expect(hasMaterialProposalChanges(lines, { short: 3, ok: 2 })).toBe(true);
    expect(hasMaterialProposalChanges(lines, { short: 4, ok: 2 })).toBe(false);
  });

  it("formats stock qty labels with UOM", () => {
    expect(formatStockQtyLabel(4, "Kilogram")).toMatch(/4.*Kg/i);
    expect(formatStockQtyLabel(3, "Kilogram")).toMatch(/3.*Kg/i);
  });
});

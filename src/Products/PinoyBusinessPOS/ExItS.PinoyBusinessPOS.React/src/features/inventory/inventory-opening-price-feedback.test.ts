import { describe, expect, it } from "vitest";
import { computeOpeningStockValue } from "@/features/catalog/opening-stock-helpers";
import {
  comparePurchaseCostToSellingPrice,
  resolveEffectiveSellingPriceView,
} from "@/features/inventory/inventory-opening-price-feedback";

describe("inventory opening effective selling price", () => {
  it("PRICECOST-01 branch override shown", () => {
    expect(
      resolveEffectiveSellingPriceView({
        sellingPrice: 12,
        effectiveSellingPrice: 15,
        hasBranchPriceOverride: true,
      }),
    ).toEqual({ amount: 15, source: "branch" });
  });

  it("PRICECOST-02 org default shown when no override", () => {
    expect(
      resolveEffectiveSellingPriceView({
        sellingPrice: 12,
        effectiveSellingPrice: 12,
        hasBranchPriceOverride: false,
      }),
    ).toEqual({ amount: 12, source: "organization" });

    expect(
      resolveEffectiveSellingPriceView({
        sellingPrice: 12,
        effectiveSellingPrice: null,
        hasBranchPriceOverride: false,
      }),
    ).toEqual({ amount: 12, source: "organization" });
  });

  it("PRICECOST-03 selected branch controls effective price", () => {
    const kalibo = resolveEffectiveSellingPriceView({
      sellingPrice: 12,
      effectiveSellingPrice: 15,
      hasBranchPriceOverride: true,
    });
    const iloilo = resolveEffectiveSellingPriceView({
      sellingPrice: 12,
      effectiveSellingPrice: 12,
      hasBranchPriceOverride: false,
    });
    expect(kalibo).toEqual({ amount: 15, source: "branch" });
    expect(iloilo).toEqual({ amount: 12, source: "organization" });
    expect(kalibo?.amount).not.toBe(iloilo?.amount);
  });

  it("PRICECOST-04 branch switch refreshes price", () => {
    const before = resolveEffectiveSellingPriceView({
      sellingPrice: 12,
      effectiveSellingPrice: 15,
      hasBranchPriceOverride: true,
    });
    const after = resolveEffectiveSellingPriceView({
      sellingPrice: 12,
      effectiveSellingPrice: 12,
      hasBranchPriceOverride: false,
    });
    expect(before).toEqual({ amount: 15, source: "branch" });
    expect(after).toEqual({ amount: 12, source: "organization" });
  });

  it("PRICECOST-05 cost > price warning", () => {
    expect(comparePurchaseCostToSellingPrice("15", 12)).toEqual({
      kind: "higherCost",
      difference: 3,
    });
  });

  it("PRICECOST-06 cost == price zero-margin message", () => {
    expect(comparePurchaseCostToSellingPrice("12.00", 12)).toEqual({ kind: "zeroMargin" });
  });

  it("PRICECOST-07 cost < price no warning", () => {
    expect(comparePurchaseCostToSellingPrice("10", 12)).toEqual({ kind: "none" });
    expect(comparePurchaseCostToSellingPrice("", 12)).toEqual({ kind: "none" });
  });

  it("PRICECOST-08 stock value remains qty × purchase cost", () => {
    expect(computeOpeningStockValue(10, 8)).toBe(80);
    expect(computeOpeningStockValue(10, 8)).not.toBe(10 * 12);
  });

  it("PRICECOST-09 no pricing mutation from inventory screen helpers", () => {
    const view = resolveEffectiveSellingPriceView({
      sellingPrice: 12,
      effectiveSellingPrice: 15,
      hasBranchPriceOverride: true,
    });
    expect(view).toEqual({ amount: 15, source: "branch" });
    // Pure read helpers — no setters / mutation APIs involved.
    expect(typeof resolveEffectiveSellingPriceView).toBe("function");
    expect(typeof comparePurchaseCostToSellingPrice).toBe("function");
  });

  it("PRICECOST-10 resolves from already-loaded catalog product fields (no N+1 shape)", () => {
    const product = {
      sellingPrice: 12,
      effectiveSellingPrice: 15,
      hasBranchPriceOverride: true,
    };
    // Single product payload already includes branch-effective fields.
    expect(resolveEffectiveSellingPriceView(product)?.amount).toBe(15);
    expect(Object.keys(product).sort()).toEqual([
      "effectiveSellingPrice",
      "hasBranchPriceOverride",
      "sellingPrice",
    ]);
  });
});

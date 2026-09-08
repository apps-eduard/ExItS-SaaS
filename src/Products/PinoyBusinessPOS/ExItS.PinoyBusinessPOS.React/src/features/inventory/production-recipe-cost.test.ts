import { describe, expect, it } from "vitest";
import {
  buildRecipeMaterialCostEstimate,
  estimatedMaterialMargin,
  estimatedUnitMaterialCost,
  toBaseQuantityForCost,
} from "@/features/inventory/production-recipe-cost";

describe("production-recipe-cost", () => {
  it("sums known acquisition costs into batch and unit estimates", () => {
    const estimate = buildRecipeMaterialCostEstimate([
      { materialProductId: "a", name: "Apple", baseQuantity: 3, unitCost: 60 },
      { materialProductId: "b", name: "Flour", baseQuantity: 10, unitCost: 45 },
      { materialProductId: "c", name: "Sugar", baseQuantity: 2, unitCost: null },
    ]);
    expect(estimate.batchCost).toBe(630);
    expect(estimate.knownLineCount).toBe(2);
    expect(estimate.missingLineCount).toBe(1);
    expect(estimatedUnitMaterialCost(estimate.batchCost, 100)).toBe(6.3);
  });

  it("computes estimated margin from selling price", () => {
    const margin = estimatedMaterialMargin(25, 11.9);
    expect(margin?.amount).toBe(13.1);
    expect(margin?.percent).toBe(52.4);
  });

  it("applies multiplier to base for costing", () => {
    expect(toBaseQuantityForCost(500, 0.001)).toBe(0.5);
    expect(toBaseQuantityForCost(10, null)).toBe(10);
  });
});

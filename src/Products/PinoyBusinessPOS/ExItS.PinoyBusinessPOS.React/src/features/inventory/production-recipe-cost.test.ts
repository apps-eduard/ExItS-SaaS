import { describe, expect, it, vi } from "vitest";
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
    const atSuggested = estimatedMaterialMargin(195, 135);
    expect(atSuggested?.amount).toBe(60);
    expect(atSuggested?.percent).toBe(30.8);
  });

  it("applies multiplier to base for costing", () => {
    expect(toBaseQuantityForCost(500, 0.001)).toBe(0.5);
    expect(toBaseQuantityForCost(10, null)).toBe(10);
  });
});

describe("resolveLatestAcquisitionUnitCost soft-fail", () => {
  it("returns null when movements request fails", async () => {
    const { resolveLatestAcquisitionUnitCost } = await import(
      "@/features/inventory/production-recipe-cost"
    );
    const inventoryClient = await import("@/api/pos/pos-inventory-client");
    vi.spyOn(inventoryClient, "listInventoryMovements").mockRejectedValue(
      new Error("Inventory account was not found."),
    );
    const cost = await resolveLatestAcquisitionUnitCost(
      { organizationId: "org", branchId: "br" },
      "product-1",
    );
    expect(cost).toBeNull();
  });
});

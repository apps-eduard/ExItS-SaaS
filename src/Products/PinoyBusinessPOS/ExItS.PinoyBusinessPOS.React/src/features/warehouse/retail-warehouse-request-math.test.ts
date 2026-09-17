import { describe, expect, it } from "vitest";
import {
  estimateLineCost,
  estimateLineGross,
  estimateLineRetail,
  estimateRequestLine,
  summarizeRequestBasket,
} from "@/features/warehouse/retail-warehouse-request-math";

describe("retail-warehouse-request-math", () => {
  it("estimates weighted line cost and retail", () => {
    expect(estimateLineCost(1.5, 80)).toBe(120);
    expect(estimateLineRetail(1.5, 120)).toBe(180);
    expect(estimateLineGross(120, 180)).toBe(60);

    const line = estimateRequestLine({
      quantity: 2.25,
      warehouseUnitCost: 40,
      branchEffectiveSellingPrice: 55,
    });
    expect(line.estimatedCost).toBe(90);
    expect(line.potentialRetail).toBe(123.75);
    expect(line.potentialGross).toBe(33.75);
  });

  it("estimates per-item line amounts", () => {
    const line = estimateRequestLine({
      quantity: 10,
      warehouseUnitCost: 12.5,
      branchEffectiveSellingPrice: 18,
    });
    expect(line.estimatedCost).toBe(125);
    expect(line.potentialRetail).toBe(180);
    expect(line.potentialGross).toBe(55);
  });

  it("returns null when cost or price is unknown", () => {
    expect(estimateLineCost(5, null)).toBeNull();
    expect(estimateLineRetail(5, undefined)).toBeNull();
    expect(estimateLineGross(null, 100)).toBeNull();
    expect(estimateLineGross(50, null)).toBeNull();
  });

  it("summarizes footer by product count without summing mixed physical qty", () => {
    const totals = summarizeRequestBasket([
      { quantity: 10, warehouseUnitCost: 5, branchEffectiveSellingPrice: 8 }, // pcs
      { quantity: 2.5, warehouseUnitCost: 40, branchEffectiveSellingPrice: 60 }, // kg
      { quantity: 4, warehouseUnitCost: null, branchEffectiveSellingPrice: 15 }, // missing cost
    ]);

    expect(totals.productCount).toBe(3);
    // Must not invent "16.50 units" — only money + product count
    expect(totals.estimatedCostTotal).toBe(150); // 50 + 100
    expect(totals.potentialRetailTotal).toBe(290); // 80 + 150 + 60
    // Gross only when every line has both sides known
    expect(totals.potentialGross).toBeNull();
  });

  it("computes potential gross when all lines have cost and retail", () => {
    const totals = summarizeRequestBasket([
      { quantity: 2, warehouseUnitCost: 10, branchEffectiveSellingPrice: 15 },
      { quantity: 1.5, warehouseUnitCost: 20, branchEffectiveSellingPrice: 30 },
    ]);
    expect(totals.productCount).toBe(2);
    expect(totals.estimatedCostTotal).toBe(50);
    expect(totals.potentialRetailTotal).toBe(75);
    expect(totals.potentialGross).toBe(25);
  });

  it("ignores zero-qty lines in product count and totals", () => {
    const totals = summarizeRequestBasket([
      { quantity: 0, warehouseUnitCost: 10, branchEffectiveSellingPrice: 20 },
      { quantity: 3, warehouseUnitCost: 10, branchEffectiveSellingPrice: 20 },
    ]);
    expect(totals.productCount).toBe(1);
    expect(totals.estimatedCostTotal).toBe(30);
    expect(totals.potentialRetailTotal).toBe(60);
    expect(totals.potentialGross).toBe(30);
  });
});

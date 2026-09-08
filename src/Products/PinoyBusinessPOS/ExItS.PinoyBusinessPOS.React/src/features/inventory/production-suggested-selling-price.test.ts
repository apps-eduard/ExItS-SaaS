import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { estimatedMaterialMargin } from "@/features/inventory/production-recipe-cost";
import {
  DEFAULT_TARGET_GROSS_MARGIN,
  formatGrossMarginPercent,
  isLowGrossMargin,
  isSellingBelowCost,
  resolveMaterialCostBasis,
  roundCommercialPhpSellingPrice,
  suggestSellingPriceFromUnitCost,
} from "@/features/inventory/production-suggested-selling-price";

const here = dirname(fileURLToPath(import.meta.url));

describe("POS-PRODUCTION-SUGGESTED-SELLING-PRICE-V1", () => {
  it("uses gross-margin formula not markup (cost 135 @ 30%)", () => {
    const suggestion = suggestSellingPriceFromUnitCost(135, 0.3);
    expect(suggestion).not.toBeNull();
    // 135 / 0.7 ≈ 192.857 → 192.86
    expect(suggestion!.raw).toBe(192.86);
    expect(suggestion!.rounded).toBe(195);
    // Markup would be 135 * 1.3 = 175.5 — must not use that.
    expect(suggestion!.rounded).not.toBe(175.5);
    expect(suggestion!.rounded).toBeGreaterThanOrEqual(suggestion!.raw);
  });

  it("recalculates for 20% and 40% targets", () => {
    const at20 = suggestSellingPriceFromUnitCost(135, 0.2);
    expect(at20!.raw).toBe(168.75);
    expect(at20!.rounded).toBe(170);

    const at40 = suggestSellingPriceFromUnitCost(135, 0.4);
    expect(at40!.raw).toBe(225);
    expect(at40!.rounded).toBe(225);
  });

  it("defaults target margin to 30%", () => {
    expect(DEFAULT_TARGET_GROSS_MARGIN).toBe(0.3);
    const suggestion = suggestSellingPriceFromUnitCost(135);
    expect(suggestion!.targetMargin).toBe(0.3);
  });

  it("applies commercial PHP upward rounding steps", () => {
    expect(roundCommercialPhpSellingPrice(12.63)).toBe(13);
    expect(roundCommercialPhpSellingPrice(47.12)).toBe(50);
    expect(roundCommercialPhpSellingPrice(168.75)).toBe(170);
    expect(roundCommercialPhpSellingPrice(192.86)).toBe(195);
    expect(roundCommercialPhpSellingPrice(225)).toBe(225);
    expect(roundCommercialPhpSellingPrice(501.1)).toBe(510);
  });

  it("computes gross profit and actual margin from selling price", () => {
    const margin = estimatedMaterialMargin(195, 135);
    expect(margin?.amount).toBe(60);
    expect(margin?.percent).toBe(30.8);
    expect(formatGrossMarginPercent(margin!.percent)).toBe("30.8");
  });

  it("detects below-cost and low-margin warnings", () => {
    expect(isSellingBelowCost(25, 135)).toBe(true);
    expect(isSellingBelowCost(195, 135)).toBe(false);
    expect(isLowGrossMargin(140, 135)).toBe(true); // ~3.6%
    expect(isLowGrossMargin(195, 135)).toBe(false);
  });

  it("withholds confident suggestion for partial or unavailable cost", () => {
    expect(
      resolveMaterialCostBasis({
        unitCost: null,
        knownLineCount: 0,
        missingLineCount: 2,
      }),
    ).toMatchObject({ complete: false, reason: "unavailable" });

    expect(
      resolveMaterialCostBasis({
        unitCost: 135,
        knownLineCount: 2,
        missingLineCount: 1,
      }),
    ).toMatchObject({ complete: false, reason: "partial" });

    expect(
      resolveMaterialCostBasis({
        unitCost: 135,
        knownLineCount: 3,
        missingLineCount: 0,
      }),
    ).toMatchObject({ complete: true, unitCost: 135 });
  });

  it("output sheet wires suggestion UX without storing price on ProductionDefinition", () => {
    const sheet = readFileSync(resolve(here, "ProductionRecipeOutputSheet.tsx"), "utf8");
    expect(sheet).toContain("suggestSellingPriceFromUnitCost");
    expect(sheet).toContain("userEditedSellingPrice");
    expect(sheet).toContain('data-testid="production-recipe-use-suggested-price"');
    expect(sheet).toContain('data-testid="production-recipe-target-margin"');
    expect(sheet).toContain('data-testid="production-recipe-below-cost-warning"');
    expect(sheet).toContain('data-testid="production-recipe-low-margin-warning"');
    expect(sheet).toContain('data-testid="production-recipe-pricing-section"');
    expect(sheet).toContain("laborOverheadNotIncluded");
    expect(sheet).toContain("sellingPrice: price");
    expect(sheet).not.toContain("productionDefinition.sellingPrice");
    // Can be sold = no hides pricing section
    expect(sheet).toMatch(/canBeSold \? \([\s\S]*production-recipe-pricing-section/);
  });
});

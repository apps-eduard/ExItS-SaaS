import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { isEligibleProductionMaterial } from "@/features/inventory/production-material-uom";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";

const here = dirname(fileURLToPath(import.meta.url));

function product(partial: Partial<PosCatalogProductDto>): PosCatalogProductDto {
  return {
    productId: partial.productId ?? "00000000-0000-0000-0000-000000000001",
    organizationId: "00000000-0000-0000-0000-000000000099",
    name: partial.name ?? "Item",
    unitOfMeasure: partial.unitOfMeasure ?? "Piece",
    sellingMode: partial.sellingMode ?? "PerItem",
    sellingPrice: 0,
    status: "Active",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...partial,
  };
}

describe("POS-PRODUCTION-NEW-ORG-INGREDIENT-ELIGIBILITY-SIDE-EFFECT-FIX-V1", () => {
  const form = readFileSync(resolve(here, "ProductionDefinitionFormPage.tsx"), "utf8");

  it("never mutates catalog/inventory from recipe ingredient selection", () => {
    expect(form).not.toContain("ensureCanBeUsedAsIngredient");
    expect(form).not.toContain("browseCatalogIngredients");
    expect(form).not.toContain("enableInventoryTracking");
    expect(form).not.toContain("updateCatalogProduct");
    expect(form).not.toContain("openingQuantity: 0");
    expect(form).not.toMatch(/updateCatalogProduct\([\s\S]*canBeUsedAsIngredient:\s*true/);
  });

  it("picker uses strict ingredient filter and empty-state CTA", () => {
    expect(form).toContain("canBeUsedAsIngredient: true");
    expect(form).toContain("isEligibleProductionMaterial");
    expect(form).toContain("production.setups.noProductionIngredientsYet");
    expect(form).toContain('data-testid="production-setup-no-eligible-ingredients"');
    expect(form).toContain('data-testid="production-setup-manage-products"');
    expect(form).toContain('navigate("/catalog")');
    expect(form).not.toContain("production-setup-browse-catalog-ingredients");
    expect(form).not.toContain("browseCatalogToEnable");
  });

  it("distinguishes untracked vs zero-stock tracked eligibility", () => {
    expect(
      isEligibleProductionMaterial(
        product({
          name: "Flour",
          canBeUsedAsIngredient: true,
          isTracked: true,
          branchAvailableQuantity: 0,
        }),
      ),
    ).toBe(true);

    expect(
      isEligibleProductionMaterial(
        product({
          name: "Tomato",
          canBeUsedAsIngredient: true,
          isTracked: false,
        }),
      ),
    ).toBe(false);

    expect(
      isEligibleProductionMaterial(
        product({
          name: "Battery",
          canBeUsedAsIngredient: false,
          isTracked: false,
        }),
      ),
    ).toBe(false);
  });

  it("preserves recipe save / output create and produce stock separation", () => {
    expect(form).toContain("ProductionRecipeOutputSheet");
    expect(form).toContain('data-testid="production-setup-save"');
    const produce = readFileSync(resolve(here, "ProductionRunCreatePage.tsx"), "utf8");
    expect(produce).toContain("produceBlocked");
    expect(produce).toContain("canBeUsedAsIngredient: true");
  });
});

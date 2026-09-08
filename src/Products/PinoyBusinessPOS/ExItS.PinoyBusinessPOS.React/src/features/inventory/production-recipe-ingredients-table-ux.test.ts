import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  draftQuantityParts,
  formatMaterialAvailableCell,
} from "@/features/inventory/production-material-uom";

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

describe("POS-PRODUCTION-RECIPE-INGREDIENTS-TABLE-UX-V1", () => {
  const source = readFileSync(resolve(here, "ProductionDefinitionFormPage.tsx"), "utf8");

  it("desktop renders selected ingredients as a compact semantic table", () => {
    expect(source).toContain('data-testid="production-setup-materials-table"');
    expect(source).toContain("<table");
    expect(source).toContain("<thead>");
    expect(source).toContain("<tbody>");
    expect(source).toContain("production.setups.tableIngredient");
    expect(source).toContain("production.setups.tableQty");
    expect(source).toContain("production.setups.tableUnit");
    expect(source).toContain("production.setups.tableAvailable");
    expect(source).toContain("production.setups.tableActions");
    expect(source).toContain("hidden overflow-x-auto md:block");
    // No permanent large selected-ingredient Card list.
    expect(source).not.toMatch(
      /production-setup-selected-materials[\s\S]*?<Card[\s\S]*?production-setup-selected-/,
    );
  });

  it("shows qty, entered UOM, and availability helpers", () => {
    expect(source).toContain("draftQuantityParts");
    expect(source).toContain("formatMaterialAvailableCell");
    expect(source).toContain("production-setup-qty-");
    expect(source).toContain("production-setup-unit-");
    expect(source).toContain("production-setup-available-");

    const parts = draftQuantityParts({
      materialProductId: "1",
      name: "Sugar",
      quantity: 0.5,
      displayUom: "g",
      weightInputUnit: "g",
    });
    expect(parts).toEqual({ qty: "500", unit: "g" });

    const apple = product({
      name: "Apple",
      sellingMode: "ByWeight",
      unitOfMeasure: "Kilogram",
      isTracked: true,
      branchAvailableQuantity: 9,
    });
    expect(formatMaterialAvailableCell(apple)).toBe("9 kg");
    expect(formatMaterialAvailableCell(null)).toBe("—");
  });

  it("keeps Edit/Remove and closes picker after add", () => {
    expect(source).toContain("production.setups.editMaterial");
    expect(source).toContain("production.setups.removeMaterial");
    expect(source).toContain("openEditMaterial");
    expect(source).toContain("removeMaterial");
    expect(source).toContain("setMaterialPickerOpen(false)");
    expect(source).toContain("ProductionMaterialQuantitySheet");
  });

  it("collapses ingredient catalog behind + Add ingredient", () => {
    expect(source).toContain('data-testid="production-setup-add-ingredient"');
    expect(source).toContain("production.setups.addIngredient");
    expect(source).toContain("materialPickerOpen");
    expect(source).toContain('data-testid="production-setup-ingredient-picker"');
    expect(source).toContain("enabled: Boolean(workspace) && online && allowManage && materialPickerOpen");
    // Browser is only rendered while picker is open.
    expect(source).toMatch(
      /materialPickerOpen \? \([\s\S]*production-setup-material-browser/,
    );
  });

  it("excludes already-selected products from picker and requires eligible/tracked", () => {
    expect(source).toContain("selectedIds.has(p.productId)");
    expect(source).toContain("isEligibleProductionMaterial");
    expect(source).toContain("canBeUsedAsIngredient: browseCatalogIngredients ? undefined : true");
  });

  it("mobile uses compact stacked rows without forcing desktop table", () => {
    expect(source).toContain('data-testid="production-setup-materials-mobile"');
    expect(source).toContain("md:hidden");
    expect(source).toContain("production-setup-selected-mobile-");
    expect(source).toContain("production.setups.availableShort");
  });

  it("preserves recipe save / output sheet flow", () => {
    expect(source).toContain("ProductionRecipeOutputSheet");
    expect(source).toContain('data-testid="production-setup-save"');
    expect(source).toContain("persistDefinition");
    expect(source).toContain("production-recipe-yield-uom");
  });
});

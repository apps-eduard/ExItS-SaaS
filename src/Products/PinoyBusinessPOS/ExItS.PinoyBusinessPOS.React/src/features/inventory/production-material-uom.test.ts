import { describe, expect, it } from "vitest";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  draftQuantityLine,
  formatMaterialAvailableCaption,
  isEligibleProductionMaterial,
  isWeightMaterial,
  normalizeMaterialQuantityInput,
  resolveMaterialEntryMode,
} from "@/features/inventory/production-material-uom";

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

describe("production-material-uom", () => {
  it("only treats CanBeUsedAsIngredient products as eligible", () => {
    expect(
      isEligibleProductionMaterial(product({ canBeUsedAsIngredient: true })),
    ).toBe(true);
    expect(
      isEligibleProductionMaterial(product({ canBeUsedAsIngredient: false })),
    ).toBe(false);
    expect(isEligibleProductionMaterial(product({}))).toBe(false);
  });

  it("detects weight materials and allows kg/g conversion", () => {
    const sugar = product({
      name: "Sugar",
      sellingMode: "ByWeight",
      unitOfMeasure: "Kilogram",
      canBeUsedAsIngredient: true,
    });
    expect(isWeightMaterial(sugar)).toBe(true);
    expect(resolveMaterialEntryMode(sugar)).toBe("weight");

    const grams = normalizeMaterialQuantityInput({
      product: sugar,
      rawValue: 500,
      weightUnit: "g",
    });
    expect(grams).toEqual({
      ok: true,
      quantity: 0.5,
      displayUom: "g",
      productUnitId: null,
      weightInputUnit: "g",
    });

    const kilos = normalizeMaterialQuantityInput({
      product: sugar,
      rawValue: 0.5,
      weightUnit: "kg",
    });
    expect(kilos).toMatchObject({ ok: true, quantity: 0.5, displayUom: "kg" });
  });

  it("does not offer weight units for piece products", () => {
    const bottle = product({
      name: "Bottle",
      unitOfMeasure: "pcs",
      sellingMode: "PerItem",
      canBeUsedAsIngredient: true,
    });
    expect(isWeightMaterial(bottle)).toBe(false);
    expect(resolveMaterialEntryMode(bottle)).toBe("base");
    const result = normalizeMaterialQuantityInput({
      product: bottle,
      rawValue: 10,
    });
    expect(result).toMatchObject({
      ok: true,
      quantity: 10,
      displayUom: "pcs",
      productUnitId: null,
    });
  });

  it("supports Pack product units", () => {
    const pack = product({
      name: "Battery Pack",
      unitOfMeasure: "Piece",
      canBeUsedAsIngredient: true,
      units: [
        {
          unitId: "11111111-1111-1111-1111-111111111111",
          productId: "00000000-0000-0000-0000-000000000001",
          kind: "Sell",
          displayName: "Pack",
          shortLabel: "Pack",
          multiplierToBase: 4,
          allowsCustomQuantity: false,
          isActive: true,
          sortOrder: 1,
        },
      ],
    });
    expect(resolveMaterialEntryMode(pack)).toBe("unit");
    const result = normalizeMaterialQuantityInput({
      product: pack,
      rawValue: 2,
      productUnitId: "11111111-1111-1111-1111-111111111111",
    });
    expect(result).toMatchObject({
      ok: true,
      quantity: 2,
      displayUom: "Pack",
      productUnitId: "11111111-1111-1111-1111-111111111111",
    });
  });

  it("formats available stock and draft quantity lines", () => {
    const flour = product({
      name: "Flour",
      sellingMode: "ByWeight",
      unitOfMeasure: "Kilogram",
      isTracked: true,
      onHandQuantity: 80,
      canBeUsedAsIngredient: true,
    });
    expect(formatMaterialAvailableCaption(flour, "Available: {qty} {uom}")).toBe(
      "Available: 80 kg",
    );
    expect(
      draftQuantityLine({
        materialProductId: flour.productId,
        name: "Flour",
        quantity: 0.5,
        displayUom: "g",
        weightInputUnit: "g",
      }),
    ).toBe("500 g");
  });

  it("rejects invalid decimal weight conversion", () => {
    const sugar = product({
      sellingMode: "ByWeight",
      unitOfMeasure: "Kilogram",
      canBeUsedAsIngredient: true,
    });
    expect(
      normalizeMaterialQuantityInput({
        product: sugar,
        rawValue: 0.0001,
        weightUnit: "kg",
      }),
    ).toMatchObject({ ok: false, error: "precision" });
  });
});

import { describe, expect, it } from "vitest";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { resolveSellUnitPrice } from "@/cart/sell-cart-helpers";

function baseProduct(overrides: Partial<PosCatalogProductDto> = {}): PosCatalogProductDto {
  return {
    productId: "p1",
    organizationId: "o1",
    name: "Test",
    unitOfMeasure: "Piece",
    sellingMode: "PerItem",
    sellingPrice: 50,
    status: "Active",
    createdAtUtc: "",
    updatedAtUtc: "",
    ...overrides,
  };
}

function sellUnit(overrides: Partial<PosCatalogProductUnitDto> = {}): PosCatalogProductUnitDto {
  return {
    unitId: "u1",
    productId: "p1",
    kind: "Sell",
    displayName: "Pack",
    shortLabel: "Pk",
    multiplierToBase: 6,
    allowsCustomQuantity: false,
    isActive: true,
    sortOrder: 0,
    ...overrides,
  };
}

describe("resolveSellUnitPrice effective prices", () => {
  it("prefers unit effectiveSellingPrice over product effective and catalog defaults", () => {
    const product = baseProduct({ effectiveSellingPrice: 65, sellingPrice: 50 });
    const unit = sellUnit({ sellingPrice: 280, effectiveSellingPrice: 300 });
    expect(resolveSellUnitPrice(product, unit)).toBe(300);
  });

  it("uses product effectiveSellingPrice when unit has no effective price", () => {
    const product = baseProduct({ effectiveSellingPrice: 65, sellingPrice: 50 });
    const unit = sellUnit({ sellingPrice: 280 });
    expect(resolveSellUnitPrice(product, unit)).toBe(65);
    expect(resolveSellUnitPrice(product, null)).toBe(65);
  });

  it("falls back to unit and product sellingPrice when no effective prices are present", () => {
    const product = baseProduct({ sellingPrice: 50 });
    const unit = sellUnit({ sellingPrice: 280 });
    expect(resolveSellUnitPrice(product, unit)).toBe(280);
    expect(resolveSellUnitPrice(product, null)).toBe(50);
  });
});

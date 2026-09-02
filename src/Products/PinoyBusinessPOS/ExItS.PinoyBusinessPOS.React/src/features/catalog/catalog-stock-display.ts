import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  resolveSellCardStock,
  type SellCardStock,
} from "@/cart/sell-cart-helpers";

export function hasBranchStockStamp(
  product: Pick<
    PosCatalogProductDto,
    "organizationOnHandQuantity" | "branchOnHandQuantity" | "branchAvailableQuantity"
  >,
): boolean {
  return (
    product.branchAvailableQuantity != null ||
    product.branchOnHandQuantity != null ||
    product.organizationOnHandQuantity != null
  );
}

/** Branch-scoped sale-eligible quantity for catalog/sell display. */
export function resolveBranchSaleEligibleQuantity(
  product: Pick<
    PosCatalogProductDto,
    | "isTracked"
    | "onHandQuantity"
    | "branchAvailableQuantity"
    | "sellableQuantity"
    | "tracksExpiration"
    | "organizationOnHandQuantity"
    | "branchOnHandQuantity"
  >,
  options?: { branchScoped?: boolean },
): number | null {
  if (!product.isTracked) {
    return null;
  }

  const branchScoped = options?.branchScoped ?? hasBranchStockStamp(product);

  if (
    product.branchAvailableQuantity != null &&
    Number.isFinite(product.branchAvailableQuantity)
  ) {
    if (
      product.tracksExpiration &&
      product.sellableQuantity != null &&
      Number.isFinite(product.sellableQuantity)
    ) {
      return Math.min(product.branchAvailableQuantity, product.sellableQuantity);
    }
    return product.branchAvailableQuantity;
  }

  if (branchScoped) {
    return null;
  }

  return null;
}

export function resolveCatalogStockDisplay(
  product: Pick<
    PosCatalogProductDto,
    | "isTracked"
    | "onHandQuantity"
    | "unitOfMeasure"
    | "tracksExpiration"
    | "sellableQuantity"
    | "stockStatus"
    | "isLowStock"
    | "branchAvailableQuantity"
    | "organizationOnHandQuantity"
    | "branchOnHandQuantity"
  >,
  options?: { branchScoped?: boolean },
): SellCardStock {
  const saleEligible = resolveBranchSaleEligibleQuantity(product, options);
  if (product.isTracked && saleEligible == null) {
    return {
      tone: "out",
      quantity: 0,
      unitOfMeasure: product.unitOfMeasure,
      quantityLabel: null,
    };
  }

  return resolveSellCardStock({
    isTracked: product.isTracked,
    onHandQuantity: saleEligible,
    unitOfMeasure: product.unitOfMeasure,
    tracksExpiration: product.tracksExpiration,
    sellableQuantity: product.sellableQuantity,
    stockStatus: product.stockStatus,
    isLowStock: product.isLowStock,
  });
}

/** Authoritative branch quantity for sell guards and cart checks. */
export function resolveBranchStockGuardQuantity(
  product: Pick<
    PosCatalogProductDto,
    | "isTracked"
    | "onHandQuantity"
    | "branchAvailableQuantity"
    | "sellableQuantity"
    | "tracksExpiration"
    | "organizationOnHandQuantity"
    | "branchOnHandQuantity"
  >,
): number | null {
  return resolveBranchSaleEligibleQuantity(product, { branchScoped: true });
}

import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  resolveSellCardStock,
  type SellCardStock,
} from "@/cart/sell-cart-helpers";

/** Branch-scoped sale-eligible quantity for catalog/sell display. */
export function resolveBranchSaleEligibleQuantity(
  product: Pick<
    PosCatalogProductDto,
    | "isTracked"
    | "onHandQuantity"
    | "branchAvailableQuantity"
    | "sellableQuantity"
    | "tracksExpiration"
  >,
): number | null {
  if (!product.isTracked) {
    return null;
  }
  if (product.branchAvailableQuantity != null && Number.isFinite(product.branchAvailableQuantity)) {
    if (
      product.tracksExpiration &&
      product.sellableQuantity != null &&
      Number.isFinite(product.sellableQuantity)
    ) {
      return Math.min(product.branchAvailableQuantity, product.sellableQuantity);
    }
    return product.branchAvailableQuantity;
  }
  if (product.onHandQuantity != null && Number.isFinite(product.onHandQuantity)) {
    return product.onHandQuantity;
  }
  return 0;
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
  >,
): SellCardStock {
  const saleEligible = resolveBranchSaleEligibleQuantity(product);
  return resolveSellCardStock({
    isTracked: product.isTracked,
    onHandQuantity: saleEligible ?? product.onHandQuantity,
    unitOfMeasure: product.unitOfMeasure,
    tracksExpiration: product.tracksExpiration,
    sellableQuantity: product.sellableQuantity,
    stockStatus: product.stockStatus,
    isLowStock: product.isLowStock,
  });
}

import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import { isByWeightSellingMode } from "@/lib/quantity-rules";
import { formatUnitOfMeasureSymbol } from "@/lib/unit-of-measure";

export const STOREFRONT_AVAILABILITY = {
  Untracked: "Untracked",
  InStock: "InStock",
  LowStock: "LowStock",
  OutOfStock: "OutOfStock",
} as const;

export const LOW_STOCK_THRESHOLD = 5;

/** Kilo products are entered as a weight before they go in the cart, the same as Sell. */
export function isKiloStorefrontProduct(product: {
  unitOfMeasure?: string | null;
  sellingMode?: string | null;
}): boolean {
  return (
    isByWeightSellingMode(product.sellingMode) ||
    formatUnitOfMeasureSymbol(product.unitOfMeasure) === "kg"
  );
}

export function canIncrementStorefrontQuantity(
  product: CustomerStorefrontProductDto,
  currentQuantity: number,
): boolean {
  if (!product.isAvailable || product.unitPrice <= 0) {
    return false;
  }
  if (!product.tracksInventory || product.availableQuantity == null) {
    return true;
  }
  return currentQuantity < product.availableQuantity;
}

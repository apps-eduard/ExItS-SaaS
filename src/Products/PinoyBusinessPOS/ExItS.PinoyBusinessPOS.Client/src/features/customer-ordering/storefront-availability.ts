import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";

export const STOREFRONT_AVAILABILITY = {
  Untracked: "Untracked",
  InStock: "InStock",
  LowStock: "LowStock",
  OutOfStock: "OutOfStock",
} as const;

export const LOW_STOCK_THRESHOLD = 5;

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

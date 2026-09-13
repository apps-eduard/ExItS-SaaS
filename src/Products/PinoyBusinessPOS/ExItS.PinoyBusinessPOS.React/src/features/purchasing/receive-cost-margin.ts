import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { pricesEqual } from "@/features/catalog/todays-prices-draft";
import { resolveEffectiveSellingPriceView } from "@/features/inventory/inventory-opening-price-feedback";

export type ReceiveCostMarginKind = "none" | "zeroMargin" | "negativeMargin";

/**
 * Branch-effective selling price for receive-stock margin checks.
 * Prefers catalog effectiveSellingPrice (branch override ?? org default).
 */
export function resolveReceiveEffectiveSellingPrice(
  product: Pick<
    PosCatalogProductDto,
    "sellingPrice" | "effectiveSellingPrice" | "hasBranchPriceOverride"
  >,
): number {
  return resolveEffectiveSellingPriceView(product)?.amount ?? 0;
}

/** cost < sell → none; cost == sell → zero; cost > sell → negative. */
export function receiveCostMarginKind(
  unitCost: number,
  effectiveSellingPrice: number,
): ReceiveCostMarginKind {
  if (!(unitCost > 0) || !(effectiveSellingPrice > 0)) {
    return "none";
  }
  if (pricesEqual(unitCost, effectiveSellingPrice)) {
    return "zeroMargin";
  }
  if (unitCost > effectiveSellingPrice) {
    return "negativeMargin";
  }
  return "none";
}

export function hasReceiveCostMarginWarning(
  unitCost: number,
  effectiveSellingPrice: number,
): boolean {
  return receiveCostMarginKind(unitCost, effectiveSellingPrice) !== "none";
}

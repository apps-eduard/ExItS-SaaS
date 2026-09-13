import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { pricesEqual } from "@/features/catalog/todays-prices-draft";
import { resolveEffectiveSellingPriceView } from "@/features/inventory/inventory-opening-price-feedback";

export type ReceiveCostMarginKind = "none" | "zeroMargin" | "negativeMargin";

export type ReceiveMarginWarningFlash = {
  count: number;
  productId?: string | null;
};

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

/**
 * cost < sell → none; cost == sell → zero; cost > sell → negative.
 * Missing / zero effective selling price with a purchase cost also needs review.
 */
export function receiveCostMarginKind(
  unitCost: number,
  effectiveSellingPrice: number,
): ReceiveCostMarginKind {
  if (!(unitCost > 0)) {
    return "none";
  }
  if (!(effectiveSellingPrice > 0)) {
    return "negativeMargin";
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

export function buildReceiveMarginWarningToast(options: {
  count: number;
  productId?: string | null;
  title: string;
  detailSingle: string;
  detailMany: string;
  reviewPrice: string;
  reviewPrices: string;
}): {
  title: string;
  description: string;
  tone: "warning";
  action: { label: string; href: string };
} {
  const single = options.count === 1;
  return {
    title: options.title,
    description: single
      ? options.detailSingle
      : options.detailMany.replace("{count}", String(options.count)),
    tone: "warning",
    action: single
      ? {
          label: options.reviewPrice,
          href: `/catalog/products/${options.productId}/edit`,
        }
      : {
          label: options.reviewPrices,
          href: "/catalog/todays-prices",
        },
  };
}

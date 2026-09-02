import { pricesEqual } from "@/features/catalog/todays-prices-draft";

export type EffectiveSellingPriceSource = "branch" | "organization";

export type EffectiveSellingPriceView = {
  amount: number;
  source: EffectiveSellingPriceSource;
};

/**
 * Branch-effective selling price for the current workspace branch.
 * Prefer effectiveSellingPrice (BranchOverride ?? OrganizationDefault);
 * fall back to organization sellingPrice when effective is absent.
 */
export function resolveEffectiveSellingPriceView(product: {
  sellingPrice: number;
  effectiveSellingPrice?: number | null;
  hasBranchPriceOverride?: boolean | null;
}): EffectiveSellingPriceView | null {
  const amount =
    product.effectiveSellingPrice != null && Number.isFinite(product.effectiveSellingPrice)
      ? product.effectiveSellingPrice
      : product.sellingPrice;
  if (!Number.isFinite(amount)) {
    return null;
  }
  return {
    amount,
    source: product.hasBranchPriceOverride === true ? "branch" : "organization",
  };
}

export type PurchaseCostVsSellingFeedback =
  | { kind: "none" }
  | { kind: "zeroMargin" }
  | { kind: "higherCost"; difference: number };

export function comparePurchaseCostToSellingPrice(
  unitCostRaw: string,
  sellingPrice: number | null | undefined,
): PurchaseCostVsSellingFeedback {
  if (sellingPrice == null || !Number.isFinite(sellingPrice)) {
    return { kind: "none" };
  }
  const trimmed = unitCostRaw.trim();
  if (!trimmed) {
    return { kind: "none" };
  }
  const cost = Number(trimmed);
  if (!Number.isFinite(cost) || cost < 0) {
    return { kind: "none" };
  }
  if (pricesEqual(cost, sellingPrice)) {
    return { kind: "zeroMargin" };
  }
  if (cost > sellingPrice) {
    return {
      kind: "higherCost",
      difference: Math.round((cost - sellingPrice) * 100) / 100,
    };
  }
  return { kind: "none" };
}

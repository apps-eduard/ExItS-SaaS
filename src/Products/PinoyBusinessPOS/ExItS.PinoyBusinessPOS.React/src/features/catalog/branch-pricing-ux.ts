import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  isBranchLocalProduct,
  isOrganizationStandardProduct,
} from "@/features/catalog/catalog-product-scope";
import { pricesEqual } from "@/features/catalog/todays-prices-draft";

export type BranchPriceMode = "inherit" | "custom";

export type PriceEditScope = "organization" | "branch" | "branchLocal";

export function resolvePriceEditScope(options: {
  scope: string | null | undefined;
  branchWorkspace: boolean;
}): PriceEditScope {
  if (isBranchLocalProduct({ scope: options.scope ?? undefined })) {
    return "branchLocal";
  }
  if (isOrganizationStandardProduct({ scope: options.scope ?? undefined }) && options.branchWorkspace) {
    return "branch";
  }
  return "organization";
}

export function resolveBranchPriceMode(hasBranchPriceOverride: boolean): BranchPriceMode {
  return hasBranchPriceOverride ? "custom" : "inherit";
}

export function resolveCatalogDisplayPrice(
  product: Pick<PosCatalogProductDto, "sellingPrice" | "effectiveSellingPrice">,
): number {
  if (product.effectiveSellingPrice != null && Number.isFinite(product.effectiveSellingPrice)) {
    return product.effectiveSellingPrice;
  }
  return product.sellingPrice;
}

export function orgDefaultPriceChanged(
  initialPrice: number | null | undefined,
  nextPriceRaw: string,
): boolean {
  if (initialPrice == null || !Number.isFinite(initialPrice)) {
    return false;
  }
  const trimmed = nextPriceRaw.trim();
  if (!trimmed) {
    return false;
  }
  const next = Number(trimmed);
  if (Number.isNaN(next)) {
    return false;
  }
  return !pricesEqual(initialPrice, next);
}

export function shouldUseBranchOverrideApi(scope: PriceEditScope): boolean {
  return scope === "branch";
}

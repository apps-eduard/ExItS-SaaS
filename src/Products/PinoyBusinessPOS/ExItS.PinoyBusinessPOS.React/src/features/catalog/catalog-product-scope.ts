import type { CatalogProductScopeCode, PosCatalogProductDto } from "@/api/pos/pos-catalog-types";

export type CatalogScopeFilter = "" | CatalogProductScopeCode;

export function normalizeCatalogProductScope(
  value: string | null | undefined,
): CatalogProductScopeCode | "Unknown" {
  if (!value) {
    return "OrganizationStandard";
  }
  const trimmed = value.trim();
  if (trimmed.localeCompare("OrganizationStandard", undefined, { sensitivity: "accent" }) === 0) {
    return "OrganizationStandard";
  }
  if (trimmed.localeCompare("BranchLocal", undefined, { sensitivity: "accent" }) === 0) {
    return "BranchLocal";
  }
  return "Unknown";
}

export function isOrganizationStandardProduct(
  product: Pick<PosCatalogProductDto, "scope"> | null | undefined,
): boolean {
  return normalizeCatalogProductScope(product?.scope) === "OrganizationStandard";
}

export function isBranchLocalProduct(
  product: Pick<PosCatalogProductDto, "scope"> | null | undefined,
): boolean {
  return normalizeCatalogProductScope(product?.scope) === "BranchLocal";
}

/** Whether a normal branch actor should treat Standard master fields as read-only. */
export function isStandardMasterReadOnlyForActor(options: {
  canGovernOrganizationCatalog: boolean;
  product: Pick<PosCatalogProductDto, "scope"> | null | undefined;
}): boolean {
  if (options.canGovernOrganizationCatalog) {
    return false;
  }
  return isOrganizationStandardProduct(options.product);
}

import type { FilterChipItem } from "@/components/exits/ListToolbar";
import type { CatalogScopeFilter } from "@/features/catalog/catalog-product-scope";
import type { ProductBusinessUsage } from "@/features/catalog/product-business-usage";

export type CatalogStatusFilter = "Active" | "Inactive" | "";
export type CatalogUsageFilter = "all" | ProductBusinessUsage;

export type CatalogActiveFilterChipId = "scope" | "usage" | "status" | "category" | "brand";

export function countCatalogSheetFilters(input: {
  scopeFilter: CatalogScopeFilter;
  usageFilter: CatalogUsageFilter;
  categoryId: string;
  brandId: string;
}): number {
  let count = 0;
  if (input.scopeFilter) {
    count += 1;
  }
  if (input.usageFilter !== "all") {
    count += 1;
  }
  if (input.categoryId) {
    count += 1;
  }
  if (input.brandId) {
    count += 1;
  }
  return count;
}

export function buildCatalogActiveFilterChips(input: {
  scopeFilter: CatalogScopeFilter;
  usageFilter: CatalogUsageFilter;
  status: CatalogStatusFilter;
  categoryId: string;
  brandId: string;
  categoryName?: string;
  brandName?: string;
  labels: {
    scopeOrganization: string;
    scopeBranch: string;
    statusActive: string;
    statusInactive: string;
    statusAll: string;
    usageResale: string;
    usageIngredient: string;
    usageInternal: string;
    usageProduced: string;
    categoryPrefix: string;
    brandPrefix: string;
  };
}): FilterChipItem[] {
  const chips: FilterChipItem[] = [];

  if (input.scopeFilter === "OrganizationStandard") {
    chips.push({ id: "scope", label: input.labels.scopeOrganization });
  } else if (input.scopeFilter === "BranchLocal") {
    chips.push({ id: "scope", label: input.labels.scopeBranch });
  }

  if (input.usageFilter === "Resale") {
    chips.push({ id: "usage", label: input.labels.usageResale });
  } else if (input.usageFilter === "Ingredient") {
    chips.push({ id: "usage", label: input.labels.usageIngredient });
  } else if (input.usageFilter === "InternalUse") {
    chips.push({ id: "usage", label: input.labels.usageInternal });
  } else if (input.usageFilter === "ProducedItem") {
    chips.push({ id: "usage", label: input.labels.usageProduced });
  }

  if (input.status === "Inactive") {
    chips.push({ id: "status", label: input.labels.statusInactive });
  } else if (input.status === "") {
    chips.push({ id: "status", label: input.labels.statusAll });
  }

  if (input.categoryId) {
    chips.push({
      id: "category",
      label: `${input.labels.categoryPrefix}: ${input.categoryName ?? input.categoryId}`,
    });
  }

  if (input.brandId) {
    chips.push({
      id: "brand",
      label: `${input.labels.brandPrefix}: ${input.brandName ?? input.brandId}`,
    });
  }

  return chips;
}

export function defaultCatalogSheetFilters(): {
  scopeFilter: CatalogScopeFilter;
  usageFilter: CatalogUsageFilter;
  categoryId: string;
  brandId: string;
} {
  return {
    scopeFilter: "",
    usageFilter: "all",
    categoryId: "",
    brandId: "",
  };
}

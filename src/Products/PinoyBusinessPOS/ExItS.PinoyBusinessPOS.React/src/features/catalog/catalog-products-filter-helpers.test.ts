import { describe, expect, it } from "vitest";
import {
  buildCatalogActiveFilterChips,
  countCatalogSheetFilters,
  defaultCatalogSheetFilters,
} from "@/features/catalog/catalog-products-filter-helpers";

const labels = {
  scopeOrganization: "Organization products",
  scopeBranch: "Branch products",
  statusActive: "Active",
  statusInactive: "Inactive",
  statusAll: "All",
  usageResale: "For resale",
  usageIngredient: "Ingredients",
  usageInternal: "Internal use",
  usageProduced: "Produced",
  categoryPrefix: "Category",
  brandPrefix: "Brand",
};

describe("catalog-products-filter-helpers", () => {
  it("counts only sheet filters", () => {
    expect(countCatalogSheetFilters(defaultCatalogSheetFilters())).toBe(0);
    expect(
      countCatalogSheetFilters({
        scopeFilter: "BranchLocal",
        usageFilter: "Resale",
        categoryId: "cat-1",
        brandId: "",
      }),
    ).toBe(3);
  });

  it("builds removable chips for non-default filters", () => {
    const chips = buildCatalogActiveFilterChips({
      scopeFilter: "OrganizationStandard",
      usageFilter: "Ingredient",
      status: "",
      categoryId: "cat-1",
      brandId: "brand-1",
      categoryName: "Snacks",
      brandName: "Local",
      labels,
    });

    expect(chips.map((chip) => chip.id)).toEqual([
      "scope",
      "usage",
      "status",
      "category",
      "brand",
    ]);
    expect(chips[3]?.label).toBe("Category: Snacks");
  });
});

import { describe, expect, it } from "vitest";
import {
  businessUsageLabelKey,
  resolveBusinessUsage,
} from "@/features/catalog/product-business-usage";
import { buildCatalogActiveFilterChips } from "@/features/catalog/catalog-products-filter-helpers";
import { en } from "@/i18n/locales/en";
import { cebPH } from "@/i18n/locales/ceb-PH";
import { filPH } from "@/i18n/locales/fil-PH";
import { hilPH } from "@/i18n/locales/hil-PH";
import { iloPH } from "@/i18n/locales/ilo-PH";

/**
 * POS-UX-LABEL-FOR-RESALE — user-facing label proof.
 * Internal domain value remains `Resale`.
 */
describe("POS-UX-LABEL-FOR-RESALE", () => {
  it("UX-RESALE-01: Products business-use filter displays For resale", () => {
    expect(en["catalog.businessUsage.filterResale"]).toBe("For resale");
    expect(en["catalog.businessUsage.label"]).toBe("Business use");
  });

  it("UX-RESALE-02: For resale filter maps to existing Resale classification", () => {
    expect(resolveBusinessUsage({ businessUsage: "Resale" })).toBe("Resale");
    const chips = buildCatalogActiveFilterChips({
      scopeFilter: "",
      usageFilter: "Resale",
      status: "Active",
      categoryId: "",
      brandId: "",
      labels: {
        scopeOrganization: "Organization products",
        scopeBranch: "Branch products",
        statusActive: "Active",
        statusInactive: "Inactive",
        statusAll: "All",
        usageResale: en["catalog.businessUsage.filterResale"],
        usageIngredient: en["catalog.businessUsage.filterIngredient"],
        usageInternal: en["catalog.businessUsage.filterInternal"],
        usageProduced: en["catalog.businessUsage.filterProduced"],
        categoryPrefix: "Category",
        brandPrefix: "Brand",
      },
    });
    expect(chips).toEqual([{ id: "usage", label: "For resale" }]);
  });

  it("UX-RESALE-03: existing Resale product displays For resale", () => {
    const usage = resolveBusinessUsage({ businessUsage: "Resale", canBeSold: true });
    expect(usage).toBe("Resale");
    expect(en[businessUsageLabelKey(usage)]).toBe("For resale");
  });

  it("UX-RESALE-04: create/edit label uses For resale while backend value stays Resale", () => {
    expect(en["catalog.businessUsage.resale"]).toBe("For resale");
    expect(businessUsageLabelKey("Resale")).toBe("catalog.businessUsage.resale");
    expect(resolveBusinessUsage({ businessUsage: "Resale" })).toBe("Resale");
  });

  it("UX-RESALE-05: no locale still exposes Sell as-is to the user", () => {
    const locales = [en, filPH, cebPH, hilPH, iloPH] as const;
    for (const locale of locales) {
      expect(locale["catalog.businessUsage.resale"]).not.toMatch(/as-is/i);
      expect(locale["catalog.businessUsage.filterResale"]).not.toMatch(/as-is/i);
      expect(locale["catalog.businessUsage.resale"]).not.toMatch(/Sell as-is/i);
      expect(locale["catalog.businessUsage.filterResale"]).not.toMatch(/Sell as-is/i);
    }
  });
});

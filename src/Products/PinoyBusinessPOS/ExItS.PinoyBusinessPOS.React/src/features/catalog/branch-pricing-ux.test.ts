import { describe, expect, it } from "vitest";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  orgDefaultPriceChanged,
  resolveBranchPriceMode,
  resolveCatalogDisplayPrice,
  resolveCatalogPriceOrigin,
  resolvePriceEditScope,
  shouldUseBranchOverrideApi,
} from "@/features/catalog/branch-pricing-ux";
import {
  applySuccessfulBranchPriceSave,
  mergePriceDraftMap,
  toPriceDraft,
} from "@/features/catalog/todays-prices-draft";

function product(
  overrides: Partial<PosCatalogProductDto> & Pick<PosCatalogProductDto, "productId" | "name" | "sellingPrice">,
): PosCatalogProductDto {
  return {
    organizationId: "org",
    unitOfMeasure: "Piece",
    sellingMode: "PerItem",
    status: "Active",
    scope: "OrganizationStandard",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

describe("branch-pricing-ux helpers", () => {
  it("BRPRICE-UX-05 resolves organization scope when not in branch workspace", () => {
    expect(
      resolvePriceEditScope({ scope: "OrganizationStandard", branchWorkspace: false }),
    ).toBe("organization");
  });

  it("BRPRICE-UX-01 resolves branch scope for OrganizationStandard in branch workspace", () => {
    expect(
      resolvePriceEditScope({ scope: "OrganizationStandard", branchWorkspace: true }),
    ).toBe("branch");
  });

  it("BRPRICE-UX-09 keeps branch-local products on branchLocal scope", () => {
    expect(resolvePriceEditScope({ scope: "BranchLocal", branchWorkspace: true })).toBe(
      "branchLocal",
    );
  });

  it("BRPRICE-UX-06 uses effective price when branch override exists", () => {
    expect(
      resolveCatalogDisplayPrice({ sellingPrice: 100, effectiveSellingPrice: 120 }),
    ).toBe(120);
  });

  it("BRPRICE-UX-05 falls back to organization default without override", () => {
    expect(resolveCatalogDisplayPrice({ sellingPrice: 100, effectiveSellingPrice: null })).toBe(
      100,
    );
  });

  it("BRPRICE-UX-07 treats inherit/custom mode from override flag", () => {
    expect(resolveBranchPriceMode(false)).toBe("inherit");
    expect(resolveBranchPriceMode(true)).toBe("custom");
  });

  it("BRPRICE-UX-08 detects organization default changes", () => {
    expect(orgDefaultPriceChanged(100, "110")).toBe(true);
    expect(orgDefaultPriceChanged(100, "100")).toBe(false);
  });

  it("BRPRICE-UX-02 routes branch workspace saves through override API", () => {
    expect(shouldUseBranchOverrideApi("branch")).toBe(true);
    expect(shouldUseBranchOverrideApi("organization")).toBe(false);
  });

  it("CATPRICE-04 resolves branch override origin label", () => {
    expect(
      resolveCatalogPriceOrigin(
        { scope: "OrganizationStandard", hasBranchPriceOverride: true },
        true,
      ),
    ).toBe("branchOverride");
  });

  it("CATPRICE-05 resolves organization default origin label", () => {
    expect(
      resolveCatalogPriceOrigin(
        { scope: "OrganizationStandard", hasBranchPriceOverride: false },
        true,
      ),
    ).toBe("organizationDefault");
  });

  it("CATPRICE-06 omits origin label for BranchLocal products", () => {
    expect(
      resolveCatalogPriceOrigin({ scope: "BranchLocal", hasBranchPriceOverride: false }, true),
    ).toBe(null);
  });
});

describe("todays prices branch scope drafts", () => {
  it("BRPRICE-UX-01 branch draft uses effective price and organization default separately", () => {
    const draft = toPriceDraft(
      product({
        productId: "p1",
        name: "Coke",
        sellingPrice: 100,
        effectiveSellingPrice: 120,
        hasBranchPriceOverride: true,
      }),
      { branchWorkspace: true },
    );
    expect(draft.priceEditScope).toBe("branch");
    expect(draft.organizationDefaultPrice).toBe(100);
    expect(draft.currentPrice).toBe(120);
    expect(draft.hasBranchPriceOverride).toBe(true);
  });

  it("BRPRICE-UX-14 organization standard in branch workspace uses branch scope", () => {
    const merged = mergePriceDraftMap(
      {},
      [
        product({
          productId: "p1",
          name: "Coke",
          sellingPrice: 100,
          effectiveSellingPrice: 120,
          hasBranchPriceOverride: true,
        }),
      ],
      { branchWorkspace: true },
    );
    expect(merged.p1.priceEditScope).toBe("branch");
  });

  it("BRPRICE-UX-04 applySuccessfulBranchPriceSave clears inherit state", () => {
    const row = toPriceDraft(
      product({ productId: "p1", name: "Coke", sellingPrice: 100, effectiveSellingPrice: 120 }),
      { branchWorkspace: true },
    );
    const next = applySuccessfulBranchPriceSave(row, 100, false);
    expect(next.currentPrice).toBe(100);
    expect(next.hasBranchPriceOverride).toBe(false);
  });
});

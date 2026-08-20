import { describe, expect, it } from "vitest";
import { catalogPlansListRequestPath } from "@/api/catalog/plan-list-query";
import { mapCatalogPlan, listCatalogPlansPage } from "@/api/catalog/plan-catalog-client";
import { parsePlanId } from "@/api/catalog/plan-id";

describe("parsePlanId", () => {
  it("accepts valid GUIDs", () => {
    expect(parsePlanId("dddddddd-dddd-dddd-dddd-dddddddddddd")).toBe(
      "dddddddd-dddd-dddd-dddd-dddddddddddd",
    );
  });

  it("rejects invalid values", () => {
    expect(parsePlanId("not-a-guid")).toBeNull();
    expect(parsePlanId(undefined)).toBeNull();
  });
});

describe("mapCatalogPlan", () => {
  it("maps catalog plan fields", () => {
    expect(
      mapCatalogPlan({
        id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        productCode: "future-product-x",
        code: "starter",
        displayName: "Starter",
        status: "Active",
        monthlyPrice: 999,
        currencyCode: "PHP",
      }),
    ).toEqual({
      id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      productCode: "future-product-x",
      code: "starter",
      displayName: "Starter",
      status: "Active",
      createdAtUtc: undefined,
      updatedAtUtc: undefined,
      productId: undefined,
      productDisplayName: undefined,
      planKey: undefined,
      description: undefined,
      maxBranches: undefined,
      maxActiveStaff: undefined,
      maxActivePosDevices: undefined,
      maxActiveBusinessTypes: undefined,
      customerCreditEnabled: undefined,
      advancedReportsEnabled: undefined,
      exportEnabled: undefined,
      trialAllowed: undefined,
      defaultTrialDays: undefined,
      sortOrder: undefined,
      monthlyPrice: 999,
      annualPrice: undefined,
      currencyCode: "PHP",
    });
  });
});

describe("catalogPlansListRequestPath", () => {
  it("includes search, status, product, sort, and paging", () => {
    const path = catalogPlansListRequestPath({
      page: 2,
      pageSize: 20,
      productCode: "future-product-x",
      status: "Active",
      search: "starter",
      sortBy: "Code",
      sortDesc: true,
    });
    expect(path).toContain("page=2");
    expect(path).toContain("productCode=future-product-x");
    expect(path).toContain("status=Active");
    expect(path).toContain("search=starter");
    expect(path).toContain("sortBy=Code");
    expect(path).toContain("sortDesc=true");
  });
});

describe("listCatalogPlansPage", () => {
  it("is exported for full catalog list queries", () => {
    expect(typeof listCatalogPlansPage).toBe("function");
  });
});

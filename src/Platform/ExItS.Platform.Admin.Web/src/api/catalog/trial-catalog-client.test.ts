import { describe, expect, it } from "vitest";
import { selectActiveTrialDefinition, type CatalogTrialDefinition } from "@/api/catalog/trial-catalog-client";

const starterId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1";
const growthId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2";

const starterTrial: CatalogTrialDefinition = {
  id: "trial-starter",
  productCode: "pinoy-business-pos",
  displayName: "Starter trial",
  status: "Active",
  planId: starterId,
};

const growthTrial: CatalogTrialDefinition = {
  id: "trial-growth",
  productCode: "pinoy-business-pos",
  displayName: "Growth trial",
  status: "Active",
  planId: growthId,
};

const productWideTrial: CatalogTrialDefinition = {
  id: "trial-product-wide",
  productCode: "pinoy-business-pos",
  displayName: "Product trial",
  status: "Active",
};

const retiredStarterTrial: CatalogTrialDefinition = {
  ...starterTrial,
  id: "trial-starter-retired",
  status: "Retired",
};

describe("selectActiveTrialDefinition", () => {
  it("selects the exact Starter trial for Starter", () => {
    expect(selectActiveTrialDefinition([growthTrial, starterTrial], starterId)?.id).toBe("trial-starter");
  });

  it("selects the exact Growth trial for Growth", () => {
    expect(selectActiveTrialDefinition([starterTrial, growthTrial], growthId)?.id).toBe("trial-growth");
  });

  it("never falls back from Starter to a Growth trial", () => {
    expect(selectActiveTrialDefinition([growthTrial], starterId)).toBeUndefined();
  });

  it("never falls back from Growth to a Starter trial", () => {
    expect(selectActiveTrialDefinition([starterTrial], growthId)).toBeUndefined();
  });

  it("returns no trial when the selected plan has no matching active definition", () => {
    expect(selectActiveTrialDefinition([retiredStarterTrial, growthTrial], starterId)).toBeUndefined();
    expect(selectActiveTrialDefinition([], starterId)).toBeUndefined();
    expect(selectActiveTrialDefinition([starterTrial], "")).toBeUndefined();
  });

  it("uses a product-wide null-planId trial when no plan-specific trial exists", () => {
    expect(selectActiveTrialDefinition([growthTrial, productWideTrial], starterId)?.id).toBe(
      "trial-product-wide",
    );
  });

  it("prefers the exact plan-specific trial over a product-wide trial", () => {
    expect(selectActiveTrialDefinition([productWideTrial, starterTrial, growthTrial], starterId)?.id).toBe(
      "trial-starter",
    );
  });
});

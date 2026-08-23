import { describe, expect, it } from "vitest";
import { mapPortfolioSummary, mapProductOverview } from "@/api/admin/admin-portfolio-client";

describe("admin-portfolio-client", () => {
  it("maps portfolio summary counts", () => {
    const summary = mapPortfolioSummary({
      activeProductCount: 2,
      organizationCount: 5,
      partialFailures: ["organizations: timeout"],
    });
    expect(summary.activeProductCount).toBe(2);
    expect(summary.organizationCount).toBe(5);
    expect(summary.partialFailures).toEqual(["organizations: timeout"]);
  });

  it("maps product overview from admin payload", () => {
    const overview = mapProductOverview({
      product: {
        id: "p1",
        code: "pinoy-business-pos",
        displayName: "Pinoy Business POS",
        status: "Active",
      },
      features: [{ code: "branches", displayName: "Branches" }],
      plans: [{ id: "plan1", code: "starter", displayName: "Starter", status: "Active" }],
      publishedPlanVersions: [],
      trials: [],
    });
    expect(overview.product.code).toBe("pinoy-business-pos");
    expect(overview.features).toHaveLength(1);
    expect(overview.plans).toHaveLength(1);
  });
});

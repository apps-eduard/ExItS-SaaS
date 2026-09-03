import { describe, expect, it } from "vitest";
import { mapCatalogPlan } from "@/api/catalog/plan-catalog-client";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import {
  commercialValuesToBody,
  planToCommercialValues,
} from "@/features/plans/plan-commercial-mapping";
import {
  MAX_MAX_AREAS,
  MIN_MAX_AREAS,
  planCommercialSchema,
} from "@/features/plans/plan-commercial-schema";

function commercialValues(maxAreas: number) {
  return {
    displayName: "Growth",
    description: "",
    monthlyPrice: 999,
    annualPrice: 9990,
    currencyCode: "PHP",
    maxBranches: 5,
    maxActiveStaff: 20,
    maxActivePosDevices: 5,
    maxActiveBusinessTypes: 3,
    maxAreas,
    customerCreditEnabled: true,
    advancedReportsEnabled: true,
    exportEnabled: true,
    trialAllowed: false,
    defaultTrialDays: 0,
    sortOrder: 20,
  };
}

/**
 * AREAH-01..04: Plan.MaxAreas must survive the whole Admin round trip — parsed from the catalog,
 * shown in the editor, validated to the same 1..10000 range the backend enforces, and sent back.
 */
describe("plan MaxAreas in Platform Admin", () => {
  it("AREAH-01 parses maxAreas from the catalog payload in either casing", () => {
    const identity = {
      id: "p1",
      productCode: "pinoy-business-pos",
      code: "growth",
      displayName: "Growth",
      status: "Active",
    };

    expect(mapCatalogPlan({ ...identity, maxAreas: 7 })?.maxAreas).toBe(7);
    expect(mapCatalogPlan({ ...identity, MaxAreas: 7 })?.maxAreas).toBe(7);
    expect(mapCatalogPlan(identity)?.maxAreas).toBeUndefined();
  });

  it("AREAH-02 carries maxAreas from the plan into the editor and back into the update body", () => {
    const plan = { id: "p1", displayName: "Growth", maxAreas: 12 } as CatalogPlan;

    const values = planToCommercialValues(plan);
    expect(values.maxAreas).toBe(12);

    const body = commercialValuesToBody({ ...values, maxAreas: 25 });
    expect(body.maxAreas).toBe(25);
  });

  it("AREAH-03 falls back to the minimum when the plan has no maxAreas yet", () => {
    const values = planToCommercialValues({ id: "p1", displayName: "Legacy" } as CatalogPlan);

    expect(values.maxAreas).toBe(MIN_MAX_AREAS);
  });

  it("AREAH-04 accepts the 1..10000 range and rejects anything outside it", () => {
    expect(planCommercialSchema.safeParse(commercialValues(MIN_MAX_AREAS)).success).toBe(true);
    expect(planCommercialSchema.safeParse(commercialValues(MAX_MAX_AREAS)).success).toBe(true);
    expect(planCommercialSchema.safeParse(commercialValues(0)).success).toBe(false);
    expect(planCommercialSchema.safeParse(commercialValues(MAX_MAX_AREAS + 1)).success).toBe(false);
    expect(planCommercialSchema.safeParse(commercialValues(2.5)).success).toBe(false);
  });
});

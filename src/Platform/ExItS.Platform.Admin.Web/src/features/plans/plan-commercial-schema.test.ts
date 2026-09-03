import { describe, expect, it } from "vitest";
import { planCommercialSchema } from "@/features/plans/plan-commercial-schema";

describe("planCommercialSchema", () => {
  it("rejects negative monthly price", () => {
    const result = planCommercialSchema.safeParse({
      displayName: "Growth",
      description: "",
      monthlyPrice: -1,
      annualPrice: 6990,
      currencyCode: "PHP",
      maxBranches: 3,
      maxActiveStaff: 10,
      maxActivePosDevices: 3,
      maxActiveBusinessTypes: 3,
      maxAreas: 5,
      customerCreditEnabled: true,
      advancedReportsEnabled: true,
      exportEnabled: true,
      trialAllowed: true,
      defaultTrialDays: 14,
      sortOrder: 20,
    });
    expect(result.success).toBe(false);
  });

  it("requires default trial days when trial is allowed", () => {
    const result = planCommercialSchema.safeParse({
      displayName: "Pro",
      description: "",
      monthlyPrice: 1499,
      annualPrice: 14990,
      currencyCode: "PHP",
      maxBranches: 10,
      maxActiveStaff: 30,
      maxActivePosDevices: 10,
      maxActiveBusinessTypes: 6,
      maxAreas: 5,
      customerCreditEnabled: true,
      advancedReportsEnabled: true,
      exportEnabled: true,
      trialAllowed: true,
      defaultTrialDays: 0,
      sortOrder: 30,
    });
    expect(result.success).toBe(false);
  });
});

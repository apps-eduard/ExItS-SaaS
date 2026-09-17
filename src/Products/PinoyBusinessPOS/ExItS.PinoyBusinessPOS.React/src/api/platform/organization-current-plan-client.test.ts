import { describe, expect, it } from "vitest";
import { normalizeOrganizationCurrentPlan } from "@/api/platform/organization-current-plan-client";

const ORG_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const GROWTH_PLAN_ID = "11111111-1111-4111-8111-111111111111";
const PRO_PLAN_ID = "22222222-2222-4222-8222-222222222222";
const SUBSCRIPTION_ID = "33333333-3333-4333-8333-333333333333";

describe("normalizeOrganizationCurrentPlan", () => {
  it("maps the full camelCase current-plan payload", () => {
    const value = normalizeOrganizationCurrentPlan({
      organizationId: ORG_ID,
      productCode: "pinoy-business-pos",
      currentSubscription: {
        id: SUBSCRIPTION_ID,
        status: "Active",
        billingCycle: "Annual",
        agreedPrice: 11880,
        currencyCode: "PHP",
        planId: GROWTH_PLAN_ID,
        planKey: "growth",
        planDisplayName: "Growth",
        trialEndUtc: "2026-01-15T00:00:00Z",
        currentPeriodStartUtc: "2026-02-01T00:00:00Z",
        currentPeriodEndUtc: "2027-02-01T00:00:00Z",
        renewalDateUtc: "2027-02-01T00:00:00Z",
        pastDueAtUtc: null,
      },
      currentPlan: {
        id: GROWTH_PLAN_ID,
        planKey: "growth",
        code: "GROWTH",
        displayName: "Growth",
        status: "Active",
        maxBranches: 3,
        maxActiveStaff: 10,
        maxActivePosDevices: 4,
        maxAreas: 5,
        maxActiveBusinessTypes: 2,
        customerCreditEnabled: true,
        advancedReportsEnabled: false,
        exportEnabled: false,
        monthlyPrice: 1099,
        annualPrice: 11880,
        currencyCode: "PHP",
        sortOrder: 20,
      },
      pendingPlanChange: {
        planId: PRO_PLAN_ID,
        planKey: "pro",
        displayName: "Pro",
        effectiveAtUtc: "2027-02-01T00:00:00Z",
      },
      availablePlans: [
        { id: PRO_PLAN_ID, planKey: "pro", displayName: "Pro", sortOrder: 30, maxBranches: 10 },
        { id: GROWTH_PLAN_ID, planKey: "growth", displayName: "Growth", sortOrder: 20 },
      ],
      entitlement: {
        subscriptionId: SUBSCRIPTION_ID,
        subscriptionStatus: "Active",
        inGracePeriod: false,
        generatedAtUtc: "2026-02-01T00:00:00Z",
      },
      productInstancePresent: true,
    });

    expect(value.currentSubscription).toMatchObject({
      id: SUBSCRIPTION_ID,
      status: "Active",
      billingCycle: "Annual",
      agreedPrice: 11880,
      currencyCode: "PHP",
      renewalDateUtc: "2027-02-01T00:00:00Z",
      pastDueAtUtc: null,
    });
    expect(value.currentPlan).toMatchObject({
      planKey: "growth",
      maxBranches: 3,
      maxActiveStaff: 10,
      maxActivePosDevices: 4,
      maxAreas: 5,
      customerCreditEnabled: true,
      advancedReportsEnabled: false,
      exportEnabled: false,
    });
    expect(value.pendingPlanChange).toMatchObject({ planKey: "pro", displayName: "Pro" });
    expect(value.availablePlans.map((plan) => plan.planKey)).toEqual(["growth", "pro"]);
    expect(value.entitlement).toMatchObject({ subscriptionStatus: "Active", inGracePeriod: false });
    expect(value.productInstancePresent).toBe(true);
  });

  it("maps PascalCase payloads and keeps the flat compatibility view", () => {
    const value = normalizeOrganizationCurrentPlan({
      OrganizationId: ORG_ID,
      ProductCode: "pinoy-business-pos",
      CurrentSubscription: { Id: SUBSCRIPTION_ID, Status: "PastDue", BillingCycle: "Monthly" },
      CurrentPlan: { Id: GROWTH_PLAN_ID, PlanKey: "growth", DisplayName: "Growth", MaxBranches: 3 },
      AvailablePlans: [],
    });

    expect(value.organizationId).toBe(ORG_ID);
    expect(value.planDisplayName).toBe("Growth");
    expect(value.planKey).toBe("growth");
    expect(value.subscriptionStatus).toBe("PastDue");
    expect(value.currentPlan?.maxBranches).toBe(3);
  });

  it("tolerates a missing subscription and plan without inventing limits", () => {
    const value = normalizeOrganizationCurrentPlan({
      organizationId: ORG_ID,
      currentSubscription: null,
      currentPlan: null,
      pendingPlanChange: null,
      availablePlans: null,
      entitlement: null,
    });

    expect(value.currentSubscription).toBeNull();
    expect(value.currentPlan).toBeNull();
    expect(value.pendingPlanChange).toBeNull();
    expect(value.availablePlans).toEqual([]);
    expect(value.entitlement).toBeNull();
    expect(value.planDisplayName).toBeNull();
    expect(value.subscriptionStatus).toBeNull();
    expect(value.productInstancePresent).toBeNull();
  });
});

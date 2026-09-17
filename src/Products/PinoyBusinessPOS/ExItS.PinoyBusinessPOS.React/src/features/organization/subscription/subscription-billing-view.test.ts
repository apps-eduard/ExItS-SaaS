import { describe, expect, it } from "vitest";
import type {
  OrganizationPlanDto,
  OrganizationSubscriptionDto,
} from "@/api/platform/organization-current-plan-client";
import {
  buildCapacityUsage,
  buildPlanFeatureRows,
  capacityAtLimitDimensions,
  classifySubscriptionStatus,
  comparePlanTier,
  parseSubscriptionTab,
  resolveNextPaymentDate,
  resolveSubscriptionStatusTone,
  selectableAvailablePlans,
} from "@/features/organization/subscription/subscription-billing-view";

function plan(overrides: Partial<OrganizationPlanDto> = {}): OrganizationPlanDto {
  return {
    id: "11111111-1111-4111-8111-111111111111",
    planKey: "growth",
    code: "GROWTH",
    displayName: "Growth",
    description: null,
    status: "Active",
    maxBranches: 3,
    maxActiveStaff: 10,
    maxActivePosDevices: 4,
    maxActiveBusinessTypes: 2,
    maxAreas: 5,
    customerCreditEnabled: true,
    advancedReportsEnabled: false,
    exportEnabled: false,
    trialAllowed: true,
    defaultTrialDays: 14,
    sortOrder: 20,
    monthlyPrice: 1099,
    annualPrice: 11880,
    currencyCode: "PHP",
    ...overrides,
  };
}

function subscription(
  overrides: Partial<OrganizationSubscriptionDto> = {},
): OrganizationSubscriptionDto {
  return {
    id: "33333333-3333-4333-8333-333333333333",
    status: "Active",
    billingCycle: "Annual",
    agreedPrice: 11880,
    currencyCode: "PHP",
    planId: null,
    planKey: "growth",
    planDisplayName: "Growth",
    trialStartUtc: null,
    trialEndUtc: null,
    paidPeriodStartUtc: null,
    paidPeriodEndUtc: null,
    currentPeriodStartUtc: null,
    currentPeriodEndUtc: null,
    gracePeriodEndUtc: null,
    renewalDateUtc: null,
    suspendedAtUtc: null,
    pastDueAtUtc: null,
    cancelledAtUtc: null,
    expiredAtUtc: null,
    ...overrides,
  };
}

describe("parseSubscriptionTab", () => {
  it("falls back to overview for unknown or missing tabs", () => {
    expect(parseSubscriptionTab(null)).toBe("overview");
    expect(parseSubscriptionTab("nope")).toBe("overview");
    expect(parseSubscriptionTab("BILLING")).toBe("billing");
    expect(parseSubscriptionTab("plan")).toBe("plan");
    expect(parseSubscriptionTab("invoices")).toBe("invoices");
  });
});

describe("classifySubscriptionStatus", () => {
  it("maps Platform subscription statuses", () => {
    expect(classifySubscriptionStatus("Active")).toBe("active");
    expect(classifySubscriptionStatus("Trialing")).toBe("trial");
    expect(classifySubscriptionStatus("PastDue")).toBe("pastDue");
    expect(classifySubscriptionStatus("Past Due")).toBe("pastDue");
    expect(classifySubscriptionStatus("Suspended")).toBe("suspended");
    expect(classifySubscriptionStatus("Expired")).toBe("ended");
    expect(classifySubscriptionStatus(null)).toBe("unknown");
  });

  it("tones unpaid states as warning/danger", () => {
    expect(resolveSubscriptionStatusTone("Active")).toBe("success");
    expect(resolveSubscriptionStatusTone("PastDue")).toBe("warning");
    expect(resolveSubscriptionStatusTone("Suspended")).toBe("danger");
  });
});

describe("buildCapacityUsage", () => {
  it("skips dimensions without a positive plan allowance", () => {
    const rows = buildCapacityUsage({
      branches: { used: 1, allowed: 3 },
      staff: null,
      devices: { used: 2, allowed: 0 },
    });
    expect(rows.map((row) => row.dimension)).toEqual(["branches"]);
  });

  it("flags near-limit at 80% and at-limit when used meets the allowance", () => {
    const rows = buildCapacityUsage({
      branches: { used: 3, allowed: 3 },
      staff: { used: 8, allowed: 10 },
      devices: { used: 1, allowed: 4 },
    });

    expect(rows.find((row) => row.dimension === "branches")).toMatchObject({
      percent: 100,
      atLimit: true,
      nearLimit: false,
    });
    expect(rows.find((row) => row.dimension === "staff")).toMatchObject({
      percent: 80,
      atLimit: false,
      nearLimit: true,
    });
    expect(rows.find((row) => row.dimension === "devices")).toMatchObject({
      atLimit: false,
      nearLimit: false,
    });
    expect(capacityAtLimitDimensions(rows)).toEqual(["branches"]);
  });
});

describe("plan comparison", () => {
  it("reads feature availability straight from the plan payload", () => {
    expect(buildPlanFeatureRows(plan())).toEqual([
      { id: "customerCredit", included: true },
      { id: "advancedReports", included: false },
      { id: "export", included: false },
    ]);
    expect(buildPlanFeatureRows(null)).toEqual([]);
  });

  it("classifies upgrade and downgrade by plan order", () => {
    const current = plan({ sortOrder: 20 });
    expect(comparePlanTier(current, plan({ id: "pro", sortOrder: 30 }))).toBe("upgrade");
    expect(comparePlanTier(current, plan({ id: "starter", sortOrder: 10 }))).toBe("downgrade");
    expect(comparePlanTier(current, current)).toBe("same");
    expect(comparePlanTier(null, current)).toBe("unknown");
  });

  it("excludes the current plan from the selectable list", () => {
    const current = plan();
    const pro = plan({ id: "pro-id", planKey: "pro", displayName: "Pro", sortOrder: 30 });
    expect(selectableAvailablePlans([current, pro], current).map((p) => p.planKey)).toEqual([
      "pro",
    ]);
  });
});

describe("resolveNextPaymentDate", () => {
  it("prefers renewal, then period end, then trial end", () => {
    expect(
      resolveNextPaymentDate(
        subscription({
          renewalDateUtc: "2027-02-01T00:00:00Z",
          currentPeriodEndUtc: "2026-12-01T00:00:00Z",
        }),
      ),
    ).toBe("2027-02-01T00:00:00Z");

    expect(
      resolveNextPaymentDate(subscription({ trialEndUtc: "2026-03-01T00:00:00Z" })),
    ).toBe("2026-03-01T00:00:00Z");

    expect(resolveNextPaymentDate(null)).toBeNull();
  });
});

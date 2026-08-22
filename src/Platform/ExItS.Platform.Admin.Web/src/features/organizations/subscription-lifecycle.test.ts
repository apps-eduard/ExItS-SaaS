import { describe, expect, it } from "vitest";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import {
  isPinoyBusinessPosSubscription,
  organizationHasPinoyBusinessPosSubscription,
  planChangeDirection,
  publishedPlanVersionId,
  subscriptionLifecycleCapabilities,
  trialEligiblePlans,
} from "@/features/organizations/subscription-lifecycle";

const growth: CatalogPlan = {
  id: "growth-id",
  productCode: "pinoy-business-pos",
  code: "growth",
  displayName: "Growth",
  status: "Active",
  trialAllowed: true,
  maxActivePosDevices: 3,
  monthlyPrice: 699,
  sortOrder: 20,
};

const pro: CatalogPlan = {
  id: "pro-id",
  productCode: "pinoy-business-pos",
  code: "pro",
  displayName: "Pro",
  status: "Active",
  trialAllowed: false,
  maxActivePosDevices: 10,
  monthlyPrice: 1499,
  sortOrder: 30,
};

describe("subscriptionLifecycleCapabilities", () => {
  it("does not invent a NoSubscription status", () => {
    expect(subscriptionLifecycleCapabilities("")).toMatchObject({
      changePlan: false,
      suspend: false,
      reactivate: false,
      cancel: false,
    });
  });

  it("allows trial/active plan change, suspend, and cancel", () => {
    expect(subscriptionLifecycleCapabilities("Trialing").changePlan).toBe(true);
    expect(subscriptionLifecycleCapabilities("Active").suspend).toBe(true);
    expect(subscriptionLifecycleCapabilities("Active").cancel).toBe(true);
    expect(subscriptionLifecycleCapabilities("Active").reactivate).toBe(false);
  });

  it("shows reactivate only for Suspended", () => {
    expect(subscriptionLifecycleCapabilities("Suspended").reactivate).toBe(true);
    expect(subscriptionLifecycleCapabilities("Suspended").suspend).toBe(false);
    expect(subscriptionLifecycleCapabilities("Suspended").changePlan).toBe(false);
    expect(subscriptionLifecycleCapabilities("Cancelled").reactivate).toBe(false);
    expect(subscriptionLifecycleCapabilities("Expired").reactivate).toBe(false);
  });

  it("exposes apply pending when a pending plan exists", () => {
    expect(subscriptionLifecycleCapabilities("Active", "pending-plan").applyPending).toBe(true);
    expect(subscriptionLifecycleCapabilities("Active").applyPending).toBe(false);
  });

  it("keeps grace/past-due/expire as support actions where the state machine allows them", () => {
    expect(subscriptionLifecycleCapabilities("Active").supportActions).toEqual([
      "gracePeriod",
      "pastDue",
      "expire",
    ]);
    expect(subscriptionLifecycleCapabilities("Suspended").supportActions).toEqual(["expire"]);
    expect(subscriptionLifecycleCapabilities("Cancelled").supportActions).toEqual([]);
  });
});

describe("plan helpers", () => {
  it("identifies Pinoy Business POS without treating other products as POS", () => {
    expect(
      isPinoyBusinessPosSubscription({
        productCode: "pinoy-business-pos",
        productDisplayName: "Pinoy Business POS",
      }),
    ).toBe(true);
    expect(isPinoyBusinessPosSubscription({ productCode: "other-product" })).toBe(false);
    expect(
      organizationHasPinoyBusinessPosSubscription([{ productCode: "other-product" }]),
    ).toBe(false);
  });

  it("filters trial-eligible active plans and prefers published versions", () => {
    expect(trialEligiblePlans([growth, pro]).map((plan) => plan.code)).toEqual(["growth"]);
    expect(
      publishedPlanVersionId([
        { id: "draft", status: "Draft" },
        { id: "live", status: "Published" },
      ]),
    ).toBe("live");
  });

  it("classifies upgrade vs scheduled downgrade from catalog device limits", () => {
    expect(planChangeDirection(growth, pro)).toBe("upgrade");
    expect(planChangeDirection(pro, growth)).toBe("downgrade");
    expect(planChangeDirection(growth, growth)).toBe("same");
  });
});

import { describe, expect, it } from "vitest";
import { resolvePlanSubscriptionChipVariant } from "@/features/admin/plan-subscription-chip";

describe("resolvePlanSubscriptionChipVariant", () => {
  it("maps catalog plan keys to distinct variants", () => {
    expect(resolvePlanSubscriptionChipVariant("starter")).toBe("starter");
    expect(resolvePlanSubscriptionChipVariant("growth")).toBe("growth");
    expect(resolvePlanSubscriptionChipVariant("pro")).toBe("pro");
    expect(resolvePlanSubscriptionChipVariant("pro-plus")).toBe("pro-plus");
    expect(resolvePlanSubscriptionChipVariant("pro+")).toBe("pro-plus");
  });

  it("falls back to display name when planKey is missing", () => {
    expect(resolvePlanSubscriptionChipVariant(null, "Pro+")).toBe("pro-plus");
    expect(resolvePlanSubscriptionChipVariant(undefined, "Growth")).toBe("growth");
  });

  it("uses other for unknown plans", () => {
    expect(resolvePlanSubscriptionChipVariant("enterprise-custom")).toBe("other");
  });
});

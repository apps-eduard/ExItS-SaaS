import { describe, expect, it } from "vitest";
import { resolveFulfillmentToggle } from "@/features/branches/fulfillment-toggle";

describe("BranchFulfillmentListPage toggle rules", () => {
  it("keeps enable disabled when pickup is not ready", () => {
    const decision = resolveFulfillmentToggle({
      channel: "pickup",
      enabled: false,
      ready: false,
      canUseDelivery: true,
    });
    expect(decision.disabled).toBe(true);
    expect(decision.hintKey).toBe("branches.toggle.completeSetupFirst");
  });

  it("keeps enable disabled when delivery is not ready", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: false,
      ready: false,
      canUseDelivery: true,
    });
    expect(decision.disabled).toBe(true);
    expect(decision.hintKey).toBe("branches.toggle.completeSetupFirst");
  });

  it("shows plan hint when delivery entitlement is missing", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: false,
      ready: true,
      canUseDelivery: false,
    });
    expect(decision.disabled).toBe(true);
    expect(decision.hintKey).toBe("branches.toggle.deliveryNotInPlan");
  });

  it("still allows turning delivery off without entitlement", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: true,
      ready: false,
      canUseDelivery: false,
    });
    expect(decision.checked).toBe(true);
    expect(decision.disabled).toBe(false);
  });
});

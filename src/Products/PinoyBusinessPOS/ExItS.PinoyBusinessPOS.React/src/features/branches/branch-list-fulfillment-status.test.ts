import { describe, expect, it } from "vitest";
import {
  resolveBranchListDeliveryStatus,
  resolveBranchListPickupStatus,
} from "@/features/branches/branch-list-fulfillment-status";

describe("resolveBranchListPickupStatus", () => {
  it("returns Off when disabled", () => {
    expect(
      resolveBranchListPickupStatus({
        pickupEnabled: false,
        pickupSectionsComplete: 2,
        pickupSectionsTotal: 2,
      }).labelKey,
    ).toBe("branches.mgmt.pickupOff");
  });

  it("returns Ready when enabled and complete", () => {
    expect(
      resolveBranchListPickupStatus({
        pickupEnabled: true,
        pickupSectionsComplete: 2,
        pickupSectionsTotal: 2,
      }).labelKey,
    ).toBe("branches.mgmt.pickupReady");
  });
});

describe("resolveBranchListDeliveryStatus", () => {
  it("shows globally paused when configured ready but Offer Delivery off", () => {
    const status = resolveBranchListDeliveryStatus({
      deliveryEnabled: true,
      deliverySectionsComplete: 5,
      deliverySectionsTotal: 5,
      orgOfferDelivery: false,
    });
    expect(status.labelKey).toBe("branches.mgmt.deliveryReadyGloballyPaused");
    expect(status.globallyPaused).toBe(true);
    expect(status.tone).toBe("warning");
  });

  it("shows Delivery Ready when Offer Delivery on and branch ready", () => {
    expect(
      resolveBranchListDeliveryStatus({
        deliveryEnabled: true,
        deliverySectionsComplete: 5,
        deliverySectionsTotal: 5,
        orgOfferDelivery: true,
      }).labelKey,
    ).toBe("branches.mgmt.deliveryReady");
  });

  it("shows Delivery Off when branch delivery disabled", () => {
    expect(
      resolveBranchListDeliveryStatus({
        deliveryEnabled: false,
        deliverySectionsComplete: 5,
        deliverySectionsTotal: 5,
        orgOfferDelivery: true,
      }).labelKey,
    ).toBe("branches.mgmt.deliveryOff");
  });
});

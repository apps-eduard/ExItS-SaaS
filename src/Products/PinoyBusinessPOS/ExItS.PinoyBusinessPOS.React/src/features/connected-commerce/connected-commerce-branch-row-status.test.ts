import { describe, expect, it } from "vitest";
import { resolveConnectedCommerceBranchRowStatus } from "@/features/connected-commerce/connected-commerce-branch-row-status";

describe("resolveConnectedCommerceBranchRowStatus", () => {
  const readyBase = {
    pickupEnabled: true,
    pickupReady: true,
    pickupSectionsComplete: 2,
    pickupSectionsTotal: 2,
    deliveryEnabled: true,
    deliveryReady: true,
    deliverySectionsComplete: 5,
    deliverySectionsTotal: 5,
    customerOrderingEnabled: false,
    customerOrderingReady: false,
    onlineOrdersPaused: false,
    orgOfferDelivery: true,
  };

  it("shows globally paused Delivery without flipping branch config", () => {
    const row = resolveConnectedCommerceBranchRowStatus({
      ...readyBase,
      orgOfferDelivery: false,
    });
    expect(row.delivery.globallyPaused).toBe(true);
    expect(row.delivery.statusKey).toBe("connectedCommerce.chip.readyGloballyPaused");
    expect(row.actionKind).toBe("configure");
  });

  it("marks needs setup when Delivery enabled but not ready", () => {
    const row = resolveConnectedCommerceBranchRowStatus({
      ...readyBase,
      deliveryReady: false,
      deliverySectionsComplete: 2,
    });
    expect(row.actionKind).toBe("completeSetup");
    expect(row.overall.statusKey).toBe("connectedCommerce.branch.statusNeedsSetup");
  });

  it("reports pickup only when Delivery is off", () => {
    const row = resolveConnectedCommerceBranchRowStatus({
      ...readyBase,
      deliveryEnabled: false,
      deliveryReady: false,
      deliverySectionsComplete: 0,
    });
    expect(row.overall.statusKey).toBe("connectedCommerce.branch.statusPickupOnly");
  });

  it("shows Online Off when ordering is disabled even if pause flag is set", () => {
    const row = resolveConnectedCommerceBranchRowStatus({
      ...readyBase,
      customerOrderingEnabled: false,
      onlineOrdersPaused: true,
    });
    expect(row.onlineOrders.statusKey).toBe("connectedCommerce.chip.off");
  });

  it("shows Online Paused when enabled and paused", () => {
    const row = resolveConnectedCommerceBranchRowStatus({
      ...readyBase,
      customerOrderingEnabled: true,
      customerOrderingReady: true,
      onlineOrdersPaused: true,
    });
    expect(row.onlineOrders.statusKey).toBe("connectedCommerce.chip.paused");
  });
});

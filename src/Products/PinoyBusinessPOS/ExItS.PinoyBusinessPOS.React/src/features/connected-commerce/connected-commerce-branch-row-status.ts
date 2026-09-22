import type { MessageKey } from "@/i18n/messages";

export type BranchFulfillmentChannelChip = {
  /** Short status shown after the channel name, e.g. Ready / Off / Setup required. */
  statusKey: MessageKey;
  tone: "success" | "warning" | "neutral";
  globallyPaused?: boolean;
};

export type BranchFulfillmentRowStatus = {
  pickup: BranchFulfillmentChannelChip;
  delivery: BranchFulfillmentChannelChip;
  onlineOrders: BranchFulfillmentChannelChip;
  overall: BranchFulfillmentChannelChip;
  actionKind: "configure" | "completeSetup";
};

/**
 * Compact Connected Commerce branch-row projection.
 * Delivery keeps configured readiness visible when Offer Delivery is globally paused.
 */
export function resolveConnectedCommerceBranchRowStatus(input: {
  pickupEnabled: boolean;
  pickupReady: boolean;
  pickupSectionsComplete: number;
  pickupSectionsTotal: number;
  deliveryEnabled: boolean;
  deliveryReady: boolean;
  deliverySectionsComplete: number;
  deliverySectionsTotal: number;
  customerOrderingEnabled: boolean;
  customerOrderingReady: boolean;
  onlineOrdersPaused: boolean;
  orgOfferDelivery: boolean;
}): BranchFulfillmentRowStatus {
  const pickupReady =
    input.pickupEnabled &&
    input.pickupSectionsTotal > 0 &&
    input.pickupSectionsComplete >= input.pickupSectionsTotal;
  const deliveryConfiguredReady =
    input.deliveryEnabled &&
    input.deliverySectionsTotal > 0 &&
    input.deliverySectionsComplete >= input.deliverySectionsTotal;

  let pickup: BranchFulfillmentChannelChip;
  if (!input.pickupEnabled) {
    pickup = { statusKey: "connectedCommerce.chip.off", tone: "neutral" };
  } else if (pickupReady) {
    pickup = { statusKey: "connectedCommerce.chip.ready", tone: "success" };
  } else {
    pickup = { statusKey: "connectedCommerce.chip.setup", tone: "warning" };
  }

  let delivery: BranchFulfillmentChannelChip;
  if (!input.deliveryEnabled) {
    delivery = { statusKey: "connectedCommerce.chip.off", tone: "neutral" };
  } else if (!input.orgOfferDelivery) {
    delivery = {
      statusKey: "connectedCommerce.chip.readyGloballyPaused",
      tone: "warning",
      globallyPaused: true,
    };
  } else if (!deliveryConfiguredReady) {
    delivery = { statusKey: "connectedCommerce.chip.setup", tone: "warning" };
  } else {
    delivery = { statusKey: "connectedCommerce.chip.ready", tone: "success" };
  }

  let onlineOrders: BranchFulfillmentChannelChip;
  if (!input.customerOrderingEnabled) {
    onlineOrders = { statusKey: "connectedCommerce.chip.off", tone: "neutral" };
  } else if (input.onlineOrdersPaused) {
    onlineOrders = { statusKey: "connectedCommerce.chip.paused", tone: "warning" };
  } else if (!input.customerOrderingReady) {
    onlineOrders = { statusKey: "connectedCommerce.chip.setup", tone: "warning" };
  } else {
    onlineOrders = { statusKey: "connectedCommerce.chip.on", tone: "success" };
  }

  const pickupNeedsSetup = input.pickupEnabled && !pickupReady;
  const deliveryNeedsSetup = input.deliveryEnabled && !deliveryConfiguredReady;
  const onlineNeedsSetup =
    input.customerOrderingEnabled &&
    !input.onlineOrdersPaused &&
    !input.customerOrderingReady;
  const needsSetup = pickupNeedsSetup || deliveryNeedsSetup || onlineNeedsSetup;

  let overall: BranchFulfillmentChannelChip;
  if (needsSetup) {
    overall = { statusKey: "connectedCommerce.branch.statusNeedsSetup", tone: "warning" };
  } else if (deliveryConfiguredReady && !input.orgOfferDelivery) {
    overall = {
      statusKey: "connectedCommerce.branch.statusDeliveryGloballyPaused",
      tone: "warning",
      globallyPaused: true,
    };
  } else if (input.pickupEnabled && !input.deliveryEnabled) {
    overall = { statusKey: "connectedCommerce.branch.statusPickupOnly", tone: "success" };
  } else if (
    (input.pickupEnabled && (input.pickupReady || pickupReady)) ||
    (deliveryConfiguredReady && input.orgOfferDelivery)
  ) {
    overall = { statusKey: "connectedCommerce.branch.statusReady", tone: "success" };
  } else {
    overall = { statusKey: "connectedCommerce.branch.statusOff", tone: "neutral" };
  }

  return {
    pickup,
    delivery,
    onlineOrders,
    overall,
    actionKind: needsSetup ? "completeSetup" : "configure",
  };
}

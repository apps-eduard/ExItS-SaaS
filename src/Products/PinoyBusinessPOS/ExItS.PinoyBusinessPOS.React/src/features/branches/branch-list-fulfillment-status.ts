import type { MessageKey } from "@/i18n/messages";

export type BranchListChannelStatus = {
  labelKey: MessageKey;
  tone: "success" | "warning" | "neutral";
  /** True when Delivery is configured ready but Offer Delivery is off. */
  globallyPaused?: boolean;
};

/**
 * Branch list presentation: configuration vs effective Delivery.
 * Never returns a plain "On" when buyers cannot use Delivery.
 */
export function resolveBranchListPickupStatus(input: {
  pickupEnabled: boolean;
  pickupSectionsComplete: number;
  pickupSectionsTotal: number;
}): BranchListChannelStatus {
  if (!input.pickupEnabled) {
    return { labelKey: "branches.mgmt.pickupOff", tone: "neutral" };
  }
  const ready =
    input.pickupSectionsTotal > 0 &&
    input.pickupSectionsComplete >= input.pickupSectionsTotal;
  if (ready) {
    return { labelKey: "branches.mgmt.pickupReady", tone: "success" };
  }
  return { labelKey: "branches.mgmt.pickupSetup", tone: "warning" };
}

export function resolveBranchListDeliveryStatus(input: {
  deliveryEnabled: boolean;
  deliverySectionsComplete: number;
  deliverySectionsTotal: number;
  orgOfferDelivery: boolean;
}): BranchListChannelStatus {
  if (!input.deliveryEnabled) {
    return { labelKey: "branches.mgmt.deliveryOff", tone: "neutral" };
  }
  const ready =
    input.deliverySectionsTotal > 0 &&
    input.deliverySectionsComplete >= input.deliverySectionsTotal;
  if (!ready) {
    return { labelKey: "branches.mgmt.deliverySetup", tone: "warning" };
  }
  if (!input.orgOfferDelivery) {
    return {
      labelKey: "branches.mgmt.deliveryReadyGloballyPaused",
      tone: "warning",
      globallyPaused: true,
    };
  }
  return { labelKey: "branches.mgmt.deliveryReady", tone: "success" };
}

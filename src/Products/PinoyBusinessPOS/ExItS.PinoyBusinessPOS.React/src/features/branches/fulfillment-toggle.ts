import type { MessageKey } from "@/i18n/messages";

export type FulfillmentChannel = "pickup" | "delivery";

export type FulfillmentToggleBlockReason = "plan" | "setup" | "orgOffer" | null;

export type FulfillmentToggleDecision = {
  checked: boolean;
  /** When true, the control cannot turn ON (OFF remains allowed when already on). */
  enableBlocked: boolean;
  blockReason: FulfillmentToggleBlockReason;
  disabled: boolean;
  hintKey: MessageKey | null;
};

/**
 * ON requires server readiness (+ delivery entitlement). OFF is always allowed.
 * Org Offer Delivery OFF blocks OFF→ON (clickable for discoverability dialog)
 * while preserving existing ON as Globally paused.
 */
export function resolveFulfillmentToggle(input: {
  channel: FulfillmentChannel;
  enabled: boolean;
  ready: boolean;
  canUseDelivery: boolean;
  pending?: boolean;
  /** When false, Delivery OFF→ON is blocked until Offer Delivery is on. */
  orgOfferDelivery?: boolean;
}): FulfillmentToggleDecision {
  const checked = input.enabled;
  if (input.channel === "delivery" && !input.canUseDelivery) {
    return {
      checked,
      enableBlocked: true,
      blockReason: "plan",
      disabled: !checked || Boolean(input.pending),
      hintKey: "branches.toggle.deliveryNotInPlan",
    };
  }
  if (input.channel === "delivery" && input.orgOfferDelivery === false && !checked) {
    return {
      checked: false,
      enableBlocked: true,
      blockReason: "orgOffer",
      // Keep clickable so the UI can show the explanatory dialog.
      disabled: Boolean(input.pending),
      hintKey: "branches.toggle.enableOfferDeliveryFirst",
    };
  }
  if (!checked && !input.ready) {
    return {
      checked: false,
      enableBlocked: true,
      blockReason: "setup",
      disabled: true,
      hintKey: "branches.toggle.completeSetupFirst",
    };
  }
  return {
    checked,
    enableBlocked: false,
    blockReason: null,
    disabled: Boolean(input.pending),
    hintKey: null,
  };
}

import type { MessageKey } from "@/i18n/messages";

export type FulfillmentChannel = "pickup" | "delivery";

export type FulfillmentToggleDecision = {
  checked: boolean;
  /** When true, the control cannot turn ON (OFF remains allowed when already on). */
  enableBlocked: boolean;
  disabled: boolean;
  hintKey: MessageKey | null;
};

/**
 * ON requires server readiness (+ delivery entitlement). OFF is always allowed.
 */
export function resolveFulfillmentToggle(input: {
  channel: FulfillmentChannel;
  enabled: boolean;
  ready: boolean;
  canUseDelivery: boolean;
  pending?: boolean;
}): FulfillmentToggleDecision {
  const checked = input.enabled;
  if (input.channel === "delivery" && !input.canUseDelivery) {
    return {
      checked,
      enableBlocked: true,
      disabled: !checked || Boolean(input.pending),
      hintKey: "branches.toggle.deliveryNotInPlan",
    };
  }
  if (!checked && !input.ready) {
    return {
      checked: false,
      enableBlocked: true,
      disabled: true,
      hintKey: "branches.toggle.completeSetupFirst",
    };
  }
  return {
    checked,
    enableBlocked: false,
    disabled: Boolean(input.pending),
    hintKey: null,
  };
}

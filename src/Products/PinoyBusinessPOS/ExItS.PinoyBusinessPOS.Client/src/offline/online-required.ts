import type { MessageKey } from "@/i18n/messages";

/**
 * Capabilities that stay online-only in RMAP-21D.
 * Each one is blocked because the server owns the decision, not because the client is lazy:
 * GCash needs a provider reference, Utang needs a live credit decision, discounts and price
 * overrides need server money math plus a capability check, opening a shift and registering a
 * device are authorization acts.
 */
export const ONLINE_REQUIRED_CODES = {
  GCashCheckout: "online_required.gcash_checkout",
  UtangCheckout: "online_required.utang_checkout",
  CommercialDiscount: "online_required.commercial_discount",
  PriceOverride: "online_required.price_override",
  OpenShift: "online_required.open_shift",
  DeviceRegister: "online_required.device_register",
} as const;

export type OnlineRequiredCode = (typeof ONLINE_REQUIRED_CODES)[keyof typeof ONLINE_REQUIRED_CODES];

const DETAIL_KEYS: Record<OnlineRequiredCode, MessageKey> = {
  [ONLINE_REQUIRED_CODES.GCashCheckout]: "offline.requiredGCash",
  [ONLINE_REQUIRED_CODES.UtangCheckout]: "offline.requiredUtang",
  [ONLINE_REQUIRED_CODES.CommercialDiscount]: "offline.requiredDiscount",
  [ONLINE_REQUIRED_CODES.PriceOverride]: "offline.requiredPriceOverride",
  [ONLINE_REQUIRED_CODES.OpenShift]: "offline.requiredOpenShift",
  [ONLINE_REQUIRED_CODES.DeviceRegister]: "offline.requiredDeviceRegister",
};

export function onlineRequiredDetailKey(code: OnlineRequiredCode): MessageKey {
  return DETAIL_KEYS[code];
}

export function isOnlineRequiredCode(value: string): value is OnlineRequiredCode {
  return Object.hasOwn(DETAIL_KEYS, value);
}

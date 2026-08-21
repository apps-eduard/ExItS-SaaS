import type { MessageKey } from "@/i18n/messages";

/**
 * Capabilities that stay online-only.
 * Each one is blocked because the server owns the decision, not because the client is lazy:
 * GCash needs a provider reference, Utang needs a live credit decision, discounts and price
 * overrides need server money math plus a capability check, opening a shift and registering a
 * device are authorization acts.
 *
 * RMAP-21E adds the customer-credit decisions that cannot be approximated on a device: extending
 * credit and reversing money need the live balance, changing a customer's active status is an
 * authorization act, a statement is a server-computed document, and linking a Business customer
 * to an ExItS Personal or Organization identity must never happen silently offline.
 */
export const ONLINE_REQUIRED_CODES = {
  GCashCheckout: "online_required.gcash_checkout",
  UtangCheckout: "online_required.utang_checkout",
  CommercialDiscount: "online_required.commercial_discount",
  PriceOverride: "online_required.price_override",
  OpenShift: "online_required.open_shift",
  DeviceRegister: "online_required.device_register",
  CreditExtend: "online_required.credit_extend",
  CreditReverse: "online_required.credit_reverse",
  CustomerStatus: "online_required.customer_status",
  CustomerStatement: "online_required.customer_statement",
  CustomerIdentityLink: "online_required.customer_identity_link",
} as const;

export type OnlineRequiredCode = (typeof ONLINE_REQUIRED_CODES)[keyof typeof ONLINE_REQUIRED_CODES];

const DETAIL_KEYS: Record<OnlineRequiredCode, MessageKey> = {
  [ONLINE_REQUIRED_CODES.GCashCheckout]: "offline.requiredGCash",
  [ONLINE_REQUIRED_CODES.UtangCheckout]: "offline.requiredUtang",
  [ONLINE_REQUIRED_CODES.CommercialDiscount]: "offline.requiredDiscount",
  [ONLINE_REQUIRED_CODES.PriceOverride]: "offline.requiredPriceOverride",
  [ONLINE_REQUIRED_CODES.OpenShift]: "offline.requiredOpenShift",
  [ONLINE_REQUIRED_CODES.DeviceRegister]: "offline.requiredDeviceRegister",
  [ONLINE_REQUIRED_CODES.CreditExtend]: "offline.requiredCreditExtend",
  [ONLINE_REQUIRED_CODES.CreditReverse]: "offline.requiredCreditReverse",
  [ONLINE_REQUIRED_CODES.CustomerStatus]: "offline.requiredCustomerStatus",
  [ONLINE_REQUIRED_CODES.CustomerStatement]: "offline.requiredCustomerStatement",
  [ONLINE_REQUIRED_CODES.CustomerIdentityLink]: "offline.requiredCustomerLink",
};

export function onlineRequiredDetailKey(code: OnlineRequiredCode): MessageKey {
  return DETAIL_KEYS[code];
}

export function isOnlineRequiredCode(value: string): value is OnlineRequiredCode {
  return Object.hasOwn(DETAIL_KEYS, value);
}

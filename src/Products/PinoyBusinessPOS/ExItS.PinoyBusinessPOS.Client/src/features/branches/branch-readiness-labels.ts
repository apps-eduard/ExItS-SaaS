import type { MessageKey } from "@/i18n/messages";

/** Server missingRequirements / reasonCodes → i18n keys (do not invent readiness rules). */
const REQUIREMENT_KEYS: Record<string, MessageKey> = {
  timezone: "branches.missing.timezone",
  branch_active: "branches.missing.branchActive",
  branch_address: "branches.missing.branchAddress",
  store_hours: "branches.missing.storeHours",
  store_contact: "branches.missing.storeContact",
  ordering_entitlement: "branches.missing.orderingEntitlement",
  delivery_entitlement: "branches.missing.deliveryEntitlement",
  map_location: "branches.missing.mapLocation",
  delivery_policy: "branches.missing.deliveryPolicy",
};

const REASON_KEYS: Record<string, MessageKey> = {
  branch_inactive: "branches.reason.branchInactive",
  branch_address_incomplete: "branches.reason.branchAddressIncomplete",
  timezone_missing: "branches.reason.timezoneMissing",
  store_hours_missing: "branches.reason.storeHoursMissing",
  store_hours_invalid: "branches.reason.storeHoursInvalid",
  store_contact_missing: "branches.reason.storeContactMissing",
  ordering_entitlement_missing: "branches.reason.orderingEntitlementMissing",
  delivery_entitlement_missing: "branches.reason.deliveryEntitlementMissing",
  customer_ordering_disabled: "branches.reason.customerOrderingDisabled",
  pickup_disabled: "branches.reason.pickupDisabled",
  delivery_disabled: "branches.reason.deliveryDisabled",
  map_location_missing: "branches.reason.mapLocationMissing",
  delivery_policy_missing: "branches.reason.deliveryPolicyMissing",
  delivery_policy_incomplete: "branches.reason.deliveryPolicyIncomplete",
  online_orders_paused: "branches.reason.onlineOrdersPaused",
  store_closed: "branches.reason.storeClosed",
};

export function missingRequirementMessageKey(code: string): MessageKey {
  return REQUIREMENT_KEYS[code] ?? "branches.missing.unknown";
}

export function reasonCodeMessageKey(code: string): MessageKey {
  return REASON_KEYS[code] ?? "branches.reason.unknown";
}

export type EnablementLabel = "enabled" | "disabled" | "paused" | "notReady";

export function pickupEnablementLabel(input: {
  pickupEnabled: boolean;
  pickupReady: boolean;
}): EnablementLabel {
  if (input.pickupEnabled) {
    return "enabled";
  }
  if (!input.pickupReady) {
    return "notReady";
  }
  return "disabled";
}

export function deliveryEnablementLabel(input: {
  deliveryEnabled: boolean;
  deliveryReady: boolean;
}): EnablementLabel {
  if (input.deliveryEnabled) {
    return "enabled";
  }
  if (!input.deliveryReady) {
    return "notReady";
  }
  return "disabled";
}

export function orderingEnablementLabel(input: {
  customerOrderingEnabled: boolean;
  customerOrderingReady: boolean;
  onlineOrdersPaused: boolean;
}): EnablementLabel {
  if (input.onlineOrdersPaused) {
    return "paused";
  }
  if (input.customerOrderingEnabled) {
    return "enabled";
  }
  if (!input.customerOrderingReady) {
    return "notReady";
  }
  return "disabled";
}

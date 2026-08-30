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
  delivery_area: "branches.missing.deliveryArea",
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
  delivery_area_missing: "branches.reason.deliveryAreaMissing",
  online_orders_paused: "branches.reason.onlineOrdersPaused",
  store_closed: "branches.reason.storeClosed",
};

/** Reason codes already explained by a matching missingRequirements entry. */
const REQUIREMENT_COVERED_REASONS: Record<string, readonly string[]> = {
  timezone: ["timezone_missing"],
  branch_active: ["branch_inactive"],
  branch_address: ["branch_address_incomplete"],
  store_hours: ["store_hours_missing", "store_hours_invalid"],
  store_contact: ["store_contact_missing"],
  ordering_entitlement: ["ordering_entitlement_missing"],
  delivery_entitlement: ["delivery_entitlement_missing"],
  map_location: ["map_location_missing"],
  delivery_policy: ["delivery_policy_missing", "delivery_policy_incomplete"],
  delivery_area: ["delivery_area_missing"],
};

export function missingRequirementMessageKey(code: string): MessageKey {
  return REQUIREMENT_KEYS[code] ?? "branches.missing.unknown";
}

export function reasonCodeMessageKey(code: string): MessageKey {
  return REASON_KEYS[code] ?? "branches.reason.unknown";
}

/**
 * Keep reason codes that add new information beyond missingRequirements
 * (e.g. pickup_disabled), dropping duplicates like timezone + timezone_missing.
 */
export function filterRedundantReasonCodes(
  missingRequirements: readonly string[],
  reasonCodes: readonly string[],
): string[] {
  const covered = new Set<string>();
  for (const requirement of missingRequirements) {
    for (const reason of REQUIREMENT_COVERED_REASONS[requirement] ?? []) {
      covered.add(reason);
    }
  }
  return reasonCodes.filter((code) => !covered.has(code));
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

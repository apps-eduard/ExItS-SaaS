/**
 * Pure Needs Attention aggregation for the org navbar alert control.
 * Counts unresolved actionable conditions only — never invents metrics.
 * Bell / operational notifications stay separate.
 */

import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import type { MessageKey } from "@/i18n/messages";

export type NeedsAttentionGroupId =
  | "inventory"
  | "connectedCommerce"
  | "branchFulfillment"
  | "creditConfiguration";

export type NeedsAttentionAlertKind =
  | "lowStock"
  | "outOfStock"
  | "expiringSoon"
  | "supplierReadiness"
  | "paymentSetup"
  | "creditSetup"
  | "deliveryReadiness"
  | "pickupReadiness"
  | "missingFulfillment"
  | "branchInfoIncomplete";

export type NeedsAttentionAlert = {
  id: string;
  kind: NeedsAttentionAlertKind;
  group: NeedsAttentionGroupId;
  /** Aggregated count shown in the row title (products, lots, connections). */
  count: number;
  href: string;
  titleKey: MessageKey;
  reasonKey: MessageKey;
  testId: string;
};

export type NeedsAttentionGroup = {
  id: NeedsAttentionGroupId;
  labelKey: MessageKey;
  alerts: NeedsAttentionAlert[];
};

export const NEEDS_ATTENTION_GROUP_ORDER: NeedsAttentionGroupId[] = [
  "inventory",
  "connectedCommerce",
  "branchFulfillment",
  "creditConfiguration",
];

export const NEEDS_ATTENTION_GROUP_LABEL_KEYS: Record<NeedsAttentionGroupId, MessageKey> = {
  inventory: "shell.needsAttention.group.inventory",
  connectedCommerce: "shell.needsAttention.group.connectedCommerce",
  branchFulfillment: "shell.needsAttention.group.branchFulfillment",
  creditConfiguration: "shell.needsAttention.group.creditConfiguration",
};

/** PO payment methods used by connected-supplier commerce readiness. */
export const NEEDS_ATTENTION_PO_PAYMENT_CODES = [
  "Cash",
  "BankTransfer",
  "ManualGCash",
  "Utang",
] as const;

export type NeedsAttentionInventoryInputs = {
  lowStockProductCount?: number | null;
  outOfStockProductCount?: number | null;
  expiredLotCount?: number | null;
  nearExpiryLotCount?: number | null;
};

export type NeedsAttentionCommerceInputs = {
  /** Active connections that fail supplier commerce readiness. */
  incompleteSupplierConnectionIds?: string[];
  /** True when org has no enabled entitled PO payment method. */
  paymentSetupIncomplete?: boolean;
  /** Active connections missing credit policy while Utang applies. */
  creditIncompleteConnectionIds?: string[];
};

export type NeedsAttentionBranchInputs = {
  branchId?: string | null;
  deliveryEnabled?: boolean;
  pickupEnabled?: boolean;
  deliveryReady?: boolean;
  pickupReady?: boolean;
  customerOrderingEnabled?: boolean;
  /**
   * Org may act as a connected supplier (B2B seller). When true, missing Delivery/Pickup
   * methods are actionable even if B2C customer ordering is still off.
   */
  supplierCommerceEligible?: boolean;
  /** Organization Offer Delivery switch (default OFF). Gates Delivery readiness alerts. */
  orgOfferDelivery?: boolean;
  branchDetailsComplete?: boolean;
  deliveryLocationComplete?: boolean;
  deliveryPolicyComplete?: boolean;
  deliveryAreasComplete?: boolean;
};

export type NeedsAttentionInputs = {
  inventory?: NeedsAttentionInventoryInputs | null;
  commerce?: NeedsAttentionCommerceInputs | null;
  branch?: NeedsAttentionBranchInputs | null;
};

function positive(value: number | null | undefined): number {
  return Math.max(0, value ?? 0);
}

function businessCustomerHref(connectionId: string): string {
  return `/customers/business/${connectionId}`;
}

function businessesListHref(): string {
  return "/customers?kind=businesses";
}

function resolveSupplierHref(connectionIds: string[]): string {
  if (connectionIds.length === 1) {
    return businessCustomerHref(connectionIds[0]!);
  }
  return businessesListHref();
}

function resolveDeliveryHref(branchId: string, branch: NeedsAttentionBranchInputs): string {
  if (branch.deliveryLocationComplete === false) {
    return branchFulfillmentEditPath(branchId, "location");
  }
  if (branch.deliveryPolicyComplete === false) {
    return branchFulfillmentEditPath(branchId, "policy");
  }
  if (branch.deliveryAreasComplete === false) {
    return branchFulfillmentEditPath(branchId, "areas");
  }
  return branchFulfillmentEditPath(branchId, "overview");
}

function resolvePickupHref(branchId: string, branch: NeedsAttentionBranchInputs): string {
  if (branch.deliveryLocationComplete === false) {
    return branchFulfillmentEditPath(branchId, "location");
  }
  return branchFulfillmentEditPath(branchId, "overview");
}

export function buildNeedsAttentionAlerts(inputs: NeedsAttentionInputs): NeedsAttentionAlert[] {
  const alerts: NeedsAttentionAlert[] = [];

  const inventory = inputs.inventory;
  if (inventory) {
    const lowStock = positive(inventory.lowStockProductCount);
    if (lowStock > 0) {
      alerts.push({
        id: "inventory-low-stock",
        kind: "lowStock",
        group: "inventory",
        count: lowStock,
        href: "/inventory?lowStock=1",
        titleKey: "shell.needsAttention.lowStock",
        reasonKey: "shell.needsAttention.lowStockReason",
        testId: "needs-attention-low-stock",
      });
    }

    const outOfStock = positive(inventory.outOfStockProductCount);
    if (outOfStock > 0) {
      alerts.push({
        id: "inventory-out-of-stock",
        kind: "outOfStock",
        group: "inventory",
        count: outOfStock,
        href: "/inventory?stockStatus=OutOfStock",
        titleKey: "shell.needsAttention.outOfStock",
        reasonKey: "shell.needsAttention.outOfStockReason",
        testId: "needs-attention-out-of-stock",
      });
    }

    const expiry =
      positive(inventory.expiredLotCount) + positive(inventory.nearExpiryLotCount);
    if (expiry > 0) {
      alerts.push({
        id: "inventory-expiring",
        kind: "expiringSoon",
        group: "inventory",
        count: expiry,
        href: "/inventory/expiration",
        titleKey: "shell.needsAttention.expiringSoon",
        reasonKey: "shell.needsAttention.expiringSoonReason",
        testId: "needs-attention-expiring",
      });
    }
  }

  const commerce = inputs.commerce;
  if (commerce) {
    const incomplete = (commerce.incompleteSupplierConnectionIds ?? []).filter(Boolean);
    if (incomplete.length > 0) {
      alerts.push({
        id: "commerce-supplier-readiness",
        kind: "supplierReadiness",
        group: "connectedCommerce",
        count: incomplete.length,
        href: resolveSupplierHref(incomplete),
        titleKey: "shell.needsAttention.supplierReadiness",
        reasonKey: "shell.needsAttention.supplierReadinessReason",
        testId: "needs-attention-supplier-readiness",
      });
    }

    if (commerce.paymentSetupIncomplete) {
      alerts.push({
        id: "commerce-payment-setup",
        kind: "paymentSetup",
        group: "connectedCommerce",
        count: 1,
        href: "/org/payment-methods",
        titleKey: "shell.needsAttention.paymentSetup",
        reasonKey: "shell.needsAttention.paymentSetupReason",
        testId: "needs-attention-payment-setup",
      });
    }

    const creditIds = (commerce.creditIncompleteConnectionIds ?? []).filter(Boolean);
    if (creditIds.length > 0) {
      alerts.push({
        id: "credit-setup",
        kind: "creditSetup",
        group: "creditConfiguration",
        count: creditIds.length,
        href: resolveSupplierHref(creditIds),
        titleKey: "shell.needsAttention.creditSetup",
        reasonKey: "shell.needsAttention.creditSetupReason",
        testId: "needs-attention-credit-setup",
      });
    }
  }

  const branch = inputs.branch;
  const branchId = branch?.branchId?.trim();
  if (branch && branchId) {
    const orgOfferDelivery = branch.orgOfferDelivery === true;

    // Delivery gaps alert only when organization Offer Delivery is ON.
    // Customer-level Block must never create Needs Attention here.
    if (orgOfferDelivery && branch.deliveryReady === false) {
      alerts.push({
        id: "branch-delivery",
        kind: "deliveryReadiness",
        group: "branchFulfillment",
        count: 1,
        href: resolveDeliveryHref(branchId, branch),
        titleKey: "shell.needsAttention.deliveryReadiness",
        reasonKey: "shell.needsAttention.deliveryReadinessReason",
        testId: "needs-attention-delivery",
      });
    }

    if (branch.pickupEnabled && branch.pickupReady === false) {
      alerts.push({
        id: "branch-pickup",
        kind: "pickupReadiness",
        group: "branchFulfillment",
        count: 1,
        href: resolvePickupHref(branchId, branch),
        titleKey: "shell.needsAttention.pickupReadiness",
        reasonKey: "shell.needsAttention.pickupReadinessReason",
        testId: "needs-attention-pickup",
      });
    }

    // Delivery/Pickup detail gaps only when that method is offered.
    // Missing method itself is actionable for B2C ordering OR connected-supplier eligibility.
    const requiresFulfillmentMethod =
      Boolean(branch.customerOrderingEnabled) || Boolean(branch.supplierCommerceEligible);
    if (requiresFulfillmentMethod && !branch.pickupEnabled && !orgOfferDelivery) {
      alerts.push({
        id: "branch-missing-fulfillment",
        kind: "missingFulfillment",
        group: "branchFulfillment",
        count: 1,
        href: branchFulfillmentEditPath(branchId, "overview"),
        titleKey: "shell.needsAttention.missingFulfillment",
        reasonKey: "shell.needsAttention.missingFulfillmentReason",
        testId: "needs-attention-missing-fulfillment",
      });
    }

    if (branch.branchDetailsComplete === false) {
      alerts.push({
        id: "branch-info",
        kind: "branchInfoIncomplete",
        group: "branchFulfillment",
        count: 1,
        href: branchFulfillmentEditPath(branchId, "details"),
        titleKey: "shell.needsAttention.branchInfo",
        reasonKey: "shell.needsAttention.branchInfoReason",
        testId: "needs-attention-branch-info",
      });
    }
  }

  return alerts;
}

export function groupNeedsAttentionAlerts(
  alerts: NeedsAttentionAlert[],
): NeedsAttentionGroup[] {
  const byGroup = new Map<NeedsAttentionGroupId, NeedsAttentionAlert[]>();
  for (const alert of alerts) {
    const list = byGroup.get(alert.group) ?? [];
    list.push(alert);
    byGroup.set(alert.group, list);
  }

  return NEEDS_ATTENTION_GROUP_ORDER.filter((id) => (byGroup.get(id)?.length ?? 0) > 0).map(
    (id) => ({
      id,
      labelKey: NEEDS_ATTENTION_GROUP_LABEL_KEYS[id],
      alerts: byGroup.get(id) ?? [],
    }),
  );
}

/** Badge text for the navbar icon — blank when zero. Caps at 9+ like notifications. */
export function formatNeedsAttentionBadge(alertCount: number): string | null {
  if (alertCount <= 0) {
    return null;
  }
  if (alertCount > 9) {
    return "9+";
  }
  return String(alertCount);
}

export function hasEnabledPoPaymentMethod(
  methods: ReadonlyArray<{
    methodCode: string;
    entitled: boolean;
    isEnabled: boolean;
    comingSoon: boolean;
  }>,
): boolean {
  return methods.some((method) => {
    if (method.comingSoon || !method.entitled || !method.isEnabled) {
      return false;
    }
    return NEEDS_ATTENTION_PO_PAYMENT_CODES.some(
      (code) => code.localeCompare(method.methodCode, undefined, { sensitivity: "accent" }) === 0,
    );
  });
}

export function requirementIsMissing(
  requirements: ReadonlyArray<{ code: string; status: string }> | null | undefined,
  code: string,
): boolean {
  return (requirements ?? []).some(
    (item) =>
      item.code.localeCompare(code, undefined, { sensitivity: "accent" }) === 0 &&
      item.status.localeCompare("Missing", undefined, { sensitivity: "accent" }) === 0,
  );
}

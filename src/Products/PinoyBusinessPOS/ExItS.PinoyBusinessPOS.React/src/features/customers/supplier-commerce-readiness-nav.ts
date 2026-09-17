/**
 * Supplier-side deep-links and filters for commerce readiness checklist.
 * Does not re-evaluate readiness — only navigates from existing requirement rows.
 */

import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";

export const SUPPLIER_COMMERCE_READINESS_FILTERS = [
  "needsSetup",
  "complete",
  "all",
] as const;

export type SupplierCommerceReadinessFilter =
  (typeof SUPPLIER_COMMERCE_READINESS_FILTERS)[number];

export const DEFAULT_SUPPLIER_COMMERCE_READINESS_FILTER: SupplierCommerceReadinessFilter =
  "needsSetup";

export const BUSINESS_RELATIONSHIP_CONTACT_ANCHOR = "business-relationship-contact";
export const BUSINESS_CREDIT_POLICY_ANCHOR = "business-credit-policy-section";

export type SupplierCommerceReadinessNavRequirement = {
  code: string;
  status: string;
  actionPath?: string | null;
};

export type SupplierCommerceReadinessNavContext = {
  connectionId: string;
  supplierBranchId?: string | null;
  /** When true, contact deep-link opens the edit drawer. */
  openContactEditor?: boolean;
};

export function isVisibleSupplierCommerceRequirement(status: string): boolean {
  return status.trim() !== "NotApplicable";
}

export function isMissingSupplierCommerceRequirement(status: string): boolean {
  return status.trim() === "Missing";
}

export function isCompleteSupplierCommerceRequirement(status: string): boolean {
  return status.trim() === "Complete";
}

export function visibleSupplierCommerceRequirements<
  T extends SupplierCommerceReadinessNavRequirement,
>(requirements: readonly T[] | null | undefined): T[] {
  return (requirements ?? []).filter((item) =>
    isVisibleSupplierCommerceRequirement(item.status),
  );
}

export function countSupplierCommerceReadinessFilters(
  requirements: readonly SupplierCommerceReadinessNavRequirement[] | null | undefined,
): { needsSetup: number; complete: number; all: number } {
  const visible = visibleSupplierCommerceRequirements(requirements);
  const needsSetup = visible.filter((item) =>
    isMissingSupplierCommerceRequirement(item.status),
  ).length;
  const complete = visible.filter((item) =>
    isCompleteSupplierCommerceRequirement(item.status),
  ).length;
  return { needsSetup, complete, all: visible.length };
}

export function filterSupplierCommerceRequirements<
  T extends SupplierCommerceReadinessNavRequirement,
>(
  requirements: readonly T[] | null | undefined,
  filter: SupplierCommerceReadinessFilter,
): T[] {
  const visible = visibleSupplierCommerceRequirements(requirements);
  if (filter === "needsSetup") {
    return visible.filter((item) => isMissingSupplierCommerceRequirement(item.status));
  }
  if (filter === "complete") {
    return visible.filter((item) => isCompleteSupplierCommerceRequirement(item.status));
  }
  return visible;
}

export function firstIncompleteSupplierCommerceRequirement<
  T extends SupplierCommerceReadinessNavRequirement,
>(requirements: readonly T[] | null | undefined): T | null {
  return (
    visibleSupplierCommerceRequirements(requirements).find((item) =>
      isMissingSupplierCommerceRequirement(item.status),
    ) ?? null
  );
}

export function firstVisibleSupplierCommerceRequirement<
  T extends SupplierCommerceReadinessNavRequirement,
>(requirements: readonly T[] | null | undefined): T | null {
  return visibleSupplierCommerceRequirements(requirements)[0] ?? null;
}

function branchSettingsPath(supplierBranchId: string | null | undefined): string {
  const id = supplierBranchId?.trim();
  return id ? `/org/branches/${id}` : "/org/branches";
}

function branchFulfillmentPath(
  supplierBranchId: string | null | undefined,
  tab?: "overview" | "policy" | "location",
): string {
  const id = supplierBranchId?.trim();
  if (!id) {
    return "/org/branches";
  }
  return branchFulfillmentEditPath(id, tab ?? "overview");
}

function contactPath(connectionId: string, openEditor: boolean): string {
  const base = `/customers/business/${connectionId}`;
  const hash = `#${BUSINESS_RELATIONSHIP_CONTACT_ANCHOR}`;
  return openEditor ? `${base}?editContact=1${hash}` : `${base}${hash}`;
}

function creditPolicyPath(connectionId: string): string {
  return `/customers/business/${connectionId}#${BUSINESS_CREDIT_POLICY_ANCHOR}`;
}

/**
 * Resolve supplier checklist deep-link. Prefers connection-aware paths over
 * generic server actionPath values (which may be null or coarse).
 */
export function resolveSupplierCommerceReadinessPath(
  code: string,
  context: SupplierCommerceReadinessNavContext,
  actionPath?: string | null,
): string {
  const normalized = code.trim();
  const branchId = context.supplierBranchId;
  const openContact = context.openContactEditor === true;

  switch (normalized) {
    case "SellingBranch":
      return branchSettingsPath(branchId);
    case "FulfillmentMethod":
      return branchFulfillmentPath(branchId);
    case "DeliveryConfig":
      return branchFulfillmentPath(branchId, "policy");
    case "PickupConfig":
      // Pickup setup = branch details + hours (overview checklist), not delivery map location.
      return branchFulfillmentPath(branchId);
    case "PaymentMethods":
      return "/org/payment-methods";
    case "SharedCatalog":
      return `/suppliers/connected/buyers/${context.connectionId}/shared-products`;
    case "ResponsibleContact":
      return contactPath(context.connectionId, openContact);
    case "CreditPolicy":
      return creditPolicyPath(context.connectionId);
    default: {
      const fallback = actionPath?.trim();
      return fallback || `/customers/business/${context.connectionId}`;
    }
  }
}

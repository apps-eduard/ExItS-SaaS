/**
 * Compact customer-list identity + connection badges.
 *
 * ExItS ID on a POS row is correlation only. Connected / Pending come from the
 * org-wide Platform overlay (linked app users + pending requests) — never from
 * linkedPersonalPublicUserId alone.
 *
 * Org list never shows Blocked. Platform projects a Personal block as Unavailable
 * on the customer-detail link-status API; the overlay does not include that fact.
 */

export type CustomerListConnectionBadgeKind =
  | "no-exits"
  | "exits-id"
  | "connected"
  | "pending";

export type CustomerListConnectionOverlay = {
  connectedBusinessCustomerIds: ReadonlySet<string>;
  pendingBusinessCustomerIds: ReadonlySet<string>;
  loaded: boolean;
};

export type CustomerListConnectionInput = {
  linkedPersonalPublicUserId?: string | null;
  linkedBuyerPublicOrganizationId?: string | null;
  resolvedPersonalDisplayName?: string | null;
  platformBusinessCustomerId?: string | null;
};

export const EMPTY_CUSTOMER_LIST_CONNECTION_OVERLAY: CustomerListConnectionOverlay = {
  connectedBusinessCustomerIds: new Set(),
  pendingBusinessCustomerIds: new Set(),
  loaded: false,
};

export function normalizeCustomerLinkId(id: string | null | undefined): string | null {
  const trimmed = id?.trim();
  return trimmed ? trimmed.toLowerCase() : null;
}

export function customerHasExItsId(customer: CustomerListConnectionInput): boolean {
  return Boolean(
    customer.linkedPersonalPublicUserId?.trim() ||
      customer.linkedBuyerPublicOrganizationId?.trim() ||
      customer.resolvedPersonalDisplayName?.trim(),
  );
}

export function resolveCustomerListConnectionBadge(
  customer: CustomerListConnectionInput,
  overlay: CustomerListConnectionOverlay | null | undefined,
): CustomerListConnectionBadgeKind {
  if (!customerHasExItsId(customer)) {
    return "no-exits";
  }

  const platformId = normalizeCustomerLinkId(customer.platformBusinessCustomerId);
  if (!overlay?.loaded || !platformId) {
    return "exits-id";
  }

  if (overlay.connectedBusinessCustomerIds.has(platformId)) {
    return "connected";
  }
  if (overlay.pendingBusinessCustomerIds.has(platformId)) {
    return "pending";
  }

  return "exits-id";
}

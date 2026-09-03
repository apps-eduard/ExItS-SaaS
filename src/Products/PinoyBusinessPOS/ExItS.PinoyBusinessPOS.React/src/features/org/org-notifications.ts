import type { OrganizationInAppNotificationDto } from "@/api/platform/organization-notifications-client";
import { formatUnreadNotificationBadge } from "@/features/personal/personal-notifications";

export const ORGANIZATION_NOTIFICATIONS_QUERY_KEY = ["organization", "notifications"] as const;

export function organizationNotificationsQueryKey(
  organizationId: string,
  branchId?: string | null,
) {
  return [...ORGANIZATION_NOTIFICATIONS_QUERY_KEY, organizationId, branchId ?? "org"] as const;
}

export function countUnreadOrganizationNotifications(
  items: OrganizationInAppNotificationDto[] | null | undefined,
): number {
  if (!items?.length) {
    return 0;
  }
  return items.reduce((count, item) => count + (item.isRead ? 0 : 1), 0);
}

export { formatUnreadNotificationBadge };

/** MAUI parity: map relatedType → in-app route (no inventing destinations). */
export function resolveOrganizationNotificationHref(
  item: Pick<OrganizationInAppNotificationDto, "relatedType" | "relatedId">,
): string | null {
  const type = item.relatedType;
  const relatedId = item.relatedId;

  if (type === "SupplierConnectionAcceptedConfirmation") {
    return "/suppliers/connected/buyers";
  }
  if (type === "SupplierConnectionDeclinedConfirmation") {
    return "/suppliers/connected/requests";
  }
  if (type === "SupplierConnectionAccepted" || type === "SupplierConnectionDeclined") {
    return "/suppliers";
  }
  if (type === "SupplierConnectionRequested") {
    return "/suppliers/connected/requests";
  }

  if (type === "ConnectedPurchaseOrderSubmitted" && relatedId) {
    return `/purchasing/incoming-orders/${relatedId}`;
  }
  if (
    (type === "ConnectedPurchaseOrderWithdrawn" ||
      type === "ConnectedPurchaseOrderReceived" ||
      type === "ConnectedPurchaseOrderPartiallyReceived" ||
      type === "ConnectedPurchaseOrderReceivingIssue" ||
      type === "ConnectedPurchaseOrderChangesAccepted" ||
      type === "ConnectedPurchaseOrderChangesRejected") &&
    relatedId
  ) {
    return `/purchasing/incoming-orders/${relatedId}`;
  }

  const buyerFacing =
    type === "ConnectedPurchaseOrderAccepted" ||
    type === "ConnectedPurchaseOrderDeclined" ||
    type === "ConnectedPurchaseOrderPreparing" ||
    type === "ConnectedPurchaseOrderFulfilled" ||
    type === "ConnectedPurchaseOrderChangesProposed";
  if (buyerFacing && relatedId) {
    return `/purchasing/${relatedId}`;
  }

  if (type.toLowerCase().includes("customerlink")) {
    return "/customers";
  }

  if (type.startsWith("CustomerOrder") && relatedId) {
    return `/orders/${relatedId}`;
  }
  if (type.startsWith("CustomerOrder")) {
    return "/orders";
  }

  return null;
}

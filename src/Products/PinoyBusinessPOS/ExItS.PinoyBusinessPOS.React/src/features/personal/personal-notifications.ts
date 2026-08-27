import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";
import type { MessageKey } from "@/i18n/messages";

export const PERSONAL_NOTIFICATIONS_QUERY_KEY = ["personal", "notifications"] as const;
export const PERSONAL_NOTIFICATIONS_UNREAD_COUNT_QUERY_KEY = [
  "personal",
  "notifications",
  "unread-count",
] as const;
export const PERSONAL_NOTIFICATIONS_ARCHIVED_QUERY_KEY = [
  "personal",
  "notifications",
  "archived",
] as const;

/** Canonical English suffix written by Platform when creating customer-link notifications. */
const CUSTOMER_LINK_PREVIEW_EN_SUFFIX =
  " added you as a customer and wants to link your ExItS account.";

export function countUnreadPersonalNotifications(
  items: ReadonlyArray<{ isRead: boolean }> | null | undefined,
): number {
  if (!items?.length) {
    return 0;
  }
  return items.reduce((count, item) => count + (item.isRead ? 0 : 1), 0);
}

/** Display badge: blank for 0, exact for 1–9, "9+" for 10+. */
export function formatUnreadNotificationBadge(unreadCount: number): string | null {
  if (unreadCount <= 0) {
    return null;
  }
  if (unreadCount > 9) {
    return "9+";
  }
  return String(unreadCount);
}

function relatedTypeEquals(actual: string, expected: string): boolean {
  return actual.localeCompare(expected, undefined, { sensitivity: "accent" }) === 0;
}

/** Pull store name from the English server preview when possible. */
export function extractCustomerLinkMerchantName(preview: string): string | null {
  const trimmed = preview.trim();
  if (!trimmed) {
    return null;
  }
  const idx = trimmed.toLowerCase().lastIndexOf(CUSTOMER_LINK_PREVIEW_EN_SUFFIX.toLowerCase());
  if (idx <= 0) {
    return null;
  }
  const name = trimmed.slice(0, idx).trim();
  return name.length > 0 ? name : null;
}

export type CustomerLinkNotificationState = "pending" | "accepted" | "declined" | "unknown";

/** Resolve inbox state for a customer-link notification from pending requests and linked stores. */
export function resolveCustomerLinkNotificationState(
  relatedId: string | null | undefined,
  preview: string,
  pendingRequests: ReadonlyArray<{ id: string }>,
  linkedMerchants: ReadonlyArray<{ organizationDisplayName: string }>,
): CustomerLinkNotificationState {
  if (relatedId && pendingRequests.some((request) => request.id === relatedId)) {
    return "pending";
  }

  const merchant = extractCustomerLinkMerchantName(preview);
  if (
    merchant
    && linkedMerchants.some(
      (row) =>
        row.organizationDisplayName.localeCompare(merchant, undefined, {
          sensitivity: "accent",
        }) === 0,
    )
  ) {
    return "accepted";
  }

  if (relatedId) {
    return "declined";
  }

  return "unknown";
}

/**
 * Map stored (English) notification copy to the active UI locale.
 * User-authored preview text (todo title / reminder message) is kept as-is.
 */
export function localizePersonalNotification(
  item: Pick<PersonalInAppNotificationDto, "title" | "preview" | "relatedType">,
  t: (key: MessageKey) => string,
): { title: string; preview: string } {
  if (relatedTypeEquals(item.relatedType, "CustomerLinkRequest")) {
    const merchant = extractCustomerLinkMerchantName(item.preview);
    return {
      title: t("personal.social.notif.customerLinkTitle"),
      preview: merchant
        ? t("personal.social.notif.customerLinkPreview").replace("{name}", merchant)
        : t("personal.social.notif.customerLinkPreviewGeneric"),
    };
  }

  if (relatedTypeEquals(item.relatedType, "PersonalTodo")) {
    return {
      title: t("personal.social.notif.todoReminderTitle"),
      preview: item.preview,
    };
  }

  if (relatedTypeEquals(item.relatedType, "PersonalDebtRelationship")) {
    return {
      title: t("personal.social.notif.utangReminderTitle"),
      preview: item.preview,
    };
  }

  if (relatedTypeEquals(item.relatedType, "PersonalConnectionRequest")) {
    return localizeNamedPreview(item, t, {
      titleKey: "notifications.connectionTitle",
      requestPreviewKey: "notifications.connectionRequestPreview",
      acceptedPreviewKey: "notifications.connectionAcceptedPreview",
    });
  }

  if (relatedTypeEquals(item.relatedType, "PersonalUtangInvitation")) {
    return localizeNamedPreview(item, t, {
      titleKey: "notifications.utangInviteTitle",
      requestPreviewKey: "notifications.utangInvitePreview",
      acceptedPreviewKey: "notifications.utangInviteAcceptedPreview",
      declinedPreviewKey: "notifications.utangInviteDeclinedPreview",
    });
  }

  return { title: item.title, preview: item.preview };
}

function localizeNamedPreview(
  item: Pick<PersonalInAppNotificationDto, "title" | "preview">,
  t: (key: MessageKey) => string,
  keys: {
    titleKey: MessageKey;
    requestPreviewKey: MessageKey;
    acceptedPreviewKey: MessageKey;
    declinedPreviewKey?: MessageKey;
  },
): { title: string; preview: string } {
  const name = extractLeadingName(item.preview);
  const lower = item.preview.toLowerCase();
  if (name && lower.includes("accepted")) {
    return {
      title: t(keys.titleKey),
      preview: t(keys.acceptedPreviewKey).replace("{name}", name),
    };
  }
  if (name && keys.declinedPreviewKey && lower.includes("declined")) {
    return {
      title: t(keys.titleKey),
      preview: t(keys.declinedPreviewKey).replace("{name}", name),
    };
  }
  if (name) {
    return {
      title: t(keys.titleKey),
      preview: t(keys.requestPreviewKey).replace("{name}", name),
    };
  }
  return { title: t(keys.titleKey), preview: item.preview };
}

function extractLeadingName(preview: string): string | null {
  const trimmed = preview.trim();
  if (!trimmed) {
    return null;
  }
  const match = /^(.*?)\s+(sent you|accepted|declined|invited you|wants to connect)/i.exec(trimmed);
  const name = match?.[1]?.trim();
  return name && name.length > 0 ? name : null;
}

/**
 * Route for notification tap — keep connection vs Utang invitation separate.
 *
 * PERS-OWNERSHIP-01 gap: Platform does not currently emit an ownership-transfer
 * in-app notification `relatedType` (no OrganizationOwnershipTransfer / similar
 * string found). When backend adds one, map it to `/personal/ownership-transfers`.
 */
export function resolveNotificationDeepLink(
  relatedType: string,
  relatedId?: string | null,
): string {
  const type = relatedType.trim().toLowerCase();
  const id = relatedId?.trim() || "";
  // Defensive: if a future/related ownership string appears, deep-link the inbox.
  if (
    type === "organizationownershiptransfer" ||
    type.includes("ownershiptransfer") ||
    type.includes("ownership_transfer")
  ) {
    return "/personal/ownership-transfers";
  }
  if (type === "personalutanginvitation" || type.includes("utanginvitation")) {
    return "/personal/utang/invitations";
  }
  // Aggregated pending proposals inbox — always Utang hub (relatedId is sender key, not a relationship).
  if (
    type === "personalutangpendingproposals" ||
    type.includes("utangpendingproposal")
  ) {
    return "/personal/utang";
  }
  if (type === "customerlinkrequest" || type.includes("customerlink")) {
    return "/personal/customer-links";
  }
  if (type === "personaltodo" || type.includes("todo")) {
    return id ? `/personal/todo/${id}` : "/personal/todo";
  }
  if (type === "personaldebtrelationship" || type.includes("debtrelationship") || type.includes("utangentry")) {
    return id ? `/personal/utang/relationships/${id}` : "/personal/utang";
  }
  if (type === "personalconnectionrequest" || type.includes("connection")) {
    return "/personal/invitations";
  }
  return "/personal/notifications";
}

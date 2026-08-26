import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";
import type { MessageKey } from "@/i18n/messages";

export const PERSONAL_NOTIFICATIONS_QUERY_KEY = ["personal", "notifications"] as const;

/** Canonical English suffix written by Platform when creating customer-link notifications. */
const CUSTOMER_LINK_PREVIEW_EN_SUFFIX =
  " added you as a customer and wants to link your ExItS account.";

export function countUnreadPersonalNotifications(
  items: PersonalInAppNotificationDto[] | null | undefined,
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

  return { title: item.title, preview: item.preview };
}

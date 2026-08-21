import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";

export const PERSONAL_NOTIFICATIONS_QUERY_KEY = ["personal", "notifications"] as const;

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

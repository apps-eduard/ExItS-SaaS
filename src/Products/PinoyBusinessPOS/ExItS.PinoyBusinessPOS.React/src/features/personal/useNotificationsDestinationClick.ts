import type { MouseEvent } from "react";
import { useLocation } from "react-router-dom";
import { useNotificationsOverlay } from "@/features/personal/NotificationsOverlay";
import { isNotificationsDestination } from "@/features/personal/notifications-return";

/**
 * Prefer the in-place notifications overlay over navigating to /personal/notifications,
 * so the current page stays mounted under the drawer.
 */
export function useNotificationsDestinationClick() {
  const location = useLocation();
  const { openNotifications } = useNotificationsOverlay();

  return (to: string, event?: MouseEvent) => {
    if (!isNotificationsDestination(to)) {
      return false;
    }
    event?.preventDefault();
    event?.stopPropagation();
    openNotifications({
      returnTo: `${location.pathname}${location.search}`,
    });
    return true;
  };
}

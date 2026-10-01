import { createContext } from "react";

export type NotificationsOverlayContextValue = {
  open: boolean;
  openNotifications: (options?: { returnTo?: string }) => void;
  closeNotifications: () => void;
};

/**
 * Kept outside the overlay component module so a hot reload of that file
 * does not create a second context and crash Personal shell with
 * "useNotificationsOverlay must be used within NotificationsOverlayProvider".
 */
export const NotificationsOverlayContext =
  createContext<NotificationsOverlayContextValue | null>(null);

import { useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { PersonalNotificationsPanel } from "@/features/personal/PersonalNotificationsPanel";
import {
  NotificationsOverlayContext,
  type NotificationsOverlayContextValue,
} from "@/features/personal/notifications-overlay-context";
import {
  clearNotificationsReturnTo,
  rememberNotificationsReturnTo,
} from "@/features/personal/notifications-return";
import { useI18n } from "@/i18n/I18nProvider";

export function NotificationsOverlayProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);

  const openNotifications = useCallback((options?: { returnTo?: string }) => {
    if (options?.returnTo) {
      rememberNotificationsReturnTo(options.returnTo);
    }
    setOpen(true);
  }, []);

  const closeNotifications = useCallback(() => {
    setOpen(false);
    clearNotificationsReturnTo();
  }, []);

  const value = useMemo(
    () => ({
      open,
      openNotifications,
      closeNotifications,
    }),
    [open, openNotifications, closeNotifications],
  );

  return (
    <NotificationsOverlayContext.Provider value={value}>
      {children}
    </NotificationsOverlayContext.Provider>
  );
}

export function useNotificationsOverlay(): NotificationsOverlayContextValue {
  const value = useContext(NotificationsOverlayContext);
  if (!value) {
    throw new Error("useNotificationsOverlay must be used within NotificationsOverlayProvider");
  }
  return value;
}

/** Floating notifications drawer — overlays the current page like preferences. */
export function NotificationsOverlayHost() {
  const { t } = useI18n();
  const { open, closeNotifications } = useNotificationsOverlay();

  return (
    <SideDrawer
      open={open}
      onClose={closeNotifications}
      title={t("notifications.title")}
      description={t("notifications.lede")}
      testId="notifications-drawer"
      closeLabel={t("personal.social.notificationsClose")}
      closeTestId="notifications-close"
      panelClassName="exits-side-drawer__panel--notifications"
    >
      <PersonalNotificationsPanel onNavigateAway={closeNotifications} />
    </SideDrawer>
  );
}

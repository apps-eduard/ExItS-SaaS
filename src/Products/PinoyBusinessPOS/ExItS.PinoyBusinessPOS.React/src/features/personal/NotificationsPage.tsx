import { useLayoutEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { useNotificationsOverlay } from "@/features/personal/NotificationsOverlay";
import { PersonalNotificationsPanel } from "@/features/personal/PersonalNotificationsPanel";
import {
  peekNotificationsReturnTo,
} from "@/features/personal/notifications-return";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Deep-link / full-page notifications route.
 * When opened from in-app (returnTo present), bounce back and open the floating overlay
 * so the current page stays under the drawer.
 */
export function NotificationsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { openNotifications } = useNotificationsOverlay();
  const returnContext = peekNotificationsReturnTo(location.state);
  const returnTo = returnContext?.returnTo ?? null;
  const bouncedRef = useRef(false);

  useLayoutEffect(() => {
    if (!returnTo || bouncedRef.current) {
      return;
    }
    bouncedRef.current = true;
    openNotifications({ returnTo });
    navigate(returnTo, { replace: true });
  }, [returnTo, navigate, openNotifications]);

  if (returnTo) {
    return null;
  }

  return (
    <section
      className="personal-page exits-page flex w-full min-w-0 flex-col gap-4"
      data-testid="personal-notifications-page"
    >
      <PageHeader title={t("notifications.title")} description={t("notifications.lede")} />
      <PersonalNotificationsPanel />
    </section>
  );
}

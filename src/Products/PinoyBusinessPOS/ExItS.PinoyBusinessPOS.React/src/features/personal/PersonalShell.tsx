import { useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNotificationButton } from "@/components/exits/ShellNotificationButton";
import { listPersonalNotifications } from "@/api/platform/personal-people-client";
import { PersonalBottomNav } from "@/features/personal/PersonalBottomNav";
import {
  PERSONAL_NOTIFICATIONS_QUERY_KEY,
  countUnreadPersonalNotifications,
  formatUnreadNotificationBadge,
} from "@/features/personal/personal-notifications";
import {
  rememberNotificationsReturnTo,
  type NotificationsLocationState,
} from "@/features/personal/notifications-return";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function PersonalShell() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { session, signOut } = useSession();
  const { clearBoundWorkspace } = useWorkspace();
  const [signingOut, setSigningOut] = useState(false);

  const notificationsQuery = useQuery({
    queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalNotifications(signal),
  });
  const unreadCount = countUnreadPersonalNotifications(notificationsQuery.data);
  const badge = formatUnreadNotificationBadge(unreadCount);
  const onNotificationsPage = location.pathname.startsWith("/personal/notifications");
  const returnTo = onNotificationsPage
    ? null
    : `${location.pathname}${location.search}`;
  const notificationsLinkState: NotificationsLocationState | undefined = returnTo
    ? { returnTo }
    : (location.state as NotificationsLocationState | null) ?? undefined;

  async function handleSignOut() {
    if (signingOut) {
      return;
    }
    setSigningOut(true);
    const result = await signOut();
    if (!result.ok) {
      setSigningOut(false);
      return;
    }
    clearBoundWorkspace();
    navigate(result.nextRoute, { replace: true });
  }

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col" data-testid="personal-shell">
      <header className="app-top-bar app-top-bar--personal" data-testid="personal-top-bar">
        <div className="app-top-bar__row">
          <div className="app-top-bar__brand min-w-0 flex-1">
            <div className="app-top-bar__brand-copy">
              <p className="app-top-bar__workspace-org m-0 truncate">
                {session?.displayName || t("personal.badge")}
              </p>
              <p className="app-top-bar__workspace-branch m-0 truncate">{t("personal.badge")}</p>
            </div>
          </div>
          <div className="app-top-bar__actions">
            <ShellConnectionButton className="app-top-bar__action" />
            <ShellNotificationButton
              to="/personal/notifications"
              label={t("shell.notifications.label")}
              unreadLabel={t("shell.notifications.unreadLabel")}
              badge={badge}
              testId="personal-notification-bell"
              className="app-top-bar__action"
              linkState={notificationsLinkState}
              onNavigate={
                onNotificationsPage || !returnTo
                  ? undefined
                  : () => {
                      rememberNotificationsReturnTo(returnTo);
                      navigate("/personal/notifications", { state: notificationsLinkState });
                    }
              }
            />
            <AccountMenu signingOut={signingOut} onSignOut={() => void handleSignOut()} compact />
          </div>
        </div>
      </header>

      <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-4 pb-20 pt-4">
        <Outlet />
      </div>

      <PersonalBottomNav />
    </div>
  );
}

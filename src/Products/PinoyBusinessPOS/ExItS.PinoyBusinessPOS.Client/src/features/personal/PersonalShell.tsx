import { useQuery } from "@tanstack/react-query";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNotificationButton } from "@/components/exits/ShellNotificationButton";
import { listPersonalNotifications } from "@/api/platform/personal-social-client";
import { PersonalBottomNav } from "@/features/personal/PersonalBottomNav";
import {
  PERSONAL_NOTIFICATIONS_QUERY_KEY,
  countUnreadPersonalNotifications,
  formatUnreadNotificationBadge,
} from "@/features/personal/personal-notifications";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";

export function PersonalShell() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { session, signOut } = useSession();
  const { clearBoundWorkspace } = useWorkspace();
  const [signingOut, setSigningOut] = useState(false);

  const notificationsQuery = useQuery({
    queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalNotifications(signal),
  });
  const unreadCount = countUnreadPersonalNotifications(notificationsQuery.data);
  const badge = formatUnreadNotificationBadge(unreadCount);

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
      <header
        className="flex min-w-0 items-center justify-between gap-2 border-b border-border py-2"
        data-testid="personal-top-bar"
      >
        <div className="min-w-0 flex-1">
          <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {session?.displayName || t("personal.badge")}
          </p>
          <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
            {t("personal.badge")}
          </p>
        </div>
        <div className="flex shrink-0 items-center gap-0.5">
          <ShellConnectionButton />
          <ShellNotificationButton
            to="/personal/notifications"
            label={t("shell.notifications.label")}
            unreadLabel={t("shell.notifications.unreadLabel")}
            badge={badge}
            testId="personal-notification-bell"
          />
          <AccountMenu signingOut={signingOut} onSignOut={() => void handleSignOut()} compact />
        </div>
      </header>

      <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-4 pb-20 pt-4">
        <Outlet />
      </div>

      <PersonalBottomNav />
    </div>
  );
}

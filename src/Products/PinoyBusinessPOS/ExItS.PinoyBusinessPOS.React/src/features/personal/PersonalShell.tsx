import { useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNotificationButton } from "@/components/exits/ShellNotificationButton";
import { ShellPreferencesButton } from "@/components/exits/ShellPreferencesButton";
import { ShellSidebarModeButton } from "@/components/exits/ShellSidebarModeButton";
import { PersonalAvatarProvider } from "@/features/personal/PersonalAvatarProvider";
import { PersonalBottomNav } from "@/features/personal/PersonalBottomNav";
import { PersonalInstallHomeOffer } from "@/features/personal/PersonalInstallOffer";
import { useNotificationsOverlay } from "@/features/personal/NotificationsOverlay";
import { PersonalSidebar } from "@/features/personal/PersonalSidebar";
import { formatUnreadNotificationBadge } from "@/features/personal/personal-notifications";
import { usePersonalNotificationUnreadCountQuery } from "@/features/personal/people-queries";
import { SHELL_DESKTOP_MIN_PX } from "@/features/shell/shell-breakpoints";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function PersonalShell() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { session, signOut } = useSession();
  const { clearBoundWorkspace } = useWorkspace();
  const { openNotifications } = useNotificationsOverlay();
  const [signingOut, setSigningOut] = useState(false);
  const isDesktop = useMediaMin(SHELL_DESKTOP_MIN_PX);

  const unreadQuery = usePersonalNotificationUnreadCountQuery();
  const unreadCount = unreadQuery.data ?? 0;
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
    <PersonalAvatarProvider>
      <div
        className={cn(
          "personal-shell admin-shell flex h-[100dvh] max-h-[100dvh] w-full min-w-0 flex-col overflow-hidden",
          /* Small: no horizontal shell pad so top bar can be true full-bleed; gutters live on body. */
          "px-0 pt-[env(safe-area-inset-top)]",
          /* Reserve space for fixed bottom nav on phone/tablet; desktop float uses lg:pb inset. */
          isDesktop
            ? "pb-[max(0.75rem,env(safe-area-inset-bottom))]"
            : "pb-[max(5.5rem,calc(4.25rem+env(safe-area-inset-bottom)))]",
          /* Desktop: Gmail-like floating chrome — inset shell + gap between sidebar and column. */
          "lg:flex-row lg:gap-3 lg:pt-[max(0.75rem,env(safe-area-inset-top))] lg:pb-[max(0.75rem,env(safe-area-inset-bottom))] lg:ps-[max(0.75rem,env(safe-area-inset-left))] lg:pe-[max(0.75rem,env(safe-area-inset-right))]",
        )}
        data-testid="personal-shell"
      >
        <a
          href="#main-content"
          className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
        >
          {t("app.skipToContent")}
        </a>

        <div
          className="admin-sidebar-rail hidden min-h-0 lg:block"
          data-testid="personal-desktop-sidebar"
        >
          <PersonalSidebar />
        </div>

        <div className="admin-shell__column flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
          <header
            className={cn(
              "app-top-bar app-top-bar--personal shrink-0",
              isDesktop && "app-top-bar--shell-desktop",
            )}
            data-testid="personal-top-bar"
          >
            <div className="app-top-bar__row app-top-bar__row--personal">
              {isDesktop ? (
                <div
                  className="app-top-bar__leading"
                  data-testid="personal-top-bar-leading"
                >
                  <ShellSidebarModeButton className="app-top-bar__action" />
                </div>
              ) : null}

              <div className="app-top-bar__brand app-top-bar__brand--personal min-w-0">
                <div className="app-top-bar__brand-copy">
                  <p className="app-top-bar__workspace-org m-0 truncate">
                    {session?.displayName || t("personal.badge")}
                  </p>
                </div>
              </div>

              <div className="app-top-bar__actions app-top-bar__actions--personal">
                <ShellConnectionButton className="app-top-bar__action" />
                <ShellNotificationButton
                  to="/personal/notifications"
                  label={t("shell.notifications.label")}
                  unreadLabel={t("shell.notifications.unreadLabel")}
                  badge={badge}
                  testId="personal-notification-bell"
                  className="app-top-bar__action"
                  onNavigate={() =>
                    openNotifications({
                      returnTo: `${location.pathname}${location.search}`,
                    })
                  }
                />
                <ShellPreferencesButton
                  label={t("topbar.preferences")}
                  className="app-top-bar__action"
                />
                <AccountMenu
                  signingOut={signingOut}
                  onSignOut={() => void handleSignOut()}
                  compact
                />
              </div>
            </div>
          </header>

          <div className="admin-shell__body personal-shell__body mt-0 flex min-h-0 min-w-0 flex-1 gap-3 overflow-hidden lg:mt-0 lg:gap-0">
            <div className="admin-shell__main flex min-h-0 min-w-0 flex-1 flex-col gap-0 overflow-hidden">
              <main
                id="main-content"
                className="admin-shell__content personal-shell__content min-h-0 min-w-0 flex-1"
                tabIndex={-1}
              >
                <PersonalInstallHomeOffer />
                <Outlet />
              </main>
            </div>
          </div>
        </div>
      </div>

      {/* Outside overflow-hidden shell so fixed bottom chrome is never clipped. */}
      <PersonalBottomNav />
    </PersonalAvatarProvider>
  );
}

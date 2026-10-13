import { useEffect, useState } from "react";
import { ensurePersonalSymbols, symbol } from "@/components/icons/MaterialSymbol";

const FullSidebarIcon = symbol("left_panel_open");
const IconSidebarIcon = symbol("view_sidebar");
const HiddenSidebarIcon = symbol("left_panel_close");
const MenuIcon = symbol("menu");
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNotificationButton } from "@/components/exits/ShellNotificationButton";
import { ShellPreferencesButton } from "@/components/exits/ShellPreferencesButton";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import { ShellSidebarModeButton } from "@/components/exits/ShellSidebarModeButton";
import { IconButton } from "@/components/ui/icon-button";
import { PersonalAvatarProvider } from "@/features/personal/PersonalAvatarProvider";
import { PersonalBottomNav } from "@/features/personal/PersonalBottomNav";
import { PersonalInstallHomeOffer } from "@/features/personal/PersonalInstallOffer";
import { useNotificationsOverlay } from "@/features/personal/NotificationsOverlay";
import { PersonalSidebar } from "@/features/personal/PersonalSidebar";
import { PersonalAppearanceMenu } from "@/features/personal/PersonalAppearanceMenu";
import { PersonalThemeModeButton } from "@/features/personal/PersonalThemeModeButton";
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
  const { signOut } = useSession();
  const { clearBoundWorkspace } = useWorkspace();
  const { openNotifications } = useNotificationsOverlay();
  const [signingOut, setSigningOut] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const isDesktop = useMediaMin(SHELL_DESKTOP_MIN_PX);

  useEffect(() => {
    ensurePersonalSymbols();
  }, []);

  useEffect(() => {
    setNavOpen(false);
  }, [location.pathname, isDesktop]);

  useEffect(() => {
    if (isDesktop || !navOpen) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setNavOpen(false);
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [isDesktop, navOpen]);

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
            ? "pb-[env(safe-area-inset-bottom)]"
            : "pb-[max(5.5rem,calc(4.25rem+env(safe-area-inset-bottom)))]",
          /* Desktop: sidenav and top bar sit flush on the left, right, and bottom. */
          "lg:flex-row lg:gap-0 lg:pt-[env(safe-area-inset-top)] lg:pb-[env(safe-area-inset-bottom)] lg:ps-[env(safe-area-inset-left)] lg:pe-[env(safe-area-inset-right)]",
        )}
        data-testid="personal-shell"
      >
        <a
          href="#main-content"
          className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
        >
          {t("app.skipToContent")}
        </a>

        {!isDesktop && navOpen ? (
          <button
            type="button"
            className="personal-mobile-nav__backdrop"
            data-testid="personal-mobile-nav-backdrop"
            aria-label={t("shell.closeNavigation")}
            onClick={() => setNavOpen(false)}
          />
        ) : null}

        <div
          id="personal-mobile-nav"
          className={cn(
            "admin-sidebar-rail hidden min-h-0 lg:block",
            !isDesktop && navOpen && "personal-mobile-nav-rail",
          )}
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
              <div
                className="app-top-bar__leading"
                data-testid="personal-top-bar-leading"
              >
                {isDesktop ? (
                  <ShellSidebarModeButton
                    className="app-top-bar__action"
                    icons={{
                      standard: <FullSidebarIcon className="size-5" aria-hidden="true" />,
                      compact: <IconSidebarIcon className="size-5" aria-hidden="true" />,
                      hidden: <HiddenSidebarIcon className="size-5" aria-hidden="true" />,
                    }}
                  />
                ) : (
                  <IconButton
                    label={navOpen ? t("shell.closeNavigation") : t("shell.openNavigation")}
                    aria-expanded={navOpen}
                    aria-controls="personal-mobile-nav"
                    data-testid="personal-mobile-nav-toggle"
                    className="app-top-bar__action personal-mobile-nav-toggle"
                    onClick={() => setNavOpen((open) => !open)}
                  >
                    <MenuIcon className="size-5" aria-hidden="true" />
                  </IconButton>
                )}
              </div>

              <div className="app-top-bar__actions app-top-bar__actions--personal">
                <PersonalThemeModeButton className="app-top-bar__action" />
                <PersonalAppearanceMenu className="app-top-bar__action" />
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
                <ShellUiStandardsButton
                  label={t("uiStandards.topbar")}
                  className="app-top-bar__action"
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

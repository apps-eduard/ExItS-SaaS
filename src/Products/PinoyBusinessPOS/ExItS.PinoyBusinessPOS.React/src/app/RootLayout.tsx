import { Outlet, useLocation } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { WorkspaceTransitionOverlay } from "@/components/exits/loading/WorkspaceTransitionOverlay";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { isAccountContextSwitchPath } from "@/features/account/account-context-switch-route";
import { OrgBottomNav } from "@/features/shell/OrgBottomNav";
import {
  isSellTransactionPath,
  useOrgBottomNavHidden,
} from "@/features/sell/sell-org-bottom-nav-chrome";
import { useI18n } from "@/i18n/I18nProvider";
import { AppShell } from "@/layouts/AppShell";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function RootLayout() {
  const { t } = useI18n();
  const location = useLocation();
  const isContextSwitch = isAccountContextSwitchPath(location.pathname);
  const isPersonal = location.pathname.startsWith("/personal") || isContextSwitch;
  const isOnboarding = location.pathname.startsWith("/onboarding");
  const { status: sessionStatus } = useSession();
  const { status: workspaceStatus, boundWorkspace } = useWorkspace();
  const cartOverlayHidesNav = useOrgBottomNavHidden();
  const sellTransactionHidesNav = isSellTransactionPath(location.pathname);
  const showOrgBottomNav =
    !isPersonal &&
    !isOnboarding &&
    isAuthenticatedOrColdStartOffline(sessionStatus) &&
    boundWorkspace != null;
  const orgBottomNavVisible =
    showOrgBottomNav && !cartOverlayHidesNav && !sellTransactionHidesNav;

  const showWorkspaceTransition = workspaceStatus === "binding";
  const isSellFloor =
    location.pathname === "/sell" || location.pathname.startsWith("/sell/");

  return (
    <>
      <WorkspaceBootNavigator />
      <PersonalMerchantCartProvider>
        <AppShell
          header={isPersonal ? undefined : <AppTopBar />}
          withOrgBottomNav={orgBottomNavVisible}
          sellFloor={isSellFloor}
        >
          <Outlet />
        </AppShell>
        {orgBottomNavVisible ? <OrgBottomNav /> : null}
        <WorkspaceTransitionOverlay
          active={showWorkspaceTransition}
          label={t("loading.switchingWorkspace")}
          detail={boundWorkspace?.organizationDisplayName ?? null}
        />
      </PersonalMerchantCartProvider>
    </>
  );
}

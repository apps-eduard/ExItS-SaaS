import { Outlet, useLocation } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { WorkspaceTransitionOverlay } from "@/components/exits/loading/WorkspaceTransitionOverlay";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { isAccountContextSwitchPath } from "@/features/account/account-context-switch-route";
import { OrgBottomNav } from "@/features/shell/OrgBottomNav";
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
  const showOrgBottomNav =
    !isPersonal &&
    !isOnboarding &&
    isAuthenticatedOrColdStartOffline(sessionStatus) &&
    boundWorkspace != null;

  const showWorkspaceTransition = workspaceStatus === "binding";
  const isSellFloor =
    location.pathname === "/sell" || location.pathname.startsWith("/sell/");

  return (
    <>
      <WorkspaceBootNavigator />
      <PersonalMerchantCartProvider>
        <AppShell
          header={isPersonal ? undefined : <AppTopBar />}
          withOrgBottomNav={showOrgBottomNav}
          sellFloor={isSellFloor}
        >
          <Outlet />
        </AppShell>
        {showOrgBottomNav ? <OrgBottomNav /> : null}
        <WorkspaceTransitionOverlay
          active={showWorkspaceTransition}
          label={t("loading.switchingWorkspace")}
          detail={boundWorkspace?.organizationDisplayName ?? null}
        />
      </PersonalMerchantCartProvider>
    </>
  );
}

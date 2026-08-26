import { Outlet, useLocation } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { isAccountContextSwitchPath } from "@/features/account/account-context-switch-route";
import { OrgBottomNav } from "@/features/shell/OrgBottomNav";
import { AppShell } from "@/layouts/AppShell";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function RootLayout() {
  const location = useLocation();
  const isContextSwitch = isAccountContextSwitchPath(location.pathname);
  const isPersonal = location.pathname.startsWith("/personal") || isContextSwitch;
  const isOnboarding = location.pathname.startsWith("/onboarding");
  const { status: sessionStatus } = useSession();
  const { boundWorkspace } = useWorkspace();
  const showOrgBottomNav =
    !isPersonal &&
    !isOnboarding &&
    isAuthenticatedOrColdStartOffline(sessionStatus) &&
    boundWorkspace != null;

  return (
    <>
      <WorkspaceBootNavigator />
      <PersonalMerchantCartProvider>
        <AppShell
          header={isPersonal ? undefined : <AppTopBar />}
          withOrgBottomNav={showOrgBottomNav}
        >
          <Outlet />
        </AppShell>
        {showOrgBottomNav ? <OrgBottomNav /> : null}
      </PersonalMerchantCartProvider>
    </>
  );
}

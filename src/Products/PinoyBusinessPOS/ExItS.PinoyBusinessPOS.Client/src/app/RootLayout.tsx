import { Outlet, useLocation } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { OrgBottomNav } from "@/features/shell/OrgBottomNav";
import { AppShell } from "@/layouts/AppShell";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function RootLayout() {
  const location = useLocation();
  const isPersonal = location.pathname.startsWith("/personal");
  const isOnboarding = location.pathname.startsWith("/onboarding");
  // Focused cash forms: sticky primary actions must not sit under the org bottom nav.
  const isShiftFocusForm =
    location.pathname === "/shifts/open" ||
    /\/shifts\/[^/]+\/close\/?$/.test(location.pathname);
  const { status: sessionStatus } = useSession();
  const { boundWorkspace } = useWorkspace();
  const showOrgBottomNav =
    !isPersonal &&
    !isOnboarding &&
    !isShiftFocusForm &&
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

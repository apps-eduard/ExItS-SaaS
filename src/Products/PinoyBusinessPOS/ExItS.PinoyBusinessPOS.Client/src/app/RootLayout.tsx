import { Outlet, useLocation } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { OrgBottomNav } from "@/features/shell/OrgBottomNav";
import { AppShell } from "@/layouts/AppShell";
import { useSession } from "@/session/SessionProvider";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function RootLayout() {
  const location = useLocation();
  const isPersonal = location.pathname.startsWith("/personal");
  const { status: sessionStatus } = useSession();
  const { boundWorkspace } = useWorkspace();
  const showOrgBottomNav =
    !isPersonal && sessionStatus === "authenticated" && boundWorkspace != null;

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

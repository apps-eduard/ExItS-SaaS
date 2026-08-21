import { Outlet, useLocation } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { AppShell } from "@/layouts/AppShell";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";

export function RootLayout() {
  const location = useLocation();
  const isPersonal = location.pathname.startsWith("/personal");

  return (
    <>
      <WorkspaceBootNavigator />
      <PersonalMerchantCartProvider>
        <AppShell header={isPersonal ? undefined : <AppTopBar />}>
          <Outlet />
        </AppShell>
      </PersonalMerchantCartProvider>
    </>
  );
}

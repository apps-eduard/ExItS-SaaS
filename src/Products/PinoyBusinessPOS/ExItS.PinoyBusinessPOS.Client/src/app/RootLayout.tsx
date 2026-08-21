import { Outlet } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { PersonalMerchantCartProvider } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { AppShell } from "@/layouts/AppShell";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";

export function RootLayout() {
  return (
    <>
      <WorkspaceBootNavigator />
      <PersonalMerchantCartProvider>
        <AppShell header={<AppTopBar />}>
          <Outlet />
        </AppShell>
      </PersonalMerchantCartProvider>
    </>
  );
}

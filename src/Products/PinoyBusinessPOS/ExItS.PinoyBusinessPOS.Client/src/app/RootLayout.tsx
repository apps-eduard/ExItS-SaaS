import { Outlet } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { AppShell } from "@/layouts/AppShell";
import { WorkspaceBootNavigator } from "@/workspace/WorkspaceBootNavigator";

export function RootLayout() {
  return (
    <>
      <WorkspaceBootNavigator />
      <AppShell header={<AppTopBar />}>
        <Outlet />
      </AppShell>
    </>
  );
}

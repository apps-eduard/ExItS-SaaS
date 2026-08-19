import { useState } from "react";
import { Outlet } from "react-router-dom";
import { useIsDesktopShell } from "@/hooks/use-media-query";
import { AppBreadcrumbs } from "@/components/exits/AppBreadcrumbs";
import { AppSidebar } from "@/components/exits/AppSidebar";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { NavigationDrawer } from "@/components/exits/NavigationDrawer";

export function AppShell() {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const desktop = useIsDesktopShell();

  return (
    <div className="flex min-h-dvh overflow-x-hidden bg-background">
      {desktop ? <AppSidebar /> : null}
      {desktop ? null : <NavigationDrawer open={drawerOpen} onOpenChange={setDrawerOpen} />}
      <div className="flex min-w-0 flex-1 flex-col">
        <AppTopBar showNavigationTrigger={!desktop} onOpenNavigation={() => setDrawerOpen(true)} />
        <AppBreadcrumbs />
        <main className="min-w-0 flex-1 overflow-y-auto p-[var(--exits-page-padding)]">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

import { Outlet } from "react-router-dom";
import { useIsDesktopShell } from "@/hooks/use-media-query";
import { AppSidebar } from "@/components/exits/AppSidebar";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { NavigationDrawer } from "@/components/exits/NavigationDrawer";
import { useState } from "react";

export function AppShell() {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const desktop = useIsDesktopShell();

  return (
    <div className="flex min-h-dvh overflow-x-hidden bg-background">
      {desktop ? <AppSidebar /> : null}
      {desktop ? null : <NavigationDrawer open={drawerOpen} onOpenChange={setDrawerOpen} />}
      <div className="flex min-w-0 flex-1 flex-col">
        <AppTopBar showNavigationTrigger={!desktop} onOpenNavigation={() => setDrawerOpen(true)} />
        <main className="min-w-0 flex-1 overflow-y-auto px-4 py-4 sm:px-5 lg:px-6">
          <div className="mx-auto w-full max-w-[86rem]">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}

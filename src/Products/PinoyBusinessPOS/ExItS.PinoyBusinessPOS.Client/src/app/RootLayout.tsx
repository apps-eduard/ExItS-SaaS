import { Outlet } from "react-router-dom";
import { ConnectivityHost } from "@/connectivity/ConnectivityHost";
import { AppShell } from "@/layouts/AppShell";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";

export function RootLayout() {
  return (
    <>
      <ConnectivityHost />
      <PwaUpdateHost />
      <AppShell>
        <Outlet />
      </AppShell>
    </>
  );
}

import { Outlet } from "react-router-dom";
import { ConnectivityHost } from "@/connectivity/ConnectivityHost";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";
import { SessionProvider } from "@/session/SessionProvider";

export function RootLayout() {
  return (
    <SessionProvider>
      <ConnectivityHost />
      <Outlet />
      <PwaUpdateHost />
    </SessionProvider>
  );
}

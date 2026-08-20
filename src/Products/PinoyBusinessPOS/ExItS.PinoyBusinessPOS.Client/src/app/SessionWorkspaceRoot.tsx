import { Outlet } from "react-router-dom";
import { SessionCartLifecycle } from "@/cart/SessionCartLifecycle";
import { SessionCartProvider } from "@/cart/SessionCartProvider";
import { ConnectivityHost } from "@/connectivity/ConnectivityHost";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";
import { SellingModeLifecycle } from "@/selling/SellingModeLifecycle";
import { SellingModeProvider } from "@/selling/SellingModeProvider";
import { SessionProvider } from "@/session/SessionProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";

export function SessionWorkspaceRoot() {
  return (
    <SessionProvider>
      <WorkspaceProvider>
        <SessionCartProvider>
          <SellingModeProvider>
            <SellingModeLifecycle />
            <SessionCartLifecycle />
            <ConnectivityHost />
            <PwaUpdateHost />
            <Outlet />
          </SellingModeProvider>
        </SessionCartProvider>
      </WorkspaceProvider>
    </SessionProvider>
  );
}

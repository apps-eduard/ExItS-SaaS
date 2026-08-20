import { Outlet } from "react-router-dom";
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
        <SellingModeProvider>
          <SellingModeLifecycle />
          <ConnectivityHost />
          <PwaUpdateHost />
          <Outlet />
        </SellingModeProvider>
      </WorkspaceProvider>
    </SessionProvider>
  );
}

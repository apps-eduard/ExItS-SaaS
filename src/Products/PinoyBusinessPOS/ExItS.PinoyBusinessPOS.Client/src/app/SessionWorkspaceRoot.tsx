import { Outlet } from "react-router-dom";
import { ConnectivityHost } from "@/connectivity/ConnectivityHost";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";
import { SessionProvider } from "@/session/SessionProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";

export function SessionWorkspaceRoot() {
  return (
    <SessionProvider>
      <WorkspaceProvider>
        <ConnectivityHost />
        <PwaUpdateHost />
        <Outlet />
      </WorkspaceProvider>
    </SessionProvider>
  );
}

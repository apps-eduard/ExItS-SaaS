import { Outlet } from "react-router-dom";
import { SessionCartLifecycle } from "@/cart/SessionCartLifecycle";
import { SessionCartProvider } from "@/cart/SessionCartProvider";
import { ConnectivityHost } from "@/connectivity/ConnectivityHost";
import { ShiftContextProvider } from "@/features/shifts/ShiftContextProvider";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";
import { SellingModeLifecycle } from "@/selling/SellingModeLifecycle";
import { SellingModeProvider } from "@/selling/SellingModeProvider";
import { SessionProvider, OfflinePinSetupGate } from "@/session/SessionProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";

export function SessionWorkspaceRoot() {
  return (
    <SessionProvider>
      <OfflinePinSetupGate>
        <WorkspaceProvider>
          <ShiftContextProvider>
            <SessionCartProvider>
              <SellingModeProvider>
                <SellingModeLifecycle />
                <SessionCartLifecycle />
                <ConnectivityHost />
                <PwaUpdateHost />
                <Outlet />
              </SellingModeProvider>
            </SessionCartProvider>
          </ShiftContextProvider>
        </WorkspaceProvider>
      </OfflinePinSetupGate>
    </SessionProvider>
  );
}

import { Outlet } from "react-router-dom";
import { SessionCartLifecycle } from "@/cart/SessionCartLifecycle";
import { SessionCartProvider } from "@/cart/SessionCartProvider";
import { ToastNavigateBridge } from "@/components/exits/ToastNavigateBridge";
import { ConnectivityHost } from "@/connectivity/ConnectivityHost";
import { OnboardingResumeGate } from "@/features/onboarding/OnboardingResumeGate";
import { NotificationsOverlayHost } from "@/features/personal/NotificationsOverlay";
import { PreferencesOverlayHost } from "@/features/preferences/PreferencesOverlay";
import { ShiftContextProvider } from "@/features/shifts/ShiftContextProvider";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";
import { SellingModeLifecycle } from "@/selling/SellingModeLifecycle";
import { SellingModeProvider } from "@/selling/SellingModeProvider";
import { SessionProvider, OfflinePinSetupGate } from "@/session/SessionProvider";
import { ExitsRealtimeProvider } from "@/realtime/ExitsRealtimeProvider";
import { PosRealtimeProvider } from "@/realtime/PosRealtimeProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";

export function SessionWorkspaceRoot() {
  return (
    <SessionProvider>
      <OfflinePinSetupGate>
        <WorkspaceProvider>
          <ExitsRealtimeProvider>
            <PosRealtimeProvider>
              <ShiftContextProvider>
                <SessionCartProvider>
                  <SellingModeProvider>
                    <ToastNavigateBridge />
                    <SellingModeLifecycle />
                    <SessionCartLifecycle />
                    <ConnectivityHost />
                    <PwaUpdateHost />
                    <OnboardingResumeGate />
                    <Outlet />
                    {/* Overlay hosts need Router context (useNavigate / Link inside panels). */}
                    <PreferencesOverlayHost />
                    <NotificationsOverlayHost />
                  </SellingModeProvider>
                </SessionCartProvider>
              </ShiftContextProvider>
            </PosRealtimeProvider>
          </ExitsRealtimeProvider>
        </WorkspaceProvider>
      </OfflinePinSetupGate>
    </SessionProvider>
  );
}

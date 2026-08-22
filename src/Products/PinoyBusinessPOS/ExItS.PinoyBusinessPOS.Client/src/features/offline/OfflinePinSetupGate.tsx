import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { isOfflinePinAndDekConfigured } from "@/offline/local-store-key";
import { useSession } from "@/session/SessionProvider";

const PIN_SETUP_PATH = "/offline-pin-setup";
const PIN_UNLOCK_PATH = "/offline-pin";
const SIGN_IN_PATH = "/sign-in";

/** Redirect authenticated users without an offline PIN to enrollment. */
export function OfflinePinSetupGate({ children }: { children: React.ReactNode }) {
  const { status, session } = useSession();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (status !== "authenticated" || !session?.userId) {
      return;
    }
    const path = location.pathname;
    if (path === PIN_SETUP_PATH || path === PIN_UNLOCK_PATH || path === SIGN_IN_PATH) {
      return;
    }
    if (!isOfflinePinAndDekConfigured(session.userId)) {
      navigate(PIN_SETUP_PATH, { replace: true, state: { from: path } });
    }
  }, [location.pathname, navigate, session?.userId, status]);

  return children;
}

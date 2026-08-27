import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getCurrentCashierShift,
  isOpenCashierShift,
  type PosCashierShiftDto,
} from "@/api/pos/pos-shifts-client";
import { canViewShifts } from "@/access/pos-capabilities";
import {
  evaluateCheckoutShiftReadiness,
  type CheckoutShiftReadiness,
} from "@/features/shifts/checkout-readiness";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ShiftContextValue = {
  currentShift: PosCashierShiftDto | null;
  loading: boolean;
  errorMessage: string | null;
  denied: boolean;
  hasOpenShift: boolean;
  readiness: CheckoutShiftReadiness;
  refresh: () => Promise<void>;
};

const ShiftContext = createContext<ShiftContextValue | null>(null);

export function ShiftContextProvider({ children }: { children: ReactNode }) {
  const { boundWorkspace, sessionGrant, posDevice, deviceEnforcementEnabled } = useWorkspace();
  const [currentShift, setCurrentShift] = useState<PosCashierShiftDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const refreshGenerationRef = useRef(0);

  const allowView = canViewShifts(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;

  const refresh = useCallback(async () => {
    const generation = ++refreshGenerationRef.current;

    if (!organizationId || !branchId || !allowView) {
      setCurrentShift(null);
      setLoading(false);
      setErrorMessage(null);
      setDenied(!allowView && Boolean(branchId));
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setDenied(false);
    try {
      const shift = await getCurrentCashierShift({
        organizationId,
        branchId,
      });
      if (generation !== refreshGenerationRef.current) {
        return;
      }
      setCurrentShift(shift);
    } catch (error) {
      if (generation !== refreshGenerationRef.current) {
        return;
      }
      setCurrentShift(null);
      if (error instanceof PosApiError && (error.status === 403 || error.status === 401)) {
        setDenied(true);
        setErrorMessage(error.message);
      } else {
        setErrorMessage(error instanceof Error ? error.message : "Could not load shift.");
      }
    } finally {
      if (generation === refreshGenerationRef.current) {
        setLoading(false);
      }
    }
  }, [allowView, branchId, organizationId]);

  useEffect(() => {
    void refresh();
    return () => {
      refreshGenerationRef.current += 1;
    };
  }, [refresh]);

  // Re-read authoritative server state when the tab becomes visible again.
  useEffect(() => {
    function onVisible() {
      if (document.visibilityState === "visible") {
        void refresh();
      }
    }
    document.addEventListener("visibilitychange", onVisible);
    return () => document.removeEventListener("visibilitychange", onVisible);
  }, [refresh]);

  const readiness = useMemo(
    () =>
      evaluateCheckoutShiftReadiness({
        loading,
        canViewShifts: allowView && !denied,
        currentShift,
        posDevice,
        deviceEnforcementEnabled,
      }),
    [allowView, currentShift, denied, deviceEnforcementEnabled, loading, posDevice],
  );

  const value = useMemo<ShiftContextValue>(
    () => ({
      currentShift,
      loading,
      errorMessage,
      denied,
      hasOpenShift: Boolean(currentShift && isOpenCashierShift(currentShift)),
      readiness,
      refresh,
    }),
    [currentShift, denied, errorMessage, loading, readiness, refresh],
  );

  return <ShiftContext.Provider value={value}>{children}</ShiftContext.Provider>;
}

export function useShiftContext(): ShiftContextValue {
  const value = useContext(ShiftContext);
  if (!value) {
    throw new Error("useShiftContext must be used within ShiftContextProvider");
  }
  return value;
}

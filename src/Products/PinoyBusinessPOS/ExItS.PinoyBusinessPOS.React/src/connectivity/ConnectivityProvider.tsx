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
import { subscribeBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";

/**
 * Coherent connectivity model for Organization Web (and shared shell).
 * Combines browser online/offline with bounded reachability probes.
 * Does not treat 401/403 as offline.
 */

export type ConnectivityPhase = "online" | "offline" | "reconnecting" | "checking";

export type ConnectivitySnapshot = {
  phase: ConnectivityPhase;
  /** Advisory: browser believes a network interface is available. */
  browserOnline: boolean;
  /** True when phase is online (reachability confirmed or assumed after browser online). */
  isOnline: boolean;
  /** True when writes must not start (offline or still checking after loss). */
  blocksMutations: boolean;
  /** Brief “Back online” flash after a genuine restore. */
  showBackOnline: boolean;
  /** Mark connectivity lost from a genuine network failure (not HTTP 4xx/5xx app errors). */
  reportNetworkFailure: () => void;
  /** Mark connectivity restored after a successful authenticated/API request. */
  reportNetworkSuccess: () => void;
  /** Bounded reconnect probe. Returns whether reachability succeeded. */
  retry: () => Promise<boolean>;
};

const ConnectivityContext = createContext<ConnectivitySnapshot | null>(null);

const PROBE_PATHS = ["/pos-api/health", "/platform-api/health", "/"] as const;
const MIN_PROBE_INTERVAL_MS = 4_000;
const MAX_PROBE_INTERVAL_MS = 60_000;
const BACK_ONLINE_TOAST_MS = 2_800;

async function probeReachability(signal: AbortSignal): Promise<boolean> {
  for (const path of PROBE_PATHS) {
    try {
      const response = await fetch(path, {
        method: "HEAD",
        cache: "no-store",
        credentials: "omit",
        signal,
      });
      // Any HTTP response means the network path exists (including 401/403/404/500).
      if (response.status > 0) {
        return true;
      }
    } catch (error) {
      if (signal.aborted) {
        return false;
      }
      if (!isLikelyNetworkFailure(error)) {
        continue;
      }
    }
  }
  return false;
}

export function ConnectivityProvider({ children }: { children: ReactNode }) {
  const [browserOnline, setBrowserOnline] = useState(() =>
    typeof navigator === "undefined" ? true : navigator.onLine,
  );
  const [phase, setPhase] = useState<ConnectivityPhase>(() =>
    typeof navigator === "undefined" || navigator.onLine ? "online" : "offline",
  );
  const [showBackOnline, setShowBackOnline] = useState(false);
  const probeAbortRef = useRef<AbortController | null>(null);
  const backoffRef = useRef(MIN_PROBE_INTERVAL_MS);
  const probeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const flashTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const phaseRef = useRef(phase);
  phaseRef.current = phase;

  const clearProbeTimer = useCallback(() => {
    if (probeTimerRef.current) {
      clearTimeout(probeTimerRef.current);
      probeTimerRef.current = null;
    }
  }, []);

  const flashBackOnline = useCallback(() => {
    setShowBackOnline(true);
    if (flashTimerRef.current) {
      clearTimeout(flashTimerRef.current);
    }
    flashTimerRef.current = setTimeout(() => setShowBackOnline(false), BACK_ONLINE_TOAST_MS);
  }, []);

  const goOnline = useCallback(
    (announce: boolean) => {
      clearProbeTimer();
      backoffRef.current = MIN_PROBE_INTERVAL_MS;
      const wasOffline = phaseRef.current === "offline" || phaseRef.current === "reconnecting";
      setPhase("online");
      if (announce && wasOffline) {
        flashBackOnline();
      }
    },
    [clearProbeTimer, flashBackOnline],
  );

  const goOffline = useCallback(() => {
    setPhase("offline");
  }, []);

  const runProbe = useCallback(async (): Promise<boolean> => {
    probeAbortRef.current?.abort();
    const controller = new AbortController();
    probeAbortRef.current = controller;
    setPhase((current) => (current === "online" ? current : "reconnecting"));
    const ok = await probeReachability(controller.signal);
    if (controller.signal.aborted) {
      return false;
    }
    if (ok) {
      goOnline(true);
      return true;
    }
    goOffline();
    return false;
  }, [goOffline, goOnline]);

  const scheduleProbe = useCallback(() => {
    clearProbeTimer();
    const delay = backoffRef.current;
    backoffRef.current = Math.min(MAX_PROBE_INTERVAL_MS, Math.round(delay * 1.6));
    probeTimerRef.current = setTimeout(() => {
      void runProbe().then((ok) => {
        if (!ok && phaseRef.current !== "online") {
          scheduleProbe();
        }
      });
    }, delay);
  }, [clearProbeTimer, runProbe]);

  const reportNetworkFailure = useCallback(() => {
    if (phaseRef.current === "online") {
      goOffline();
      scheduleProbe();
    }
  }, [goOffline, scheduleProbe]);

  const reportNetworkSuccess = useCallback(() => {
    if (phaseRef.current !== "online") {
      goOnline(true);
    }
  }, [goOnline]);

  const retry = useCallback(async () => {
    backoffRef.current = MIN_PROBE_INTERVAL_MS;
    const ok = await runProbe();
    if (!ok) {
      scheduleProbe();
    }
    return ok;
  }, [runProbe, scheduleProbe]);

  useEffect(() => {
    return subscribeBrowserOnline((online) => {
      setBrowserOnline(online);
      if (!online) {
        goOffline();
        scheduleProbe();
        return;
      }
      setPhase((current) => (current === "online" ? current : "reconnecting"));
      void runProbe().then((ok) => {
        if (!ok) {
          scheduleProbe();
        }
      });
    });
  }, [goOffline, runProbe, scheduleProbe]);

  useEffect(() => {
    return () => {
      clearProbeTimer();
      probeAbortRef.current?.abort();
      if (flashTimerRef.current) {
        clearTimeout(flashTimerRef.current);
      }
    };
  }, [clearProbeTimer]);

  const value = useMemo<ConnectivitySnapshot>(
    () => ({
      phase,
      browserOnline,
      isOnline: phase === "online",
      blocksMutations: phase !== "online",
      showBackOnline,
      reportNetworkFailure,
      reportNetworkSuccess,
      retry,
    }),
    [
      browserOnline,
      phase,
      reportNetworkFailure,
      reportNetworkSuccess,
      retry,
      showBackOnline,
    ],
  );

  return <ConnectivityContext.Provider value={value}>{children}</ConnectivityContext.Provider>;
}

export function useConnectivity(): ConnectivitySnapshot {
  const context = useContext(ConnectivityContext);
  if (!context) {
    throw new Error("useConnectivity requires ConnectivityProvider");
  }
  return context;
}

/** Safe for SessionGuards mounted before or without ConnectivityProvider in tests. */
export function useOptionalConnectivity(): ConnectivitySnapshot | null {
  return useContext(ConnectivityContext);
}

/**
 * Hook-safe online signal for Organization and shared surfaces.
 * Falls back to browser online when provider is absent (unit tests / early boot).
 */
export function useAppOnline(): boolean {
  const context = useContext(ConnectivityContext);
  const [browserOnline, setBrowserOnline] = useState(() =>
    typeof navigator === "undefined" ? true : navigator.onLine,
  );
  useEffect(() => {
    if (context) {
      return;
    }
    return subscribeBrowserOnline(setBrowserOnline);
  }, [context]);
  return context?.isOnline ?? browserOnline;
}

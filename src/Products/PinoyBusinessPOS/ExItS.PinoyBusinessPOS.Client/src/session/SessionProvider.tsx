import { useQueryClient } from "@tanstack/react-query";
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
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import { clearPosAccessToken } from "@/api/platform/pos-access-token";
import { clearPosSessionGrant } from "@/api/platform/pos-session-grant";
import {
  fetchCurrentSession,
  loginWithPassword,
  logoutSession,
} from "@/api/platform/platform-auth-client";
import {
  isPlatformAntiforgeryValidationError,
  PlatformApiError,
  clearPlatformAntiforgeryToken,
} from "@/api/platform/platform-http";
import {
  evaluateColdStartOfflineGrant,
  synthesizeSessionFromGrant,
  type ColdStartGrantDenialReason,
  type StoredOfflineOperatingGrant,
} from "@/offline/offline-operating-grant";

export type SessionStatus =
  | "loading"
  | "authenticated"
  | "unauthenticated"
  | "expired"
  | "cold_start_offline";

export type SignOutResult =
  | { ok: true; reason: "logged_out" | "already_signed_out" }
  | { ok: false; detail: string };

type SessionContextValue = {
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  coldStartGrant: StoredOfflineOperatingGrant | null;
  coldStartDenial: ColdStartGrantDenialReason | null;
  signIn: (usernameOrEmail: string, password: string) => Promise<boolean>;
  signOut: () => Promise<SignOutResult>;
  refreshSession: () => Promise<SessionStatus>;
};

const SessionContext = createContext<SessionContextValue | null>(null);

function clearClientSessionArtifacts(queryClient: { clear: () => void }): void {
  queryClient.clear();
  clearPlatformAntiforgeryToken();
  clearPosAccessToken();
  clearPosSessionGrant();
}

function isBrowserOffline(): boolean {
  return typeof navigator !== "undefined" && navigator.onLine === false;
}

async function resolveBootstrapSession(): Promise<{
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  coldStartGrant: StoredOfflineOperatingGrant | null;
  coldStartDenial: ColdStartGrantDenialReason | null;
}> {
  try {
    const result = await fetchCurrentSession();
    if (result.status === "authenticated") {
      return {
        status: "authenticated",
        session: result.session,
        coldStartGrant: null,
        coldStartDenial: null,
      };
    }
    if (result.status === "expired") {
      return {
        status: "expired",
        session: null,
        coldStartGrant: null,
        coldStartDenial: null,
      };
    }
  } catch {
    // Network failure — fall through to offline cold-start grant when offline.
  }

  if (isBrowserOffline()) {
    const cold = await evaluateColdStartOfflineGrant();
    if (cold.ok) {
      return {
        status: "cold_start_offline",
        session: synthesizeSessionFromGrant(cold.grant),
        coldStartGrant: cold.grant,
        coldStartDenial: null,
      };
    }
    return {
      status: "unauthenticated",
      session: null,
      coldStartGrant: null,
      coldStartDenial: cold.reason,
    };
  }

  return {
    status: "unauthenticated",
    session: null,
    coldStartGrant: null,
    coldStartDenial: null,
  };
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const signInLock = useRef(false);
  const signOutLock = useRef(false);
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [session, setSession] = useState<BrowserSessionSnapshot | null>(null);
  const [coldStartGrant, setColdStartGrant] = useState<StoredOfflineOperatingGrant | null>(null);
  const [coldStartDenial, setColdStartDenial] = useState<ColdStartGrantDenialReason | null>(null);

  useEffect(() => {
    let cancelled = false;
    void resolveBootstrapSession().then((resolved) => {
      if (cancelled) {
        return;
      }
      setSession(resolved.session);
      setStatus(resolved.status);
      setColdStartGrant(resolved.coldStartGrant);
      setColdStartDenial(resolved.coldStartDenial);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const signIn = useCallback(
    async (usernameOrEmail: string, password: string) => {
      if (signInLock.current) {
        return false;
      }
      signInLock.current = true;
      try {
        clearClientSessionArtifacts(queryClient);
        const result = await loginWithPassword(usernameOrEmail, password);
        if (!result.ok) {
          return false;
        }
        setSession(result.session);
        setStatus("authenticated");
        setColdStartGrant(null);
        setColdStartDenial(null);
        return true;
      } finally {
        signInLock.current = false;
      }
    },
    [queryClient],
  );

  const signOut = useCallback(async (): Promise<SignOutResult> => {
    if (signOutLock.current) {
      return { ok: false, detail: "Sign out is already in progress." };
    }
    signOutLock.current = true;
    try {
      const reason = await logoutSession();
      clearClientSessionArtifacts(queryClient);
      setSession(null);
      setStatus("unauthenticated");
      setColdStartGrant(null);
      setColdStartDenial(null);
      return { ok: true, reason };
    } catch (error) {
      const detail =
        error instanceof PlatformApiError
          ? isPlatformAntiforgeryValidationError(error)
            ? "__ANTIFORGERY__"
            : (error.problem.detail ?? error.message)
          : "Sign out failed. Check your connection and try again.";
      return { ok: false, detail };
    } finally {
      signOutLock.current = false;
    }
  }, [queryClient]);

  const refreshSession = useCallback(async () => {
    const resolved = await resolveBootstrapSession();
    setSession(resolved.session);
    setStatus(resolved.status);
    setColdStartGrant(resolved.coldStartGrant);
    setColdStartDenial(resolved.coldStartDenial);
    if (resolved.status === "unauthenticated" || resolved.status === "expired") {
      clearClientSessionArtifacts(queryClient);
    }
    return resolved.status === "expired" ? "expired" : resolved.status;
  }, [queryClient]);

  const value = useMemo(
    () => ({
      status,
      session,
      coldStartGrant,
      coldStartDenial,
      signIn,
      signOut,
      refreshSession,
    }),
    [coldStartDenial, coldStartGrant, refreshSession, session, signIn, signOut, status],
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession() {
  const context = useContext(SessionContext);
  if (!context) {
    throw new Error("useSession must be used within SessionProvider");
  }
  return context;
}

export function isAuthenticatedOrColdStartOffline(status: SessionStatus): boolean {
  return status === "authenticated" || status === "cold_start_offline";
}

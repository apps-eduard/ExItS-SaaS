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
import { isOfflinePinAndDekConfigured, unlockOfflineCryptoWithPin } from "@/offline/local-store-key";
import { clearUnlockedDek } from "@/offline/offline-unlock-session";
import {
  evaluateColdStartOfflineGrant,
  peekStoredOfflineGrant,
  synthesizeSessionFromGrant,
  type ColdStartGrantDenialReason,
  type StoredOfflineOperatingGrant,
} from "@/offline/offline-operating-grant";

export type SessionStatus =
  | "loading"
  | "authenticated"
  | "unauthenticated"
  | "expired"
  | "cold_start_offline"
  | "offline_pin_required"
  | "needs_offline_unlock";

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
  unlockOfflinePin: (pin: string) => Promise<boolean>;
  enterColdStartOffline: () => void;
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
    // Network failure — fall through to offline grant evaluation when offline.
  }

  const cold = await evaluateColdStartOfflineGrant();
  if (cold.ok) {
    if (isBrowserOffline()) {
      if (isOfflinePinAndDekConfigured(cold.grant.userId)) {
        return {
          status: "offline_pin_required",
          session: null,
          coldStartGrant: cold.grant,
          coldStartDenial: null,
        };
      }
      return {
        status: "unauthenticated",
        session: null,
        coldStartGrant: null,
        coldStartDenial: "no_grant",
      };
    }
    return {
      status: "needs_offline_unlock",
      session: null,
      coldStartGrant: cold.grant,
      coldStartDenial: null,
    };
  }

  if (isBrowserOffline()) {
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

  const enterColdStartOffline = useCallback(() => {
    if (!coldStartGrant) {
      return;
    }
    setSession(synthesizeSessionFromGrant(coldStartGrant));
    setStatus("cold_start_offline");
  }, [coldStartGrant]);

  const unlockOfflinePin = useCallback(
    async (pin: string) => {
      const grant = coldStartGrant ?? (session?.userId ? peekStoredOfflineGrant(session.userId) : null);
      if (!grant) {
        return false;
      }
      const unlocked = await unlockOfflineCryptoWithPin(grant.userId, pin);
      if (!unlocked) {
        return false;
      }
      if (status === "offline_pin_required" || status === "needs_offline_unlock") {
        setColdStartGrant(grant);
        setSession(synthesizeSessionFromGrant(grant));
        setStatus("cold_start_offline");
        setColdStartDenial(null);
      }
      return true;
    },
    [coldStartGrant, session?.userId, status],
  );

  const signIn = useCallback(
    async (usernameOrEmail: string, password: string) => {
      if (signInLock.current) {
        return false;
      }
      signInLock.current = true;
      try {
        clearClientSessionArtifacts(queryClient);
        clearUnlockedDek();
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
      clearUnlockedDek();
      setSession(null);
      setColdStartGrant(null);
      setColdStartDenial(null);

      const cold = await evaluateColdStartOfflineGrant();
      if (cold.ok) {
        setColdStartGrant(cold.grant);
        setStatus("needs_offline_unlock");
      } else {
        setStatus("unauthenticated");
      }
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
      clearUnlockedDek();
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
      unlockOfflinePin,
      enterColdStartOffline,
    }),
    [
      coldStartDenial,
      coldStartGrant,
      enterColdStartOffline,
      refreshSession,
      session,
      signIn,
      signOut,
      status,
      unlockOfflinePin,
    ],
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

export function isOfflinePinFlowStatus(status: SessionStatus): boolean {
  return status === "offline_pin_required" || status === "needs_offline_unlock";
}

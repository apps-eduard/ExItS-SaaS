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
import { useLocation, useNavigate } from "react-router-dom";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import { clearPosAccessToken } from "@/api/platform/pos-access-token";
import { clearPosSessionGrant } from "@/api/platform/pos-session-grant";
import {
  fetchCurrentSession,
  loginWithPassword,
  logoutSession,
} from "@/api/platform/platform-auth-client";
import {
  clearPlatformAntiforgeryToken,
  prefetchPlatformAntiforgeryToken,
  refreshPlatformAntiforgeryToken,
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
import {
  clearPendingRemoteLogout,
  completePendingRemoteLogoutIfNeeded,
  hasPendingRemoteLogout,
  markPendingRemoteLogout,
} from "@/session/remote-logout-retry";
import {
  buildAuthLoginFailure,
  type AuthLoginFailureDiagnostic,
} from "@/diagnostics/auth-login-failure";

export type SessionStatus =
  | "loading"
  | "authenticated"
  | "unauthenticated"
  | "expired"
  | "cold_start_offline"
  | "offline_pin_required"
  | "needs_offline_unlock";

export type SignOutResult =
  | {
      ok: true;
      reason: "logged_out" | "already_signed_out";
      remoteLogoutPending?: boolean;
      nextRoute: "/sign-in" | "/offline-pin";
    }
  | { ok: false; detail: string };

export type SignInResult = { ok: true } | { ok: false; failure: AuthLoginFailureDiagnostic };

type SessionContextValue = {
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  coldStartGrant: StoredOfflineOperatingGrant | null;
  coldStartDenial: ColdStartGrantDenialReason | null;
  signIn: (usernameOrEmail: string, password: string) => Promise<SignInResult>;
  signOut: () => Promise<SignOutResult>;
  refreshSession: () => Promise<SessionStatus>;
  unlockOfflinePin: (pin: string) => Promise<boolean>;
  enterColdStartOffline: () => void;
  prepareOfflinePinUnlock: (grant: StoredOfflineOperatingGrant) => void;
};

const SessionContext = createContext<SessionContextValue | null>(null);

import { ACCOUNT_CONTEXT_SWITCH_PATH } from "@/features/account/account-context-switch-route";

const PIN_SETUP_PATH = "/offline-pin-setup";
const PIN_UNLOCK_PATH = "/offline-pin";
const SIGN_IN_PATH = "/sign-in";

/** Redirect authenticated users without an offline PIN to enrollment. */
export function OfflinePinSetupGate({ children }: { children: ReactNode }) {
  const sessionState = useContext(SessionContext);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (!sessionState) {
      return;
    }
    const { status, session } = sessionState;
    if (status !== "authenticated" || !session?.userId) {
      return;
    }
    const path = location.pathname;
    if (
      path === PIN_SETUP_PATH ||
      path === PIN_UNLOCK_PATH ||
      path === SIGN_IN_PATH ||
      path === ACCOUNT_CONTEXT_SWITCH_PATH
    ) {
      return;
    }
    if (!isOfflinePinAndDekConfigured(session.userId)) {
      navigate(PIN_SETUP_PATH, { replace: true, state: { from: path } });
    }
  }, [location.pathname, navigate, sessionState]);

  return children;
}

function clearPosCredentialArtifacts(): void {
  clearPosAccessToken();
  clearPosSessionGrant();
}

function clearClientSessionArtifacts(queryClient: { clear: () => void }): void {
  queryClient.clear();
  clearPlatformAntiforgeryToken();
  clearPosCredentialArtifacts();
}

function isBrowserOffline(): boolean {
  return typeof navigator !== "undefined" && navigator.onLine === false;
}

async function resolveOfflineLockedBootstrap(): Promise<{
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  coldStartGrant: StoredOfflineOperatingGrant | null;
  coldStartDenial: ColdStartGrantDenialReason | null;
}> {
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

async function resolveBootstrapSession(): Promise<{
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  coldStartGrant: StoredOfflineOperatingGrant | null;
  coldStartDenial: ColdStartGrantDenialReason | null;
}> {
  if (hasPendingRemoteLogout()) {
    const cleared = await completePendingRemoteLogoutIfNeeded();
    if (!cleared) {
      clearPlatformAntiforgeryToken();
      clearPosAccessToken();
      clearPosSessionGrant();
      return resolveOfflineLockedBootstrap();
    }
  }

  try {
    const result = await fetchCurrentSession();
    if (result.status === "authenticated") {
      clearPendingRemoteLogout();
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

  return resolveOfflineLockedBootstrap();
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
    void prefetchPlatformAntiforgeryToken();
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

  const prepareOfflinePinUnlock = useCallback((grant: StoredOfflineOperatingGrant) => {
    setColdStartGrant(grant);
    setColdStartDenial(null);
  }, []);

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
      setColdStartGrant(grant);
      setSession(synthesizeSessionFromGrant(grant));
      setStatus("cold_start_offline");
      setColdStartDenial(null);
      return true;
    },
    [coldStartGrant, session?.userId],
  );

  const signIn = useCallback(
    async (usernameOrEmail: string, password: string): Promise<SignInResult> => {
      if (signInLock.current) {
        return {
          ok: false,
          failure: buildAuthLoginFailure(new Error("Sign in is already in progress.")),
        };
      }
      signInLock.current = true;
      try {
        queryClient.clear();
        clearPosCredentialArtifacts();
        clearUnlockedDek();
        clearPendingRemoteLogout();
        const result = await loginWithPassword(usernameOrEmail, password);
        if (!result.ok) {
          return result;
        }
        await refreshPlatformAntiforgeryToken();
        setSession(result.session);
        setStatus("authenticated");
        setColdStartGrant(null);
        setColdStartDenial(null);
        return { ok: true };
      } catch (error) {
        return { ok: false, failure: buildAuthLoginFailure(error) };
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
      clearClientSessionArtifacts(queryClient);
      clearUnlockedDek();
      setSession(null);
      setColdStartDenial(null);

      const cold = await evaluateColdStartOfflineGrant();
      let nextRoute: "/sign-in" | "/offline-pin" = "/sign-in";
      if (cold.ok) {
        setColdStartGrant(cold.grant);
        if (isOfflinePinAndDekConfigured(cold.grant.userId)) {
          setStatus(isBrowserOffline() ? "offline_pin_required" : "needs_offline_unlock");
          nextRoute = "/offline-pin";
        } else {
          setStatus("unauthenticated");
          setColdStartGrant(null);
        }
      } else {
        setColdStartGrant(null);
        setStatus("unauthenticated");
      }

      try {
        const reason = await logoutSession();
        clearPendingRemoteLogout();
        return { ok: true, reason, nextRoute };
      } catch {
        markPendingRemoteLogout();
        return { ok: true, reason: "logged_out", remoteLogoutPending: true, nextRoute };
      }
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
      prepareOfflinePinUnlock,
    }),
    [
      coldStartDenial,
      coldStartGrant,
      enterColdStartOffline,
      prepareOfflinePinUnlock,
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

/** @internal Vitest hook for bootstrap behavior without mounting SessionProvider. */
export async function resolveBootstrapSessionForTests() {
  return resolveBootstrapSession();
}

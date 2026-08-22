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
import { isPlatformAntiforgeryValidationError, PlatformApiError, clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

export type SessionStatus = "loading" | "authenticated" | "unauthenticated" | "expired";

export type SignOutResult =
  { ok: true; reason: "logged_out" | "already_signed_out" } | { ok: false; detail: string };

type SessionContextValue = {
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
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

export function SessionProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const signInLock = useRef(false);
  const signOutLock = useRef(false);
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [session, setSession] = useState<BrowserSessionSnapshot | null>(null);

  useEffect(() => {
    let cancelled = false;
    void fetchCurrentSession()
      .then((result) => {
        if (cancelled) {
          return;
        }
        setSession(result.session);
        setStatus(result.status === "expired" ? "expired" : result.status);
      })
      .catch(() => {
        if (!cancelled) {
          setSession(null);
          setStatus("unauthenticated");
        }
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
        // Prevent prior POS bearer/grant from leaking into the next login principal.
        clearClientSessionArtifacts(queryClient);
        const result = await loginWithPassword(usernameOrEmail, password);
        if (!result.ok) {
          return false;
        }
        setSession(result.session);
        setStatus("authenticated");
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
      return { ok: true, reason };
    } catch (error) {
      // Do not pretend success when the server logout mutation failed.
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
    const result = await fetchCurrentSession();
    setSession(result.session);
    setStatus(result.status === "expired" ? "expired" : result.status);
    if (result.status === "unauthenticated" || result.status === "expired") {
      clearClientSessionArtifacts(queryClient);
    }
    return result.status === "expired" ? "expired" : result.status;
  }, [queryClient]);

  const value = useMemo(
    () => ({ status, session, signIn, signOut, refreshSession }),
    [refreshSession, session, signIn, signOut, status],
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

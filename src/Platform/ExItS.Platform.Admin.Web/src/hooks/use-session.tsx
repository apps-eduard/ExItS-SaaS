import { useQueryClient } from "@tanstack/react-query";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { getAuthMe, login as loginRequest, logout as logoutRequest } from "@/api/auth/auth-client";
import { isNetworkFailure, isSessionInvalidError } from "@/api/auth/auth-errors";
import {
  resetAuthenticationLostLatch,
  setAuthenticationLostHandler,
} from "@/api/auth/session-expiry";
import type { AuthSession } from "@/api/auth/auth-types";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import { env } from "@/lib/env";
import { isAbortError } from "@/lib/diagnostics/diagnostic-redaction";
import { useDiagnostics } from "@/hooks/use-diagnostics";

export type SessionStatus = "loading" | "authenticated" | "unauthenticated" | "expired";

type SessionContextValue = {
  status: SessionStatus;
  session: AuthSession | null;
  signIn: (usernameOrEmail: string, password: string) => Promise<AuthSession>;
  signOut: () => Promise<void>;
  markExpired: () => void;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { report } = useDiagnostics();
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [session, setSession] = useState<AuthSession | null>(null);

  const clearClientState = useCallback(() => {
    clearPlatformAntiforgeryToken();
    queryClient.clear();
  }, [queryClient]);

  const markExpired = useCallback(() => {
    setSession(null);
    setStatus("expired");
    clearClientState();
  }, [clearClientState]);

  useEffect(() => {
    setAuthenticationLostHandler(() => {
      markExpired();
    });
    return () => setAuthenticationLostHandler(null);
  }, [markExpired]);

  useEffect(() => {
    const controller = new AbortController();

    void (async () => {
      try {
        const next = await getAuthMe(env.platformApiBaseUrl, controller.signal);
        if (controller.signal.aborted) {
          return;
        }
        setSession(next);
        setStatus("authenticated");
        resetAuthenticationLostLatch();
      } catch (error) {
        if (controller.signal.aborted || isAbortError(error)) {
          return;
        }
        setSession(null);
        setStatus("unauthenticated");
        if (!isSessionInvalidError(error) && !isNetworkFailure(error)) {
          report(error, { operation: "Load session" });
        }
      }
    })();

    return () => controller.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- session bootstrap once
  }, []);

  const signIn = useCallback(async (usernameOrEmail: string, password: string) => {
    const next = await loginRequest(env.platformApiBaseUrl, { usernameOrEmail, password });
    resetAuthenticationLostLatch();
    setSession(next);
    setStatus("authenticated");
    return next;
  }, []);

  const signOut = useCallback(async () => {
    try {
      await logoutRequest(env.platformApiBaseUrl);
    } catch (error) {
      if (!isSessionInvalidError(error)) {
        report(error, {
          operation: "Sign out",
          category: isNetworkFailure(error) ? "NETWORK_ERROR" : "UNEXPECTED_CLIENT_ERROR",
        });
        throw error;
      }
    }
    setSession(null);
    setStatus("unauthenticated");
    clearClientState();
    resetAuthenticationLostLatch();
  }, [clearClientState, report]);

  const value = useMemo<SessionContextValue>(
    () => ({ status, session, signIn, signOut, markExpired }),
    [status, session, signIn, signOut, markExpired],
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession(): SessionContextValue {
  const context = useContext(SessionContext);
  if (!context) {
    throw new Error("useSession must be used within SessionProvider.");
  }
  return context;
}

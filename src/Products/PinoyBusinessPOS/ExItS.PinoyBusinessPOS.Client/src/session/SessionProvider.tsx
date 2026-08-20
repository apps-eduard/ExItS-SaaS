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
import {
  fetchCurrentSession,
  loginWithPassword,
  logoutSession,
} from "@/api/platform/platform-auth-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

export type SessionStatus = "loading" | "authenticated" | "unauthenticated" | "expired";

type SessionContextValue = {
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  signIn: (usernameOrEmail: string, password: string) => Promise<boolean>;
  signOut: () => Promise<void>;
  refreshSession: () => Promise<SessionStatus>;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const signInLock = useRef(false);
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

  const signIn = useCallback(async (usernameOrEmail: string, password: string) => {
    if (signInLock.current) {
      return false;
    }
    signInLock.current = true;
    try {
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
  }, []);

  const signOut = useCallback(async () => {
    await logoutSession();
    queryClient.clear();
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
    setSession(null);
    setStatus("unauthenticated");
  }, [queryClient]);

  const refreshSession = useCallback(async () => {
    const result = await fetchCurrentSession();
    setSession(result.session);
    setStatus(result.status === "expired" ? "expired" : result.status);
    return result.status === "expired" ? "expired" : result.status;
  }, []);

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

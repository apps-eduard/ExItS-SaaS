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
import type { BrowserSessionSnapshot } from "@/api/platform-auth/browser-session";
import {
  fetchCurrentSession,
  loginWithPassword,
  logoutSession,
} from "@/api/platform-auth/platform-auth-client";

export type SessionStatus = "loading" | "authenticated" | "unauthenticated" | "expired";

type SessionContextValue = {
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  signIn: (usernameOrEmail: string, password: string) => Promise<boolean>;
  signOut: () => Promise<void>;
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
        setStatus(result.status);
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
    setSession(null);
    setStatus("unauthenticated");
  }, [queryClient]);

  const value = useMemo(
    () => ({ status, session, signIn, signOut }),
    [session, signIn, signOut, status],
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

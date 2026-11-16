import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { currentSession, logoutSession, type PlatformSession } from "./platform-client";

export type SessionStatus = "loading" | "authenticated" | "anonymous" | "unreachable";

type SessionContextValue = {
  status: SessionStatus;
  session: PlatformSession | null;
  logout: () => Promise<void>;
  reload: () => Promise<void>;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [session, setSession] = useState<PlatformSession | null>(null);

  const reload = useCallback(async () => {
    try {
      const next = await currentSession();
      setSession(next);
      setStatus(next ? "authenticated" : "anonymous");
    } catch {
      setSession(null);
      setStatus("unreachable");
    }
  }, []);

  useEffect(() => {
    const onPageShow = (event: PageTransitionEvent) => {
      if (event.persisted) {
        setSession(null);
        setStatus("loading");
      }
      void reload();
    };
    const timer = window.setTimeout(() => void reload(), 0);
    window.addEventListener("pageshow", onPageShow);
    return () => {
      window.clearTimeout(timer);
      window.removeEventListener("pageshow", onPageShow);
    };
  }, [reload]);

  const logout = useCallback(async () => {
    try {
      await logoutSession();
    } finally {
      setSession(null);
      setStatus("anonymous");
    }
  }, []);

  const value = useMemo(() => ({ status, session, logout, reload }), [status, session, logout, reload]);

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession() {
  const value = useContext(SessionContext);
  if (!value) throw new Error("useSession must be used within SessionProvider");
  return value;
}

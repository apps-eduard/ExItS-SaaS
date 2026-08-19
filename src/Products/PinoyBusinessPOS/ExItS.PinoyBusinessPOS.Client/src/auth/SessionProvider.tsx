import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { ApiClientError } from "@/api/http";
import {
  fetchPlatformSession,
  loginPlatformSession,
  logoutPlatformSession,
} from "@/auth/platform-session";
import type { PlatformSessionSnapshot } from "@/auth/session-fields";

export type SessionStatus = "checking" | "unauthenticated" | "authenticated";

type SessionContextValue = {
  status: SessionStatus;
  session: PlatformSessionSnapshot | null;
  signIn: (usernameOrEmail: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<SessionStatus>("checking");
  const [session, setSession] = useState<PlatformSessionSnapshot | null>(null);

  useEffect(() => {
    let cancelled = false;
    void fetchPlatformSession()
      .then((next) => {
        if (cancelled) {
          return;
        }
        setSession(next);
        setStatus(next ? "authenticated" : "unauthenticated");
      })
      .catch(() => {
        if (cancelled) {
          return;
        }
        setSession(null);
        setStatus("unauthenticated");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const value = useMemo<SessionContextValue>(
    () => ({
      status,
      session,
      async signIn(usernameOrEmail: string, password: string) {
        const next = await loginPlatformSession(usernameOrEmail, password);
        setSession(next);
        setStatus("authenticated");
      },
      async signOut() {
        try {
          await logoutPlatformSession();
        } catch (error) {
          if (!(error instanceof ApiClientError) || error.status >= 500) {
            throw error;
          }
        }
        setSession(null);
        setStatus("unauthenticated");
      },
    }),
    [session, status],
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

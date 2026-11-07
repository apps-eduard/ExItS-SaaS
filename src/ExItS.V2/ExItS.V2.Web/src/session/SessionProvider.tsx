import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  fetchCurrentSession,
  loginWithPassword,
  logoutSession,
  type BrowserSessionSnapshot,
} from "@/api/auth";
import { clearPosAccessToken } from "@/api/pos-access-token";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import { V2_PERSONAL_ONLY_HOME } from "./account-class";
import {
  ensurePersonalSessionProfile,
  shouldEnsurePersonalHomeProfile,
} from "./ensure-personal-profile";

export type SessionStatus =
  | "loading"
  | "authenticated"
  | "unauthenticated"
  | "expired";

type SessionContextValue = {
  status: SessionStatus;
  session: BrowserSessionSnapshot | null;
  signIn: (
    usernameOrEmail: string,
    password: string,
  ) => Promise<{ ok: true } | { ok: false; detail: string }>;
  signOut: () => Promise<void>;
  refreshSession: () => Promise<SessionStatus>;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [session, setSession] = useState<BrowserSessionSnapshot | null>(null);

  const refreshSession = useCallback(async () => {
    try {
      const result = await fetchCurrentSession();
      setStatus(result.status);
      setSession(result.session);
      if (result.status !== "authenticated") {
        clearPosAccessToken();
      }
      return result.status;
    } catch {
      setStatus("unauthenticated");
      setSession(null);
      clearPosAccessToken();
      return "unauthenticated";
    }
  }, []);

  const alignPersonalHomeSession = useCallback(
    async (incoming: BrowserSessionSnapshot) => {
      if (!V2_PERSONAL_ONLY_HOME || !shouldEnsurePersonalHomeProfile(incoming)) {
        return { ok: true as const, session: incoming };
      }
      clearPosAccessToken();
      return ensurePersonalSessionProfile({
        session: incoming,
        refreshSession,
      });
    },
    [refreshSession],
  );

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const result = await fetchCurrentSession();
        if (cancelled) return;
        if (result.status !== "authenticated" || !result.session) {
          setStatus(result.status);
          setSession(null);
          clearPosAccessToken();
          return;
        }

        const aligned = await alignPersonalHomeSession(result.session);
        if (cancelled) return;
        if (aligned.ok) {
          setSession(aligned.session);
          setStatus("authenticated");
          return;
        }

        // Staff-locked / Platform: keep session; Home shows deferred copy.
        setSession(result.session);
        setStatus("authenticated");
      } catch {
        if (cancelled) return;
        setStatus("unauthenticated");
        setSession(null);
        clearPosAccessToken();
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [alignPersonalHomeSession]);

  const signIn = useCallback(
    async (usernameOrEmail: string, password: string) => {
      const result = await loginWithPassword(usernameOrEmail, password);
      if (!result.ok) {
        return { ok: false as const, detail: result.detail };
      }

      const aligned = await alignPersonalHomeSession(result.session);
      if (!aligned.ok) {
        // Still authenticated as Organization staff / Platform — do not invent Personal.
        setSession(result.session);
        setStatus("authenticated");
        return { ok: true as const };
      }

      setSession(aligned.session);
      setStatus("authenticated");
      return { ok: true as const };
    },
    [alignPersonalHomeSession],
  );

  const signOut = useCallback(async () => {
    await logoutSession();
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
    setSession(null);
    setStatus("unauthenticated");
  }, []);

  const value = useMemo(
    () => ({ status, session, signIn, signOut, refreshSession }),
    [status, session, signIn, signOut, refreshSession],
  );

  return (
    <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
  );
}

export function useSession() {
  const ctx = useContext(SessionContext);
  if (!ctx) {
    throw new Error("useSession must be used within SessionProvider");
  }
  return ctx;
}

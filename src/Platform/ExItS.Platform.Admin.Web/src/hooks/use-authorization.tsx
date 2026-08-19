import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getMyAuthorization } from "@/api/authorization/authorization-client";
import type { ResolvedPermissionsDto } from "@/api/authorization/authorization-types";
import { env } from "@/lib/env";
import { useSession } from "@/hooks/use-session";

export type AuthorizationStatus = "loading" | "loaded" | "failed";

type AuthorizationContextValue = {
  status: AuthorizationStatus;
  permissions: ReadonlySet<string>;
  actorType: string | null;
  hasPermission: (code: string) => boolean;
  hasAnyPermission: (codes: readonly string[]) => boolean;
  isPlatformAdministrator: boolean;
};

const AuthorizationContext = createContext<AuthorizationContextValue | null>(null);

function isPlatformActor(
  actorType: string | null,
  accountClass: string | null | undefined,
): boolean {
  return (
    accountClass === "Platform" ||
    actorType === "PlatformUser" ||
    actorType === "DevelopmentOperator"
  );
}

export function AuthorizationProvider({ children }: { children: ReactNode }) {
  const { session } = useSession();
  const [status, setStatus] = useState<AuthorizationStatus>("loading");
  const [snapshot, setSnapshot] = useState<ResolvedPermissionsDto | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    void (async () => {
      try {
        const next = await getMyAuthorization(env.platformApiBaseUrl, controller.signal);
        if (controller.signal.aborted) {
          return;
        }
        setSnapshot(next);
        setStatus("loaded");
      } catch {
        if (controller.signal.aborted) {
          return;
        }
        setSnapshot(null);
        setStatus("failed");
      }
    })();

    return () => controller.abort();
  }, []);

  const value = useMemo<AuthorizationContextValue>(() => {
    const permissions = new Set(snapshot?.permissions ?? []);
    const actorType = snapshot?.actorType ?? null;
    const hasPermission = (code: string) => permissions.has(code);
    const hasAnyPermission = (codes: readonly string[]) =>
      codes.some((code) => permissions.has(code));
    const isPlatformAdministrator =
      status === "loaded" &&
      permissions.size > 0 &&
      isPlatformActor(actorType, session?.accountClass);

    return {
      status,
      permissions,
      actorType,
      hasPermission,
      hasAnyPermission,
      isPlatformAdministrator,
    };
  }, [snapshot, status, session?.accountClass]);

  return <AuthorizationContext.Provider value={value}>{children}</AuthorizationContext.Provider>;
}

export function useAuthorization(): AuthorizationContextValue {
  const context = useContext(AuthorizationContext);
  if (!context) {
    throw new Error("useAuthorization must be used within AuthorizationProvider.");
  }
  return context;
}

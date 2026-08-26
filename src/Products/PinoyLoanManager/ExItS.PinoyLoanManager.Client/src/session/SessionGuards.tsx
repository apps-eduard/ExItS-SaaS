import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function SessionLoading() {
  const { t } = useI18n();
  return (
    <div className="flex min-h-dvh items-center justify-center bg-background px-4">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
        {t("session.loading")}
      </p>
    </div>
  );
}

export function RequireSession({ children }: { children: ReactNode }) {
  const { status } = useSession();
  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/sign-in" replace />;
  }
  return children;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useSession();
  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }
  return children;
}

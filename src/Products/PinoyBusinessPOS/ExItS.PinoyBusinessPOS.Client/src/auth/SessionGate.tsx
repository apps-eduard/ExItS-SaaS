import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useSession } from "@/auth/SessionProvider";
import { LoadingState } from "@/components/ui/skeleton";
import { useI18n } from "@/i18n/I18nProvider";

export function SessionGate() {
  const { t } = useI18n();
  const { status } = useSession();
  const location = useLocation();
  const onSignIn = location.pathname === "/sign-in";

  if (status === "checking") {
    return (
      <div className="flex min-h-dvh items-center justify-center bg-background px-[var(--exits-page-padding)] pt-[env(safe-area-inset-top)]">
        <LoadingState label={t("auth.checking")} />
      </div>
    );
  }

  if (status === "unauthenticated") {
    return onSignIn ? <Outlet /> : <Navigate to="/sign-in" replace />;
  }

  return onSignIn ? <Navigate to="/" replace /> : <Outlet />;
}

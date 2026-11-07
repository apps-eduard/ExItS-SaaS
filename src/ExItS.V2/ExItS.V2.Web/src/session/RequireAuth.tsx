import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { Navigate, useLocation } from "react-router-dom";
import { Spinner } from "@/components/ui/spinner";
import { useSession } from "./SessionProvider";

function RequireAuth({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { status } = useSession();
  const location = useLocation();

  if (status === "loading") {
    return (
      <div className="flex min-h-dvh items-center justify-center bg-background">
        <Spinner label={t("auth.loadingSession")} />
      </div>
    );
  }

  if (status === "unauthenticated" || status === "expired") {
    return (
      <Navigate
        to="/sign-in"
        replace
        state={{ from: location.pathname, expired: status === "expired" }}
      />
    );
  }

  return children;
}

export { RequireAuth };

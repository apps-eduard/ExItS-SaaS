import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useSession } from "@/hooks/use-session";
import { Skeleton } from "@/components/ui/skeleton";
import { buildLoginPath } from "@/lib/auth/safe-return-path";

export function RequireSession({ children }: { children: ReactNode }) {
  const { status } = useSession();
  const location = useLocation();

  if (status === "loading") {
    return (
      <div className="flex min-h-dvh items-center justify-center p-6">
        <Skeleton className="h-24 w-full max-w-sm" />
      </div>
    );
  }

  if (status === "authenticated") {
    return children;
  }

  return (
    <Navigate
      to={buildLoginPath({
        returnPath: `${location.pathname}${location.search}`,
        notice: status === "expired" ? "session-expired" : undefined,
      })}
      replace
    />
  );
}

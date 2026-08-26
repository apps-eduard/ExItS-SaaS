import type { ReactNode } from "react";
import { Navigate, useSearchParams } from "react-router-dom";
import { Skeleton } from "@/components/ui/skeleton";
import { useSession } from "@/hooks/use-session";
import { resolvePostLoginPath } from "@/lib/auth/safe-return-path";

export function RedirectIfAuthenticated({ children }: { children: ReactNode }) {
  const { status } = useSession();
  const [params] = useSearchParams();

  if (status === "loading") {
    return (
      <div className="flex min-h-dvh items-center justify-center p-6">
        <Skeleton className="h-24 w-full max-w-sm" />
      </div>
    );
  }

  if (status === "authenticated") {
    return <Navigate to={resolvePostLoginPath(params.get("return"))} replace />;
  }

  return children;
}

import { Skeleton } from "@/components/ui/skeleton";
import { UnderDevelopmentPage } from "@/features/overview/UnderDevelopmentPage";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { useLocation } from "react-router-dom";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { resolveKnownReactRoute } from "@/lib/navigation/known-react-routes";

export function ShellCatchAllPage() {
  const location = useLocation();
  const authorization = useAuthorization();
  const resolution = resolveKnownReactRoute({
    pathname: location.pathname,
    permissionStatus: authorization.status,
    hasAnyPermission: authorization.hasAnyPermission,
    isPlatformAdministrator: authorization.isPlatformAdministrator,
    developmentToolsAllowed: areDevelopmentToolsAllowed(),
  });

  if (resolution === "pending") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (resolution === "under-development") {
    return <UnderDevelopmentPage />;
  }

  return <ShellNotFoundPage />;
}

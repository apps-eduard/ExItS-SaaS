import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { ForbiddenState } from "@/components/exits/ForbiddenState";
import { LoadingState } from "@/components/exits/LoadingState";
import { useAuthorization } from "@/hooks/use-authorization";

type GlobalCatalogViewPermission =
  | typeof PLATFORM_PERMISSIONS.viewGlobalCatalog
  | typeof PLATFORM_PERMISSIONS.importGlobalProducts;

export function useGlobalCatalogPageGate(viewPermission: GlobalCatalogViewPermission = PLATFORM_PERMISSIONS.viewGlobalCatalog) {
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" && authorization.hasPermission(viewPermission);

  if (authorization.status === "loading") {
    return {
      status: "loading" as const,
      canView: false,
      gate: <LoadingState rows={3} />,
    };
  }

  if (!canView) {
    return {
      status: "forbidden" as const,
      canView: false,
      gate: <ForbiddenState requiredPermission={viewPermission} />,
    };
  }

  return {
    status: "ready" as const,
    canView: true,
    gate: null,
  };
}

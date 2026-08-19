import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { useAuthorization } from "@/hooks/use-authorization";

export function useDashboardAuthorization() {
  const authorization = useAuthorization();
  const loaded = authorization.status === "loaded";

  return {
    status: authorization.status,
    canViewOrganizations:
      loaded &&
      authorization.hasAnyPermission([
        PLATFORM_PERMISSIONS.viewPortfolio,
        PLATFORM_PERMISSIONS.manageOrganizations,
      ]),
    canViewSubscriptions:
      loaded && authorization.hasPermission(PLATFORM_PERMISSIONS.manageSubscriptions),
    canReviewAccounts:
      loaded && authorization.hasPermission(PLATFORM_PERMISSIONS.managePlatformUsers),
    canViewAudit: loaded && authorization.hasPermission(PLATFORM_PERMISSIONS.viewAuditRecords),
    canViewHealth: loaded && authorization.isPlatformAdministrator,
  };
}

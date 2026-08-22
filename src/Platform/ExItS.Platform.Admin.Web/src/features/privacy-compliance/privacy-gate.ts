import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import { useAuthorization } from "@/hooks/use-authorization";

export function usePrivacyViewGate() {
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewPrivacyCompliance);

  return { authorization, canView };
}

export function privacyForbiddenFromError(error: unknown): boolean {
  return (
    error instanceof PlatformApiError && (error.status === 401 || error.status === 403)
  );
}

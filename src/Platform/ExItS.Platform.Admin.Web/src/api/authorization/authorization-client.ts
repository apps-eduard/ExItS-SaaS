import { platformRequest } from "@/api/platform-http";
import type { ResolvedPermissionsDto } from "@/api/authorization/authorization-types";

export function getMyAuthorization(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<ResolvedPermissionsDto> {
  return platformRequest<ResolvedPermissionsDto>(baseUrl, {
    path: "/api/v1/platform/authorization/me",
    signal,
  });
}

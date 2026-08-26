import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { roleAssignmentsRequestPath } from "@/api/authorization/assignment-list-query";
import type {
  PlatformRoleAssignment,
  RoleAssignmentsQuery,
} from "@/api/authorization/assignment-types";
import type { ResolvedPermissionsDto } from "@/api/authorization/authorization-types";

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

export function mapPlatformRoleAssignment(payload: unknown): PlatformRoleAssignment {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid platform role assignment.");
  }
  const record = payload as Record<string, unknown>;
  const id = readString(record, "id", "Id");
  const platformUserId = readString(record, "platformUserId", "PlatformUserId");
  const role = readString(record, "role", "Role");
  const status = readString(record, "status", "Status");
  const grantedByActor = readString(record, "grantedByActor", "GrantedByActor");
  const grantedAtUtc = readString(record, "grantedAtUtc", "GrantedAtUtc");
  if (!id || !platformUserId || !role || !status || !grantedByActor || !grantedAtUtc) {
    throw new Error("Invalid platform role assignment.");
  }
  return {
    id,
    platformUserId,
    role,
    status,
    grantedByActor,
    grantedAtUtc,
    organizationId: readString(record, "organizationId", "OrganizationId"),
    reason: readString(record, "reason", "Reason"),
    revokedByActor: readString(record, "revokedByActor", "RevokedByActor"),
    revokedAtUtc: readString(record, "revokedAtUtc", "RevokedAtUtc"),
    revokeReason: readString(record, "revokeReason", "RevokeReason"),
  };
}

export function listPlatformRoleAssignments(
  baseUrl: string,
  options: RoleAssignmentsQuery,
): Promise<PagedResult<PlatformRoleAssignment>> {
  return platformRequest<unknown>(baseUrl, {
    path: roleAssignmentsRequestPath(options),
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapPlatformRoleAssignment),
    };
  });
}

export function getMyAuthorization(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<ResolvedPermissionsDto> {
  return platformRequest<ResolvedPermissionsDto>(baseUrl, {
    path: "/api/v1/platform/authorization/me",
    signal,
  });
}

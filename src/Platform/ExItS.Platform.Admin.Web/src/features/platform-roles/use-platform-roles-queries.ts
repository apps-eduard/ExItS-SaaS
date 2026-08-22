import { useQuery } from "@tanstack/react-query";
import {
  getPlatformRoleDefinition,
  listPlatformPermissions,
  listPlatformRoleDefinitions,
} from "@/api/platform-roles/platform-roles-client";
import type { PlatformRolesUrlState } from "@/api/platform-roles/platform-roles-query";
import { env } from "@/lib/env";

export const platformPermissionsQueryKey = ["platform-roles", "permissions"] as const;

export const platformRolesListQueryKey = (state: PlatformRolesUrlState) =>
  ["platform-roles", "list", state.search, state.kind, state.status, state.page] as const;

export const platformRoleDetailQueryKey = (roleId: string) =>
  ["platform-roles", "detail", roleId] as const;

export function usePlatformPermissionsQuery(enabled: boolean) {
  return useQuery({
    queryKey: platformPermissionsQueryKey,
    enabled,
    queryFn: ({ signal }) => listPlatformPermissions(env.platformApiBaseUrl, signal),
  });
}

export function usePlatformRolesListQuery(enabled: boolean, state: PlatformRolesUrlState) {
  return useQuery({
    queryKey: platformRolesListQueryKey(state),
    enabled,
    queryFn: ({ signal }) => listPlatformRoleDefinitions(env.platformApiBaseUrl, state, signal),
  });
}

export function usePlatformRoleDetailQuery(roleId: string | null) {
  return useQuery({
    queryKey: platformRoleDetailQueryKey(roleId ?? ""),
    enabled: roleId != null,
    queryFn: ({ signal }) => getPlatformRoleDefinition(env.platformApiBaseUrl, roleId!, signal),
  });
}

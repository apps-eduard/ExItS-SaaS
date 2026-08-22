import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import {
  platformRolesListPath,
  type PlatformRolesUrlState,
} from "@/api/platform-roles/platform-roles-query";
import type {
  CreatePlatformRoleDefinitionRequest,
  PermissionCatalogEntry,
  PlatformRoleDefinition,
  RoleLifecycleRequest,
  UpdatePlatformRoleDefinitionRequest,
} from "@/api/platform-roles/platform-roles-types";

const BASE = "/api/v1/platform/authorization";

export function listPlatformPermissions(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PermissionCatalogEntry[]> {
  return platformRequest<PermissionCatalogEntry[]>(baseUrl, {
    path: `${BASE}/permissions`,
    signal,
  });
}

export function listPlatformRoleDefinitions(
  baseUrl: string,
  state: PlatformRolesUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<PlatformRoleDefinition>> {
  return platformRequest<unknown>(baseUrl, {
    path: platformRolesListPath(state),
    signal,
  }).then((payload) => parsePagedResult<PlatformRoleDefinition>(payload));
}

export function getPlatformRoleDefinition(
  baseUrl: string,
  roleId: string,
  signal?: AbortSignal,
): Promise<PlatformRoleDefinition> {
  return platformRequest<PlatformRoleDefinition>(baseUrl, {
    path: `${BASE}/role-definitions/${encodeURIComponent(roleId)}`,
    signal,
  });
}

export function createPlatformRoleDefinition(
  baseUrl: string,
  body: CreatePlatformRoleDefinitionRequest,
  signal?: AbortSignal,
): Promise<PlatformRoleDefinition> {
  return platformRequest<PlatformRoleDefinition>(baseUrl, {
    path: `${BASE}/role-definitions`,
    method: "POST",
    body,
    signal,
  });
}

export function updatePlatformRoleDefinition(
  baseUrl: string,
  roleId: string,
  body: UpdatePlatformRoleDefinitionRequest,
  signal?: AbortSignal,
): Promise<PlatformRoleDefinition> {
  return platformRequest<PlatformRoleDefinition>(baseUrl, {
    path: `${BASE}/role-definitions/${encodeURIComponent(roleId)}`,
    method: "PUT",
    body,
    signal,
  });
}

export function activatePlatformRoleDefinition(
  baseUrl: string,
  roleId: string,
  body: RoleLifecycleRequest,
  signal?: AbortSignal,
): Promise<PlatformRoleDefinition> {
  return platformRequest<PlatformRoleDefinition>(baseUrl, {
    path: `${BASE}/role-definitions/${encodeURIComponent(roleId)}/activate`,
    method: "POST",
    body,
    signal,
  });
}

export function deactivatePlatformRoleDefinition(
  baseUrl: string,
  roleId: string,
  body: RoleLifecycleRequest,
  signal?: AbortSignal,
): Promise<PlatformRoleDefinition> {
  return platformRequest<PlatformRoleDefinition>(baseUrl, {
    path: `${BASE}/role-definitions/${encodeURIComponent(roleId)}/deactivate`,
    method: "POST",
    body,
    signal,
  });
}

export function retirePlatformRoleDefinition(
  baseUrl: string,
  roleId: string,
  body: RoleLifecycleRequest,
  signal?: AbortSignal,
): Promise<PlatformRoleDefinition> {
  return platformRequest<PlatformRoleDefinition>(baseUrl, {
    path: `${BASE}/role-definitions/${encodeURIComponent(roleId)}/retire`,
    method: "POST",
    body,
    signal,
  });
}

import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";
import type { OrganizationRoleDefinition } from "@/api/organizations/organization-types";

function asRecord(payload: unknown): Record<string, unknown> | null {
  if (typeof payload !== "object" || payload === null) {
    return null;
  }
  return payload as Record<string, unknown>;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return undefined;
}

export function mapOrganizationRoleDefinition(payload: unknown): OrganizationRoleDefinition {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization role definition.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const code = readString(record, "code", "Code");
  const name = readString(record, "name", "Name");
  const status = readString(record, "status", "Status");
  const permissionsPayload = record.permissions ?? record.Permissions;
  if (!id || !organizationId || !code || !name || !status || !Array.isArray(permissionsPayload)) {
    throw new Error("Invalid organization role definition.");
  }
  const permissions = permissionsPayload.flatMap((item) =>
    typeof item === "string" && item.length > 0 ? [item] : [],
  );
  return {
    id,
    organizationId,
    code,
    name,
    status,
    permissions,
    description: readString(record, "description", "Description"),
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    version: readNumber(record, "version", "Version"),
  };
}

export type OrganizationRolesUrlState = {
  page: number;
  status: string;
  search: string;
};

export function organizationRolesRequestPath(
  organizationId: string,
  state: OrganizationRolesUrlState & { pageSize?: number },
): string {
  const params = new URLSearchParams();
  params.set("page", String(state.page));
  params.set("pageSize", String(state.pageSize ?? 20));
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.search) {
    params.set("search", state.search);
  }
  return `/api/v1/platform/organizations/${organizationId}/role-definitions?${params.toString()}`;
}

export function listOrganizationRoleDefinitions(
  baseUrl: string,
  organizationId: string,
  state: OrganizationRolesUrlState & { pageSize?: number; signal?: AbortSignal },
): Promise<PagedResult<OrganizationRoleDefinition>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationRolesRequestPath(organizationId, state),
    signal: state.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationRoleDefinition),
    };
  });
}

export function getOrganizationRoleDefinition(
  baseUrl: string,
  organizationId: string,
  roleId: string,
  signal?: AbortSignal,
): Promise<OrganizationRoleDefinition> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/organizations/${organizationId}/role-definitions/${roleId}`,
    signal,
  }).then(mapOrganizationRoleDefinition);
}

export type CreateOrganizationRoleBody = {
  code: string;
  name: string;
  description?: string | null;
  permissions: string[];
};

export type UpdateOrganizationRoleBody = {
  name: string;
  description?: string | null;
  permissions?: string[] | null;
  expectedVersion?: number | null;
};

export type RoleLifecycleBody = {
  expectedVersion?: number | null;
  reason?: string | null;
};

export function createOrganizationRoleDefinition(
  baseUrl: string,
  organizationId: string,
  body: CreateOrganizationRoleBody,
  signal?: AbortSignal,
): Promise<OrganizationRoleDefinition> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/role-definitions`,
    body,
    signal,
  }).then(mapOrganizationRoleDefinition);
}

export function updateOrganizationRoleDefinition(
  baseUrl: string,
  organizationId: string,
  roleId: string,
  body: UpdateOrganizationRoleBody,
  signal?: AbortSignal,
): Promise<OrganizationRoleDefinition> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/organizations/${organizationId}/role-definitions/${roleId}`,
    body,
    signal,
  }).then(mapOrganizationRoleDefinition);
}

export function activateOrganizationRoleDefinition(
  baseUrl: string,
  organizationId: string,
  roleId: string,
  body?: RoleLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationRoleDefinition> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/role-definitions/${roleId}/activate`,
    body: body ?? {},
    signal,
  }).then(mapOrganizationRoleDefinition);
}

export function deactivateOrganizationRoleDefinition(
  baseUrl: string,
  organizationId: string,
  roleId: string,
  body?: RoleLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationRoleDefinition> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/role-definitions/${roleId}/deactivate`,
    body: body ?? {},
    signal,
  }).then(mapOrganizationRoleDefinition);
}

export function retireOrganizationRoleDefinition(
  baseUrl: string,
  organizationId: string,
  roleId: string,
  body?: RoleLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationRoleDefinition> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/role-definitions/${roleId}/retire`,
    body: body ?? {},
    signal,
  }).then(mapOrganizationRoleDefinition);
}

export type PermissionCatalogEntry = {
  code: string;
  displayName?: string;
  description?: string;
};

export function mapPermissionCatalogEntry(payload: unknown): PermissionCatalogEntry | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const code = readString(record, "code", "Code");
  if (!code) {
    return null;
  }
  return {
    code,
    displayName: readString(record, "displayName", "DisplayName"),
    description: readString(record, "description", "Description"),
  };
}

export function listOrganizationPermissionCatalog(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PermissionCatalogEntry[]> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/authorization/organization-permissions",
    signal,
  }).then((payload) => {
    if (!Array.isArray(payload)) {
      throw new Error("Invalid organization permission catalog.");
    }
    return payload.flatMap((item) => {
      const mapped = mapPermissionCatalogEntry(item);
      return mapped ? [mapped] : [];
    });
  });
}

export type PermissionCatalogEntry = {
  code: string;
  description: string;
  area: string;
};

export type PlatformRoleDefinition = {
  id: string;
  code: string;
  name: string;
  description: string | null;
  kind: string;
  status: string;
  permissions: string[];
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
};

export type CreatePlatformRoleDefinitionRequest = {
  code: string;
  name: string;
  description?: string | null;
  permissions: string[];
};

export type UpdatePlatformRoleDefinitionRequest = {
  name: string;
  description?: string | null;
  permissions?: string[] | null;
  expectedVersion?: number | null;
};

export type RoleLifecycleRequest = {
  expectedVersion?: number | null;
  reason?: string | null;
};

export const PLATFORM_ROLE_PAGE_SIZE = 20;
export const PLATFORM_ROLE_KINDS = ["BuiltIn", "Custom"] as const;
export const PLATFORM_ROLE_STATUSES = ["Active", "Inactive", "Retired"] as const;

export type PlatformRoleKindFilter = (typeof PLATFORM_ROLE_KINDS)[number] | "";
export type PlatformRoleStatusFilter = (typeof PLATFORM_ROLE_STATUSES)[number] | "";

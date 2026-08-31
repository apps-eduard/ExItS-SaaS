import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type ProductLocalRolePermissionItemWire = {
  code: string;
  displayName: string;
  allowed: boolean;
};

export type ProductLocalRolePermissionGroupWire = {
  code: string;
  displayName: string;
  items: ProductLocalRolePermissionItemWire[];
};

export type ProductLocalRoleDefinitionWire = {
  code: string;
  displayName: string;
  description: string;
  sortOrder: number;
  isSystemRole: boolean;
  isAssignable: boolean;
  mappedPosRoleCode: string;
  activeStaffCount: number | null;
  permissionGroups: ProductLocalRolePermissionGroupWire[];
};

function definitionsPath(organizationId: string): string {
  return `/api/v1/organizations/${organizationId}/product-local-role-definitions`;
}

export async function listProductLocalRoleDefinitions(
  organizationId: string,
): Promise<
  | { ok: true; roles: ProductLocalRoleDefinitionWire[] }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const roles = await platformRequest<ProductLocalRoleDefinitionWire[]>({
      method: "GET",
      path: definitionsPath(organizationId),
    });
    return {
      ok: true,
      roles: [...(Array.isArray(roles) ? roles : [])].sort((a, b) => a.sortOrder - b.sortOrder),
    };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

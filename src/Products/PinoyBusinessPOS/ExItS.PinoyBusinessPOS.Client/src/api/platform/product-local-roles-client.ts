import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type ProductLocalRoleGrantWire = {
  id: string;
  organizationId: string;
  userIdentityId: string;
  productCode: string;
  roleCode: string;
  mappedPosRoleCode: string;
  status: string;
  grantedAtUtc: string;
  grantedByUserIdentityId: string;
  source: string;
  revokedAtUtc?: string | null;
  revokedByUserIdentityId?: string | null;
  reason?: string | null;
  userDisplayName?: string | null;
  roleDisplay?: string | null;
};

export const POS_LOCAL_ROLE_OWNER = "Owner";
export const POS_LOCAL_ROLE_MANAGER = "Manager";
export const POS_LOCAL_ROLE_CASHIER = "Cashier";

export const POS_LOCAL_ROLE_CODES = [
  POS_LOCAL_ROLE_OWNER,
  POS_LOCAL_ROLE_MANAGER,
  POS_LOCAL_ROLE_CASHIER,
] as const;

export type PosLocalRoleCode = (typeof POS_LOCAL_ROLE_CODES)[number];

function rolesPath(organizationId: string, status?: string): string {
  const base = `/api/v1/organizations/${organizationId}/product-local-roles`;
  if (!status?.trim()) {
    return base;
  }
  return `${base}?status=${encodeURIComponent(status.trim())}`;
}

export async function listProductLocalRoles(
  organizationId: string,
  status: string = "Active",
): Promise<
  | { ok: true; grants: ProductLocalRoleGrantWire[] }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const grants = await platformRequest<ProductLocalRoleGrantWire[]>({
      method: "GET",
      path: rolesPath(organizationId, status),
    });
    return { ok: true, grants: Array.isArray(grants) ? grants : [] };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function assignProductLocalRole(input: {
  organizationId: string;
  userIdentityId: string;
  roleCode: string;
  productCode?: string;
  reason?: string;
}): Promise<
  | { ok: true; grant: ProductLocalRoleGrantWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const grant = await platformRequest<ProductLocalRoleGrantWire>({
      method: "POST",
      path: rolesPath(input.organizationId),
      body: {
        userIdentityId: input.userIdentityId,
        productCode: input.productCode ?? POS_PRODUCT_CODE,
        roleCode: input.roleCode,
        reason: input.reason ?? null,
      },
    });
    return { ok: true, grant };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function revokeProductLocalRole(input: {
  organizationId: string;
  grantId: string;
  reason?: string;
}): Promise<
  | { ok: true; grant: ProductLocalRoleGrantWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const grant = await platformRequest<ProductLocalRoleGrantWire>({
      method: "POST",
      path: `${rolesPath(input.organizationId)}/${input.grantId}/revoke`,
      body: { reason: input.reason ?? null },
    });
    return { ok: true, grant };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export function friendlyPosRoleLabel(
  mappedPosRoleCode: string | null | undefined,
  roleCode: string | null | undefined,
  roleDisplay?: string | null,
): string {
  if (roleDisplay?.trim()) {
    return roleDisplay.trim();
  }
  const code = (mappedPosRoleCode ?? roleCode ?? "").trim().toLowerCase();
  if (code === "owner" || code === "posowner") {
    return "Owner";
  }
  if (code === "manager" || code === "storemanager") {
    return "Manager";
  }
  if (code === "cashier") {
    return "Cashier";
  }
  return roleCode?.trim() || mappedPosRoleCode?.trim() || "Role";
}

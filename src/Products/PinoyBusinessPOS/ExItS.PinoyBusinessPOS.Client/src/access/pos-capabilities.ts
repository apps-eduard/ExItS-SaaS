import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

export type PosSessionGrantFacts = Pick<
  SessionGrantResponse,
  | "productAccessAllowed"
  | "mappedPosRoleCode"
  | "productLocalRoleCode"
  | "membershipRole"
  | "organizationManagementAuthority"
>;

const SELL_FLOOR_ROLE_CODES = new Set(["owner", "admin", "storemanager", "cashier", "manager"]);

export function normalizeRoleCode(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

/** Prefer mapped POS role; fall back to Platform product-local role (Manager → StoreManager). */
export function resolveEffectivePosRoleCode(
  grant: PosSessionGrantFacts | null | undefined,
): string | null {
  if (!grant) {
    return null;
  }

  const mapped = normalizeRoleCode(grant.mappedPosRoleCode);
  if (mapped) {
    return mapped;
  }

  const local = normalizeRoleCode(grant.productLocalRoleCode);
  if (!local) {
    return null;
  }

  if (local.localeCompare("Manager", undefined, { sensitivity: "accent" }) === 0) {
    return "StoreManager";
  }

  return local;
}

function isSellFloorPosRole(roleCode: string | null): boolean {
  if (!roleCode) {
    return false;
  }
  return SELL_FLOOR_ROLE_CODES.has(roleCode.toLowerCase());
}

export function canEnterSellFloor(grant: PosSessionGrantFacts | null | undefined): boolean {
  if (!grant?.productAccessAllowed) {
    return false;
  }

  const roleCode = resolveEffectivePosRoleCode(grant);
  return isSellFloorPosRole(roleCode);
}

/** WP04 UI gate — server remains authoritative for CreateSale later. */
export function canCreateSale(grant: PosSessionGrantFacts | null | undefined): boolean {
  return canEnterSellFloor(grant);
}

export function resolveRoleHomeRoute(grant: PosSessionGrantFacts | null | undefined): string {
  if (!grant?.productAccessAllowed) {
    return "/";
  }

  const roleCode = resolveEffectivePosRoleCode(grant);
  if (roleCode) {
    switch (roleCode.toLowerCase()) {
      case "owner":
      case "admin":
        return "/role/owner";
      case "storemanager":
      case "manager":
        return "/role/manager";
      case "cashier":
        return "/role/cashier";
      default:
        break;
    }
  }

  if (
    grant.organizationManagementAuthority ||
    normalizeRoleCode(grant.membershipRole)?.localeCompare("OrganizationOwner", undefined, {
      sensitivity: "accent",
    }) === 0
  ) {
    return "/org";
  }

  return "/org";
}

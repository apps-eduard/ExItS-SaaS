import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

export type PosSessionGrantFacts = Pick<
  SessionGrantResponse,
  | "productAccessAllowed"
  | "mappedPosRoleCode"
  | "productLocalRoleCode"
  | "membershipRole"
  | "organizationManagementAuthority"
>;

/** Default POS sell-floor roles (Owner / Manager / Cashier). Legacy Admin kept for compat. */
const SELL_FLOOR_ROLE_CODES = new Set(["owner", "admin", "storemanager", "cashier", "manager"]);

export type PosExperienceMode = "admin" | "operations" | "selling";

export function normalizeRoleCode(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

function roleEquals(value: string | null | undefined, expected: string): boolean {
  const normalized = normalizeRoleCode(value);
  return (
    normalized != null &&
    normalized.localeCompare(expected, undefined, { sensitivity: "accent" }) === 0
  );
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

export function isOrganizationOwnerMembership(
  grant: PosSessionGrantFacts | null | undefined,
): boolean {
  return roleEquals(grant?.membershipRole, "OrganizationOwner");
}

export function isOrganizationAdministratorMembership(
  grant: PosSessionGrantFacts | null | undefined,
): boolean {
  return roleEquals(grant?.membershipRole, "OrganizationAdministrator");
}

/**
 * Admin-side organization management (Owner or OrganizationAdministrator).
 * Independent of POS StoreManager / Manager.
 */
export function hasOrganizationManagementAuthority(
  grant: PosSessionGrantFacts | null | undefined,
): boolean {
  if (!grant) {
    return false;
  }
  if (grant.organizationManagementAuthority === true) {
    return true;
  }
  return isOrganizationOwnerMembership(grant) || isOrganizationAdministratorMembership(grant);
}

/**
 * Staff invite — matches Platform EnsureCanManageMemberships org path:
 * OrganizationOwner membership only (not POS Manager, not Administrator alone).
 */
export function canInviteOrganizationStaff(
  grant: PosSessionGrantFacts | null | undefined,
): boolean {
  return isOrganizationOwnerMembership(grant);
}

export function isPosOwnerRole(grant: PosSessionGrantFacts | null | undefined): boolean {
  const role = resolveEffectivePosRoleCode(grant);
  if (!role) {
    return false;
  }
  const lower = role.toLowerCase();
  return lower === "owner" || lower === "admin";
}

/** POS StoreManager / Manager — operations, not Organization Web admin. */
export function isPosOperationsManager(grant: PosSessionGrantFacts | null | undefined): boolean {
  const role = resolveEffectivePosRoleCode(grant);
  if (!role) {
    return false;
  }
  const lower = role.toLowerCase();
  return lower === "storemanager" || lower === "manager";
}

export function isPosCashierRole(grant: PosSessionGrantFacts | null | undefined): boolean {
  const role = resolveEffectivePosRoleCode(grant);
  return role != null && role.toLowerCase() === "cashier";
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

/**
 * UI gate for catalog administration (ManageCatalog).
 * Mirrors PosRoleMatrix: Owner/Admin/StoreManager (+ Manager alias).
 * OrganizationAdministrator alone does NOT imply ManageCatalog.
 * Server remains authoritative via StoreCatalogManage feature grants.
 */
export function canManageCatalog(grant: PosSessionGrantFacts | null | undefined): boolean {
  if (!grant?.productAccessAllowed) {
    return false;
  }
  return isPosOwnerRole(grant) || isPosOperationsManager(grant);
}

/**
 * ManageInventory UI gate — PosRoleMatrix Owner/Admin/StoreManager (+ InventoryStaff compat).
 * Server remains authoritative.
 */
export function canManageInventory(grant: PosSessionGrantFacts | null | undefined): boolean {
  if (!grant?.productAccessAllowed) {
    return false;
  }
  if (isPosOwnerRole(grant) || isPosOperationsManager(grant)) {
    return true;
  }
  const role = resolveEffectivePosRoleCode(grant)?.toLowerCase();
  return role === "inventorystaff";
}

/** ViewInventory — same matrix as manage for default roles; Cashier excluded. */
export function canViewInventory(grant: PosSessionGrantFacts | null | undefined): boolean {
  return canManageInventory(grant);
}

/** Admin / business-management experience (Organization Web essentials in React). */
export function canUseAdminExperience(grant: PosSessionGrantFacts | null | undefined): boolean {
  return hasOrganizationManagementAuthority(grant);
}

/** Manager-style operations experience. */
export function canUseOperationsExperience(
  grant: PosSessionGrantFacts | null | undefined,
): boolean {
  if (!grant?.productAccessAllowed) {
    return false;
  }
  return isPosOwnerRole(grant) || isPosOperationsManager(grant);
}

/** Cashier / selling experience — CreateSale / EnterPos via role matrix. */
export function canUseSellingExperience(grant: PosSessionGrantFacts | null | undefined): boolean {
  return canCreateSale(grant);
}

export function canEnterOwnerRoleHome(grant: PosSessionGrantFacts | null | undefined): boolean {
  if (!grant?.productAccessAllowed) {
    return false;
  }
  return isPosOwnerRole(grant);
}

export function canEnterManagerRoleHome(grant: PosSessionGrantFacts | null | undefined): boolean {
  return canUseOperationsExperience(grant);
}

export function canEnterCashierRoleHome(grant: PosSessionGrantFacts | null | undefined): boolean {
  return canUseSellingExperience(grant);
}

/**
 * Whether the principal may intentionally select an experience mode.
 * Does not mutate security role — presentation only.
 */
export function canSelectExperienceMode(
  grant: PosSessionGrantFacts | null | undefined,
  mode: PosExperienceMode,
): boolean {
  switch (mode) {
    case "admin":
      return canUseAdminExperience(grant);
    case "operations":
      return canUseOperationsExperience(grant);
    case "selling":
      return canUseSellingExperience(grant);
    default:
      return false;
  }
}

export function resolveRoleHomeRoute(grant: PosSessionGrantFacts | null | undefined): string {
  if (!grant?.productAccessAllowed) {
    // Admin-only (management authority, no POS sell role) still lands on /org.
    if (canUseAdminExperience(grant)) {
      return "/org";
    }
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

  if (canUseAdminExperience(grant)) {
    return "/org";
  }

  return "/org";
}

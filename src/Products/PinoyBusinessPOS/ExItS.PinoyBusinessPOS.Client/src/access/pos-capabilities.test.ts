import { describe, expect, it } from "vitest";
import {
  canCreateSale,
  canEnterCashierRoleHome,
  canEnterManagerRoleHome,
  canEnterOwnerRoleHome,
  canEnterSellFloor,
  canInviteOrganizationStaff,
  canSelectExperienceMode,
  canUseAdminExperience,
  canUseOperationsExperience,
  canUseSellingExperience,
  hasOrganizationManagementAuthority,
  isPosOperationsManager,
  resolveEffectivePosRoleCode,
  resolveRoleHomeRoute,
} from "@/access/pos-capabilities";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

function grant(partial: Partial<SessionGrantResponse>): SessionGrantResponse {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    ...partial,
  };
}

describe("pos-capabilities", () => {
  it("denies sell floor for owner membership without POS role", () => {
    const ownerOnly = grant({
      productAccessAllowed: true,
      organizationManagementAuthority: true,
      membershipRole: "OrganizationOwner",
      mappedPosRoleCode: null,
      productLocalRoleCode: null,
    });

    expect(canEnterSellFloor(ownerOnly)).toBe(false);
    expect(canCreateSale(ownerOnly)).toBe(false);
    expect(canUseAdminExperience(ownerOnly)).toBe(true);
    expect(canInviteOrganizationStaff(ownerOnly)).toBe(true);
    expect(resolveRoleHomeRoute(ownerOnly)).toBe("/org");
  });

  it("allows sell floor for cashier grant", () => {
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });

    expect(canEnterSellFloor(cashier)).toBe(true);
    expect(canCreateSale(cashier)).toBe(true);
    expect(canUseAdminExperience(cashier)).toBe(false);
    expect(canUseOperationsExperience(cashier)).toBe(false);
    expect(canInviteOrganizationStaff(cashier)).toBe(false);
    expect(canEnterOwnerRoleHome(cashier)).toBe(false);
    expect(canEnterManagerRoleHome(cashier)).toBe(false);
    expect(canEnterCashierRoleHome(cashier)).toBe(true);
    expect(resolveRoleHomeRoute(cashier)).toBe("/role/cashier");
  });

  it("maps manager product-local role to store manager home without admin", () => {
    const manager = grant({
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
      organizationManagementAuthority: false,
    });

    expect(resolveEffectivePosRoleCode(manager)).toBe("StoreManager");
    expect(isPosOperationsManager(manager)).toBe(true);
    expect(resolveRoleHomeRoute(manager)).toBe("/role/manager");
    expect(canEnterSellFloor(manager)).toBe(true);
    expect(canUseOperationsExperience(manager)).toBe(true);
    expect(canUseSellingExperience(manager)).toBe(true);
    expect(canUseAdminExperience(manager)).toBe(false);
    expect(canInviteOrganizationStaff(manager)).toBe(false);
    expect(canEnterOwnerRoleHome(manager)).toBe(false);
    expect(canSelectExperienceMode(manager, "admin")).toBe(false);
    expect(canSelectExperienceMode(manager, "operations")).toBe(true);
    expect(canSelectExperienceMode(manager, "selling")).toBe(true);
  });

  it("owner POS role may select admin operations and selling without mutating role", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });

    expect(canUseAdminExperience(owner)).toBe(true);
    expect(canUseOperationsExperience(owner)).toBe(true);
    expect(canUseSellingExperience(owner)).toBe(true);
    expect(canInviteOrganizationStaff(owner)).toBe(true);
    expect(canEnterOwnerRoleHome(owner)).toBe(true);
    expect(canEnterManagerRoleHome(owner)).toBe(true);
    expect(canEnterCashierRoleHome(owner)).toBe(true);
    expect(resolveEffectivePosRoleCode(owner)).toBe("Owner");
    expect(resolveRoleHomeRoute(owner)).toBe("/role/owner");
  });

  it("OrganizationAdministrator has admin experience without CreateSale", () => {
    const admin = grant({
      membershipRole: "OrganizationAdministrator",
      organizationManagementAuthority: true,
      mappedPosRoleCode: null,
      productLocalRoleCode: null,
      productAccessAllowed: true,
    });

    expect(hasOrganizationManagementAuthority(admin)).toBe(true);
    expect(canUseAdminExperience(admin)).toBe(true);
    expect(canInviteOrganizationStaff(admin)).toBe(false);
    expect(canCreateSale(admin)).toBe(false);
    expect(canUseOperationsExperience(admin)).toBe(false);
  });

  it("denies sell floor when product access is not allowed", () => {
    const denied = grant({
      productAccessAllowed: false,
      mappedPosRoleCode: "Cashier",
    });

    expect(canEnterSellFloor(denied)).toBe(false);
    expect(resolveRoleHomeRoute(denied)).toBe("/");
  });
});

import { describe, expect, it } from "vitest";
import {
  canApplyCommercialDiscount,
  canCreateCredit,
  canCreateCustomer,
  canCreateSale,
  canEditCustomer,
  canEnterCashierRoleHome,
  canEnterManagerRoleHome,
  canEnterOwnerRoleHome,
  canEnterSellFloor,
  canInviteOrganizationStaff,
  canManageCatalog,
  canManageRegisters,
  canManagePurchasing,
  canManageShifts,
  canProcessReturn,
  canRecordRepayment,
  canSelectExperienceMode,
  canUseAdminExperience,
  canUseOperationsExperience,
  canUseSellingExperience,
  canManageSuppliers,
  canViewCustomers,
  canViewPurchasing,
  canViewSuppliers,
  canViewRegisters,
  canViewReturns,
  canViewShifts,
  canViewStatement,
  canVoidSale,
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
    expect(canManageCatalog(manager)).toBe(true);
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
    expect(canManageCatalog(owner)).toBe(true);
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
    expect(canManageCatalog(admin)).toBe(false);
    expect(canUseOperationsExperience(admin)).toBe(false);
  });

  it("denies ManageCatalog for Cashier", () => {
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });
    expect(canManageCatalog(cashier)).toBe(false);
  });

  it("denies sell floor when product access is not allowed", () => {
    const denied = grant({
      productAccessAllowed: false,
      mappedPosRoleCode: "Cashier",
    });

    expect(canEnterSellFloor(denied)).toBe(false);
    expect(resolveRoleHomeRoute(denied)).toBe("/");
  });

  it("allows cashier ManageShifts without admin or register manage", () => {
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });

    expect(canManageShifts(cashier)).toBe(true);
    expect(canViewShifts(cashier)).toBe(true);
    expect(canViewRegisters(cashier)).toBe(true);
    expect(canManageRegisters(cashier)).toBe(false);
    expect(canUseAdminExperience(cashier)).toBe(false);
    expect(canManageCatalog(cashier)).toBe(false);
  });

  it("ApplyCommercialDiscount allows Owner/Manager and denies Cashier and OrgAdmin alone", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const manager = grant({
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
    });
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });
    const orgAdmin = grant({
      membershipRole: "OrganizationAdministrator",
      organizationManagementAuthority: true,
      mappedPosRoleCode: null,
      productLocalRoleCode: null,
      productAccessAllowed: true,
    });

    expect(canApplyCommercialDiscount(owner)).toBe(true);
    expect(canApplyCommercialDiscount(manager)).toBe(true);
    expect(canApplyCommercialDiscount(cashier)).toBe(false);
    expect(canApplyCommercialDiscount(orgAdmin)).toBe(false);
  });

  it("VoidSale / ViewCustomers / CreateCredit mirror PosRoleMatrix gaps", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const manager = grant({
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
    });
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });

    expect(canVoidSale(owner)).toBe(true);
    expect(canVoidSale(manager)).toBe(true);
    expect(canVoidSale(cashier)).toBe(false);

    expect(canViewCustomers(owner)).toBe(true);
    expect(canViewCustomers(manager)).toBe(true);
    expect(canViewCustomers(cashier)).toBe(false);

    expect(canCreateCredit(owner)).toBe(true);
    expect(canCreateCredit(manager)).toBe(true);
    expect(canCreateCredit(cashier)).toBe(true);
  });

  it("CreateCustomer / EditCustomer / RecordRepayment / ViewStatement mirror PosRoleMatrix", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const manager = grant({
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
    });
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });
    const reporting = grant({
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
      membershipRole: "OrganizationMember",
    });

    expect(canCreateCustomer(owner)).toBe(true);
    expect(canCreateCustomer(manager)).toBe(true);
    expect(canCreateCustomer(cashier)).toBe(false);
    expect(canCreateCustomer(reporting)).toBe(false);

    expect(canEditCustomer(owner)).toBe(true);
    expect(canEditCustomer(cashier)).toBe(false);

    expect(canRecordRepayment(owner)).toBe(true);
    expect(canRecordRepayment(manager)).toBe(true);
    expect(canRecordRepayment(cashier)).toBe(false);
    expect(canRecordRepayment(reporting)).toBe(false);

    expect(canViewStatement(owner)).toBe(true);
    expect(canViewStatement(manager)).toBe(true);
    expect(canViewStatement(reporting)).toBe(true);
    expect(canViewStatement(cashier)).toBe(false);
  });

  it("ViewReturns includes Cashier; ProcessReturn is Owner/Manager only", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const manager = grant({
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
    });
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });
    const reporting = grant({
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
      membershipRole: "OrganizationMember",
    });

    expect(canViewReturns(owner)).toBe(true);
    expect(canViewReturns(manager)).toBe(true);
    expect(canViewReturns(cashier)).toBe(true);
    expect(canViewReturns(reporting)).toBe(true);

    expect(canProcessReturn(owner)).toBe(true);
    expect(canProcessReturn(manager)).toBe(true);
    expect(canProcessReturn(cashier)).toBe(false);
    expect(canProcessReturn(reporting)).toBe(false);
  });

  it("ViewSuppliers includes InventoryStaff/ReportingUser; ManageSuppliers is Owner/Manager only", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const manager = grant({
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
    });
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });
    const inventory = grant({
      mappedPosRoleCode: "InventoryStaff",
      productLocalRoleCode: "InventoryStaff",
      membershipRole: "OrganizationMember",
    });
    const reporting = grant({
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
      membershipRole: "OrganizationMember",
    });

    expect(canViewSuppliers(owner)).toBe(true);
    expect(canViewSuppliers(manager)).toBe(true);
    expect(canViewSuppliers(inventory)).toBe(true);
    expect(canViewSuppliers(reporting)).toBe(true);
    expect(canViewSuppliers(cashier)).toBe(false);

    expect(canManageSuppliers(owner)).toBe(true);
    expect(canManageSuppliers(manager)).toBe(true);
    expect(canManageSuppliers(inventory)).toBe(false);
    expect(canManageSuppliers(reporting)).toBe(false);
    expect(canManageSuppliers(cashier)).toBe(false);

    expect(canViewPurchasing(owner)).toBe(true);
    expect(canViewPurchasing(manager)).toBe(true);
    expect(canViewPurchasing(inventory)).toBe(true);
    expect(canViewPurchasing(reporting)).toBe(true);
    expect(canViewPurchasing(cashier)).toBe(false);

    expect(canManagePurchasing(owner)).toBe(true);
    expect(canManagePurchasing(manager)).toBe(true);
    expect(canManagePurchasing(inventory)).toBe(true);
    expect(canManagePurchasing(reporting)).toBe(false);
    expect(canManagePurchasing(cashier)).toBe(false);

    expect(canManageCatalog(owner)).toBe(true);
    expect(canManageCatalog(manager)).toBe(true);
    expect(canManageCatalog(inventory)).toBe(false);
    expect(canManageCatalog(cashier)).toBe(false);
  });
});

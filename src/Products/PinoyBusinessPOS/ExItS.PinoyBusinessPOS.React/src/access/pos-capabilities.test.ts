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
  canGovernOrganizationCatalog,
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
  canOverrideSalePrice,
  canOverrideSalePriceUnlimited,
  canViewCustomers,
  canViewCustomerOrders,
  canManageCustomerOrders,
  canViewDashboard,
  canViewExpenses,
  canManageExpenses,
  canViewPurchasing,
  canViewReports,
  canViewSuppliers,
  canViewInventory,
  canManageInventory,
  canViewRegisters,
  canViewReturns,
  canViewShifts,
  canViewStatement,
  canVoidSale,
  canManageStoreAreas,
  canUseWarehouseBranches,
  FEATURE_STORE_AREA_MANAGEMENT,
  FEATURE_STORE_WAREHOUSE,
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
    expect(canCreateSale(cashier, "Retail")).toBe(true);
    expect(canCreateSale(cashier, "Warehouse")).toBe(false);
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

  it("owner with POS sell role keeps admin experience even when management flag was historically false", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      // Historical bug: CanOperate cleared this flag for owners who also sell.
      organizationManagementAuthority: false,
    });

    expect(hasOrganizationManagementAuthority(owner)).toBe(true);
    expect(canUseAdminExperience(owner)).toBe(true);
    expect(canSelectExperienceMode(owner, "admin")).toBe(true);
    expect(canSelectExperienceMode(owner, "operations")).toBe(true);
    expect(canSelectExperienceMode(owner, "selling")).toBe(true);
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
    expect(canEnterManagerRoleHome(admin)).toBe(false);
  });

  it("pure OrganizationAdministrator without product access cannot enter Manager", () => {
    const admin = grant({
      membershipRole: "OrganizationAdministrator",
      organizationManagementAuthority: true,
      mappedPosRoleCode: null,
      productLocalRoleCode: null,
      productAccessAllowed: false,
    });
    expect(canUseAdminExperience(admin)).toBe(true);
    expect(canUseOperationsExperience(admin)).toBe(false);
    expect(canEnterManagerRoleHome(admin)).toBe(false);
    expect(canUseSellingExperience(admin)).toBe(false);
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

  it("OverrideSalePrice mirrors PosRoleMatrix and prefers feature codes when present", () => {
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
    const cashierWithFeature = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
      featureCodes: ["store-sales-override-price"],
    });
    const managerUnlimitedFeature = grant({
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      membershipRole: "OrganizationMember",
      featureCodes: ["store-sales-override-price-unlimited"],
    });

    expect(canOverrideSalePrice(owner)).toBe(true);
    expect(canOverrideSalePriceUnlimited(owner)).toBe(true);
    expect(canOverrideSalePrice(manager)).toBe(true);
    expect(canOverrideSalePriceUnlimited(manager)).toBe(false);
    expect(canOverrideSalePrice(cashier)).toBe(false);
    expect(canOverrideSalePriceUnlimited(cashier)).toBe(false);
    expect(canOverrideSalePrice(orgAdmin)).toBe(false);
    expect(canOverrideSalePrice(cashierWithFeature)).toBe(true);
    expect(canOverrideSalePriceUnlimited(cashierWithFeature)).toBe(false);
    expect(canOverrideSalePriceUnlimited(managerUnlimitedFeature)).toBe(true);
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

    expect(canViewCustomerOrders(owner)).toBe(true);
    expect(canViewCustomerOrders(manager)).toBe(true);
    expect(canViewCustomerOrders(reporting)).toBe(true);
    expect(canViewCustomerOrders(cashier)).toBe(false);
    expect(canManageCustomerOrders(owner)).toBe(true);
    expect(canManageCustomerOrders(manager)).toBe(true);
    expect(canManageCustomerOrders(reporting)).toBe(false);
    expect(canManageCustomerOrders(cashier)).toBe(false);

    expect(canViewDashboard(owner)).toBe(true);
    expect(canViewReports(owner)).toBe(true);
    expect(canViewDashboard(reporting)).toBe(true);
    expect(canViewDashboard(cashier)).toBe(false);
    expect(canViewReports(cashier)).toBe(false);

    expect(canViewExpenses(owner)).toBe(true);
    expect(canViewExpenses(manager)).toBe(true);
    expect(canViewExpenses(reporting)).toBe(true);
    expect(canViewExpenses(cashier)).toBe(false);
    expect(canViewExpenses(inventory)).toBe(false);
    expect(canManageExpenses(owner)).toBe(true);
    expect(canManageExpenses(manager)).toBe(true);
    expect(canManageExpenses(reporting)).toBe(false);
    expect(canManageExpenses(cashier)).toBe(false);

    expect(canViewInventory(owner)).toBe(true);
    expect(canViewInventory(manager)).toBe(true);
    expect(canViewInventory(inventory)).toBe(true);
    expect(canViewInventory(reporting)).toBe(true);
    expect(canViewInventory(cashier)).toBe(false);
    expect(canManageInventory(owner)).toBe(true);
    expect(canManageInventory(manager)).toBe(true);
    expect(canManageInventory(inventory)).toBe(true);
    expect(canManageInventory(reporting)).toBe(false);
    expect(canManageInventory(cashier)).toBe(false);
  });

  it("keeps ViewExpenses independent of ViewReports feature deny", () => {
    const reporting = grant({
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
      membershipRole: "OrganizationMember",
      featureCodes: ["store-expenses-view"],
    });
    expect(canViewReports(reporting)).toBe(false);
    expect(canViewExpenses(reporting)).toBe(true);
  });

  it("keeps canGovernOrganizationCatalog distinct from canManageCatalog", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const manager = grant({
      mappedPosRoleCode: "StoreManager",
      membershipRole: "OrganizationMember",
      organizationManagementAuthority: false,
    });
    expect(canGovernOrganizationCatalog(owner)).toBe(true);
    expect(canManageCatalog(owner)).toBe(true);
    expect(canGovernOrganizationCatalog(manager)).toBe(false);
    expect(canManageCatalog(manager)).toBe(true);
  });

  it("gates Area and Warehouse on dedicated feature codes", () => {
    expect(canManageStoreAreas(grant({ featureCodes: [] }))).toBe(false);
    expect(
      canManageStoreAreas(grant({ featureCodes: [FEATURE_STORE_AREA_MANAGEMENT] })),
    ).toBe(true);
    expect(canUseWarehouseBranches(grant({ featureCodes: [] }))).toBe(false);
    expect(
      canUseWarehouseBranches(grant({ grantedFeatureCodes: [FEATURE_STORE_WAREHOUSE] })),
    ).toBe(true);
  });
});

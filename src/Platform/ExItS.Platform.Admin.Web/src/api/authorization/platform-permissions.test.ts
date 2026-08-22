import { describe, expect, it } from "vitest";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import permissionSource from "../../../../ExItS.Platform.Domain/Authorization/PlatformPermission.cs?raw";

function csharpConstant(name: string): string {
  const match = permissionSource.match(
    new RegExp(`public const string ${name} = "([^"]+)";`),
  );
  if (!match?.[1]) {
    throw new Error(`Missing PlatformPermission.${name} in backend source.`);
  }
  return match[1];
}

describe("PLATFORM_PERMISSIONS", () => {
  it("manageCatalog matches backend canonical value", () => {
    expect(PLATFORM_PERMISSIONS.manageCatalog).toBe("platform.permission.manage_catalog");
    expect(PLATFORM_PERMISSIONS.manageCatalog).toBe(csharpConstant("ManageCatalog"));
  });

  it("manageProductAccess matches backend canonical value", () => {
    expect(PLATFORM_PERMISSIONS.manageProductAccess).toBe(
      "platform.permission.manage_product_access",
    );
    expect(PLATFORM_PERMISSIONS.manageProductAccess).toBe(csharpConstant("ManageProductAccess"));
  });

  it("manageGlobalCategories and manageGlobalProducts match backend canonical values", () => {
    expect(PLATFORM_PERMISSIONS.manageGlobalCategories).toBe(
      "platform.permission.manage_global_categories",
    );
    expect(PLATFORM_PERMISSIONS.manageGlobalCategories).toBe(csharpConstant("ManageGlobalCategories"));
    expect(PLATFORM_PERMISSIONS.manageGlobalProducts).toBe(
      "platform.permission.manage_global_products",
    );
    expect(PLATFORM_PERMISSIONS.manageGlobalProducts).toBe(csharpConstant("ManageGlobalProducts"));
  });

  it("keeps existing permission constants aligned with backend", () => {
    expect(PLATFORM_PERMISSIONS.viewPortfolio).toBe(csharpConstant("ViewPortfolio"));
    expect(PLATFORM_PERMISSIONS.manageOrganizations).toBe(csharpConstant("ManageOrganizations"));
    expect(PLATFORM_PERMISSIONS.managePlatformUsers).toBe(csharpConstant("ManagePlatformUsers"));
    expect(PLATFORM_PERMISSIONS.manageMemberships).toBe(csharpConstant("ManageMemberships"));
    expect(PLATFORM_PERMISSIONS.manageSubscriptions).toBe(csharpConstant("ManageSubscriptions"));
    expect(PLATFORM_PERMISSIONS.manageManualPayments).toBe(csharpConstant("ManageManualPayments"));
    expect(PLATFORM_PERMISSIONS.manageEntitlementOverrides).toBe(
      csharpConstant("ManageEntitlementOverrides"),
    );
    expect(PLATFORM_PERMISSIONS.viewAuditRecords).toBe(csharpConstant("ViewAuditRecords"));
    expect(PLATFORM_PERMISSIONS.viewGlobalCatalog).toBe(csharpConstant("ViewGlobalCatalog"));
    expect(PLATFORM_PERMISSIONS.viewPrivacyCompliance).toBe(csharpConstant("ViewPrivacyCompliance"));
  });
});

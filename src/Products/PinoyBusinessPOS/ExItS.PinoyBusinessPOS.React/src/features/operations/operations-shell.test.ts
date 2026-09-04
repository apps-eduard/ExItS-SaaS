import { describe, expect, it } from "vitest";
import {
  canEnterManagerRoleHome,
  canSelectExperienceMode,
  canUseAdminExperience,
  canUseOperationsExperience,
  canUseSellingExperience,
} from "@/access/pos-capabilities";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import {
  buildOperationsBottomNavTabs,
  buildOperationsSidebarGroups,
  flattenOperationsSidebarItems,
  isAdminOnlyOperationsPath,
  shouldUseOperationsShell,
} from "@/features/operations/operations-nav-config";
import { buildOrgMoreSections } from "@/features/shell/org-nav-config";

function grant(partial: Partial<SessionGrantResponse>): SessionGrantResponse {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    ...partial,
  };
}

const pureOrgAdmin = grant({
  membershipRole: "OrganizationAdministrator",
  organizationManagementAuthority: true,
  productAccessAllowed: false,
  mappedPosRoleCode: null,
  productLocalRoleCode: null,
});

const storeManager = grant({
  membershipRole: "OrganizationMember",
  organizationManagementAuthority: false,
  mappedPosRoleCode: "StoreManager",
  productLocalRoleCode: "Manager",
});

const owner = grant({
  membershipRole: "OrganizationOwner",
  organizationManagementAuthority: true,
  mappedPosRoleCode: "Owner",
  productLocalRoleCode: "Owner",
});

const cashier = grant({
  membershipRole: "OrganizationMember",
  organizationManagementAuthority: false,
  mappedPosRoleCode: "Cashier",
  productLocalRoleCode: "Cashier",
});

const reportingUser = grant({
  membershipRole: "OrganizationMember",
  organizationManagementAuthority: false,
  mappedPosRoleCode: "ReportingUser",
  productLocalRoleCode: "ReportingUser",
});

const inventoryStaff = grant({
  membershipRole: "OrganizationMember",
  organizationManagementAuthority: false,
  mappedPosRoleCode: "InventoryStaff",
  productLocalRoleCode: "InventoryStaff",
});

describe("POS-MANAGER-OPERATIONS-SHELL authority matrix", () => {
  it("PURE_ORG_ADMIN: Admin allow, Operations deny, Sell deny", () => {
    expect(canUseAdminExperience(pureOrgAdmin)).toBe(true);
    expect(canUseOperationsExperience(pureOrgAdmin)).toBe(false);
    expect(canEnterManagerRoleHome(pureOrgAdmin)).toBe(false);
    expect(canUseSellingExperience(pureOrgAdmin)).toBe(false);
    expect(canSelectExperienceMode(pureOrgAdmin, "operations")).toBe(false);
    expect(
      shouldUseOperationsShell({
        experience: "operations",
        pathname: "/role/manager",
        grant: pureOrgAdmin,
      }),
    ).toBe(false);
  });

  it("STORE_MANAGER: Admin deny, Operations allow", () => {
    expect(canUseAdminExperience(storeManager)).toBe(false);
    expect(canUseOperationsExperience(storeManager)).toBe(true);
    expect(canEnterManagerRoleHome(storeManager)).toBe(true);
    expect(
      shouldUseOperationsShell({
        experience: "operations",
        pathname: "/role/manager",
        grant: storeManager,
      }),
    ).toBe(true);
  });

  it("OWNER: Admin + Operations allow; Sell by branch", () => {
    expect(canUseAdminExperience(owner)).toBe(true);
    expect(canUseOperationsExperience(owner)).toBe(true);
    expect(canUseSellingExperience(owner, "Retail")).toBe(true);
    expect(canUseSellingExperience(owner, "Warehouse")).toBe(false);
  });

  it("CASHIER: Admin deny, Operations deny", () => {
    expect(canUseAdminExperience(cashier)).toBe(false);
    expect(canUseOperationsExperience(cashier)).toBe(false);
    expect(canEnterManagerRoleHome(cashier)).toBe(false);
    expect(canUseSellingExperience(cashier, "Retail")).toBe(true);
  });

  it("REPORTING_USER and INVENTORY_STAFF do not get full Manager shell", () => {
    expect(canUseOperationsExperience(reportingUser)).toBe(false);
    expect(canUseOperationsExperience(inventoryStaff)).toBe(false);
    expect(canEnterManagerRoleHome(reportingUser)).toBe(false);
    expect(canEnterManagerRoleHome(inventoryStaff)).toBe(false);
  });
});

describe("operations navigation", () => {
  it("Retail bottom nav: Home Sell Inventory Orders More when permitted", () => {
    const tabs = buildOperationsBottomNavTabs({
      grant: storeManager,
      experience: "operations",
      branchType: "Retail",
    });
    expect(tabs.map((t) => t.id)).toEqual(["home", "sell", "inventory", "orders", "more"]);
    expect(tabs[0]?.to).toBe("/role/manager");
    expect(tabs.some((t) => t.id === "sell")).toBe(true);
    expect(tabs.some((t) => t.id === "inventory")).toBe(true);
    expect(tabs.some((t) => t.id === "more")).toBe(true);
    expect(tabs.length).toBeLessThanOrEqual(5);
  });

  it("Warehouse bottom nav has no Sell and emphasizes stock", () => {
    const tabs = buildOperationsBottomNavTabs({
      grant: storeManager,
      experience: "operations",
      branchType: "Warehouse",
    });
    expect(tabs.some((t) => t.id === "sell")).toBe(false);
    expect(tabs.map((t) => t.id)).toContain("inventory");
    expect(tabs.map((t) => t.id)).toContain("transfers");
    expect(tabs.map((t) => t.id)).toContain("purchasing");
    expect(tabs.map((t) => t.id)).toContain("more");
    expect(tabs[0]?.to).toBe("/warehouse");
  });

  it("desktop sidebar omits Admin-only destinations", () => {
    const groups = buildOperationsSidebarGroups({
      grant: owner,
      branchType: "Retail",
      experience: "operations",
    });
    const paths = flattenOperationsSidebarItems(groups).map((i) => i.to);
    expect(paths.some((p) => isAdminOnlyOperationsPath(p))).toBe(false);
    expect(paths.some((p) => p.startsWith("/org"))).toBe(false);
    expect(paths).toContain("/sell");
  });

  it("Warehouse sidebar never includes Sell", () => {
    const groups = buildOperationsSidebarGroups({
      grant: owner,
      branchType: "Warehouse",
      experience: "operations",
    });
    const paths = flattenOperationsSidebarItems(groups).map((i) => i.to);
    expect(paths).not.toContain("/sell");
  });

  it("Manager More excludes Admin configuration links", () => {
    const sections = buildOrgMoreSections(owner, {
      branchType: "Retail",
      excludeAdminDestinations: true,
    });
    const links = sections.flatMap((s) => s.links);
    expect(links.some((l) => l.to.startsWith("/org"))).toBe(false);
    expect(links.some((l) => l.testId === "org-more-staff")).toBe(false);
    expect(links.some((l) => l.testId === "org-more-branches")).toBe(false);
    expect(sections.some((s) => s.id === "organization")).toBe(false);
  });

  it("shouldUseOperationsShell respects experience and authority", () => {
    expect(
      shouldUseOperationsShell({
        experience: "manage_business",
        pathname: "/inventory",
        grant: owner,
      }),
    ).toBe(false);
    expect(
      shouldUseOperationsShell({
        experience: "operations",
        pathname: "/org",
        grant: owner,
      }),
    ).toBe(false);
    expect(
      shouldUseOperationsShell({
        experience: "operations",
        pathname: "/inventory",
        grant: owner,
      }),
    ).toBe(true);
  });
});

import { describe, expect, it } from "vitest";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  buildAdminMobileTabs,
  buildAdminNavGroups,
  flattenAdminNavItems,
  matchAdminMobileTab,
  matchAdminNavItem,
  shouldUseAdminManagementShell,
} from "@/features/admin/admin-nav-config";

function grant(
  overrides: Partial<PosSessionGrantFacts> & Pick<PosSessionGrantFacts, "mappedPosRoleCode">,
): PosSessionGrantFacts {
  return {
    productAccessAllowed: true,
    productLocalRoleCode: overrides.mappedPosRoleCode,
    membershipRole: null,
    organizationManagementAuthority: false,
    featureCodes: [],
    grantedFeatureCodes: [],
    ...overrides,
  };
}

describe("admin-nav-config", () => {
  it("builds Owner admin nav with Areas, Branches & Warehouses, Staff, Roles, Devices", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      featureCodes: ["store-area-management", "store-warehouse"],
    });
    const items = flattenAdminNavItems(buildAdminNavGroups(owner));
    expect(items.some((i) => i.id === "areas" && !i.locked)).toBe(true);
    expect(items.some((i) => i.id === "branches" && i.to === "/org/branches")).toBe(true);
    expect(items.some((i) => i.id === "staff")).toBe(true);
    expect(items.some((i) => i.id === "roles")).toBe(true);
    expect(items.some((i) => i.id === "devices")).toBe(true);
    expect(items.some((i) => i.to === "/sell")).toBe(false);
  });

  it("locks Areas without entitlement and keeps Owner-only items Owner-only", () => {
    const ownerNoArea = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      featureCodes: [],
    });
    const items = flattenAdminNavItems(buildAdminNavGroups(ownerNoArea));
    expect(items.find((i) => i.id === "areas")?.locked).toBe(true);

    const orgAdmin = grant({
      mappedPosRoleCode: "Admin",
      membershipRole: "OrganizationAdministrator",
      organizationManagementAuthority: true,
    });
    const adminItems = flattenAdminNavItems(buildAdminNavGroups(orgAdmin));
    expect(adminItems.some((i) => i.id === "staff")).toBe(false);
    expect(adminItems.some((i) => i.id === "ownership")).toBe(false);
    expect(adminItems.some((i) => i.id === "devices")).toBe(true);
  });

  it("denies admin nav for Manager-only principal", () => {
    const manager = grant({ mappedPosRoleCode: "StoreManager" });
    expect(buildAdminNavGroups(manager)).toEqual([]);
    expect(buildAdminMobileTabs(manager)).toEqual([]);
  });

  it("resolves nested active routes", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      featureCodes: ["store-area-management"],
    });
    const items = flattenAdminNavItems(buildAdminNavGroups(owner));
    expect(matchAdminNavItem("/org", items)).toBe("overview");
    expect(matchAdminNavItem("/org/areas/abc", items)).toBe("areas");
    expect(matchAdminNavItem("/org/branches/new", items)).toBe("branches");
    expect(matchAdminNavItem("/org/staff/invite", items)).toBe("staff");
    expect(matchAdminNavItem("/org/roles/Cashier", items)).toBe("roles");
    expect(matchAdminNavItem("/org/devices", items)).toBe("devices");
    expect(matchAdminNavItem("/dashboard", items)).toBe("dashboard");
    expect(matchAdminNavItem("/reports/operational/sales", items)).toBe("reports");
    expect(matchAdminNavItem("/settings/preferences", items)).toBe("preferences");
  });

  it("mobile tabs exclude Sell and map nested paths", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const tabs = buildAdminMobileTabs(owner);
    expect(tabs.map((t) => t.id)).toEqual(["home", "manage", "review", "more"]);
    expect(tabs.every((t) => t.to !== "/sell")).toBe(true);
    expect(matchAdminMobileTab("/org/branches/x", tabs)).toBe("manage");
    expect(matchAdminMobileTab("/org/cash-handling", tabs)).toBe("more");
    expect(matchAdminMobileTab("/dashboard", tabs)).toBe("review");
  });

  it("only enables admin shell for manage_business experience", () => {
    expect(
      shouldUseAdminManagementShell({ experience: "manage_business", pathname: "/org" }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({ experience: "manage_business", pathname: "/dashboard" }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({ experience: "operations", pathname: "/org" }),
    ).toBe(false);
    expect(
      shouldUseAdminManagementShell({ experience: "manage_business", pathname: "/sell" }),
    ).toBe(false);
  });
});

import { describe, expect, it } from "vitest";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  buildAdminMobileTabs,
  buildAdminNavGroups,
  flattenAdminNavItems,
  matchAdminMobileTab,
  matchAdminNavItem,
  resolveConfigureFulfillmentBranchId,
  shouldUseAdminManagementShell,
} from "@/features/admin/admin-nav-config";
import {
  buildOperationsBottomNavTabs,
  buildOperationsSidebarGroups,
  flattenOperationsSidebarItems,
  isAdminOnlyOperationsPath,
} from "@/features/operations/operations-nav-config";

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

  it("exposes Subscription & Billing to the Owner under the organization group", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const groups = buildAdminNavGroups(owner);
    const organization = groups.find((g) => g.id === "organization");
    const subscription = organization?.items.find((i) => i.id === "subscription");

    expect(subscription).toMatchObject({
      to: "/org/subscription",
      testId: "admin-nav-subscription",
      matchPrefixes: ["/org/subscription"],
      labelKey: "admin.nav.subscription",
    });
    expect(subscription?.locked).toBeUndefined();
    expect(matchAdminNavItem("/org/subscription", flattenAdminNavItems(groups))).toBe(
      "subscription",
    );
    expect(
      matchAdminNavItem("/org/subscription?tab=billing", flattenAdminNavItems(groups)),
    ).toBe("subscription");
  });

  it("hides Subscription & Billing from non-owner Manage Business principals", () => {
    const orgAdmin = grant({
      mappedPosRoleCode: "Admin",
      membershipRole: "OrganizationAdministrator",
      organizationManagementAuthority: true,
    });
    expect(
      flattenAdminNavItems(buildAdminNavGroups(orgAdmin)).some((i) => i.id === "subscription"),
    ).toBe(false);

    const manager = grant({ mappedPosRoleCode: "StoreManager" });
    expect(
      flattenAdminNavItems(buildAdminNavGroups(manager)).some((i) => i.id === "subscription"),
    ).toBe(false);
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
    expect(
      matchAdminNavItem("/org/branches/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/fulfillment", items),
    ).toBe("configureFulfillment");
  });

  it("adds Configure Fulfillment under Review for org admins", () => {
    const owner = grant({
      mappedPosRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    const branchId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    const withBranch = flattenAdminNavItems(
      buildAdminNavGroups(owner, { branchId }),
    ).find((i) => i.id === "configureFulfillment");
    expect(withBranch?.to).toBe(`/org/branches/${branchId}/fulfillment`);
    expect(withBranch?.testId).toBe("admin-nav-configure-fulfillment");

    const withoutBranch = flattenAdminNavItems(buildAdminNavGroups(owner)).find(
      (i) => i.id === "configureFulfillment",
    );
    expect(withoutBranch?.to).toBe("/org/branches");
  });

  it("resolves primary retail branch when Manage Business has no bound branch", () => {
    const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const primaryId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    const secondaryId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
    expect(
      resolveConfigureFulfillmentBranchId({
        boundBranchId: null,
        organizationId: orgId,
        workspaces: [
          {
            organizationId: orgId,
            displayName: "Mica",
            branches: [
              {
                branchId: secondaryId,
                name: "Secondary",
                secondaryLine: "",
                isPrimary: false,
                isActive: true,
                branchType: "Retail",
              },
              {
                branchId: primaryId,
                name: "Main Branch",
                secondaryLine: "",
                isPrimary: true,
                isActive: true,
                branchType: "Retail",
              },
            ],
          },
        ],
      }),
    ).toBe(primaryId);

    expect(
      resolveConfigureFulfillmentBranchId({
        boundBranchId: secondaryId,
        organizationId: orgId,
        workspaces: [],
      }),
    ).toBe(secondaryId);
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

  it("uses admin shell for /org management paths even outside manage_business", () => {
    expect(
      shouldUseAdminManagementShell({ experience: "manage_business", pathname: "/org" }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({ experience: "manage_business", pathname: "/dashboard" }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({ experience: "operations", pathname: "/org" }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({
        experience: "operations",
        pathname: "/org/branches/b1/fulfillment",
      }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({
        experience: "operations",
        pathname: "/org/payment-methods",
      }),
    ).toBe(true);
    expect(
      shouldUseAdminManagementShell({
        experience: "operations",
        pathname: "/org/notifications",
      }),
    ).toBe(false);
    expect(
      shouldUseAdminManagementShell({ experience: "operations", pathname: "/customers" }),
    ).toBe(false);
    expect(
      shouldUseAdminManagementShell({ experience: "manage_business", pathname: "/sell" }),
    ).toBe(false);
  });
});

describe("operations navigation stays free of Subscription & Billing", () => {
  const owner = grant({
    mappedPosRoleCode: "Owner",
    membershipRole: "OrganizationOwner",
    organizationManagementAuthority: true,
  });

  it("never adds a subscription destination to the operations sidenav or bottom tabs", () => {
    const items = flattenOperationsSidebarItems(
      buildOperationsSidebarGroups({ grant: owner, experience: "operations" }),
    );
    expect(items.some((i) => i.id === "subscription")).toBe(false);
    expect(items.some((i) => i.to.startsWith("/org/subscription"))).toBe(false);

    const tabs = buildOperationsBottomNavTabs({ grant: owner, experience: "operations" });
    expect(tabs.some((t) => t.to.startsWith("/org/subscription"))).toBe(false);
  });

  it("treats /org/subscription as an admin-only path for the operations shell", () => {
    expect(isAdminOnlyOperationsPath("/org/subscription")).toBe(true);
    expect(isAdminOnlyOperationsPath("/org/subscription?tab=billing")).toBe(true);
  });
});

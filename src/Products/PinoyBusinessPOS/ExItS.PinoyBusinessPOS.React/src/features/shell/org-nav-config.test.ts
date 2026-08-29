import { describe, expect, it } from "vitest";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  buildOrgBottomNavTabs,
  buildOrgMoreLinks,
  matchOrgNavTab,
} from "@/features/shell/org-nav-config";

function baseGrant(
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

describe("org bottom nav config", () => {
  it("keeps at most five tabs and marks Sell as primary for managers", () => {
    const tabs = buildOrgBottomNavTabs({
      grant: baseGrant({ mappedPosRoleCode: "StoreManager" }),
      experience: "operations",
    });
    expect(tabs.length).toBeLessThanOrEqual(5);
    expect(tabs.map((t) => t.id)).toEqual(["home", "catalog", "sell", "orders", "more"]);
    expect(tabs.find((t) => t.id === "sell")?.primary).toBe(true);
    expect(tabs[2]?.id).toBe("sell");
    expect(tabs[0]?.to).toBe("/role/manager");
  });

  it("omits Catalog for Cashier but keeps Sell and More", () => {
    const tabs = buildOrgBottomNavTabs({
      grant: baseGrant({ mappedPosRoleCode: "Cashier", productLocalRoleCode: "Cashier" }),
      experience: "start_selling",
    });
    expect(tabs.some((t) => t.id === "sell")).toBe(true);
    expect(tabs.some((t) => t.id === "catalog")).toBe(false);
    expect(tabs.some((t) => t.id === "more")).toBe(true);
    expect(tabs[0]?.to).toBe("/role/cashier");
  });

  it("matches nested sell routes to the Sell tab", () => {
    const tabs = buildOrgBottomNavTabs({
      grant: baseGrant({ mappedPosRoleCode: "StoreManager" }),
      experience: "operations",
    });
    expect(matchOrgNavTab("/sell/checkout", tabs)).toBe("sell");
    expect(matchOrgNavTab("/catalog/products", tabs)).toBe("catalog");
    expect(matchOrgNavTab("/more", tabs)).toBe("more");
  });

  it("filters More links by role", () => {
    const managerLinks = buildOrgMoreLinks(baseGrant({ mappedPosRoleCode: "StoreManager" }));
    expect(managerLinks.some((l) => l.to === "/customers")).toBe(true);
    expect(managerLinks.some((l) => l.to === "/reports")).toBe(true);
    expect(managerLinks.some((l) => l.to === "/inventory")).toBe(true);

    const inventoryStaffLinks = buildOrgMoreLinks(
      baseGrant({ mappedPosRoleCode: "InventoryStaff", productLocalRoleCode: "InventoryStaff" }),
    );
    expect(inventoryStaffLinks.some((l) => l.to === "/inventory")).toBe(true);

    const reportingLinks = buildOrgMoreLinks(
      baseGrant({ mappedPosRoleCode: "ReportingUser", productLocalRoleCode: "ReportingUser" }),
    );
    expect(reportingLinks.some((l) => l.to === "/inventory")).toBe(true);

    const cashierLinks = buildOrgMoreLinks(
      baseGrant({ mappedPosRoleCode: "Cashier", productLocalRoleCode: "Cashier" }),
    );
    expect(cashierLinks.some((l) => l.to === "/customers")).toBe(false);
    expect(cashierLinks.some((l) => l.to === "/inventory")).toBe(false);
    expect(cashierLinks.some((l) => l.to === "/settings/preferences")).toBe(true);
  });
});

import { describe, expect, it } from "vitest";
import { navigationRegistry } from "@/lib/navigation/navigation-registry";
import { resolveNavigation } from "@/lib/navigation/resolve-navigation";
import { reactImplementationStatus } from "@/lib/navigation/react-implementation";
import {
  parseUsageLimitsSearchParams,
  usageLimitsSearchParams,
} from "@/features/usage-limits/usage-limits-query";
import { resolveKnownReactRoute } from "@/lib/navigation/known-react-routes";

const loadedAuthorized = {
  permissionStatus: "loaded" as const,
  hasAnyPermission: () => true,
  isPlatformAdministrator: true,
  developmentToolsAllowed: false,
};

describe("saas ops navigation", () => {
  it("registers Usage & Limits after Personal Features in Products & Commercial", () => {
    const items = navigationRegistry.find((section) => section.id === "products")?.items ?? [];
    const personalIndex = items.findIndex((item) => item.id === "PWEB-NAV-PERSONAL-FEATURES");
    const usageIndex = items.findIndex((item) => item.id === "PWEB-NAV-USAGE-LIMITS");
    expect(personalIndex).toBeGreaterThanOrEqual(0);
    expect(usageIndex).toBe(personalIndex + 1);
    expect(items[usageIndex]?.href).toBe("/admin/usage");
  });

  it("inserts Support between Global Catalog and Governance", () => {
    const sections = navigationRegistry.map((section) => section.id);
    expect(sections.indexOf("catalog")).toBeLessThan(sections.indexOf("support"));
    expect(sections.indexOf("support")).toBeLessThan(sections.indexOf("governance"));
  });

  it("registers operations routes for product operations and background jobs", () => {
    const items = navigationRegistry.find((section) => section.id === "operations")?.items ?? [];
    expect(items.map((item) => item.href)).toEqual(
      expect.arrayContaining([
        "/admin/system-health",
        "/admin/operations/products",
        "/admin/operations/jobs",
      ]),
    );
  });

  it("marks new SaaS ops nav items as implemented", () => {
    for (const id of [
      "PWEB-NAV-USAGE-LIMITS",
      "PWEB-NAV-SUPPORT-CONSOLE",
      "PWEB-NAV-PRODUCT-OPERATIONS",
      "PWEB-NAV-BACKGROUND-JOBS",
    ]) {
      const item = navigationRegistry
        .flatMap((section) => section.items)
        .find((entry) => entry.id === id);
      expect(item, id).toBeDefined();
      expect(reactImplementationStatus(item!)).toBe("IMPLEMENTED");
    }
  });

  it("shows support and operations items to platform administrators", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: false,
    });
    const ids = sections.flatMap((section) => section.items.map((item) => item.id));
    expect(ids).toContain("PWEB-NAV-SUPPORT-CONSOLE");
    expect(ids).toContain("PWEB-NAV-PRODUCT-OPERATIONS");
    expect(ids).toContain("PWEB-NAV-BACKGROUND-JOBS");
  });
});

describe("saas ops known routes", () => {
  it.each([
    "/admin/usage",
    "/admin/support",
    "/admin/operations/products",
    "/admin/operations/jobs",
  ])("treats %s as implemented for authorized admins", (pathname) => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname })).toBe("implemented");
  });

  it("fails closed for support console without platform administrator", () => {
    expect(
      resolveKnownReactRoute({
        pathname: "/admin/support",
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: false,
        developmentToolsAllowed: false,
      }),
    ).toBe("unknown");
  });
});

describe("usage limits url state", () => {
  it("round-trips organization and product filters", () => {
    const params = usageLimitsSearchParams({
      organizationId: "11111111-1111-1111-1111-111111111111",
      productCode: "pinoy-business-pos",
      page: 2,
    });
    const parsed = parseUsageLimitsSearchParams(params);
    expect(parsed.organizationId).toBe("11111111-1111-1111-1111-111111111111");
    expect(parsed.productCode).toBe("pinoy-business-pos");
    expect(parsed.page).toBe(2);
  });
});

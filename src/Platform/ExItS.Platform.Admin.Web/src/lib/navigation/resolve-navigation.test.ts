import { describe, expect, it } from "vitest";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { navigationRegistry } from "@/lib/navigation/navigation-registry";
import { resolveNavigation } from "@/lib/navigation/resolve-navigation";

function itemIds(sections: ReturnType<typeof resolveNavigation>): string[] {
  return sections.flatMap((section) => section.items.map((item) => item.id));
}

function developmentItem(sections: ReturnType<typeof resolveNavigation>, id: string) {
  return sections
    .find((section) => section.id === "development")
    ?.items.find((item) => item.id === id);
}

describe("resolveNavigation", () => {
  it("shows only authenticated implemented items while permissions are loading", () => {
    const sections = resolveNavigation({
      permissionStatus: "loading",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: true,
    });
    expect(itemIds(sections)).toEqual(["PWEB-NAV-OVERVIEW"]);
    expect(itemIds(sections)).not.toContain("PWEB-NAV-ALL-ORGANIZATIONS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-EVENT-DELIVERY");
  });

  it("hides unauthorized items when permission state is loaded", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => false,
      isPlatformAdministrator: false,
      developmentToolsAllowed: true,
    });
    expect(itemIds(sections)).toEqual(["PWEB-NAV-OVERVIEW"]);
    expect(itemIds(sections)).not.toContain("PWEB-NAV-ALL-ORGANIZATIONS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-EVENT-DELIVERY");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-TEST-PAYMENTS");
  });

  it("keeps under-development and planned items in canonical sections without Development tools", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: false,
    });
    expect(itemIds(sections)).toContain("PWEB-NAV-OVERVIEW");
    expect(itemIds(sections)).toContain("PWEB-NAV-ALL-ORGANIZATIONS");
    expect(itemIds(sections)).toContain("PWEB-NAV-BY-PRODUCT");
    expect(itemIds(sections)).toContain("PWEB-NAV-ALL-ACCOUNTS");
    expect(itemIds(sections)).toContain("PWEB-NAV-EVENT-DELIVERY");
    expect(itemIds(sections)).toContain("PWEB-NAV-PLATFORM-SETTINGS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-TEST-PAYMENTS");
    expect(sections.some((section) => section.id === "development")).toBe(false);
    expect(
      sections
        .find((section) => section.id === "people")
        ?.items.find((item) => item.id === "PWEB-NAV-ALL-ACCOUNTS")?.presentation,
    ).toBe("link");
    expect(
      sections
        .find((section) => section.id === "operations")
        ?.items.find((item) => item.id === "PWEB-NAV-EVENT-DELIVERY")?.presentation,
    ).toBe("planned");
    expect(
      sections
        .find((section) => section.id === "settings")
        ?.items.find((item) => item.id === "PWEB-NAV-PLATFORM-SETTINGS")?.presentation,
    ).toBe("link");
    expect(
      sections
        .find((section) => section.id === "settings")
        ?.items.find((item) => item.id === "PWEB-NAV-PLATFORM-SETTINGS")?.href,
    ).toBe("/admin/settings");
  });

  it("keeps Development section for DEV_TEST_ONLY only when tools are allowed", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: true,
    });
    expect(developmentItem(sections, "PWEB-NAV-TEST-PAYMENTS")?.presentation).toBe(
      "underDevelopment",
    );
    expect(developmentItem(sections, "PWEB-NAV-EVENT-DELIVERY")).toBeUndefined();
    expect(developmentItem(sections, "PWEB-NAV-ALL-ACCOUNTS")).toBeUndefined();
    expect(
      sections.find((section) => section.id === "people")?.items.map((item) => item.id),
    ).toContain("PWEB-NAV-ALL-ACCOUNTS");
  });

  it("injects dynamic By Product children from catalog without hardcoding names", () => {
    const sections = resolveNavigation(
      {
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: false,
        developmentToolsAllowed: false,
      },
      [
        { code: "future-product-x", displayName: "Future Product X" },
        { code: "pinoy-business-pos", displayName: "Pinoy Business POS" },
      ],
    );
    const byProduct = sections
      .find((section) => section.id === "organizations")
      ?.items.find((item) => item.id === "PWEB-NAV-BY-PRODUCT");
    expect(byProduct?.presentation).toBe("group");
    expect(byProduct?.children?.map((child) => child.id)).toEqual([
      "PWEB-NAV-ORG-BY-PRODUCT:future-product-x",
      "PWEB-NAV-ORG-BY-PRODUCT:pinoy-business-pos",
    ]);
    expect(byProduct?.children?.[0]?.label).toBe("Future Product X");
    expect(byProduct?.children?.[0]?.href).toBe("/admin/organizations?product=future-product-x");
    expect(
      navigationRegistry.some((section) => JSON.stringify(section).includes("Future Product")),
    ).toBe(false);
  });

  it.each(["development", "test", "testing"] as const)(
    "shows DEV_TEST_ONLY for frontend mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(true);
      const sections = resolveNavigation({
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: areDevelopmentToolsAllowed(mode),
      });
      expect(developmentItem(sections, "PWEB-NAV-TEST-PAYMENTS")).toBeDefined();
    },
  );

  it("preserves canonical lifecycle metadata independently of presentation", () => {
    const items = navigationRegistry.flatMap((section) => section.items);
    expect(items.find((item) => item.id === "PWEB-NAV-ALL-ORGANIZATIONS")?.lifecycle).toBe(
      "AVAILABLE",
    );
    expect(items.find((item) => item.id === "PWEB-NAV-EVENT-DELIVERY")?.lifecycle).toBe(
      "PLANNED_DISABLED",
    );
  });
});

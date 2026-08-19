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
    expect(itemIds(sections)).not.toContain("PWEB-NAV-ORGANIZATIONS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-EVENT-DELIVERY");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-PLATFORM-SETTINGS");
  });

  it("hides unauthorized migration and planned items when permission state is loaded", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => false,
      isPlatformAdministrator: false,
      developmentToolsAllowed: true,
    });
    expect(itemIds(sections)).toEqual(["PWEB-NAV-OVERVIEW"]);
    expect(itemIds(sections)).not.toContain("PWEB-NAV-ORGANIZATIONS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-EVENT-DELIVERY");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-PLATFORM-SETTINGS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-TEST-PAYMENTS");
  });

  it("keeps production-shaped navigation implemented-only for a fully authorized actor", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: false,
    });
    expect(itemIds(sections)).toEqual(["PWEB-NAV-OVERVIEW"]);
    expect(itemIds(sections)).not.toContain("PWEB-NAV-ORGANIZATIONS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-EVENT-DELIVERY");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-PLATFORM-SETTINGS");
    expect(itemIds(sections)).not.toContain("PWEB-NAV-TEST-PAYMENTS");
    expect(sections.some((section) => section.id === "development")).toBe(false);
  });

  it("relocates authorized migration and planned items under Development when tools are allowed", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: true,
    });
    const items = sections.flatMap((section) => section.items);
    expect(items.find((item) => item.id === "PWEB-NAV-OVERVIEW")?.presentation).toBe("link");
    expect(sections.find((section) => section.id === "home")?.items.map((item) => item.id)).toEqual(
      ["PWEB-NAV-OVERVIEW"],
    );
    expect(sections.find((section) => section.id === "organizations")).toBeUndefined();
    expect(sections.find((section) => section.id === "operations")).toBeUndefined();
    expect(sections.find((section) => section.id === "settings")).toBeUndefined();
    expect(developmentItem(sections, "PWEB-NAV-ORGANIZATIONS")?.presentation).toBe(
      "underDevelopment",
    );
    expect(developmentItem(sections, "PWEB-NAV-TEST-PAYMENTS")?.presentation).toBe(
      "underDevelopment",
    );
    expect(developmentItem(sections, "PWEB-NAV-EVENT-DELIVERY")?.presentation).toBe("planned");
    expect(developmentItem(sections, "PWEB-NAV-PLATFORM-SETTINGS")?.presentation).toBe("planned");
  });

  it.each(["development", "test", "testing"] as const)(
    "shows authorized under-development and planned items for frontend mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(true);
      const sections = resolveNavigation({
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: areDevelopmentToolsAllowed(mode),
      });
      expect(developmentItem(sections, "PWEB-NAV-ORGANIZATIONS")?.presentation).toBe(
        "underDevelopment",
      );
      expect(developmentItem(sections, "PWEB-NAV-EVENT-DELIVERY")?.presentation).toBe("planned");
      expect(developmentItem(sections, "PWEB-NAV-PLATFORM-SETTINGS")?.presentation).toBe("planned");
    },
  );

  it.each(["production", "staging", "preview", "qa", "uat", "unknown"] as const)(
    "hides planned and under-development migration items for frontend mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(false);
      const sections = resolveNavigation({
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: areDevelopmentToolsAllowed(mode),
      });
      expect(itemIds(sections)).toEqual(["PWEB-NAV-OVERVIEW"]);
      expect(itemIds(sections)).not.toContain("PWEB-NAV-ORGANIZATIONS");
      expect(itemIds(sections)).not.toContain("PWEB-NAV-EVENT-DELIVERY");
      expect(itemIds(sections)).not.toContain("PWEB-NAV-PLATFORM-SETTINGS");
      expect(itemIds(sections)).not.toContain("PWEB-NAV-TEST-PAYMENTS");
      expect(sections.some((section) => section.id === "development")).toBe(false);
    },
  );

  it("preserves canonical lifecycle metadata independently of presentation", () => {
    const items = navigationRegistry.flatMap((section) => section.items);
    expect(items.find((item) => item.id === "PWEB-NAV-ORGANIZATIONS")?.lifecycle).toBe("AVAILABLE");
    expect(items.find((item) => item.id === "PWEB-NAV-EVENT-DELIVERY")?.lifecycle).toBe(
      "PLANNED_DISABLED",
    );
    expect(items.find((item) => item.id === "PWEB-NAV-PLATFORM-SETTINGS")?.lifecycle).toBe(
      "PLANNED_DISABLED",
    );
  });
});

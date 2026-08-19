import { describe, expect, it } from "vitest";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { resolveNavigation } from "@/lib/navigation/resolve-navigation";

describe("resolveNavigation", () => {
  it("shows only authenticated items while permissions are loading", () => {
    const sections = resolveNavigation({
      permissionStatus: "loading",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: true,
    });
    const ids = sections.flatMap((section) => section.items.map((item) => item.id));
    expect(ids).toEqual(["PWEB-NAV-OVERVIEW"]);
    expect(ids).not.toContain("PWEB-NAV-ORGANIZATIONS");
  });

  it("hides unauthorized items when permission state is loaded", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => false,
      isPlatformAdministrator: false,
      developmentToolsAllowed: true,
    });
    const ids = sections.flatMap((section) => section.items.map((item) => item.id));
    expect(ids).toEqual(["PWEB-NAV-OVERVIEW"]);
    expect(ids).not.toContain("PWEB-NAV-ORGANIZATIONS");
    expect(ids).not.toContain("PWEB-NAV-EVENT-DELIVERY");
  });

  it("shows planned items as disabled when the actor is authorized", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: false,
    });
    const planned = sections
      .flatMap((section) => section.items)
      .find((item) => item.id === "PWEB-NAV-EVENT-DELIVERY");
    expect(planned?.presentation).toBe("planned");
  });

  it("omits DEV_TEST_ONLY items outside development/test/testing", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: false,
    });
    const ids = sections.flatMap((section) => section.items.map((item) => item.id));
    expect(ids).not.toContain("PWEB-NAV-TEST-PAYMENTS");
    expect(sections.some((section) => section.id === "development")).toBe(false);
  });

  it("includes DEV_TEST_ONLY items when development tools are allowed", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: true,
    });
    const ids = sections.flatMap((section) => section.items.map((item) => item.id));
    expect(ids).toContain("PWEB-NAV-TEST-PAYMENTS");
  });

  it("keeps Overview as an implemented link and relocates under-development items", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: true,
    });
    const items = sections.flatMap((section) => section.items);
    expect(items.find((item) => item.id === "PWEB-NAV-OVERVIEW")?.presentation).toBe("link");
    expect(items.find((item) => item.id === "PWEB-NAV-ORGANIZATIONS")?.presentation).toBe(
      "underDevelopment",
    );
    expect(sections.find((section) => section.id === "organizations")).toBeUndefined();
    expect(
      sections.find((section) => section.id === "development")?.items.map((item) => item.id),
    ).toContain("PWEB-NAV-ORGANIZATIONS");
    expect(items.find((item) => item.id === "PWEB-NAV-EVENT-DELIVERY")?.presentation).toBe(
      "planned",
    );
  });

  it("hides under-development migration items when tools are disallowed", () => {
    const sections = resolveNavigation({
      permissionStatus: "loaded",
      hasAnyPermission: () => true,
      isPlatformAdministrator: true,
      developmentToolsAllowed: false,
    });
    const ids = sections.flatMap((section) => section.items.map((item) => item.id));
    expect(ids).toContain("PWEB-NAV-OVERVIEW");
    expect(ids).not.toContain("PWEB-NAV-ORGANIZATIONS");
    expect(ids).not.toContain("PWEB-NAV-ALL-ACCOUNTS");
    expect(ids).not.toContain("PWEB-NAV-PRODUCTS");
    expect(sections.some((section) => section.id === "development")).toBe(false);
  });

  it.each(["development", "test", "testing"] as const)(
    "shows authorized under-development items for frontend mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(true);
      const sections = resolveNavigation({
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: areDevelopmentToolsAllowed(mode),
      });
      const organizations = sections
        .find((section) => section.id === "development")
        ?.items.find((item) => item.id === "PWEB-NAV-ORGANIZATIONS");
      expect(organizations?.presentation).toBe("underDevelopment");
    },
  );

  it.each(["production", "staging", "preview", "qa", "uat", "unknown"] as const)(
    "hides under-development migration items for frontend mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(false);
      const sections = resolveNavigation({
        permissionStatus: "loaded",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: areDevelopmentToolsAllowed(mode),
      });
      const ids = sections.flatMap((section) => section.items.map((item) => item.id));
      expect(ids).not.toContain("PWEB-NAV-ORGANIZATIONS");
      expect(sections.some((section) => section.id === "development")).toBe(false);
    },
  );
});

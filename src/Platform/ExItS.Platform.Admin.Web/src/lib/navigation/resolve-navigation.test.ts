import { describe, expect, it } from "vitest";
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
});

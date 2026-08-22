import { describe, expect, it } from "vitest";
import { navigationRegistry } from "@/lib/navigation/navigation-registry";
import { resolveKnownReactRoute } from "@/lib/navigation/known-react-routes";
import { reactImplementationStatus } from "@/lib/navigation/react-implementation";

const loadedAuthorized = {
  permissionStatus: "loaded" as const,
  hasAnyPermission: () => true,
  isPlatformAdministrator: true,
  developmentToolsAllowed: false,
};

describe("react implementation status", () => {
  it("keeps Overview and Organizations implemented without changing lifecycle", () => {
    const overview = navigationRegistry
      .flatMap((section) => section.items)
      .find((item) => item.id === "PWEB-NAV-OVERVIEW");
    const organizations = navigationRegistry
      .flatMap((section) => section.items)
      .find((item) => item.id === "PWEB-NAV-ALL-ORGANIZATIONS");
    expect(overview?.lifecycle).toBe("AVAILABLE");
    expect(organizations?.lifecycle).toBe("AVAILABLE");
    expect(reactImplementationStatus(overview!)).toBe("IMPLEMENTED");
    expect(reactImplementationStatus(organizations!)).toBe("IMPLEMENTED");
  });
});

describe("resolveKnownReactRoute", () => {
  it("treats /admin as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin" })).toBe("implemented");
  });

  it("treats /admin/products as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/products" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/plans as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/plans" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/subscriptions as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/subscriptions" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/payments as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/payments" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/entitlements as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/entitlements" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/audit as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/audit" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/privacy-compliance as implemented", () => {
    expect(
      resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/privacy-compliance" }),
    ).toBe("implemented");
  });

  it("treats /admin/platform-roles as implemented", () => {
    expect(
      resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/platform-roles" }),
    ).toBe("implemented");
  });

  it("treats /admin/users as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/users" })).toBe(
      "implemented",
    );
  });

  it("treats /admin/organizations as implemented", () => {
    expect(resolveKnownReactRoute({ ...loadedAuthorized, pathname: "/admin/organizations" })).toBe(
      "implemented",
    );
  });

  it("treats unknown pathnames as unknown", () => {
    expect(
      resolveKnownReactRoute({
        ...loadedAuthorized,
        pathname: "/admin/this-route-does-not-exist-xyz",
      }),
    ).toBe("unknown");
  });

  it("stays pending while authorization is loading", () => {
    expect(
      resolveKnownReactRoute({
        pathname: "/admin/organizations",
        permissionStatus: "loading",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: true,
      }),
    ).toBe("pending");
  });

  it("fails closed when authorization load failed", () => {
    expect(
      resolveKnownReactRoute({
        pathname: "/admin/organizations",
        permissionStatus: "failed",
        hasAnyPermission: () => true,
        isPlatformAdministrator: true,
        developmentToolsAllowed: true,
      }),
    ).toBe("unknown");
  });

  it("does not reveal privileged known routes to unauthorized users", () => {
    expect(
      resolveKnownReactRoute({
        pathname: "/admin/organizations",
        permissionStatus: "loaded",
        hasAnyPermission: () => false,
        isPlatformAdministrator: false,
        developmentToolsAllowed: true,
      }),
    ).toBe("unknown");
  });

  it("hides DEV_TEST_ONLY routes outside development tools", () => {
    expect(
      resolveKnownReactRoute({
        ...loadedAuthorized,
        pathname: "/admin/local-validation/test-payments",
        developmentToolsAllowed: false,
      }),
    ).toBe("unknown");
  });
});

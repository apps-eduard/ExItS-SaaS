import { describe, expect, it } from "vitest";
import {
  isOrganizationContextLocked,
  looksLikeOrgScopedStaffLogin,
  normalizeAccountClass,
  sessionAccountClass,
} from "@/session/account-class";

describe("account-class", () => {
  it("normalizes AccountClass wire values without inferring from email", () => {
    expect(normalizeAccountClass("Personal")).toBe("Personal");
    expect(normalizeAccountClass("organization")).toBe("Organization");
    expect(normalizeAccountClass("PLATFORM")).toBe("Platform");
    expect(normalizeAccountClass("paul@gmail.com")).toBeNull();
    expect(normalizeAccountClass("paul@ORG907757")).toBeNull();
    expect(normalizeAccountClass(null)).toBeNull();
  });

  it("reads AccountClass only from session snapshot", () => {
    expect(
      sessionAccountClass({
        accountClass: "Organization",
        homeOrganizationId: "org-a",
        organizationContextLocked: true,
        email: "paul@ORG907757",
      }),
    ).toBe("Organization");
    expect(
      sessionAccountClass({
        accountClass: "Personal",
        email: "paul@gmail.com",
      }),
    ).toBe("Personal");
    expect(sessionAccountClass({ email: "paul@gmail.com" })).toBeNull();
  });

  it("treats organizationContextLocked as staff HomeOrganization lock", () => {
    expect(isOrganizationContextLocked({ organizationContextLocked: true })).toBe(true);
    expect(isOrganizationContextLocked({ organizationContextLocked: false })).toBe(false);
    expect(isOrganizationContextLocked({})).toBe(false);
  });

  it("detects org-scoped staff login shape for display hints only", () => {
    expect(looksLikeOrgScopedStaffLogin("paul@ORG907757")).toBe(true);
    expect(looksLikeOrgScopedStaffLogin("maria@ORG001842")).toBe(true);
    expect(looksLikeOrgScopedStaffLogin("paul@gmail.com")).toBe(false);
    expect(looksLikeOrgScopedStaffLogin("paul@org907757.example")).toBe(false);
  });
});

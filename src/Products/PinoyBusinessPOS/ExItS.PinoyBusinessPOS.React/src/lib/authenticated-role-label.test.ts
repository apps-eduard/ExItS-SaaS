import { describe, expect, it } from "vitest";
import { resolveAuthenticatedRoleLabelKey } from "@/lib/authenticated-role-label";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";

function session(accountClass: string): BrowserSessionSnapshot {
  return { accountClass } as BrowserSessionSnapshot;
}

describe("resolveAuthenticatedRoleLabelKey", () => {
  it("prefers Personal account class over grant", () => {
    expect(
      resolveAuthenticatedRoleLabelKey(session("Personal"), {
        membershipRole: "OrganizationOwner",
        mappedPosRoleCode: "Owner",
      } as PosSessionGrantFacts),
    ).toBe("personal.badge");
  });

  it("shows Owner for OrganizationOwner membership even with manager POS mapping", () => {
    expect(
      resolveAuthenticatedRoleLabelKey(session("Organization"), {
        membershipRole: "OrganizationOwner",
        organizationManagementAuthority: true,
        mappedPosRoleCode: "StoreManager",
        productLocalRoleCode: "StoreManager",
        productAccessAllowed: true,
      }),
    ).toBe("account.role.owner");
  });

  it("shows Manager for StoreManager without owner membership", () => {
    expect(
      resolveAuthenticatedRoleLabelKey(session("Organization"), {
        productAccessAllowed: true,
        mappedPosRoleCode: "StoreManager",
        productLocalRoleCode: "StoreManager",
      }),
    ).toBe("account.role.manager");
  });

  it("shows Cashier for Cashier grant", () => {
    expect(
      resolveAuthenticatedRoleLabelKey(session("Organization"), {
        productAccessAllowed: true,
        mappedPosRoleCode: "Cashier",
        productLocalRoleCode: "Cashier",
      }),
    ).toBe("account.role.cashier");
  });

  it("shows Admin for OrganizationAdministrator membership", () => {
    expect(
      resolveAuthenticatedRoleLabelKey(session("Organization"), {
        membershipRole: "OrganizationAdministrator",
        mappedPosRoleCode: "StoreManager",
      } as PosSessionGrantFacts),
    ).toBe("account.role.admin");
  });

  it("returns null when role cannot be resolved (no workspace fallback)", () => {
    expect(resolveAuthenticatedRoleLabelKey(session("Organization"), null)).toBeNull();
    expect(
      resolveAuthenticatedRoleLabelKey(session("Organization"), {
        productAccessAllowed: true,
        mappedPosRoleCode: "ReportingUser",
      }),
    ).toBeNull();
  });
});

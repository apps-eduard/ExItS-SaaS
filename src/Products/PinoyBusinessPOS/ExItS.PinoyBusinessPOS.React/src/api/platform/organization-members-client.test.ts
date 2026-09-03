import { describe, expect, it } from "vitest";
import {
  buildWorkspaceRoster,
  friendlyMembershipRoleLabel,
  friendlyProductRoleLabel,
  personAppearsOnBranch,
  type OrganizationMemberWire,
} from "@/api/platform/organization-members-client";

describe("organization members roster helpers", () => {
  it("maps management Owner/Admin separately from branch staff", () => {
    const members: OrganizationMemberWire[] = [
      {
        id: "m1",
        organizationId: "o1",
        userId: "u1",
        role: "OrganizationOwner",
        status: "Active",
        displayName: "Owner Person",
      },
      {
        id: "m2",
        organizationId: "o1",
        userId: "u2",
        role: "OrganizationAdministrator",
        status: "Active",
        displayName: "Admin Person",
      },
      {
        id: "m3",
        organizationId: "o1",
        userId: "u3",
        role: "OrganizationMember",
        status: "Active",
        displayName: "Manager Person",
        productRoles: ["StoreManager"],
        branch: "Main Branch",
      },
      {
        id: "m4",
        organizationId: "o1",
        userId: "u4",
        role: "OrganizationMember",
        status: "Active",
        displayName: "Cashier Person",
        productRoles: ["Cashier"],
        branch: "Main Branch",
      },
    ];

    const roster = buildWorkspaceRoster(members);
    expect(roster.managementTeam.map((p) => p.roleLabel)).toEqual(["Owner", "Admin"]);
    expect(roster.branchStaff.map((p) => `${p.displayName}:${p.roleLabel}`)).toEqual([
      "Manager Person:Manager",
      "Cashier Person:Cashier",
    ]);
  });

  it("uses friendly labels", () => {
    expect(friendlyMembershipRoleLabel("OrganizationOwner")).toBe("Owner");
    expect(friendlyMembershipRoleLabel("OrganizationAdministrator")).toBe("Admin");
    expect(friendlyProductRoleLabel(["StoreManager"])).toBe("Manager");
    expect(friendlyProductRoleLabel(["Cashier"])).toBe("Cashier");
  });

  it("filters staff by explicit branch ids and AllActive scope", () => {
    const main = { branchId: "main", name: "Main Branch" };
    const iloilo = { branchId: "iloilo", name: "Iloilo Branch" };
    const kalibo = { branchId: "kalibo", name: "Kalibo Branch" };

    expect(
      personAppearsOnBranch(
        { branchIds: ["main", "iloilo"], allActiveBranches: false, branchName: null },
        main,
      ),
    ).toBe(true);
    expect(
      personAppearsOnBranch(
        { branchIds: ["main", "iloilo"], allActiveBranches: false, branchName: null },
        kalibo,
      ),
    ).toBe(false);
    expect(
      personAppearsOnBranch(
        { branchIds: [], allActiveBranches: true, branchName: null },
        kalibo,
      ),
    ).toBe(true);
    expect(
      personAppearsOnBranch(
        { branchIds: [], allActiveBranches: false, branchName: null },
        iloilo,
      ),
    ).toBe(false);
  });
});

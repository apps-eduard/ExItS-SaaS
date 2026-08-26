import { describe, expect, it } from "vitest";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import type { AccessibleOrganizationWorkspace } from "@/workspace/types";
import {
  buildOrganizationDestinations,
  resolveDestinationRouting,
} from "@/workspace/workspace-destinations";

function grant(partial: Partial<SessionGrantResponse>): SessionGrantResponse {
  return {
    accessToken: "test",
    productAccessAllowed: true,
    mappedPosRoleCode: null,
    productLocalRoleCode: null,
    membershipRole: null,
    organizationManagementAuthority: false,
    ...partial,
  };
}

function workspace(
  orgId: string,
  name: string,
  branches: Array<{ id: string; name: string }>,
): AccessibleOrganizationWorkspace {
  return {
    organizationId: orgId,
    displayName: name,
    branches: branches.map((b, index) => ({
      branchId: b.id,
      name: b.name,
      secondaryLine: "Active",
      isPrimary: index === 0,
      isActive: true,
    })),
  };
}

describe("workspace destinations", () => {
  it("OWNER_ONE_ORG_ONE_BRANCH shows Manage Business + Operations + Start Selling", () => {
    const owner = grant({
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      mappedPosRoleCode: "Owner",
      productAccessAllowed: true,
    });
    const destinations = buildOrganizationDestinations({
      workspace: workspace("org-1", "Store", [{ id: "b1", name: "Main" }]),
      grant: owner,
    });
    expect(destinations.map((d) => d.experience)).toEqual([
      "manage_business",
      "operations",
      "start_selling",
    ]);
    expect(destinations[0].branchId).toBeNull();
    expect(
      resolveDestinationRouting({
        workspaces: [workspace("org-1", "Store", [{ id: "b1", name: "Main" }])],
        grantByOrganizationId: new Map([["org-1", owner]]),
      }).outcome,
    ).toBe("ShowChooser");
  });

  it("ADMIN_ONLY_ONE_ORG auto-routes Manage Business without branch", () => {
    const admin = grant({
      membershipRole: "OrganizationAdministrator",
      organizationManagementAuthority: true,
      productAccessAllowed: false,
      mappedPosRoleCode: null,
    });
    const routing = resolveDestinationRouting({
      workspaces: [workspace("org-1", "Store", [])],
      grantByOrganizationId: new Map([["org-1", admin]]),
    });
    expect(routing.outcome).toBe("AutoDestination");
    if (routing.outcome === "AutoDestination") {
      expect(routing.destination.experience).toBe("manage_business");
      expect(routing.destination.branchId).toBeNull();
      expect(routing.destination.route).toBe("/org");
    }
  });

  it("CASHIER_ONE_BRANCH auto-routes Start Selling only", () => {
    const cashier = grant({
      membershipRole: "OrganizationMember",
      mappedPosRoleCode: "Cashier",
      productAccessAllowed: true,
    });
    const routing = resolveDestinationRouting({
      workspaces: [workspace("org-1", "Store", [{ id: "b1", name: "Main" }])],
      grantByOrganizationId: new Map([["org-1", cashier]]),
    });
    expect(routing.outcome).toBe("AutoDestination");
    if (routing.outcome === "AutoDestination") {
      expect(routing.destination.experience).toBe("start_selling");
      expect(routing.destination.branchId).toBe("b1");
    }
  });

  it("MANAGER_ONE_BRANCH shows Operations + Start Selling chooser", () => {
    const manager = grant({
      membershipRole: "OrganizationMember",
      mappedPosRoleCode: "StoreManager",
      productAccessAllowed: true,
    });
    const destinations = buildOrganizationDestinations({
      workspace: workspace("org-1", "Store", [{ id: "b1", name: "Main" }]),
      grant: manager,
    });
    expect(destinations.map((d) => d.experience)).toEqual(["operations", "start_selling"]);
    expect(destinations.every((d) => d.experience !== "manage_business")).toBe(true);
  });

  it("multi-org never auto-selects", () => {
    const owner = grant({
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      mappedPosRoleCode: "Owner",
    });
    const routing = resolveDestinationRouting({
      workspaces: [
        workspace("org-1", "A", [{ id: "b1", name: "Main" }]),
        workspace("org-2", "B", [{ id: "b2", name: "Main" }]),
      ],
      grantByOrganizationId: new Map([
        ["org-1", owner],
        ["org-2", owner],
      ]),
    });
    expect(routing.outcome).toBe("ShowChooser");
  });

  it("OWNER_MULTI_BRANCH lists Manage Business once and branch actions per branch", () => {
    const owner = grant({
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      mappedPosRoleCode: "Owner",
    });
    const destinations = buildOrganizationDestinations({
      workspace: workspace("org-1", "Store", [
        { id: "b1", name: "Main" },
        { id: "b2", name: "Two" },
      ]),
      grant: owner,
    });
    expect(destinations.filter((d) => d.experience === "manage_business")).toHaveLength(1);
    expect(destinations.filter((d) => d.experience === "operations")).toHaveLength(2);
    expect(destinations.filter((d) => d.experience === "start_selling")).toHaveLength(2);
  });
});

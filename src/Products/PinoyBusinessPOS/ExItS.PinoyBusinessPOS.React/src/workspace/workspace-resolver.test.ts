import { describe, expect, it } from "vitest";
import {
  buildAccessibleWorkspaces,
  isActiveBranchStatus,
  resolveWorkspaceRoutingPlan,
} from "@/workspace/workspace-resolver";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";

const orgA = "11111111-1111-1111-1111-111111111111";
const orgB = "22222222-2222-2222-2222-222222222222";
const mainA = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const branchA2 = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const mainB = "cccccccc-cccc-cccc-cccc-cccccccccccc";

function branch(
  id: string,
  organizationId: string,
  name: string,
  isPrimary = false,
): PlatformBranch {
  return {
    id,
    organizationId,
    code: name.slice(0, 3).toUpperCase(),
    name,
    isPrimary,
    status: "Active",
  };
}

describe("workspace resolver AMEND-03", () => {
  it("case A — single org and single Active branch shows chooser until destination probe", () => {
    const workspaces = buildAccessibleWorkspaces(
      [{ organizationId: orgA, displayName: "Store" }],
      new Map([[orgA, [branch(mainA, orgA, "Main Branch", true)]]]),
    );
    const plan = resolveWorkspaceRoutingPlan({ organizationCount: 1, workspaces });
    expect(plan.outcome).toBe("ShowChooser");
  });

  it("includes Owner/Admin orgs with no Active branches for Manage Business", () => {
    const workspaces = buildAccessibleWorkspaces(
      [
        {
          organizationId: orgA,
          displayName: "Admin Org",
          membershipRole: "OrganizationAdministrator",
        },
      ],
      new Map([[orgA, []]]),
      { includeManagementOrgsWithoutBranches: true },
    );
    expect(workspaces).toHaveLength(1);
    expect(workspaces[0].branches).toHaveLength(0);
  });

  it("case B — single org with multiple Active branches shows chooser", () => {
    const workspaces = buildAccessibleWorkspaces(
      [{ organizationId: orgA, displayName: "Kizy Store" }],
      new Map([
        [orgA, [branch(mainA, orgA, "Main Branch", true), branch(branchA2, orgA, "Branch 02")]],
      ]),
    );
    expect(resolveWorkspaceRoutingPlan({ organizationCount: 1, workspaces }).outcome).toBe(
      "ShowChooser",
    );
  });

  it("case C — multiple orgs shows chooser", () => {
    const workspaces = buildAccessibleWorkspaces(
      [
        { organizationId: orgA, displayName: "Kizy Store" },
        { organizationId: orgB, displayName: "Kizy Mech" },
      ],
      new Map([
        [orgA, [branch(mainA, orgA, "Main Branch", true)]],
        [orgB, [branch(mainB, orgB, "Main Branch", true)]],
      ]),
    );
    expect(resolveWorkspaceRoutingPlan({ organizationCount: 2, workspaces }).outcome).toBe(
      "ShowChooser",
    );
  });

  it("case D — orgs exist but no Active branches routes to no accessible branch", () => {
    const workspaces = buildAccessibleWorkspaces(
      [{ organizationId: orgA, displayName: "Empty Org" }],
      new Map([[orgA, [{ ...branch(mainA, orgA, "Suspended"), status: "Suspended" }]]]),
    );
    expect(workspaces).toHaveLength(0);
    expect(resolveWorkspaceRoutingPlan({ organizationCount: 1, workspaces }).outcome).toBe(
      "NoAccessibleBranch",
    );
  });

  it("case E — no eligible organizations routes to personal home for Personal class", () => {
    expect(resolveWorkspaceRoutingPlan({ organizationCount: 0, workspaces: [] }).outcome).toBe(
      "PersonalHome",
    );
    expect(
      resolveWorkspaceRoutingPlan({
        organizationCount: 0,
        workspaces: [],
        accountClass: "Personal",
      }).outcome,
    ).toBe("PersonalHome");
  });

  it("case E2 — Organization/Platform with no eligible orgs does not become Personal home", () => {
    expect(
      resolveWorkspaceRoutingPlan({
        organizationCount: 0,
        workspaces: [],
        accountClass: "Organization",
      }).outcome,
    ).toBe("NoAccessibleBranch");
    expect(
      resolveWorkspaceRoutingPlan({
        organizationCount: 0,
        workspaces: [],
        accountClass: "Platform",
      }).outcome,
    ).toBe("NoAccessibleBranch");
  });

  it("treats branch status Active case-insensitively", () => {
    expect(isActiveBranchStatus("active")).toBe(true);
    expect(isActiveBranchStatus("ACTIVE")).toBe(true);
    expect(isActiveBranchStatus("Suspended")).toBe(false);
  });
});

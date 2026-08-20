import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import type { AccountClassName } from "@/session/account-class";
import type {
  AccessibleOrganizationWorkspace,
  AccessibleWorkspaceBranch,
  WorkspaceRoutingPlan,
} from "@/workspace/types";

export function isActiveBranchStatus(status: string): boolean {
  return status.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

export function resolveBranchSecondaryLine(branch: PlatformBranch): string {
  if (branch.city?.trim()) {
    return branch.city.trim();
  }
  if (branch.region?.trim()) {
    return branch.region.trim();
  }
  if (branch.customerOrderingReady) {
    return "Active";
  }
  return "Setup required";
}

export function mapActiveBranches(branches: PlatformBranch[]): AccessibleWorkspaceBranch[] {
  return branches
    .filter((branch) => isActiveBranchStatus(branch.status))
    .sort((left, right) => {
      if (left.isPrimary !== right.isPrimary) {
        return left.isPrimary ? -1 : 1;
      }
      return left.name.localeCompare(right.name, undefined, { sensitivity: "base" });
    })
    .map((branch) => ({
      branchId: branch.id,
      name: branch.name,
      secondaryLine: resolveBranchSecondaryLine(branch),
      isPrimary: branch.isPrimary,
      isActive: true,
    }));
}

export function buildAccessibleWorkspaces(
  organizations: Array<{ organizationId: string; displayName: string }>,
  branchesByOrganizationId: ReadonlyMap<string, PlatformBranch[]>,
): AccessibleOrganizationWorkspace[] {
  const workspaces: AccessibleOrganizationWorkspace[] = [];

  for (const organization of organizations) {
    const branches = mapActiveBranches(
      branchesByOrganizationId.get(organization.organizationId) ?? [],
    );
    if (branches.length === 0) {
      continue;
    }
    workspaces.push({
      organizationId: organization.organizationId,
      displayName: organization.displayName,
      branches,
    });
  }

  return workspaces;
}

export function resolveWorkspaceRoutingPlan(input: {
  organizationCount: number;
  workspaces: AccessibleOrganizationWorkspace[];
  accountClass?: AccountClassName | null;
}): WorkspaceRoutingPlan {
  if (input.workspaces.length === 0) {
    if (input.organizationCount === 0) {
      // Personal home is only for Personal AccountClass. Staff/Platform with no eligible orgs
      // must not be treated as Personal — fail closed to no-location.
      if (input.accountClass === "Organization" || input.accountClass === "Platform") {
        return { outcome: "NoAccessibleBranch" };
      }
      return { outcome: "PersonalHome" };
    }
    return { outcome: "NoAccessibleBranch" };
  }

  if (input.workspaces.length === 1 && input.workspaces[0].branches.length === 1) {
    const only = input.workspaces[0];
    return {
      outcome: "AutoSelect",
      autoOrganizationId: only.organizationId,
      autoBranchId: only.branches[0].branchId,
    };
  }

  return { outcome: "ShowChooser" };
}

export function workspaceRouteForOutcome(outcome: WorkspaceRoutingPlan["outcome"]): string {
  switch (outcome) {
    case "PersonalHome":
      return "/personal";
    case "ShowChooser":
      return "/workspace";
    case "NoAccessibleBranch":
      return "/no-location";
    case "AutoSelect":
      return "/";
    default:
      return "/";
  }
}

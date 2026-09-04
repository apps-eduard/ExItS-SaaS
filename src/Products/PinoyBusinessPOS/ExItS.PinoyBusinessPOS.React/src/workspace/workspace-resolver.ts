import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import { normalizeBranchType } from "@/features/branches/branch-type";
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
      areaId: branch.areaId ?? null,
      areaName: branch.areaName ?? null,
      branchType: normalizeBranchType(branch.branchType),
    }));
}

export function buildAccessibleWorkspaces(
  organizations: Array<{
    organizationId: string;
    displayName: string;
    membershipRole?: string | null;
  }>,
  branchesByOrganizationId: ReadonlyMap<string, PlatformBranch[]>,
  options?: { includeManagementOrgsWithoutBranches?: boolean },
): AccessibleOrganizationWorkspace[] {
  const workspaces: AccessibleOrganizationWorkspace[] = [];
  const includeEmpty = options?.includeManagementOrgsWithoutBranches === true;

  for (const organization of organizations) {
    const branches = mapActiveBranches(
      branchesByOrganizationId.get(organization.organizationId) ?? [],
    );
    const membershipRole = organization.membershipRole ?? null;
    const isManagementMembership =
      membershipRole != null &&
      (membershipRole.localeCompare("OrganizationOwner", undefined, {
        sensitivity: "accent",
      }) === 0 ||
        membershipRole.localeCompare("OrganizationAdministrator", undefined, {
          sensitivity: "accent",
        }) === 0);

    if (branches.length === 0 && !(includeEmpty && isManagementMembership)) {
      continue;
    }
    workspaces.push({
      organizationId: organization.organizationId,
      displayName: organization.displayName,
      membershipRole,
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

  // Do not AutoSelect on one-org/one-branch alone — Owner still has multiple experiences
  // (Manage Business / Operations / Start Selling). Destination smart-routing runs after
  // an authoritative session-grant probe in WorkspaceProvider.
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
    case "AutoDestination":
      return "/";
    default:
      return "/";
  }
}

import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import {
  canUseAdminExperience,
  canUseOperationsExperience,
  canUseSellingExperience,
  type PosSessionGrantFacts,
} from "@/access/pos-capabilities";
import type { AccessibleOrganizationWorkspace } from "@/workspace/types";
import {
  isBranchRequiredExperience,
  workingExperienceRoute,
  type WorkingExperience,
} from "@/workspace/working-experience";

export type WorkspaceDestination = {
  organizationId: string;
  organizationDisplayName: string;
  branchId: string | null;
  branchName: string | null;
  experience: WorkingExperience;
  route: string;
  /** i18n message key for the action label */
  labelKey: "experience.manageBusiness" | "experience.operations" | "experience.startSelling";
};

/**
 * Build capability-gated destinations for one organization after an authoritative session grant.
 * Does not invent Main Branch. Manage Business is organization-level (branchId null).
 */
export function buildOrganizationDestinations(input: {
  workspace: AccessibleOrganizationWorkspace;
  grant: PosSessionGrantFacts | SessionGrantResponse | null | undefined;
}): WorkspaceDestination[] {
  const { workspace, grant } = input;
  const destinations: WorkspaceDestination[] = [];

  if (canUseAdminExperience(grant)) {
    destinations.push({
      organizationId: workspace.organizationId,
      organizationDisplayName: workspace.displayName,
      branchId: null,
      branchName: null,
      experience: "manage_business",
      route: workingExperienceRoute("manage_business"),
      labelKey: "experience.manageBusiness",
    });
  }

  const canOps = canUseOperationsExperience(grant);
  const canSell = canUseSellingExperience(grant);

  for (const branch of workspace.branches) {
    if (canOps) {
      destinations.push({
        organizationId: workspace.organizationId,
        organizationDisplayName: workspace.displayName,
        branchId: branch.branchId,
        branchName: branch.name,
        experience: "operations",
        route: workingExperienceRoute("operations"),
        labelKey: "experience.operations",
      });
    }
    if (canSell) {
      destinations.push({
        organizationId: workspace.organizationId,
        organizationDisplayName: workspace.displayName,
        branchId: branch.branchId,
        branchName: branch.name,
        experience: "start_selling",
        route: workingExperienceRoute("start_selling"),
        labelKey: "experience.startSelling",
      });
    }
  }

  return destinations;
}

export function buildAllDestinations(input: {
  workspaces: AccessibleOrganizationWorkspace[];
  grantByOrganizationId: ReadonlyMap<string, PosSessionGrantFacts | SessionGrantResponse | null>;
}): WorkspaceDestination[] {
  const all: WorkspaceDestination[] = [];
  for (const workspace of input.workspaces) {
    const grant = input.grantByOrganizationId.get(workspace.organizationId);
    all.push(...buildOrganizationDestinations({ workspace, grant }));
  }
  return all;
}

/**
 * Smart routing: exactly one meaningful destination → auto; otherwise chooser.
 * Never auto-selects across multiple organizations.
 */
export function resolveDestinationRouting(input: {
  workspaces: AccessibleOrganizationWorkspace[];
  grantByOrganizationId: ReadonlyMap<string, PosSessionGrantFacts | SessionGrantResponse | null>;
}):
  | { outcome: "AutoDestination"; destination: WorkspaceDestination }
  | { outcome: "ShowChooser" }
  | { outcome: "NoDestination" } {
  if (input.workspaces.length === 0) {
    return { outcome: "NoDestination" };
  }

  if (input.workspaces.length > 1) {
    return { outcome: "ShowChooser" };
  }

  const onlyOrg = input.workspaces[0];
  const destinations = buildOrganizationDestinations({
    workspace: onlyOrg,
    grant: input.grantByOrganizationId.get(onlyOrg.organizationId),
  });

  if (destinations.length === 0) {
    return { outcome: "NoDestination" };
  }

  if (destinations.length === 1) {
    return { outcome: "AutoDestination", destination: destinations[0] };
  }

  return { outcome: "ShowChooser" };
}

export function destinationRequiresBranch(destination: WorkspaceDestination): boolean {
  return isBranchRequiredExperience(destination.experience);
}

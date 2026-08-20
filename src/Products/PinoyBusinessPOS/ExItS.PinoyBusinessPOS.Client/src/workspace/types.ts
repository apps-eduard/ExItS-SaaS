export type WorkspaceRoutingOutcome =
  "PersonalHome" | "AutoSelect" | "ShowChooser" | "NoAccessibleBranch" | "AutoDestination";

export type AccessibleWorkspaceBranch = {
  branchId: string;
  name: string;
  secondaryLine: string;
  isPrimary: boolean;
  isActive: boolean;
};

export type AccessibleOrganizationWorkspace = {
  organizationId: string;
  displayName: string;
  membershipRole?: string | null;
  branches: AccessibleWorkspaceBranch[];
};

export type WorkspaceRoutingPlan = {
  outcome: WorkspaceRoutingOutcome;
  autoOrganizationId?: string;
  autoBranchId?: string;
};

export type BoundWorkspace = {
  organizationId: string;
  organizationDisplayName: string;
  /** Null when bound for organization-level Manage Business only. */
  branchId: string | null;
  branchName: string | null;
  experience: import("@/workspace/working-experience").WorkingExperience;
};

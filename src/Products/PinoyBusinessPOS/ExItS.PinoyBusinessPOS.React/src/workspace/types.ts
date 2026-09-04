export type WorkspaceRoutingOutcome =
  "PersonalHome" | "AutoSelect" | "ShowChooser" | "NoAccessibleBranch" | "AutoDestination";

export type AccessibleWorkspaceBranch = {
  branchId: string;
  name: string;
  secondaryLine: string;
  isPrimary: boolean;
  isActive: boolean;
  /** Grouping area. Areas are never selectable as a workspace — only a branch is. */
  areaId?: string | null;
  areaName?: string | null;
  /** Retail (default) or Warehouse. */
  branchType?: import("@/features/branches/branch-type").OrganizationBranchType;
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
  /** Null when no branch is bound (Manage Business). Defaults to Retail when bound. */
  branchType?: import("@/features/branches/branch-type").OrganizationBranchType | null;
  experience: import("@/workspace/working-experience").WorkingExperience;
};

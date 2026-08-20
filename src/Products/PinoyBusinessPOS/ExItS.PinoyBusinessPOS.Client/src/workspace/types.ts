export type WorkspaceRoutingOutcome =
  "PersonalHome" | "AutoSelect" | "ShowChooser" | "NoAccessibleBranch";

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
  branchId: string;
  branchName: string;
};

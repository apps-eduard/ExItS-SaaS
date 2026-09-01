import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

export type BranchReadinessSectionStatus = "Complete" | "NeedsAttention" | "Optional" | "NotApplicable";
export type BranchReadinessOverallStatus = "NotStarted" | "NeedsAttention" | "Ready";

export type BranchReadinessSection = {
  key: string;
  status: BranchReadinessSectionStatus;
  summary?: string | null;
  count?: number | null;
  managementPath?: string | null;
};

export type BranchReadinessResponse = {
  organizationId: string;
  branchId: string;
  overallStatus: BranchReadinessOverallStatus;
  sections: BranchReadinessSection[];
  setupProgress?: {
    lastVisitedStep?: string | null;
    startedAtUtc?: string | null;
    lastVisitedAtUtc?: string | null;
    completedAtUtc?: string | null;
  } | null;
};

function scope(organizationId: string, branchId: string): PosWorkspaceScope {
  return { organizationId, branchId };
}

export async function getBranchReadiness(
  organizationId: string,
  branchId: string,
): Promise<BranchReadinessResponse> {
  return posRequest<BranchReadinessResponse>({
    method: "GET",
    path: `/api/v1/pos/branches/${branchId}/readiness`,
    workspace: scope(organizationId, branchId),
  });
}

export async function upsertBranchSetupProgress(
  organizationId: string,
  branchId: string,
  body: { lastVisitedStep?: string; markCompleted?: boolean },
): Promise<BranchReadinessResponse["setupProgress"]> {
  return posRequest({
    method: "PUT",
    path: `/api/v1/pos/branches/${branchId}/setup-progress`,
    workspace: scope(organizationId, branchId),
    body,
  });
}

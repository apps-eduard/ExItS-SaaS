import type { AccessibleWorkspaceBranch } from "@/workspace/types";

/**
 * `single` keeps the simple one-branch flow, `flat` keeps the current list, and `grouped`
 * adds area headings. An area is a heading only — the workspace selection stays a branch id.
 */
export type WorkspaceBranchGroupingMode = "single" | "flat" | "grouped";

export type WorkspaceAreaGroup = {
  key: string;
  areaId: string | null;
  areaName: string | null;
  isUnassigned: boolean;
  branches: AccessibleWorkspaceBranch[];
};

const UNASSIGNED_KEY = "unassigned";

export function resolveWorkspaceBranchGroupingMode(
  branches: readonly AccessibleWorkspaceBranch[],
): WorkspaceBranchGroupingMode {
  const hasArea = branches.some((branch) => Boolean(branch.areaId));
  if (hasArea) {
    return "grouped";
  }
  return branches.length <= 1 ? "single" : "flat";
}

/**
 * Groups the branches the actor may already see. Callers must pass an access-filtered list:
 * grouping never widens visibility, and the unassigned group only appears when the actor can
 * reach a branch without an area.
 */
export function groupWorkspaceBranchesByArea(
  branches: readonly AccessibleWorkspaceBranch[],
): WorkspaceAreaGroup[] {
  const groups = new Map<string, WorkspaceAreaGroup>();

  for (const branch of branches) {
    const areaId = branch.areaId ?? null;
    const key = areaId ?? UNASSIGNED_KEY;
    const existing = groups.get(key);
    if (existing) {
      existing.branches.push(branch);
      if (!existing.areaName && branch.areaName) {
        existing.areaName = branch.areaName;
      }
      continue;
    }
    groups.set(key, {
      key,
      areaId,
      areaName: areaId ? (branch.areaName ?? null) : null,
      isUnassigned: areaId === null,
      branches: [branch],
    });
  }

  return [...groups.values()].sort((left, right) => {
    if (left.isUnassigned !== right.isUnassigned) {
      return left.isUnassigned ? 1 : -1;
    }
    return (left.areaName ?? "").localeCompare(right.areaName ?? "", undefined, {
      sensitivity: "base",
    });
  });
}

import type {
  BranchAccessScopeDto,
  MembershipBranchAssignmentDto,
} from "@/api/platform/membership-branch-assignments-client";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import { resolvePlatformBranchId } from "@/api/platform/platform-auth-client";

export type BranchScopeMode = "all" | "specific";

export function isImplicitAllBranchesMembershipRole(role: string): boolean {
  const normalized = role.trim();
  return (
    normalized.localeCompare("OrganizationOwner", undefined, { sensitivity: "accent" }) === 0 ||
    normalized.localeCompare("OrganizationAdministrator", undefined, { sensitivity: "accent" }) === 0
  );
}

export function isActiveBranch(branch: Pick<PlatformBranch, "status">): boolean {
  return branch.status.trim().localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

export function listActiveBranches(branches: PlatformBranch[]): PlatformBranch[] {
  return branches.filter(isActiveBranch);
}

export function resolvePrimaryOrOnlyBranch(activeBranches: PlatformBranch[]): PlatformBranch | null {
  if (activeBranches.length === 0) {
    return null;
  }
  return activeBranches.find((branch) => branch.isPrimary) ?? activeBranches[0] ?? null;
}

export function branchIdsEqual(left: readonly string[], right: readonly string[]): boolean {
  if (left.length !== right.length) {
    return false;
  }
  const sortedLeft = [...left].map((id) => id.trim()).filter(Boolean).sort();
  const sortedRight = [...right].map((id) => id.trim()).filter(Boolean).sort();
  return sortedLeft.every((id, index) => id === sortedRight[index]);
}

export function activeBranchIds(activeBranches: PlatformBranch[]): string[] {
  return activeBranches
    .map((branch) => resolvePlatformBranchId(branch))
    .filter((id): id is string => Boolean(id));
}

export function assignmentBranchIds(assignments: MembershipBranchAssignmentDto[]): string[] {
  return assignments.map((item) => item.branchId.trim()).filter(Boolean);
}

export function scopeToMode(scope: BranchAccessScopeDto): BranchScopeMode {
  return scope === "AllActive" ? "all" : "specific";
}

export function modeToScope(mode: BranchScopeMode): BranchAccessScopeDto {
  return mode === "all" ? "AllActive" : "Explicit";
}

export function formatStaffBranchAccessSummary(input: {
  membershipRole: string;
  scope: BranchAccessScopeDto | null;
  activeBranches: PlatformBranch[];
  assignedIds: readonly string[];
  allActiveLabel: string;
  automaticAllLabel: string;
  unknownLabel: string;
}): string {
  if (isImplicitAllBranchesMembershipRole(input.membershipRole)) {
    return input.automaticAllLabel;
  }

  const active = input.activeBranches;
  const activeIds = activeBranchIds(active);
  if (activeIds.length === 0) {
    return input.unknownLabel;
  }

  if (input.scope === "AllActive") {
    return input.allActiveLabel;
  }

  if (activeIds.length === 1) {
    const only = resolvePrimaryOrOnlyBranch(active);
    return only?.name?.trim() || only?.code?.trim() || input.unknownLabel;
  }

  if (input.assignedIds.length === 0) {
    return input.unknownLabel;
  }

  const names = input.assignedIds
    .map((id) => active.find((branch) => resolvePlatformBranchId(branch) === id))
    .map((branch) => branch?.name?.trim() || branch?.code?.trim() || null)
    .filter((name): name is string => Boolean(name));

  if (names.length === 0) {
    return input.unknownLabel;
  }

  if (names.length === 1) {
    return names[0]!;
  }

  return `${names[0]} + ${names.length - 1}`;
}

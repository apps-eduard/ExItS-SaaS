import type {
  BranchAccessScopeDto,
  MembershipAreaAssignmentDto,
  MembershipBranchAssignmentDto,
} from "@/api/platform/membership-branch-assignments-client";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import { resolvePlatformBranchId } from "@/api/platform/platform-auth-client";
import { isWarehouseBranch } from "@/features/branches/branch-type";

export type BranchScopeMode = "all" | "areas" | "specific";

export type AreaLocationCounts = {
  total: number;
  retail: number;
  warehouse: number;
};

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

export function assignmentAreaIds(assignments: MembershipAreaAssignmentDto[]): string[] {
  return assignments.map((item) => item.areaId.trim()).filter(Boolean);
}

export function scopeToMode(scope: BranchAccessScopeDto): BranchScopeMode {
  if (scope === "AllActive") {
    return "all";
  }
  return scope === "Areas" ? "areas" : "specific";
}

export function modeToScope(mode: BranchScopeMode): BranchAccessScopeDto {
  if (mode === "all") {
    return "AllActive";
  }
  return mode === "areas" ? "Areas" : "Explicit";
}

/**
 * Areas stay hidden until the business actually needs them: a single-branch shop with no
 * areas keeps the simple two-way choice, and no area setup is ever required to run POS.
 */
export function shouldOfferAreaScope(input: {
  activeBranchCount: number;
  activeAreaCount: number;
}): boolean {
  if (input.activeAreaCount <= 0) {
    return false;
  }
  return input.activeBranchCount > 1;
}

export function countActiveLocationsInArea(
  areaId: string,
  branches: readonly PlatformBranch[],
): AreaLocationCounts {
  const target = areaId.trim();
  let retail = 0;
  let warehouse = 0;
  for (const branch of branches) {
    if (!isActiveBranch(branch)) {
      continue;
    }
    if ((branch.areaId ?? "").trim() !== target) {
      continue;
    }
    if (isWarehouseBranch(branch.branchType)) {
      warehouse += 1;
    } else {
      retail += 1;
    }
  }
  return { total: retail + warehouse, retail, warehouse };
}

function joinAreaNames(
  names: readonly string[],
  formatSingleAreaName: (name: string) => string,
): string {
  if (names.length === 0) {
    return "";
  }
  if (names.length === 1) {
    return formatSingleAreaName(names[0]!);
  }
  if (names.length <= 3) {
    return names.join(" + ");
  }
  return `${names[0]} + ${names.length - 1}`;
}

function withLocationCount(label: string, count: number, formatLocationCount: (count: number) => string): string {
  if (count <= 0) {
    return label;
  }
  return `${label} · ${formatLocationCount(count)}`;
}

export function formatStaffBranchAccessSummary(input: {
  membershipRole: string;
  scope: BranchAccessScopeDto | null;
  activeBranches: PlatformBranch[];
  assignedIds: readonly string[];
  allActiveLabel: string;
  automaticAllLabel: string;
  unknownLabel: string;
  areaNames?: readonly string[];
  areasLabel?: string;
  formatLocationCount?: (count: number) => string;
  formatSingleAreaName?: (name: string) => string;
}): string {
  if (isImplicitAllBranchesMembershipRole(input.membershipRole)) {
    return input.automaticAllLabel;
  }

  const active = input.activeBranches;
  const activeIds = activeBranchIds(active);
  if (activeIds.length === 0) {
    return input.unknownLabel;
  }

  const formatCount = input.formatLocationCount ?? ((count: number) => String(count));
  const formatArea =
    input.formatSingleAreaName ?? ((name: string) => name);

  if (input.scope === "AllActive") {
    return input.allActiveLabel;
  }

  if (input.scope === "Areas") {
    const names = (input.areaNames ?? []).map((name) => name.trim()).filter(Boolean);
    if (names.length === 0) {
      return input.areasLabel ?? input.unknownLabel;
    }
    return withLocationCount(
      joinAreaNames(names, formatArea),
      input.assignedIds.length,
      formatCount,
    );
  }

  if (activeIds.length === 1) {
    const only = resolvePrimaryOrOnlyBranch(active);
    const name = only?.name?.trim() || only?.code?.trim() || input.unknownLabel;
    return withLocationCount(name, 1, formatCount);
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
    return withLocationCount(names[0]!, 1, formatCount);
  }

  return withLocationCount(`${names[0]} + ${names.length - 1}`, names.length, formatCount);
}

/**
 * Customer branch visibility — Area checkboxes are bulk selection helpers only.
 * Persistence is always an explicit branch id set (never AreaId alone).
 */

export type CustomerVisibilityMode = "this_branch" | "selected" | "all";

export type AreaCheckboxState = "unchecked" | "checked" | "indeterminate";

export type VisibilityBranch = {
  id: string;
  name: string;
  areaId: string | null;
  areaName: string | null;
  status?: string | null;
};

export type VisibilityAreaGroup = {
  key: string;
  areaId: string | null;
  name: string;
  branches: VisibilityBranch[];
};

export function isActiveVisibilityBranch(branch: VisibilityBranch): boolean {
  const status = (branch.status ?? "Active").trim().toLowerCase();
  return status === "" || status === "active";
}

export function normalizeBranchIdSet(ids: Iterable<string>): Set<string> {
  const next = new Set<string>();
  for (const id of ids) {
    const trimmed = id.trim();
    if (trimmed) {
      next.add(trimmed);
    }
  }
  return next;
}

/** Ensure home remains selected; returns a new set. */
export function withHomeBranchLocked(
  selected: Iterable<string>,
  homeBranchId: string | null | undefined,
): Set<string> {
  const next = normalizeBranchIdSet(selected);
  const home = homeBranchId?.trim();
  if (home) {
    next.add(home);
  }
  return next;
}

export function resolveVisibilityMode(
  selected: ReadonlySet<string>,
  eligibleBranchIds: readonly string[],
  homeBranchId: string | null | undefined,
): CustomerVisibilityMode {
  const eligible = eligibleBranchIds.filter((id) => id.trim());
  const locked = withHomeBranchLocked(selected, homeBranchId);
  if (eligible.length === 0) {
    return "this_branch";
  }
  if (eligible.every((id) => locked.has(id))) {
    return "all";
  }
  const home = homeBranchId?.trim();
  if (home && locked.size === 1 && locked.has(home)) {
    return "this_branch";
  }
  return "selected";
}

export function applyVisibilityMode(
  mode: CustomerVisibilityMode,
  eligibleBranchIds: readonly string[],
  homeBranchId: string | null | undefined,
): Set<string> {
  const home = homeBranchId?.trim() ?? null;
  if (mode === "this_branch") {
    return withHomeBranchLocked([], home);
  }
  if (mode === "all") {
    return withHomeBranchLocked(eligibleBranchIds, home);
  }
  return withHomeBranchLocked(home ? [home] : [], home);
}

export function groupBranchesByArea(
  branches: readonly VisibilityBranch[],
  areas: readonly { id: string; name: string }[],
  unassignedLabel: string,
): VisibilityAreaGroup[] {
  const active = branches.filter(isActiveVisibilityBranch);

  // No configured Areas → flat checklist (ignore branch.areaId for grouping).
  if (areas.length === 0) {
    return [
      {
        key: "__all__",
        areaId: null,
        name: unassignedLabel,
        branches: active
          .slice()
          .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: "base" })),
      },
    ].filter((g) => g.branches.length > 0);
  }

  const areaNameById = new Map(areas.map((a) => [a.id, a.name]));
  const byArea = new Map<string, VisibilityBranch[]>();

  for (const branch of active) {
    const areaId = branch.areaId?.trim() || null;
    const key = areaId && areaNameById.has(areaId) ? areaId : "__unassigned__";
    const list = byArea.get(key) ?? [];
    list.push(branch);
    byArea.set(key, list);
  }

  const orderedAreaIds = areas.map((a) => a.id).filter((id) => byArea.has(id));

  const groups: VisibilityAreaGroup[] = orderedAreaIds.map((areaId) => {
    const members = (byArea.get(areaId) ?? []).slice().sort((a, b) =>
      a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
    );
    return {
      key: areaId,
      areaId,
      name: areaNameById.get(areaId) ?? members[0]?.areaName ?? areaId,
      branches: members,
    };
  });

  const unassigned = byArea.get("__unassigned__");
  if (unassigned && unassigned.length > 0) {
    groups.push({
      key: "__unassigned__",
      areaId: null,
      name: unassignedLabel,
      branches: unassigned
        .slice()
        .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: "base" })),
    });
  }

  return groups;
}

export function areaCheckboxState(
  group: VisibilityAreaGroup,
  selected: ReadonlySet<string>,
): AreaCheckboxState {
  const ids = group.branches.map((b) => b.id);
  if (ids.length === 0) {
    return "unchecked";
  }
  const selectedCount = ids.filter((id) => selected.has(id)).length;
  if (selectedCount === 0) {
    return "unchecked";
  }
  if (selectedCount === ids.length) {
    return "checked";
  }
  return "indeterminate";
}

/**
 * Toggle an Area helper checkbox:
 * - unchecked / indeterminate → select all branches in the area
 * - checked → clear all in the area except locked home
 */
export function toggleAreaSelection(args: {
  group: VisibilityAreaGroup;
  selected: ReadonlySet<string>;
  homeBranchId: string | null | undefined;
}): Set<string> {
  const state = areaCheckboxState(args.group, args.selected);
  const home = args.homeBranchId?.trim() ?? null;
  const next = new Set(args.selected);
  if (state === "checked") {
    for (const branch of args.group.branches) {
      if (home && branch.id === home) {
        continue;
      }
      next.delete(branch.id);
    }
  } else {
    for (const branch of args.group.branches) {
      next.add(branch.id);
    }
  }
  return withHomeBranchLocked(next, home);
}

export function toggleBranchSelection(args: {
  branchId: string;
  selected: ReadonlySet<string>;
  homeBranchId: string | null | undefined;
}): Set<string> {
  const home = args.homeBranchId?.trim() ?? null;
  if (home && args.branchId === home) {
    return withHomeBranchLocked(args.selected, home);
  }
  const next = new Set(args.selected);
  if (next.has(args.branchId)) {
    next.delete(args.branchId);
  } else {
    next.add(args.branchId);
  }
  return withHomeBranchLocked(next, home);
}

/** Diff current access vs desired explicit selection for grant/revoke calls. */
export function diffBranchAccess(args: {
  currentBranchIds: readonly string[];
  desiredBranchIds: ReadonlySet<string>;
  homeBranchId: string | null | undefined;
}): { toGrant: string[]; toRevoke: string[] } {
  const desired = withHomeBranchLocked(args.desiredBranchIds, args.homeBranchId);
  const current = normalizeBranchIdSet(args.currentBranchIds);
  const toGrant = [...desired].filter((id) => !current.has(id)).sort();
  const home = args.homeBranchId?.trim() ?? null;
  const toRevoke = [...current]
    .filter((id) => !desired.has(id) && id !== home)
    .sort();
  return { toGrant, toRevoke };
}

export function areaSelectedCount(
  group: VisibilityAreaGroup,
  selected: ReadonlySet<string>,
): { selected: number; total: number } {
  const total = group.branches.length;
  const count = group.branches.filter((b) => selected.has(b.id)).length;
  return { selected: count, total };
}

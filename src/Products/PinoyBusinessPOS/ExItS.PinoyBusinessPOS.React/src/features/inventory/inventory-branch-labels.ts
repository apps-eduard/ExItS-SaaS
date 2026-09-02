/**
 * Resolve a human-readable branch name for inventory org breakdown rows.
 * Prefer workspace branch directory; never present a raw GUID as the primary label.
 */
export function buildBranchNameById(
  branches: ReadonlyArray<{ branchId?: string; id?: string; name: string }>,
): Map<string, string> {
  const map = new Map<string, string>();
  for (const branch of branches) {
    const id = (branch.branchId ?? branch.id ?? "").trim();
    const name = branch.name.trim();
    if (!id || !name) {
      continue;
    }
    map.set(id.toLowerCase(), name);
  }
  return map;
}

export function resolveInventoryBranchDisplayName(options: {
  branchId: string;
  branchNameById: ReadonlyMap<string, string>;
  currentBranchId?: string | null;
  currentBranchName?: string | null;
  unknownLabel?: string;
}): string {
  const id = options.branchId.trim();
  const currentId = options.currentBranchId?.trim() ?? "";
  if (
    currentId &&
    id.toLowerCase() === currentId.toLowerCase() &&
    options.currentBranchName?.trim()
  ) {
    return options.currentBranchName.trim();
  }
  const named = options.branchNameById.get(id.toLowerCase());
  if (named) {
    return named;
  }
  return options.unknownLabel?.trim() || "Branch";
}

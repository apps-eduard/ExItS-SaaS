import { useMemo } from "react";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import type { BoundWorkspace } from "@/workspace/types";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** POS API scope from bound workspace — branch optional (Manage Business is org-only). */
export function posWorkspaceScopeFromBound(
  bound: BoundWorkspace | null | undefined,
): PosWorkspaceScope | null {
  if (!bound?.organizationId) {
    return null;
  }
  return {
    organizationId: bound.organizationId,
    branchId: bound.branchId ?? null,
  };
}

export function usePosWorkspaceScope(): PosWorkspaceScope | null {
  const { boundWorkspace } = useWorkspace();
  return useMemo(() => posWorkspaceScopeFromBound(boundWorkspace), [boundWorkspace]);
}

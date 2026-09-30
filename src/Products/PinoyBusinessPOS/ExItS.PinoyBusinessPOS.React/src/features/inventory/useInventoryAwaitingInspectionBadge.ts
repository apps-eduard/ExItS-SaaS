import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { canViewInventory } from "@/access/pos-capabilities";
import { listInventoryTransfersAwaitingInspection } from "@/api/pos/pos-inventory-transfer-client";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatInspectionNavBadgeCount,
  inventoryAwaitingInspectionQueryKey,
} from "@/features/inventory/inventory-awaiting-inspection-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export type InventoryAwaitingInspectionBadge = {
  /** Positive count only — null means hide badge (loading / error / zero / no access). */
  count: number | null;
  /** Formatted for CountBadge (e.g. "2", "99+"). */
  display: string | null;
};

/**
 * Shared awaiting-inspection activity for sidenav / More hub.
 * Reuses the same React Query key as the Inspection queue page.
 */
export function useInventoryAwaitingInspectionBadge(): InventoryAwaitingInspectionBadge {
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowView = canViewInventory(sessionGrant);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const enabled = Boolean(workspace) && online && allowView;

  const query = useQuery({
    queryKey: inventoryAwaitingInspectionQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled,
    staleTime: 30_000,
    queryFn: ({ signal }) => listInventoryTransfersAwaitingInspection(workspace!, signal),
  });

  if (!enabled) {
    return { count: null, display: null };
  }

  if (query.isError || !query.isSuccess) {
    return { count: null, display: null };
  }

  const total = Math.max(0, query.data.totalCount);
  if (total <= 0) {
    return { count: null, display: null };
  }

  return { count: total, display: formatInspectionNavBadgeCount(total) };
}

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { canViewPurchasing } from "@/access/pos-capabilities";
import { listIncomingOrders } from "@/api/pos/pos-connected-suppliers-client";
import { listPurchaseOrders } from "@/api/pos/pos-purchase-orders-client";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  countReceivablePurchaseOrders,
  derivePurchasingNavigationCount,
  formatPurchasingNavBadgeCount,
  purchasingHubIncomingPendingQueryKey,
  purchasingHubPurchaseOrdersQueryKey,
} from "@/features/purchasing/purchasing-nav-activity";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export type PurchasingNavigationBadge = {
  /** Positive count only — null means hide badge (loading / error / zero / no access). */
  count: number | null;
  /** Formatted for CountBadge (e.g. "2", "99+"). */
  display: string | null;
};

/**
 * Shared Purchasing module activity for sidenav / bottom nav.
 * Reuses the same React Query keys as Purchasing hub tab counts.
 */
export function usePurchasingNavigationBadge(): PurchasingNavigationBadge {
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowView = canViewPurchasing(sessionGrant);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const enabled = Boolean(workspace) && online && allowView;

  const purchaseOrdersQuery = useQuery({
    queryKey: purchasingHubPurchaseOrdersQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled,
    staleTime: 30_000,
    queryFn: ({ signal }) => listPurchaseOrders(workspace!, { page: 1, pageSize: 40 }, signal),
  });

  const incomingPendingQuery = useQuery({
    queryKey: purchasingHubIncomingPendingQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled,
    staleTime: 30_000,
    queryFn: ({ signal }) => listIncomingOrders(workspace!, { status: "New" }, signal),
  });

  if (!enabled) {
    return { count: null, display: null };
  }

  if (purchaseOrdersQuery.isError || incomingPendingQuery.isError) {
    return { count: null, display: null };
  }

  if (!purchaseOrdersQuery.isSuccess || !incomingPendingQuery.isSuccess) {
    return { count: null, display: null };
  }

  const total = derivePurchasingNavigationCount({
    incomingPendingCount: incomingPendingQuery.data.length,
    receivableCount: countReceivablePurchaseOrders(purchaseOrdersQuery.data.items),
  });

  if (total <= 0) {
    return { count: null, display: null };
  }

  return { count: total, display: formatPurchasingNavBadgeCount(total) };
}

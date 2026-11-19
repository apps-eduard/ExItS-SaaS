import { useEffect, useMemo, useState } from "react";
import { useQueries, useQuery } from "@tanstack/react-query";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listPersonalUtangHistory } from "@/api/platform/personal-utang-client";
import {
  listMyCustomerOrders,
  sellerWorkspace,
  type CustomerOrderListItemDto,
} from "@/api/pos/pos-customer-orders-client";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  HOME_RECENT_ACTIVITY_HISTORY_RELATIONSHIP_LIMIT,
  HOME_RECENT_ACTIVITY_ORDERS_PAGE_SIZE,
  selectRecentHistoryRelationships,
  type HomeRecentActivityHistoryBundle,
} from "@/features/personal/home-recent-activity";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";

const BUYER_SCOPE_ORG = "00000000-0000-4000-8000-000000000001";

/**
 * Bounded Home activity sources: recent relationship histories (shared React Query
 * cache with KPI sparklines) + a small personal orders page. Failures are soft —
 * callers must keep other available sources.
 */
export function useHomeRecentActivitySources(accounts: ReadonlyArray<UtangAccountRow>): {
  history: HomeRecentActivityHistoryBundle[];
  orders: CustomerOrderListItemDto[];
  historyLoading: boolean;
  ordersLoading: boolean;
  hasHistoryData: boolean;
  hasOrdersData: boolean;
} {
  const online = useBrowserOnline();
  const [buyerTokenReady, setBuyerTokenReady] = useState(false);
  const [buyerTokenResolved, setBuyerTokenResolved] = useState(!online);

  useEffect(() => {
    if (!online) {
      setBuyerTokenReady(false);
      setBuyerTokenResolved(true);
      return;
    }
    setBuyerTokenResolved(false);
    let cancelled = false;
    void ensurePersonalBuyerPosToken().then((result) => {
      if (!cancelled) {
        setBuyerTokenReady(result.ok);
        setBuyerTokenResolved(true);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [online]);

  const recentRelationships = useMemo(
    () => selectRecentHistoryRelationships(accounts, HOME_RECENT_ACTIVITY_HISTORY_RELATIONSHIP_LIMIT),
    [accounts],
  );

  const historyQueries = useQueries({
    queries: recentRelationships.map((row) => ({
      queryKey: ["personal", "utang", "history", row.relationshipId],
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        listPersonalUtangHistory(row.relationshipId, signal),
      enabled: online && recentRelationships.length > 0,
      staleTime: 5 * 60 * 1000,
      meta: { suppressGlobalError: true, operation: "list personal utang history" },
    })),
  });

  const history = useMemo((): HomeRecentActivityHistoryBundle[] => {
    const bundles: HomeRecentActivityHistoryBundle[] = [];
    recentRelationships.forEach((row, index) => {
      const query = historyQueries[index];
      if (!query?.isSuccess || !query.data) {
        return;
      }
      bundles.push({
        relationshipId: row.relationshipId,
        displayName: row.displayName,
        perspective: row.perspective,
        isLedgerOwner: row.isLedgerOwner,
        entries: query.data,
      });
    });
    return bundles;
  }, [historyQueries, recentRelationships]);

  const ordersQuery = useQuery({
    queryKey: ["personal", "my-orders", "home-recent", HOME_RECENT_ACTIVITY_ORDERS_PAGE_SIZE],
    enabled: online && buyerTokenReady,
    queryFn: ({ signal }) =>
      listMyCustomerOrders(
        sellerWorkspace(BUYER_SCOPE_ORG),
        { partyType: "Personal", page: 1, pageSize: HOME_RECENT_ACTIVITY_ORDERS_PAGE_SIZE },
        signal,
      ),
    staleTime: 60 * 1000,
    meta: { suppressGlobalError: true, operation: "list personal orders for home activity" },
  });

  return {
    history,
    orders: ordersQuery.data?.items ?? [],
    historyLoading: historyQueries.some((query) => query.isPending),
    ordersLoading:
      !buyerTokenResolved || (buyerTokenReady && ordersQuery.isPending),
    hasHistoryData: history.length > 0,
    hasOrdersData: (ordersQuery.data?.items.length ?? 0) > 0,
  };
}

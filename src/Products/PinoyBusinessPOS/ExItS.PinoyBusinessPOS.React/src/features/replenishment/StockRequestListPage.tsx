import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listIncomingStockRequests,
  listOutgoingStockRequests,
} from "@/api/pos/pos-stock-requests-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { formatTransferTimestamp } from "@/features/inventory/inventory-transfer-labels";
import {
  filterStockRequestsByTab,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
  type RetailStockRequestTab,
  type WarehouseStockRequestTab,
} from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function StockRequestListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const isWarehouse = isWarehouseBranch(boundWorkspace?.branchType);
  const [retailTab, setRetailTab] = useState<RetailStockRequestTab>("submitted");
  const [warehouseTab, setWarehouseTab] = useState<WarehouseStockRequestTab>("incoming");

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: [
      "stock-requests",
      isWarehouse ? "incoming" : "outgoing",
      workspace?.organizationId,
      workspace?.branchId,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      isWarehouse
        ? listIncomingStockRequests(workspace!, 1, 50, signal)
        : listOutgoingStockRequests(workspace!, 1, 50, signal),
  });

  const items = useMemo(() => {
    const all = query.data?.items ?? [];
    if (isWarehouse) {
      return filterStockRequestsByTab(all, warehouseTab, "warehouse");
    }
    return filterStockRequestsByTab(all, retailTab, "retail");
  }, [query.data?.items, isWarehouse, warehouseTab, retailTab]);

  if (!workspace) {
    return <EmptyState title={t("stockRequest.listTitle")} detail={t("stockRequest.needBranch")} />;
  }

  const tabItems = isWarehouse
    ? (
        [
          ["incoming", "stockRequest.tab.incoming"],
          ["preparing", "stockRequest.tab.preparing"],
          ["dispatched", "stockRequest.tab.dispatched"],
          ["history", "stockRequest.tab.history"],
          ["all", "stockRequest.tab.all"],
        ] as const
      ).map(([key, labelKey]) => ({
        key,
        label: t(labelKey),
        state: warehouseTab === key ? ("active" as const) : ("idle" as const),
        onSelect: () => setWarehouseTab(key),
      }))
    : (
        [
          ["submitted", "stockRequest.tab.submitted"],
          ["inProgress", "stockRequest.tab.inProgress"],
          ["inTransit", "stockRequest.tab.inTransit"],
          ["completed", "stockRequest.tab.completed"],
          ["all", "stockRequest.tab.all"],
        ] as const
      ).map(([key, labelKey]) => ({
        key,
        label: t(labelKey),
        state: retailTab === key ? ("active" as const) : ("idle" as const),
        onSelect: () => setRetailTab(key),
      }));

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-list">
      <PageHeader
        title={t("stockRequest.listTitle")}
        description={
          isWarehouse ? t("stockRequest.listLedeWarehouse") : t("stockRequest.listLedeRetail")
        }
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        trailing={
          !isWarehouse && allowManage ? (
            <Button asChild data-testid="stock-request-create-cta">
              <Link to="/inventory/stock-requests/new">
                <Plus className="size-4" aria-hidden />
                {t("stockRequest.requestStock")}
              </Link>
            </Button>
          ) : undefined
        }
      />

      <ExitsChipBar
        ariaLabel={t("stockRequest.tabs")}
        variant="filter"
        items={tabItems}
        testId="stock-request-tabs"
      />

      {query.isLoading ? <LoadingState label={t("stockRequest.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("stockRequest.loadError")} detail={t("stockRequest.loadError")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <EmptyState title={t("stockRequest.empty")} detail={t("stockRequest.emptyDetail")} />
      ) : null}

      {!query.isLoading && items.length > 0 ? (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {items.map((item) => {
            const otherLocation = isWarehouse
              ? (item.destinationLocationName ?? item.destinationLocationId)
              : (item.requestedSourceLocationName ?? item.requestedSourceLocationId);
            return (
              <li key={item.stockRequestId}>
                <Link
                  to={`/inventory/stock-requests/${item.stockRequestId}`}
                  className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border p-3 no-underline"
                  data-testid={`stock-request-row-${item.stockRequestId}`}
                  data-status={item.status}
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium text-foreground">
                        {item.requestNumber ?? item.stockRequestId.slice(0, 8)}
                      </span>
                      <StatusChip tone={stockRequestStatusTone(item.status)}>
                        {t(stockRequestStatusLabelKey(item.status) as MessageKey)}
                      </StatusChip>
                    </div>
                    <div className="mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                      {otherLocation}
                      {" · "}
                      {item.lineCount} {t("stockRequest.items")}
                      {" · "}
                      {formatTransferTimestamp(item.updatedAtUtc)}
                    </div>
                  </div>
                  <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden />
                </Link>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}

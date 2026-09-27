import { useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
import { navigateWithReturn } from "@/navigation/smart-back";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeftRight, ChevronRight, Plus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listIncomingStockRequests,
  listOutgoingStockRequests,
  type StockRequestListItemDto,
} from "@/api/pos/pos-stock-requests-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { RESPONSIVE_DATA_TABLE_MIN_MD } from "@/components/exits/responsive-data-view";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
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

function stockRequestOtherLocation(
  item: StockRequestListItemDto,
  isWarehouse: boolean,
): string {
  return isWarehouse
    ? (item.destinationLocationName ?? item.destinationLocationId)
    : (item.requestedSourceLocationName ?? item.requestedSourceLocationId);
}

export function StockRequestListPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { layout } = useResponsiveDataLayout({ tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_MD });
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
    return (
      <EmptyState
        align="center"
        icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />}
        title={t("stockRequest.listTitle")}
        detail={t("stockRequest.needBranch")}
      />
    );
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

  const locationColumnLabel = isWarehouse
    ? t("purchasing.branch")
    : t("stockRequest.supplyFrom");

  const openRequest = (stockRequestId: string) => {
    navigateWithReturn(navigate, `/inventory/stock-requests/${stockRequestId}`, {
      pathname: location.pathname,
      search: location.search,
      hash: location.hash,
    });
  };

  const tableBody = (
    <ExitsTableContainer data-testid="stock-request-list-desktop">
      <ExitsTable>
        <ExitsTableHeader>
          <ExitsTableRow>
            <ExitsTableHead cellAlign="text">{t("stockRequest.colNumber")}</ExitsTableHead>
            <ExitsTableHead cellAlign="text" colSize="flex">
              {locationColumnLabel}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="whitespace-nowrap" colWidth="3.5rem">
              {t("transfer.colLines")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="whitespace-nowrap" colWidth="7.5rem">
              {t("purchasing.fieldStatus")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="text">{t("transfer.colUpdated")}</ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {items.map((item) => {
            const otherLocation = stockRequestOtherLocation(item, isWarehouse);
            const requestNumber = item.requestNumber ?? item.stockRequestId.slice(0, 8);
            return (
              <ExitsTableRow
                key={item.stockRequestId}
                interactive
                data-testid={`stock-request-row-${item.stockRequestId}`}
                data-status={item.status}
                onClick={() => openRequest(item.stockRequestId)}
              >
                <ExitsTableCell cellAlign="text" className="font-medium tabular-nums">
                  {requestNumber}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                  {otherLocation}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="whitespace-nowrap tabular-nums">
                  {item.lineCount}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="whitespace-nowrap">
                  <StatusChip tone={stockRequestStatusTone(item.status)}>
                    {t(stockRequestStatusLabelKey(item.status) as MessageKey)}
                  </StatusChip>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="text" className="text-muted">
                  {formatTransferTimestamp(item.updatedAtUtc)}
                </ExitsTableCell>
              </ExitsTableRow>
            );
          })}
        </ExitsTableBody>
      </ExitsTable>
    </ExitsTableContainer>
  );

  const listBody = (
    <ul
      className="m-0 flex list-none flex-col gap-2 p-0"
      data-testid="stock-request-list-mobile"
    >
      {items.map((item) => {
        const otherLocation = stockRequestOtherLocation(item, isWarehouse);
        return (
          <li key={item.stockRequestId}>
            <AppLinkWithReturn
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
            </AppLinkWithReturn>
          </li>
        );
      })}
    </ul>
  );

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
              <Link to="/warehouse/request-stock">
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
        <EmptyState
          align="center"
          icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />}
          title={t("stockRequest.empty")}
          detail={t("stockRequest.emptyDetail")}
        />
      ) : null}

      {!query.isLoading && items.length > 0 ? (
        <ExitsResponsiveDataView
          layout={layout}
          testId="stock-request-responsive"
          table={layout === "table" ? tableBody : null}
          list={layout === "list" ? listBody : null}
        />
      ) : null}
    </div>
  );
}

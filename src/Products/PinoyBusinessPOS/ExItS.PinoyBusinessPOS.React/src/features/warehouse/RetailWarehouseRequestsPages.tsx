import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Package } from "lucide-react";
import { listOutgoingStockRequests } from "@/api/pos/pos-stock-requests-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatTransferTimestamp } from "@/features/inventory/inventory-transfer-labels";
import {
  retailTabStatuses,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
  type RetailStockRequestTab,
} from "@/features/replenishment/stock-request-helpers";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function parseTab(raw: string | null): RetailStockRequestTab {
  if (
    raw === "submitted" ||
    raw === "inProgress" ||
    raw === "inTransit" ||
    raw === "completed" ||
    raw === "all"
  ) {
    return raw;
  }
  return "submitted";
}

function statusesForApi(tab: RetailStockRequestTab): string[] | undefined {
  const set = retailTabStatuses(tab);
  if (!set) return undefined;
  return Array.from(set);
}

type ListMode = "my-requests" | "incoming" | "history";

const MODE_CONFIG: Record<
  ListMode,
  {
    testId: string;
    fixedStatuses?: string[];
    defaultTab?: RetailStockRequestTab;
    showTabs: boolean;
    allowSearch: boolean;
  }
> = {
  "my-requests": {
    testId: "retail-warehouse-my-requests",
    defaultTab: "submitted",
    showTabs: true,
    allowSearch: false,
  },
  incoming: {
    testId: "retail-warehouse-incoming",
    fixedStatuses: ["InTransit"],
    showTabs: false,
    allowSearch: false,
  },
  history: {
    testId: "retail-warehouse-history",
    fixedStatuses: ["Fulfilled", "PartiallyFulfilled", "Rejected", "Cancelled"],
    showTabs: false,
    allowSearch: true,
  },
};

export function RetailWarehouseRequestsListPage({ mode }: { mode: ListMode }) {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const { workspace } = useRetailWarehouseResolve();
  const config = MODE_CONFIG[mode];
  const [tab, setTab] = useState<RetailStockRequestTab>(
    config.defaultTab ?? parseTab(searchParams.get("tab")),
  );
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");

  useEffect(() => {
    if (mode !== "my-requests") return;
    const fromQuery = parseTab(searchParams.get("tab"));
    setTab(fromQuery);
  }, [mode, searchParams]);

  useEffect(() => {
    setPage(1);
  }, [tab, mode]);

  const statuses = config.fixedStatuses ?? statusesForApi(tab);

  const query = useQuery({
    queryKey: [
      "retail-warehouse-requests",
      mode,
      workspace?.organizationId,
      workspace?.branchId,
      statuses?.join(",") ?? "all",
      page,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listOutgoingStockRequests(workspace!, {
        page,
        pageSize: 40,
        statuses,
        signal,
      }),
  });

  const items = useMemo(() => {
    const all = query.data?.items ?? [];
    if (!config.allowSearch || !search.trim()) return all;
    const q = search.trim().toLowerCase();
    return all.filter((item) =>
      (item.requestNumber ?? item.stockRequestId).toLowerCase().includes(q),
    );
  }, [query.data?.items, config.allowSearch, search]);

  function selectTab(next: RetailStockRequestTab) {
    setTab(next);
    setSearchParams(next === "submitted" ? {} : { tab: next });
  }

  if (!workspace) {
    return <LoadingState label={t("retailWarehouse.loading")} />;
  }

  const tabItems = (
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
    state: tab === key ? ("active" as const) : ("idle" as const),
    onSelect: () => selectTab(key),
  }));

  const totalPages = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / 40));

  return (
    <div className="flex flex-col gap-3" data-testid={config.testId}>
      {config.showTabs ? (
        <ExitsChipBar
          ariaLabel={t("stockRequest.tabs")}
          variant="filter"
          items={tabItems}
          testId="retail-warehouse-request-tabs"
        />
      ) : null}

      {config.allowSearch ? (
        <SearchField
          label={t("retailWarehouse.history.search")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t("retailWarehouse.history.search")}
          data-testid="retail-warehouse-history-search"
          onClear={() => setSearch("")}
        />
      ) : null}

      {query.isLoading ? <LoadingState label={t("stockRequest.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("stockRequest.loadError")} detail={t("stockRequest.loadError")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <EmptyState
              align="center"
              icon={<Package className="size-5" strokeWidth={1.75} />} title={t("stockRequest.empty")} detail={t("stockRequest.emptyDetail")} />
      ) : null}

      {!query.isLoading && items.length > 0 ? (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {items.map((item) => (
            <li key={item.stockRequestId}>
              <Link
                to={`/warehouse/requests/${item.stockRequestId}`}
                className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border p-3 no-underline"
                data-testid={`retail-warehouse-request-row-${item.stockRequestId}`}
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
                    {item.requestedSourceLocationName ?? item.requestedSourceLocationId}
                    {" · "}
                    {item.lineCount} {t("stockRequest.items")}
                    {" · "}
                    {formatTransferTimestamp(item.updatedAtUtc)}
                  </div>
                  {mode === "incoming" ? (
                    <div className="mt-1 text-[length:var(--exits-text-sm)] font-medium text-[var(--exits-primary)]">
                      {t("retailWarehouse.incoming.receiveHint")}
                    </div>
                  ) : null}
                </div>
                <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden />
              </Link>
            </li>
          ))}
        </ul>
      ) : null}

      {(query.data?.totalCount ?? 0) > 40 ? (
        <div className="flex items-center justify-between gap-2">
          <button
            type="button"
            className="exits-button-outline"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("retailWarehouse.request.prev")}
          </button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {page} / {totalPages}
          </span>
          <button
            type="button"
            className="exits-button-outline"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("retailWarehouse.request.next")}
          </button>
        </div>
      ) : null}
    </div>
  );
}

export function RetailWarehouseRequestsPage() {
  return <RetailWarehouseRequestsListPage mode="my-requests" />;
}

export function RetailWarehouseIncomingPage() {
  return <RetailWarehouseRequestsListPage mode="incoming" />;
}

export function RetailWarehouseHistoryPage() {
  return <RetailWarehouseRequestsListPage mode="history" />;
}

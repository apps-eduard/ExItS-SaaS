import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listStockCounts } from "@/api/pos/pos-stock-count-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  differenceProductCount,
  formatStockCountDate,
  formatStockCountTimestamp,
  stockCountStatusLabelKey,
  stockCountStatusTone,
} from "@/features/inventory/stock-count-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

const STATUS_FILTERS = [
  { value: "", labelKey: "stockCount.filter.all" as const },
  { value: "Draft", labelKey: "stockCount.status.draft" as const },
  { value: "InProgress", labelKey: "stockCount.status.inProgress" as const },
  { value: "Completed", labelKey: "stockCount.status.completed" as const },
  { value: "Cancelled", labelKey: "stockCount.status.cancelled" as const },
];

export function StockCountListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [countNumber, setCountNumber] = useState("");
  const [debouncedNumber, setDebouncedNumber] = useState("");
  const allowManage = canManageInventory(sessionGrant);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedNumber(countNumber.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [countNumber]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: [
      "stock-counts",
      workspace?.organizationId,
      workspace?.branchId,
      page,
      status,
      debouncedNumber,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listStockCounts(
        workspace!,
        {
          page,
          pageSize: PAGE_SIZE,
          status: status || undefined,
          countNumber: debouncedNumber || undefined,
        },
        signal,
      ),
  });

  useEffect(() => {
    setPage(1);
  }, [workspace?.organizationId, workspace?.branchId, status, debouncedNumber]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = query.data?.items ?? [];
  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;

  return (
    <div
      className="stock-count-list-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="stock-count-list-page"
    >
      <PageHeader
        title={t("stockCount.title")}
        description={t("stockCount.lede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="stock-count-scope-note">
        {t("stockCount.orgScopeNote")}
      </p>

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockCount.offline")}</p>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("stockCount.title")}
          testId="stock-count-toolbar"
          items={[
            {
              key: "new",
              label: t("stockCount.new"),
              icon: <ClipboardList />,
              href: online ? "/inventory/stock-counts/new" : undefined,
              disabled: !online,
              testId: "stock-count-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <SearchField
        label={t("stockCount.searchCountNumber")}
        value={countNumber}
        onChange={(e) => setCountNumber(e.target.value)}
        onClear={() => setCountNumber("")}
        placeholder={t("stockCount.searchCountNumber")}
        data-testid="stock-count-search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("stockCount.filter.status")}
        testId="stock-count-status-filters"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.value || "all",
          label: t(filter.labelKey),
          state: status === filter.value ? "active" : "idle",
          onSelect: () => setStatus(filter.value),
          testId: `stock-count-filter-${filter.value || "all"}`,
        }))}
      />

      {query.isLoading ? <LoadingState label={t("stockCount.loading")} /> : null}

      {query.isError ? (
        <ErrorState title={t("stockCount.errorTitle")} detail={t("stockCount.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <>
          <EmptyState title={t("stockCount.empty")} detail={t("stockCount.emptyDetail")} />
          {allowManage && online ? (
            <Button asChild>
              <Link to="/inventory/stock-counts/new" data-testid="stock-count-empty-cta">
                {t("stockCount.createFirst")}
              </Link>
            </Button>
          ) : null}
        </>
      ) : null}

      {items.length > 0 ? (
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="stock-count-list">
          {items.map((item) => {
            const diffs = differenceProductCount(item.lines);
            return (
              <li key={item.stockCountId}>
                <Link
                  to={`/inventory/stock-counts/${item.stockCountId}`}
                  className="exits-list__card stock-count-row block min-w-0 text-foreground no-underline"
                  data-testid={`stock-count-row-${item.stockCountId}`}
                >
                  <span className="stock-count-row__main min-w-0">
                    <span className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold">
                        {item.countNumber?.trim() || t("stockCount.draftNumber")}
                      </span>
                      <StatusChip tone={stockCountStatusTone(item.status)}>
                        {t(stockCountStatusLabelKey(item.status))}
                      </StatusChip>
                    </span>
                    <span className="block text-[length:var(--exits-text-sm)]">{item.title}</span>
                    <span className="block text-[length:var(--exits-text-sm)] text-muted">
                      {formatStockCountDate(item.countDate)} ·{" "}
                      {t("stockCount.linesCount").replace("{count}", String(item.lines.length))}
                      {diffs > 0
                        ? ` · ${t("stockCount.productsWithDifferences").replace("{count}", String(diffs))}`
                        : ""}
                    </span>
                    {item.startedAtUtc ? (
                      <span className="block text-[length:var(--exits-text-xs)] text-muted">
                        {t("stockCount.started")}: {formatStockCountTimestamp(item.startedAtUtc)}
                      </span>
                    ) : null}
                    {item.completedAtUtc ? (
                      <span className="block text-[length:var(--exits-text-xs)] text-muted">
                        {t("stockCount.completed")}: {formatStockCountTimestamp(item.completedAtUtc)}
                      </span>
                    ) : null}
                  </span>
                  <span className="stock-count-row__aside flex shrink-0 items-center gap-2">
                    <ChevronRight className="size-5 text-muted" aria-hidden />
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
      ) : null}

      {totalCount > PAGE_SIZE ? (
        <div className="flex flex-wrap items-center justify-between gap-2" data-testid="stock-count-pagination">
          <Button
            type="button"
            variant="outline"
            disabled={!canPrev}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            data-testid="stock-count-prev"
          >
            {t("stockCount.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("stockCount.pageOf")
              .replace("{page}", String(page))
              .replace("{pages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={!canNext}
            onClick={() => setPage((p) => p + 1)}
            data-testid="stock-count-next"
          >
            {t("stockCount.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}

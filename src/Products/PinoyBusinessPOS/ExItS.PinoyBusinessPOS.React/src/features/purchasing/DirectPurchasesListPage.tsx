import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList, PackagePlus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listDirectPurchases,
  type DirectPurchaseHistoryItem,
  type DirectPurchaseHistorySourceType,
} from "@/api/pos/pos-direct-purchases-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

type DateFilter = "all" | "7" | "30" | "90";
type SourceFilter = "All" | DirectPurchaseHistorySourceType;
type StatusFilter = "All" | "Completed" | "Voided";

const DATE_FILTERS: Array<{
  value: DateFilter;
  key: string;
  labelKey:
    | "purchasing.directDateAll"
    | "purchasing.directDate7"
    | "purchasing.directDate30"
    | "purchasing.directDate90";
}> = [
  { value: "all", key: "all", labelKey: "purchasing.directDateAll" },
  { value: "7", key: "7", labelKey: "purchasing.directDate7" },
  { value: "30", key: "30", labelKey: "purchasing.directDate30" },
  { value: "90", key: "90", labelKey: "purchasing.directDate90" },
];

function dateRangeForFilter(filter: DateFilter): { fromDate?: string; toDate?: string } {
  if (filter === "all") return {};

  const today = new Date();
  const toDate = today.toISOString().slice(0, 10);
  const from = new Date(today);
  const days = filter === "7" ? 7 : filter === "30" ? 30 : 90;
  from.setDate(from.getDate() - days);

  return {
    fromDate: from.toISOString().slice(0, 10),
    toDate,
  };
}

function rowHref(item: DirectPurchaseHistoryItem): string {
  return item.sourceType === "B2B"
    ? `/purchasing/direct-purchases/b2b/${item.sourceId}`
    : `/purchasing/direct-purchases/${item.sourceId}`;
}

function formatDisplayDate(item: DirectPurchaseHistoryItem): string {
  if (item.purchaseDate) return item.purchaseDate;
  return new Date(item.occurredAtUtc).toISOString().slice(0, 10);
}

function loadErrorDetail(error: unknown, fallback: string): string {
  if (error instanceof PosApiError) {
    return error.message || fallback;
  }
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }
  return fallback;
}

export function DirectPurchasesListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [dateFilter, setDateFilter] = useState<DateFilter>("all");
  const [sourceFilter, setSourceFilter] = useState<SourceFilter>("All");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const allowManage = canManageInventory(sessionGrant);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debounced, dateFilter, sourceFilter, statusFilter]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.organizationId
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId ?? undefined,
          }
        : null,
    [boundWorkspace],
  );

  const dateRange = useMemo(() => dateRangeForFilter(dateFilter), [dateFilter]);

  const query = useQuery({
    queryKey: [
      "direct-purchases-history",
      workspace?.organizationId,
      page,
      debounced,
      dateFilter,
      sourceFilter,
      statusFilter,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listDirectPurchases(
        workspace!,
        {
          ...dateRange,
          search: debounced || undefined,
          sourceType: sourceFilter,
          status: statusFilter,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = query.data?.items ?? [];
  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;
  const hasLoaded = query.isSuccess;
  const showTrueEmpty = hasLoaded && totalCount === 0;
  const showResults = hasLoaded && items.length > 0;

  return (
    <div
      className="purchasing-direct-page exits-page mx-auto flex w-full max-w-[80rem] min-w-0 flex-col gap-3"
      data-testid="direct-purchases-list-page"
    >
      <PageHeader
        title={t("purchasing.directPurchases")}
        description={t("purchasing.directPurchasesLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("purchasing.offline")}</p>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("purchasing.directPurchases")}
          testId="direct-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "record",
              label: t("purchasing.recordDirectPurchase"),
              icon: <PackagePlus />,
              href: online ? "/purchasing/receive-stock" : undefined,
              disabled: !online,
              testId: "direct-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <div className="purchasing-direct-page__controls flex min-w-0 flex-col gap-2">
        <SearchField
          label={t("purchasing.searchDirect")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("purchasing.searchDirect")}
          data-testid="direct-search"
          containerClassName="purchasing-direct-page__search exits-page__search w-full max-w-xl"
        />

        <div
          className="purchasing-direct-page__filters min-w-0"
          data-testid="direct-filters"
        >
          <div className="purchasing-direct-page__filter-row">
            <section
              className="purchasing-direct-page__filter-group flex flex-col gap-1"
              aria-labelledby="direct-filter-source-label"
            >
              <p
                id="direct-filter-source-label"
                className="catalog-page__filter-section-label m-0"
              >
                {t("purchasing.directSourceFilter")}
              </p>
              <ExitsChipBar
                variant="filter"
                ariaLabel={t("purchasing.directSourceFilter")}
                testId="direct-source-filter"
                items={[
                  {
                    key: "all",
                    label: t("purchasing.directSourceAll"),
                    state: sourceFilter === "All" ? "active" : "idle",
                    testId: "direct-source-all",
                    onSelect: () => setSourceFilter("All"),
                  },
                  {
                    key: "b2b",
                    label: t("purchasing.directSourceB2b"),
                    state: sourceFilter === "B2B" ? "active" : "idle",
                    testId: "direct-source-b2b",
                    onSelect: () => setSourceFilter("B2B"),
                  },
                  {
                    key: "local",
                    label: t("purchasing.directSourceLocal"),
                    state: sourceFilter === "Local" ? "active" : "idle",
                    testId: "direct-source-local",
                    onSelect: () => setSourceFilter("Local"),
                  },
                ]}
              />
            </section>

            <span className="purchasing-direct-page__filter-sep" aria-hidden />

            <section
              className="purchasing-direct-page__filter-group flex flex-col gap-1"
              aria-labelledby="direct-filter-date-label"
            >
              <p
                id="direct-filter-date-label"
                className="catalog-page__filter-section-label m-0"
              >
                {t("purchasing.directDateFilter")}
              </p>
              <ExitsChipBar
                variant="filter"
                ariaLabel={t("purchasing.directDateFilter")}
                testId="direct-date-filter"
                items={DATE_FILTERS.map((filter) => ({
                  key: filter.key,
                  label: t(filter.labelKey),
                  state: dateFilter === filter.value ? "active" : "idle",
                  testId: `direct-date-${filter.key}`,
                  onSelect: () => setDateFilter(filter.value),
                }))}
              />
            </section>

            <span className="purchasing-direct-page__filter-sep" aria-hidden />

            <section
              className="purchasing-direct-page__filter-group flex flex-col gap-1"
              aria-labelledby="direct-filter-status-label"
            >
              <p
                id="direct-filter-status-label"
                className="catalog-page__filter-section-label m-0"
              >
                {t("purchasing.directStatusFilter")}
              </p>
              <ExitsChipBar
                variant="filter"
                ariaLabel={t("purchasing.directStatusFilter")}
                testId="direct-status-filter"
                items={[
                  {
                    key: "all",
                    label: t("purchasing.directStatusAll"),
                    state: statusFilter === "All" ? "active" : "idle",
                    testId: "direct-status-all",
                    onSelect: () => setStatusFilter("All"),
                  },
                  {
                    key: "completed",
                    label: t("purchasing.directStatusCompleted"),
                    state: statusFilter === "Completed" ? "active" : "idle",
                    testId: "direct-status-completed",
                    onSelect: () => setStatusFilter("Completed"),
                  },
                  {
                    key: "voided",
                    label: t("purchasing.directStatusVoided"),
                    state: statusFilter === "Voided" ? "active" : "idle",
                    testId: "direct-status-voided",
                    onSelect: () => setStatusFilter("Voided"),
                  },
                ]}
              />
            </section>
          </div>
        </div>
      </div>

      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}

      {query.isError ? (
        <div className="flex max-w-xl flex-col gap-2" data-testid="direct-purchases-error">
          <ErrorState
            title={t("purchasing.errorTitle")}
            detail={loadErrorDetail(query.error, t("purchasing.loadFailed"))}
            error={query.error}
            operation="listDirectPurchases"
          />
          <Button
            type="button"
            variant="secondary"
            className="w-fit min-h-9"
            data-testid="direct-purchases-retry"
            onClick={() => void query.refetch()}
          >
            {t("offline.tryAgain")}
          </Button>
        </div>
      ) : null}

      {showTrueEmpty ? (
        <EmptyState
              align="center"
              icon={<ClipboardList className="size-5" strokeWidth={1.75} />} title={t("purchasing.directEmpty")} detail={t("purchasing.directEmptyDetail")} />
      ) : null}

      {showResults ? (
        <>
          <div
            className={cn(
              "hidden overflow-hidden rounded-[var(--exits-radius-md)] border border-border md:block",
            )}
            data-testid="direct-purchases-table"
          >
            <div className="overflow-x-auto">
              <table className="w-full min-w-[44rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="border-b border-border bg-[color-mix(in_srgb,var(--exits-surface-muted)_70%,transparent)] text-muted">
                    <th className="whitespace-nowrap px-3 py-2.5 font-medium">
                      {t("purchasing.directColDate")}
                    </th>
                    <th className="min-w-[10rem] px-3 py-2.5 font-medium">
                      {t("purchasing.directColSeller")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 font-medium">
                      {t("purchasing.directColType")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 font-medium">
                      {t("purchasing.directColReference")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 font-medium">
                      {t("purchasing.directColItems")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 font-medium">
                      {t("purchasing.directColTotal")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 font-medium">
                      {t("purchasing.directColStatus")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr
                      key={`${item.sourceType}-${item.sourceId}`}
                      className="border-b border-border/60 last:border-b-0 hover:bg-[color-mix(in_srgb,var(--exits-surface-muted)_45%,transparent)]"
                    >
                      <td className="whitespace-nowrap px-3 py-2.5">
                        {formatDisplayDate(item)}
                      </td>
                      <td className="px-3 py-2.5">
                        <div className="font-medium">{item.sellerDisplayName}</div>
                        {item.sellerPublicOrganizationId ? (
                          <div className="text-muted">{item.sellerPublicOrganizationId}</div>
                        ) : null}
                      </td>
                      <td className="px-3 py-2.5">
                        <StatusChip tone={item.sourceType === "B2B" ? "info" : "neutral"}>
                          {item.sourceType === "B2B"
                            ? t("purchasing.directBadgeB2b")
                            : t("purchasing.directBadgeLocal")}
                        </StatusChip>
                      </td>
                      <td className="whitespace-nowrap px-3 py-2.5 font-mono text-[length:var(--exits-text-xs)]">
                        <Link
                          to={rowHref(item)}
                          className="font-mono font-semibold text-primary underline-offset-2 hover:underline"
                          data-testid={`direct-row-${item.sourceType.toLowerCase()}-${item.sourceId}`}
                          aria-label={`${t("purchasing.directColReference")}: ${item.referenceNumber}`}
                        >
                          {item.referenceNumber}
                        </Link>
                      </td>
                      <td className="px-3 py-2.5">{item.lineCount}</td>
                      <td className="whitespace-nowrap px-3 py-2.5 font-medium">
                        {formatPeso(item.totalAmount)}
                      </td>
                      <td className="px-3 py-2.5">
                        <StatusChip tone={item.status === "Voided" ? "danger" : "success"}>
                          {item.status === "Voided"
                            ? t("purchasing.receiptStatus.voided")
                            : t("purchasing.directStatusCompleted")}
                        </StatusChip>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <ul
            className="exits-list m-0 grid list-none gap-2 p-0 md:hidden"
            data-testid="direct-purchases-list"
          >
            {items.map((item) => {
              const metaParts = [
                formatDisplayDate(item),
                item.sourceType === "B2B"
                  ? t("purchasing.directBadgeB2b")
                  : t("purchasing.directBadgeLocal"),
                t("purchasing.linesCount").replace("{count}", String(item.lineCount)),
                item.referenceNumber,
              ];
              if (item.sellerPublicOrganizationId) {
                metaParts.splice(1, 0, item.sellerPublicOrganizationId);
              }

              return (
                <li key={`${item.sourceType}-${item.sourceId}`}>
                  <Link
                    to={rowHref(item)}
                    className="exits-list__card purchasing-row block min-w-0 text-foreground no-underline"
                    data-testid={`direct-mobile-row-${item.sourceType.toLowerCase()}-${item.sourceId}`}
                  >
                    <span className="purchasing-row__main min-w-0">
                      <span className="exits-list__name block truncate font-semibold">
                        {item.sellerDisplayName}
                      </span>
                      <span className="purchasing-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                        {metaParts.join(" · ")}
                      </span>
                    </span>
                    <span className="purchasing-row__aside">
                      <span className="purchasing-row__qty">
                        {formatPeso(item.totalAmount)}
                        <span className="purchasing-row__uom">{t("purchasing.directColTotal")}</span>
                      </span>
                      <StatusChip tone={item.status === "Voided" ? "danger" : "success"}>
                        {item.status === "Voided"
                          ? t("purchasing.receiptStatus.voided")
                          : t("purchasing.directStatusCompleted")}
                      </StatusChip>
                      <ChevronRight
                        className="purchasing-row__chevron size-4 shrink-0 text-muted"
                        aria-hidden
                      />
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>

          {totalCount > 0 ? (
            <div className="exits-pagination">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.pageLabel")
                  .replace("{page}", String(page))
                  .replace("{totalPages}", String(totalPages))}
              </p>
              <div className="exits-pagination__actions flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-9"
                  disabled={!canPrev}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  {t("purchasing.prevPage")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-9"
                  disabled={!canNext}
                  onClick={() => setPage((current) => current + 1)}
                >
                  {t("purchasing.nextPage")}
                </Button>
              </div>
            </div>
          ) : null}
        </>
      ) : null}
    </div>
  );
}

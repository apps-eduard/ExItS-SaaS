import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, PackagePlus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listDirectPurchaseReceipts,
  type DirectPurchaseReceiptListItemDto,
} from "@/api/pos/pos-direct-purchase-receipts-client";
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
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

type DateFilter = "all" | "7" | "30" | "90";

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

function dateRangeForFilter(filter: DateFilter): { fromPurchaseDate?: string; toPurchaseDate?: string } {
  if (filter === "all") return {};

  const today = new Date();
  const toPurchaseDate = today.toISOString().slice(0, 10);
  const from = new Date(today);
  const days = filter === "7" ? 7 : filter === "30" ? 30 : 90;
  from.setDate(from.getDate() - days);

  return {
    fromPurchaseDate: from.toISOString().slice(0, 10),
    toPurchaseDate,
  };
}

function matchesDirectSearch(item: DirectPurchaseReceiptListItemDto, query: string): boolean {
  if (!query) return true;

  const haystack = [
    item.receiptNumber,
    item.sourceNameSnapshot,
    item.referenceNumber,
    item.purchaseDate,
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();

  return haystack.includes(query.toLowerCase());
}

export function DirectPurchasesListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [dateFilter, setDateFilter] = useState<DateFilter>("all");
  const allowManage = canManageInventory(sessionGrant);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debounced, dateFilter]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const dateRange = useMemo(() => dateRangeForFilter(dateFilter), [dateFilter]);

  const query = useQuery({
    queryKey: [
      "direct-purchases",
      workspace?.organizationId,
      workspace?.branchId,
      page,
      debounced,
      dateFilter,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listDirectPurchaseReceipts(
        workspace!,
        {
          ...dateRange,
          sourceSearch: debounced || undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  const items = useMemo(() => {
    const rows = query.data?.items ?? [];
    return rows.filter((item) => matchesDirectSearch(item, debounced));
  }, [query.data?.items, debounced]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;
  const hasLoaded = query.isSuccess;
  const showFilteredEmpty = hasLoaded && totalCount > 0 && items.length === 0;
  const showTrueEmpty = hasLoaded && totalCount === 0;

  return (
    <div
      className="purchasing-direct-page exits-page flex min-w-0 flex-col gap-3"
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
              key: "receive",
              label: t("purchasing.receiveStock"),
              icon: <PackagePlus />,
              href: online ? "/purchasing/receive-stock" : undefined,
              disabled: !online,
              testId: "direct-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <SearchField
        label={t("purchasing.searchDirect")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("purchasing.searchDirect")}
        data-testid="direct-search"
        containerClassName="purchasing-direct-page__search exits-page__search"
      />

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

      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}
      {showTrueEmpty ? (
        <EmptyState title={t("purchasing.directEmpty")} detail={t("purchasing.directEmptyDetail")} />
      ) : null}
      {showFilteredEmpty ? (
        <EmptyState
          title={t("purchasing.directNoMatch")}
          detail={t("purchasing.directNoMatchDetail")}
        />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="direct-purchases-list">
        {items.map((item) => {
          const metaParts = [
            item.purchaseDate,
            item.sourceNameSnapshot ?? t("purchasing.sourceEmpty"),
            t("purchasing.linesCount").replace("{count}", String(item.lineCount)),
          ];
          if (item.referenceNumber) {
            metaParts.push(item.referenceNumber);
          }

          return (
            <li key={item.directPurchaseReceiptId}>
              <Link
                to={`/purchasing/direct-purchases/${item.directPurchaseReceiptId}`}
                className="exits-list__card purchasing-row block min-w-0 text-foreground no-underline"
                data-testid={`direct-row-${item.directPurchaseReceiptId}`}
              >
                <span className="purchasing-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">{item.receiptNumber}</span>
                  <span className="purchasing-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {metaParts.join(" · ")}
                  </span>
                </span>
                <span className="purchasing-row__aside">
                  <span className="purchasing-row__qty">
                    {formatPeso(item.totalCost)}
                    <span className="purchasing-row__uom">{t("purchasing.totalCost")}</span>
                  </span>
                  <StatusChip tone="success">{t("purchasing.received")}</StatusChip>
                  <ChevronRight className="purchasing-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          );
        })}
      </ul>

      {query.isSuccess && totalCount > 0 ? (
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
    </div>
  );
}

import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { CalendarClock, ChevronRight } from "lucide-react";
import { listInventory } from "@/api/pos/pos-inventory-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type TrackingFilter = "all" | "tracked" | "untracked";

const TRACKING_FILTERS: Array<{
  value: TrackingFilter;
  key: string;
  labelKey: "inventory.filterAll" | "inventory.filterTracked" | "inventory.filterUntracked";
}> = [
  { value: "all", key: "all", labelKey: "inventory.filterAll" },
  { value: "tracked", key: "tracked", labelKey: "inventory.filterTracked" },
  { value: "untracked", key: "untracked", labelKey: "inventory.filterUntracked" },
];

function stockTone(stockStatus: string, isLowStock: boolean): "success" | "warning" | "danger" | "info" {
  const status = stockStatus.trim().toLowerCase();
  if (status.includes("out")) return "danger";
  if (isLowStock || status.includes("low")) return "warning";
  if (status.includes("ok") || status.includes("in")) return "success";
  return "info";
}

export function InventoryListPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [trackingFilter, setTrackingFilter] = useState<TrackingFilter>("all");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["inventory", workspace?.organizationId, workspace?.branchId, debounced],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listInventory(workspace!, { search: debounced || undefined, pageSize: 50 }, signal),
  });

  const items = useMemo(() => {
    const all = query.data?.items ?? [];
    if (trackingFilter === "tracked") return all.filter((item) => item.isTracked);
    if (trackingFilter === "untracked") return all.filter((item) => !item.isTracked);
    return all;
  }, [query.data?.items, trackingFilter]);

  if (!workspace) {
    return <BranchRequiredPanel title={t("inventory.title")} />;
  }

  return (
    <div
      className="inventory-list-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="inventory-list-page"
    >
      <PageHeader
        title={t("inventory.title")}
        description={t("inventory.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-inventory"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("inventory.title")}
        testId="inventory-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "expiring",
            label: t("inventory.openExpiring"),
            icon: <CalendarClock />,
            href: "/inventory/expiration",
            testId: "open-expiring-stock",
            emphasis: "primary",
          },
        ]}
      />

      <SearchField
        label={t("inventory.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("inventory.search")}
        data-testid="inventory-search"
        containerClassName="inventory-list-page__search exits-page__search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("inventory.trackingFilter")}
        testId="inventory-tracking-filters"
        items={TRACKING_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: trackingFilter === filter.value ? "active" : "idle",
          testId: `inventory-filter-${filter.key}`,
          onSelect: () => setTrackingFilter(filter.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState title={t("inventory.empty")} detail={t("inventory.emptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="inventory-list">
        {items.map((item) => {
          const tracked = item.isTracked;
          return (
            <li key={item.productId}>
              <Link
                className={cn(
                  "exits-list__card inventory-row block min-w-0 text-foreground no-underline",
                  !tracked && "inventory-row--untracked",
                )}
                to={`/inventory/${item.productId}`}
                data-testid={`inventory-row-${item.productId}`}
              >
                <div className="inventory-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">{item.name}</span>
                  <span className="inventory-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {tracked
                      ? `${t("inventory.onHand")}: ${item.onHandQuantity} ${item.unitOfMeasure}`
                      : t("inventory.notTracked")}
                    {tracked && item.tracksExpiration
                      ? ` · ${t("inventory.tracksExpirationShort")}`
                      : ""}
                  </span>
                  <div className="inventory-row__chips mt-2 flex flex-wrap gap-1.5">
                    <StatusChip tone={tracked ? "success" : "info"}>
                      {tracked ? t("inventory.tracked") : t("inventory.notTracked")}
                    </StatusChip>
                    {tracked && item.isLowStock ? (
                      <StatusChip tone="warning">{t("inventory.lowStock")}</StatusChip>
                    ) : null}
                    {tracked && item.stockStatus ? (
                      <StatusChip tone={stockTone(item.stockStatus, item.isLowStock)}>
                        {item.stockStatus}
                      </StatusChip>
                    ) : null}
                  </div>
                </div>
                <div className="inventory-row__aside">
                  {tracked ? (
                    <span className="inventory-row__qty tabular-nums">
                      {item.onHandQuantity}
                      <span className="inventory-row__uom">{item.unitOfMeasure}</span>
                    </span>
                  ) : (
                    <span className="inventory-row__qty inventory-row__qty--muted">—</span>
                  )}
                  <ChevronRight className="inventory-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                </div>
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

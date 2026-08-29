import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeftRight, CalendarClock, ChevronRight, ClipboardList, Factory, PackageMinus, Trash2 } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listInventory } from "@/api/pos/pos-inventory-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { OrganizationQueryGate } from "@/runtime/OrganizationQueryGate";
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

export function InventoryListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
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

      {!allowManage ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="inventory-view-only-hint"
        >
          {t("inventory.viewOnlyHint")}
        </p>
      ) : (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="inventory-manage-scope-hint"
        >
          {t("inventory.manageScopeHint")}
        </p>
      )}

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
          {
            key: "stock-counts",
            label: t("inventory.openStockCount"),
            icon: <ClipboardList />,
            href: "/inventory/stock-counts",
            testId: "open-stock-count",
          },
          {
            key: "transfers",
            label: t("inventory.openTransfers"),
            icon: <ArrowLeftRight />,
            href: "/inventory/transfers",
            testId: "open-transfers",
          },
          {
            key: "stock-use",
            label: t("inventory.openStockUse"),
            icon: <PackageMinus />,
            href: "/inventory/stock-use",
            testId: "open-stock-use",
          },
          {
            key: "waste-loss",
            label: t("inventory.openWasteLoss"),
            icon: <Trash2 />,
            href: "/inventory/waste-loss",
            testId: "open-waste-loss",
          },
          {
            key: "production",
            label: t("inventory.openProduction"),
            icon: <Factory />,
            href: "/inventory/production",
            testId: "open-production",
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

      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}

      <OrganizationQueryGate
        title={t("inventory.title")}
        isLoading={query.isLoading}
        isError={query.isError}
        hasData={Boolean(query.data)}
        onRetry={() => void query.refetch()}
      >
        {query.isError && query.data ? (
          <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
        ) : null}
        {query.isSuccess && items.length === 0 ? (
          <EmptyState title={t("inventory.empty")} detail={t("inventory.emptyDetail")} />
        ) : null}

        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="inventory-list">
          {items.map((item) => {
            const tracked = item.isTracked;
            const lowStock = tracked && item.isLowStock;
            const stockStatus = tracked ? item.stockStatus?.trim() ?? "" : "";
            const stockStatusKey = stockStatus.toLowerCase();
            const outOfStock = stockStatusKey.includes("out");
            const showStockChip =
              tracked &&
              Boolean(stockStatus) &&
              (lowStock || outOfStock || stockStatusKey.includes("low"));
            const tracksExpiry = tracked && item.tracksExpiration === true;

            return (
              <li key={item.productId}>
                <Link
                  className={cn(
                    "exits-list__card inventory-row block min-w-0 text-foreground no-underline",
                    !tracked && "inventory-row--untracked",
                    lowStock && "inventory-row--low",
                    outOfStock && "inventory-row--out",
                  )}
                  to={`/inventory/${item.productId}`}
                  data-testid={`inventory-row-${item.productId}`}
                >
                  <div className="inventory-row__main min-w-0">
                    <span className="exits-list__name block truncate font-semibold">{item.name}</span>
                    {!tracked || tracksExpiry || showStockChip ? (
                      <div className="inventory-row__chips mt-1.5 flex flex-wrap gap-1">
                        {!tracked ? (
                          <span className="inventory-row__badge inventory-row__badge--untracked">
                            {t("inventory.notTracked")}
                          </span>
                        ) : null}
                        {tracksExpiry ? (
                          <span className="inventory-row__badge inventory-row__badge--expiry">
                            {t("inventory.tracksExpirationShort")}
                          </span>
                        ) : null}
                        {showStockChip ? (
                          <span
                            className={
                              outOfStock
                                ? "inventory-row__badge inventory-row__badge--out"
                                : "inventory-row__badge inventory-row__badge--low"
                            }
                          >
                            {outOfStock ? stockStatus : t("inventory.lowStock")}
                          </span>
                        ) : null}
                      </div>
                    ) : null}
                  </div>
                  <div className="inventory-row__aside">
                    {tracked ? (
                      <span
                        className={cn(
                          "inventory-row__qty tabular-nums",
                          lowStock && "inventory-row__qty--warn",
                          outOfStock && "inventory-row__qty--danger",
                        )}
                      >
                        {item.onHandQuantity}
                        <span className="inventory-row__uom">{item.unitOfMeasure}</span>
                      </span>
                    ) : (
                      <span className="inventory-row__qty inventory-row__qty--muted" aria-hidden>
                        —
                      </span>
                    )}
                    <ChevronRight className="inventory-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                  </div>
                </Link>
              </li>
            );
          })}
        </ul>
      </OrganizationQueryGate>
    </div>
  );
}

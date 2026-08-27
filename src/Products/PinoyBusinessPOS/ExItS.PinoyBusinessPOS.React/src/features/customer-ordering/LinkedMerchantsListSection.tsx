import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Ban,
  CalendarClock,
  ChevronDown,
  Link2,
  Loader2,
  Package,
  Receipt,
  ShoppingBag,
  Truck,
} from "lucide-react";
import type { LinkedMerchantDto } from "@/api/platform/linked-merchants-client";
import {
  disconnectAndBlockLinkedMerchant,
  disconnectLinkedMerchant,
} from "@/api/platform/customer-link-requests-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { SearchField } from "@/components/exits/SearchField";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import {
  CommerceLoadMore,
  MerchantOrderingBadge,
  storeDisplayInitial,
} from "@/features/customer-ordering/personal-commerce-ui";
import type { MerchantOrderingProbe } from "@/features/customer-ordering/useLinkedMerchantsOrderingProbes";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type StoreListFilter = "all" | "can_order" | "unavailable";

const FILTERS: ReadonlyArray<{
  key: StoreListFilter;
  labelKey:
    | "personal.merchants.filter.all"
    | "personal.merchants.filter.canOrder"
    | "personal.merchants.filter.unavailable";
}> = [
  { key: "all", labelKey: "personal.merchants.filter.all" },
  { key: "can_order", labelKey: "personal.merchants.filter.canOrder" },
  { key: "unavailable", labelKey: "personal.merchants.filter.unavailable" },
];

export type LinkedMerchantRow = {
  merchant: LinkedMerchantDto;
  ordering: MerchantOrderingProbe;
};

function matchesFilter(row: LinkedMerchantRow, filter: StoreListFilter): boolean {
  switch (filter) {
    case "can_order":
      return row.ordering.resolved && row.ordering.canCustomerOrder;
    case "unavailable":
      return row.ordering.resolved && !row.ordering.canCustomerOrder;
    default:
      return true;
  }
}

export function filterLinkedMerchantRows(
  rows: LinkedMerchantRow[],
  filter: StoreListFilter,
  search: string,
): LinkedMerchantRow[] {
  const needle = search.trim().toLowerCase();
  return rows
    .filter((row) => matchesFilter(row, filter))
    .filter((row) => {
      if (!needle) {
        return true;
      }
      const hay =
        `${row.merchant.organizationDisplayName} ${row.merchant.customerDisplayName}`.toLowerCase();
      return hay.includes(needle);
    })
    .sort((a, b) =>
      a.merchant.organizationDisplayName.localeCompare(b.merchant.organizationDisplayName),
    );
}

function LinkedMerchantStoreCard({
  row,
  index,
}: {
  row: LinkedMerchantRow;
  index: number;
}) {
  const { t } = useI18n();
  const { merchant, ordering } = row;
  const queryClient = useQueryClient();
  const [connError, setConnError] = useState<string | null>(null);
  const [manageOpen, setManageOpen] = useState(false);

  const statementTo = `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`;
  const shopTo = `/personal/linked-merchants/${merchant.organizationId}/shop`;
  const canCustomerOrder = ordering.resolved && ordering.canCustomerOrder;
  const canCustomerDelivery = ordering.resolved && ordering.canCustomerDelivery;

  const disconnect = useMutation({
    mutationFn: (mode: "disconnect" | "block") =>
      mode === "block"
        ? disconnectAndBlockLinkedMerchant(merchant.organizationId)
        : disconnectLinkedMerchant(merchant.organizationId),
    onSuccess: async () => {
      setConnError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "linked-merchants"] });
      await queryClient.invalidateQueries({ queryKey: ["personal", "blocked-businesses"] });
    },
    onError: (error) =>
      setConnError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.merchants.disconnectFailed"),
      ),
  });

  return (
    <li
      className="pc-store-directory__item"
      style={{ animationDelay: `${Math.min(index, 8) * 45 + 40}ms` }}
    >
      <article className="pc-store-card" data-testid="linked-merchant-card">
        <div className="pc-store-card__top">
          <span className="pc-store-card__avatar" aria-hidden>
            {storeDisplayInitial(merchant.organizationDisplayName)}
          </span>
          <div className="pc-store-card__body">
            <h3 className="pc-store-card__name">{merchant.organizationDisplayName}</h3>
            <p className="pc-store-card__linked-as m-0">
              <Link2 className="pc-store-card__link-icon size-3.5 shrink-0" aria-hidden />
              <span>
                {t("personal.merchants.linkedAs").replace(
                  "{name}",
                  merchant.customerDisplayName,
                )}
              </span>
            </p>
            <div className="pc-store-card__badge-row">
              <ConnectionStatusChip
                state="Linked"
                audience="personal"
                testId="linked-merchant-connection-chip"
              />
              <MerchantOrderingBadge
                available={canCustomerOrder}
                pending={ordering.pending}
              />
            </div>
          </div>
        </div>

        <div className="pc-store-card__meta">
          <span className="pc-store-card__meta-item">
            <CalendarClock className="size-3.5 shrink-0" aria-hidden />
            {t("personal.merchants.linkedSince")}{" "}
            {new Date(merchant.linkedAtUtc).toLocaleDateString()}
          </span>
          {canCustomerOrder ? (
            canCustomerDelivery ? (
              <span className="pc-store-card__meta-item">
                <Truck className="size-3.5 shrink-0" aria-hidden />
                {t("orders.delivery")}
              </span>
            ) : (
              <span className="pc-store-card__meta-item">
                <Package className="size-3.5 shrink-0" aria-hidden />
                {t("orders.pickup")}
              </span>
            )
          ) : null}
        </div>

        <div
          className={
            canCustomerOrder
              ? "pc-store-card__actions"
              : "pc-store-card__actions pc-store-card__actions--solo"
          }
        >
          {canCustomerOrder ? (
            <>
              <Button
                asChild
                className="pc-store-card__action pc-store-card__action--shop"
                data-testid="open-merchant-shop"
              >
                <Link to={shopTo}>
                  <ShoppingBag className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                  {t("personal.shopLink")}
                </Link>
              </Button>
              <Button
                asChild
                variant="outline"
                className="pc-store-card__action pc-store-card__action--statement"
                data-testid="open-merchant-statement"
              >
                <Link to={statementTo}>
                  <Receipt className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                  {t("personal.merchantStatement.openPurchases")}
                </Link>
              </Button>
            </>
          ) : (
            <Button
              asChild
              className="pc-store-card__action pc-store-card__action--statement"
              data-testid="open-merchant-statement"
            >
              <Link to={statementTo}>
                <Receipt className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                {t("personal.merchantStatement.openPurchases")}
              </Link>
            </Button>
          )}
        </div>

        <div className="pc-store-card__manage">
          <button
            type="button"
            className="pc-store-card__manage-toggle"
            aria-expanded={manageOpen}
            data-testid={`manage-merchant-${merchant.organizationId}`}
            onClick={() => setManageOpen((value) => !value)}
          >
            <span>{t("personal.merchants.manageConnection")}</span>
            <ChevronDown
              className={cn("size-4 shrink-0 transition-transform", manageOpen && "rotate-180")}
              aria-hidden
            />
          </button>
          {manageOpen ? (
            <div className="pc-store-card__manage-panel">
              <Button
                type="button"
                variant="outline"
                className="min-h-10 w-full"
                disabled={disconnect.isPending}
                data-testid={`disconnect-merchant-${merchant.organizationId}`}
                onClick={() => {
                  if (window.confirm(t("personal.merchants.disconnectConfirm"))) {
                    disconnect.mutate("disconnect");
                  }
                }}
              >
                {disconnect.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : null}
                {t("personal.merchants.disconnect")}
              </Button>
              <Button
                type="button"
                variant="ghost"
                className="min-h-10 w-full text-destructive"
                disabled={disconnect.isPending}
                data-testid={`disconnect-block-merchant-${merchant.organizationId}`}
                onClick={() => {
                  if (window.confirm(t("personal.merchants.disconnectBlockConfirm"))) {
                    disconnect.mutate("block");
                  }
                }}
              >
                <Ban className="size-4 shrink-0" aria-hidden />
                {t("personal.merchants.disconnectAndBlock")}
              </Button>
            </div>
          ) : null}
        </div>

        {connError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
            {connError}
          </p>
        ) : null}
      </article>
    </li>
  );
}

export function LinkedMerchantsListSection({
  rows,
  hasNextPage,
  isFetchingNextPage,
  onLoadMore,
}: {
  rows: LinkedMerchantRow[];
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  onLoadMore: () => void;
}) {
  const { t } = useI18n();
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<StoreListFilter>("all");

  const filteredRows = useMemo(
    () => filterLinkedMerchantRows(rows, filter, search),
    [filter, rows, search],
  );

  const filterCounts = useMemo(() => {
    const counts: Record<StoreListFilter, number> = {
      all: rows.length,
      can_order: 0,
      unavailable: 0,
    };
    for (const row of rows) {
      if (row.ordering.resolved && row.ordering.canCustomerOrder) {
        counts.can_order += 1;
      }
      if (row.ordering.resolved && !row.ordering.canCustomerOrder) {
        counts.unavailable += 1;
      }
    }
    return counts;
  }, [rows]);

  return (
    <section
      className="pc-store-list-section catalog-form-section exits-animate-panel flex flex-col gap-3"
      aria-label={t("personal.merchants.listTitle")}
      data-testid="linked-merchants-list-section"
    >
      <div className="pc-store-list-section__header">
        <div className="min-w-0">
          <h2 className="catalog-form-section__title m-0">{t("personal.merchants.listTitle")}</h2>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.merchants.listLede")}
          </p>
        </div>
        {rows.length > 0 ? (
          <span className="pc-store-list-section__count">{rows.length}</span>
        ) : null}
      </div>

      <SearchField
        label={t("personal.merchants.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("personal.merchants.searchPlaceholder")}
        data-testid="linked-merchants-search"
        containerClassName="linked-merchants-page__search exits-page__search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("personal.merchants.filter.label")}
        testId="linked-merchants-filters"
        className="exits-animate-toolbar"
        items={FILTERS.map((item) => ({
          key: item.key,
          label: `${t(item.labelKey)}${filterCounts[item.key] > 0 ? ` (${filterCounts[item.key]})` : ""}`,
          state: filter === item.key ? "active" : "idle",
          testId: `linked-merchants-filter-${item.key}`,
          onSelect: () => setFilter(item.key),
        }))}
      />

      {rows.length === 0 ? (
        <EmptyState
          title={t("personal.merchantsEmptyTitle")}
          detail={t("personal.merchantsEmptyDetail")}
        />
      ) : filteredRows.length === 0 ? (
        <EmptyState
          title={t("personal.merchants.noResultsTitle")}
          detail={t("personal.merchants.noResultsBody")}
        />
      ) : (
        <ul className="pc-store-directory">
          {filteredRows.map((row, index) => (
            <LinkedMerchantStoreCard key={row.merchant.linkedCustomerId} row={row} index={index} />
          ))}
        </ul>
      )}

      {hasNextPage ? (
        <CommerceLoadMore
          label={t("inventory.loadMore")}
          loadingLabel={t("loading.label")}
          busy={isFetchingNextPage}
          testId="linked-merchants-load-more"
          onClick={onLoadMore}
        />
      ) : null}
    </section>
  );
}

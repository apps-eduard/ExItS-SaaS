import { useMemo, useState, type CSSProperties } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Ban,
  CalendarClock,
  Check,
  ChevronDown,
  Link2,
  Loader2,
  MoreHorizontal,
  Package,
  Receipt,
  Search,
  ShoppingBag,
  Truck,
  Unlink,
  Users,
} from "lucide-react";
import type { LinkedMerchantDto } from "@/api/platform/linked-merchants-client";
import {
  disconnectAndBlockLinkedMerchant,
  disconnectLinkedMerchant,
} from "@/api/platform/customer-link-requests-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  MenuItem,
  MenuSeparator,
  useDismissibleOpen,
} from "@/components/ui/dropdown-menu";
import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ExitsOutlineInput } from "@/components/exits/ExitsOutlineField";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
  ExitsTableToolbar,
} from "@/components/exits/ExitsTable";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import { RESPONSIVE_DATA_TABLE_MIN_LG } from "@/components/exits/responsive-data-view";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import { PersonalStoreIdentity } from "@/features/customer-ordering/PersonalStoreIdentity";
import {
  personalCustomerRelationshipLabel,
  personalStoreDisplayName,
  personalStoreSearchText,
} from "@/features/customer-ordering/format-personal-store-label";
import { CommerceLoadMore } from "@/features/customer-ordering/personal-commerce-ui";
import type { MerchantOrderingProbe } from "@/features/customer-ordering/useLinkedMerchantsOrderingProbes";
import {
  TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_FOOTER_INTENT,
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
} from "@/features/ui-standards/table-global-default";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { useSession } from "@/session/SessionProvider";

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
      return personalStoreSearchText(
        row.merchant.organizationDisplayName,
        row.merchant.customerDisplayName,
      ).includes(needle);
    })
    .sort((a, b) =>
      personalStoreDisplayName(a.merchant.organizationDisplayName).localeCompare(
        personalStoreDisplayName(b.merchant.organizationDisplayName),
      ),
    );
}

function orderingChip(
  ordering: MerchantOrderingProbe,
): { tone: StatusChipTone; labelKey: MessageKey } {
  if (!ordering.resolved || ordering.pending) {
    return { tone: "info", labelKey: "personal.merchants.status.checking" };
  }
  if (ordering.canCustomerOrder) {
    return { tone: "success", labelKey: "personal.merchants.status.canOrder" };
  }
  return { tone: "warning", labelKey: "personal.merchants.status.unavailable" };
}

function fulfillmentLabel(
  ordering: MerchantOrderingProbe,
  t: (key: MessageKey) => string,
): string {
  if (!ordering.resolved || !ordering.canCustomerOrder) {
    return t("personal.merchants.fulfillment.none");
  }
  return ordering.canCustomerDelivery ? t("orders.delivery") : t("orders.pickup");
}

function LinkedMerchantManagePanel({ merchant }: { merchant: LinkedMerchantDto }) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [connError, setConnError] = useState<string | null>(null);
  const [manageOpen, setManageOpen] = useState(false);

  const disconnect = useMutation({
    mutationFn: (mode: "disconnect" | "block") =>
      mode === "block"
        ? disconnectAndBlockLinkedMerchant(merchant.organizationId)
        : disconnectLinkedMerchant(merchant.organizationId),
    onSuccess: async () => {
      setConnError(null);
      setManageOpen(false);
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
            intent="warning"
            appearance="solid"
            emphasis="soft"
            className="min-h-10 min-w-0 flex-1 basis-0 whitespace-normal text-center"
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
            intent="danger"
            appearance="solid"
            emphasis="soft"
            className="min-h-10 min-w-0 flex-1 basis-0 whitespace-normal text-center"
            disabled={disconnect.isPending}
            data-testid={`disconnect-block-merchant-${merchant.organizationId}`}
            onClick={() => {
              if (window.confirm(t("personal.merchants.disconnectBlockConfirm"))) {
                disconnect.mutate("block");
              }
            }}
          >
            {t("personal.merchants.disconnectAndBlock")}
          </Button>
        </div>
      ) : null}
      {connError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {connError}
        </p>
      ) : null}
    </div>
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
  const { session } = useSession();
  const { merchant, ordering } = row;

  const statementTo = `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`;
  const shopTo = `/personal/linked-merchants/${merchant.organizationId}/shop`;
  const storeName = personalStoreDisplayName(merchant.organizationDisplayName);
  const relationshipName = personalCustomerRelationshipLabel(
    merchant.customerDisplayName,
    session?.displayName,
  );
  const canCustomerOrder = ordering.resolved && ordering.canCustomerOrder;
  const canCustomerDelivery = ordering.resolved && ordering.canCustomerDelivery;

  return (
    <li
      className="pc-store-directory__item"
      style={{ animationDelay: `${Math.min(index, 8) * 45 + 40}ms` }}
    >
      <article className="pc-store-card" data-testid="linked-merchant-card">
        <PersonalStoreIdentity
          storeName={storeName}
          relationshipLabel={relationshipName}
          canCustomerOrder={canCustomerOrder}
          orderingPending={ordering.pending}
          headingLevel="h3"
          connectionTestId="linked-merchant-connection-chip"
        />

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
                className="pc-store-card__action pc-store-card__action--shop !h-full !min-h-[var(--exits-touch-target-min)]"
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
                className="pc-store-card__action pc-store-card__action--statement !h-full !min-h-[var(--exits-touch-target-min)]"
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
              className="pc-store-card__action pc-store-card__action--statement !h-full !min-h-[var(--exits-touch-target-min)]"
              data-testid="open-merchant-statement"
            >
              <Link to={statementTo}>
                <Receipt className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                {t("personal.merchantStatement.openPurchases")}
              </Link>
            </Button>
          )}
        </div>

        <LinkedMerchantManagePanel merchant={merchant} />
      </article>
    </li>
  );
}

function LinkedMerchantTableActions({ row }: { row: LinkedMerchantRow }) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const menu = useDismissibleOpen(false);
  const [connError, setConnError] = useState<string | null>(null);
  const { merchant, ordering } = row;
  const statementTo = `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`;
  const shopTo = `/personal/linked-merchants/${merchant.organizationId}/shop`;
  const canCustomerOrder = ordering.resolved && ordering.canCustomerOrder;
  const moreLabel = t("personal.merchants.more");

  const disconnect = useMutation({
    mutationFn: (mode: "disconnect" | "block") =>
      mode === "block"
        ? disconnectAndBlockLinkedMerchant(merchant.organizationId)
        : disconnectLinkedMerchant(merchant.organizationId),
    onSuccess: async () => {
      setConnError(null);
      menu.close();
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
    <div className="linked-merchants-table__actions inline-flex flex-col items-center gap-1">
      <DropdownMenu
        align="end"
        open={menu.open}
        onOpenChange={menu.setOpen}
        menuLabel={moreLabel}
        menuClassName="linked-merchants-table__more-menu"
        trigger={(triggerProps) => (
          <button
            type="button"
            className="linked-merchants-table__more"
            id={triggerProps.id}
            aria-haspopup="menu"
            aria-expanded={triggerProps.expanded}
            aria-controls={triggerProps.controls}
            aria-label={moreLabel}
            title={moreLabel}
            data-testid={`linked-merchant-more-${merchant.organizationId}`}
            onClick={triggerProps.onClick}
            onKeyDown={triggerProps.onKeyDown}
          >
            <MoreHorizontal className="size-4" aria-hidden />
          </button>
        )}
      >
        {canCustomerOrder ? (
          <MenuItem
            data-testid={`open-merchant-shop-${merchant.organizationId}`}
            onSelect={() => {
              menu.close();
              navigate(shopTo);
            }}
          >
            <ShoppingBag className="size-4 shrink-0" aria-hidden />
            {t("personal.shopLink")}
          </MenuItem>
        ) : null}
        <MenuItem
          data-testid={`open-merchant-statement-${merchant.organizationId}`}
          onSelect={() => {
            menu.close();
            navigate(statementTo);
          }}
        >
          <Receipt className="size-4 shrink-0" aria-hidden />
          {t("personal.merchantStatement.openPurchases")}
        </MenuItem>
        <MenuSeparator />
        <MenuItem
          data-selected="true"
          aria-checked="true"
          data-testid={`linked-merchant-connected-${merchant.organizationId}`}
          className="linked-merchants-table__more-item--active"
          onSelect={() => menu.close()}
        >
          <Link2 className="size-4 shrink-0" aria-hidden />
          <span className="min-w-0 flex-1">{t("connection.status.connected")}</span>
          <Check className="ms-auto size-4 shrink-0" strokeWidth={3} aria-hidden />
        </MenuItem>
        <MenuItem
          data-testid={`disconnect-merchant-${merchant.organizationId}`}
          disabled={disconnect.isPending}
          onSelect={() => {
            if (window.confirm(t("personal.merchants.disconnectConfirm"))) {
              disconnect.mutate("disconnect");
            }
          }}
        >
          {disconnect.isPending ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : (
            <Unlink className="size-4 shrink-0" aria-hidden />
          )}
          {t("personal.merchants.disconnect")}
        </MenuItem>
        <MenuItem
          destructive
          data-testid={`disconnect-block-merchant-${merchant.organizationId}`}
          disabled={disconnect.isPending}
          onSelect={() => {
            if (window.confirm(t("personal.merchants.disconnectBlockConfirm"))) {
              disconnect.mutate("block");
            }
          }}
        >
          <Ban className="size-4 shrink-0" aria-hidden />
          {t("personal.merchants.block")}
        </MenuItem>
      </DropdownMenu>
      {connError ? (
        <p className="m-0 max-w-[12rem] text-end text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {connError}
        </p>
      ) : null}
    </div>
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
  const { session } = useSession();
  const { layout } = useResponsiveDataLayout({ tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_LG });
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

  const tableChromeStyle = {
    "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qheader-ink": "var(--exits-text, #000)",
    "--qfooter-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qfooter-ink": "var(--exits-text, #000)",
  } as CSSProperties;

  const renderFooter = (opts: { testId: string; rangeTestId: string; loadMoreTestId: string }) => (
    <div
      className="my-orders-table-footer"
      data-testid={opts.testId}
      data-appearance={TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE}
      data-intent={TABLE_GLOBAL_DEFAULT_FOOTER_INTENT}
      style={
        {
          "--qfooter-bg": "var(--exits-surface-elevated, #fff)",
          "--qfooter-ink": "var(--exits-text, #000)",
        } as CSSProperties
      }
    >
      <span className="my-orders-table-footer__range" data-testid={opts.rangeTestId}>
        {filteredRows.length}
        {search.trim() || filter !== "all" ? ` / ${rows.length}` : ""}
      </span>
      {hasNextPage ? (
        <CommerceLoadMore
          label={t("inventory.loadMore")}
          loadingLabel={t("loading.label")}
          busy={isFetchingNextPage}
          testId={opts.loadMoreTestId}
          onClick={onLoadMore}
        />
      ) : null}
    </div>
  );

  return (
    <section
      className="pc-store-list-section flex min-w-0 flex-col gap-3"
      aria-label={t("personal.merchants.listTitle")}
      data-testid="linked-merchants-list-section"
    >
      <div className="pc-store-list-section__header">
        <div className="min-w-0">
          <h2 className="pc-commerce-nav__body-title m-0">{t("personal.merchants.listTitle")}</h2>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.merchants.listLede")}
          </p>
        </div>
        {rows.length > 0 ? (
          <CountBadge count={rows.length} tone="neutral" className="pc-store-list-section__count" />
        ) : null}
      </div>

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("personal.merchants.filter.label")}
        testId="linked-merchants-filters"
        className="exits-animate-toolbar"
        items={FILTERS.map((item) => ({
          key: item.key,
          label: t(item.labelKey),
          count: filterCounts[item.key],
          state: filter === item.key ? "active" : "idle",
          testId: `linked-merchants-filter-${item.key}`,
          onSelect: () => setFilter(item.key),
        }))}
      />

      {rows.length === 0 ? (
        <EmptyState
          align="center"
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("personal.merchantsEmptyTitle")}
          detail={t("personal.merchantsEmptyDetail")}
        />
      ) : (
        <ExitsResponsiveDataView
          layout={layout}
          testId="linked-merchants-responsive"
          className="linked-merchants-responsive"
          table={
            <ExitsTableContainer
              className="my-orders-table linked-merchants-table"
              data-shell="soft-glass"
              data-testid="linked-merchants-table"
              style={tableChromeStyle}
            >
              <ExitsTableToolbar
                search={
                  <ExitsOutlineInput
                    label={t("personal.merchants.search")}
                    placeholder={t("personal.merchants.searchPlaceholder")}
                    placeholderOnly
                    icon={Search}
                    type="search"
                    value={search}
                    onChange={setSearch}
                    clearable
                    testId="linked-merchants-search"
                  />
                }
              />
              {filteredRows.length === 0 ? (
                <EmptyState
                  variant="filtered"
                  align="center"
                  icon={<Users className="size-5" strokeWidth={1.75} />}
                  title={t("personal.merchants.noResultsTitle")}
                  detail={t("personal.merchants.noResultsBody")}
                />
              ) : (
                <ExitsTable>
                  <ExitsTableHeader
                    data-appearance={TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE}
                    data-intent={TABLE_GLOBAL_DEFAULT_HEADER_INTENT}
                    style={
                      {
                        "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
                        "--qheader-ink": "var(--exits-text, #000)",
                      } as CSSProperties
                    }
                  >
                    <ExitsTableRow>
                      <ExitsTableHead colSize="flex">{t("personal.merchants.table.store")}</ExitsTableHead>
                      <ExitsTableHead>{t("personal.merchants.table.linkedAs")}</ExitsTableHead>
                      <ExitsTableHead>{t("personal.merchants.table.connection")}</ExitsTableHead>
                      <ExitsTableHead>{t("personal.merchants.table.status")}</ExitsTableHead>
                      <ExitsTableHead>{t("personal.merchants.table.fulfillment")}</ExitsTableHead>
                      <ExitsTableHead>{t("personal.merchants.table.linkedSince")}</ExitsTableHead>
                      <ExitsTableHead cellAlign="center" colSize="actions">
                        {t("personal.merchants.table.actions")}
                      </ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    {filteredRows.map((row) => {
                      const { merchant, ordering } = row;
                      const storeName = personalStoreDisplayName(merchant.organizationDisplayName);
                      const relationshipName = personalCustomerRelationshipLabel(
                        merchant.customerDisplayName,
                        session?.displayName,
                      );
                      const status = orderingChip(ordering);
                      const canCustomerOrder = ordering.resolved && ordering.canCustomerOrder;
                      const canCustomerDelivery = ordering.resolved && ordering.canCustomerDelivery;
                      return (
                        <ExitsTableRow
                          key={merchant.organizationId}
                          data-testid={`linked-merchant-table-row-${merchant.organizationId}`}
                        >
                          <ExitsTableCell emphasis="semibold" truncate title={storeName}>
                            {storeName}
                          </ExitsTableCell>
                          <ExitsTableCell truncate title={relationshipName ?? undefined}>
                            {relationshipName}
                          </ExitsTableCell>
                          <ExitsTableCell>
                            <ConnectionStatusChip
                              state="Linked"
                              audience="personal"
                              appearance="emphasis"
                              testId={`linked-merchant-connection-${merchant.organizationId}`}
                            />
                          </ExitsTableCell>
                          <ExitsTableCell>
                            <StatusChip tone={status.tone} appearance="emphasis">
                              {t(status.labelKey)}
                            </StatusChip>
                          </ExitsTableCell>
                          <ExitsTableCell truncate>
                            {canCustomerOrder ? (
                              <span className="inline-flex min-w-0 items-center gap-1.5">
                                {canCustomerDelivery ? (
                                  <Truck className="size-3.5 shrink-0" aria-hidden />
                                ) : (
                                  <Package className="size-3.5 shrink-0" aria-hidden />
                                )}
                                <span className="truncate">{fulfillmentLabel(ordering, t)}</span>
                              </span>
                            ) : (
                              fulfillmentLabel(ordering, t)
                            )}
                          </ExitsTableCell>
                          <ExitsTableCell truncate>
                            {new Date(merchant.linkedAtUtc).toLocaleDateString()}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="center" colSize="actions">
                            <LinkedMerchantTableActions row={row} />
                          </ExitsTableCell>
                        </ExitsTableRow>
                      );
                    })}
                  </ExitsTableBody>
                </ExitsTable>
              )}
              {renderFooter({
                testId: "linked-merchants-table-footer",
                rangeTestId: "linked-merchants-table-range",
                loadMoreTestId: "linked-merchants-load-more",
              })}
            </ExitsTableContainer>
          }
          list={
            <div className="my-orders-list-shell linked-merchants-list-shell flex min-w-0 flex-col gap-3">
              <SearchField
                label={t("personal.merchants.search")}
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("personal.merchants.searchPlaceholder")}
                data-testid="linked-merchants-search-list"
                containerClassName="linked-merchants-page__search exits-page__search"
              />
              {filteredRows.length === 0 ? (
                <EmptyState
                  variant="filtered"
                  align="center"
                  icon={<Users className="size-5" strokeWidth={1.75} />}
                  title={t("personal.merchants.noResultsTitle")}
                  detail={t("personal.merchants.noResultsBody")}
                />
              ) : (
                <ul className="pc-store-directory">
                  {filteredRows.map((row, index) => (
                    <LinkedMerchantStoreCard
                      key={row.merchant.organizationId}
                      row={row}
                      index={index}
                    />
                  ))}
                </ul>
              )}
              {renderFooter({
                testId: "linked-merchants-list-footer",
                rangeTestId: "linked-merchants-list-range",
                loadMoreTestId: "linked-merchants-load-more-list",
              })}
            </div>
          }
        />
      )}
    </section>
  );
}

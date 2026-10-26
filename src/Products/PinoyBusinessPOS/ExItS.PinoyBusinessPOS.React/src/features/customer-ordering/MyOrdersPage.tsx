import { useEffect, useMemo, useState, type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { RefreshCw, Search, Store, Users } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listMyCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
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
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { RESPONSIVE_DATA_TABLE_MIN_LG } from "@/components/exits/responsive-data-view";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { CommerceLoadMore } from "@/features/customer-ordering/personal-commerce-ui";
import {
  fulfillmentLabel,
  orderCardToneClass,
  paymentMethodLabel,
  paymentStatusLabel,
  PersonalOrderFactChips,
} from "@/features/customer-ordering/personal-order-card";
import {
  displayOrderStatusKey,
  orderStatusChipTone,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import {
  TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_FOOTER_INTENT,
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
} from "@/features/ui-standards/table-global-default";

const BUYER_SCOPE_ORG = "00000000-0000-4000-8000-000000000001";
const ORDERS_PAGE_SIZE = 40;

export function MyOrdersPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { layout } = useResponsiveDataLayout({ tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_LG });
  const [tokenReady, setTokenReady] = useState(false);
  const [search, setSearch] = useState("");

  useEffect(() => {
    void ensurePersonalBuyerPosToken().then((r) => setTokenReady(r.ok));
  }, []);

  const query = useInfiniteQuery({
    queryKey: ["personal", "my-orders"],
    enabled: tokenReady && online,
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      listMyCustomerOrders(
        sellerWorkspace(BUYER_SCOPE_ORG),
        { partyType: "Personal", page: pageParam, pageSize: ORDERS_PAGE_SIZE },
        signal,
      ),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.totalCount ? lastPage.page + 1 : undefined;
    },
  });

  const items = query.data?.pages.flatMap((page) => page.items) ?? [];
  const filteredItems = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return items;
    return items.filter((order) => {
      const statusLabel = t(displayOrderStatusKey(order) as MessageKey);
      const haystack = [
        order.branchNameSnapshot,
        statusLabel,
        paymentStatusLabel(order.paymentStatus, t),
        paymentMethodLabel(order.paymentMethod, t),
        fulfillmentLabel(order.fulfillmentType, t),
        String(order.lineCount),
      ]
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [items, search, t]);
  const pageShell =
    "personal-page personal-commerce-page my-orders-page exits-page flex min-w-0 flex-col gap-3";
  const tableChromeStyle = {
    "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qheader-ink": "var(--exits-text, #000)",
    "--qfooter-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qfooter-ink": "var(--exits-text, #000)",
  } as CSSProperties;

  if (!tokenReady || (online && query.isLoading)) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.myOrdersTitle")}
          description={t("personal.myOrdersLede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-my-orders"
        />
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (!online) {
    return (
      <div className={pageShell} data-testid="my-orders-offline">
        <PageHeader
          title={t("personal.myOrdersTitle")}
          description={t("personal.myOrdersLede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-my-orders"
        />
        <EmptyState
          variant="setup"
          align="center"
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("offline.internetRequiredTitle")}
          detail={t("offline.internetRequiredDetail")}
        />
      </div>
    );
  }

  if (query.isError) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.myOrdersTitle")}
          description={t("personal.myOrdersLede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-my-orders"
        />
        <ErrorState
          title={t("orders.error")}
          detail={query.error instanceof Error ? query.error.message : t("error.detail")}
        />
        <div className="exits-animate-toolbar">
          <ActionTileGrid
            tiles={[
              {
                key: "retry",
                label: t("orders.retry"),
                icon: RefreshCw,
                testId: "my-orders-retry",
                onClick: () => void query.refetch(),
              },
            ]}
          />
        </div>
      </div>
    );
  }

  return (
    <div className={pageShell} data-testid="my-orders-page">
      <PageHeader
        title={t("personal.myOrdersTitle")}
        description={t("personal.myOrdersLede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-my-orders"
      />

      <PersonalCommerceNav active="orders">
      {items.length === 0 ? (
        <div className="pc-empty-panel exits-animate-panel flex flex-col gap-3">
          <EmptyState
            align="center"
            icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("orders.emptyTitle")}
            detail={t("orders.emptyBuyerDetail")}
          />
          <Button asChild className="w-full gap-2" data-testid="my-orders-open-stores-empty">
            <Link to="/personal/linked-merchants">
              <Store className="size-4 shrink-0" aria-hidden />
              {t("personal.merchantsTitle")}
            </Link>
          </Button>
        </div>
      ) : (
        <section
          className="my-orders-list-section flex min-w-0 flex-col gap-3"
          aria-label={t("personal.myOrdersTitle")}
          data-testid="my-orders-list-section"
        >
          <div className="pc-store-list-section__header">
            <div className="min-w-0">
              <h2 className="pc-commerce-nav__body-title m-0">{t("personal.myOrdersTitle")}</h2>
            </div>
          </div>
          <ExitsResponsiveDataView
            layout={layout}
            testId="my-orders-responsive"
            className="my-orders-responsive"
            table={
              <ExitsTableContainer
                className="my-orders-table"
                data-shell="soft-glass"
                data-testid="my-orders-table"
                style={tableChromeStyle}
              >
                <ExitsTableToolbar
                  search={
                    <ExitsOutlineInput
                      label={t("orders.search")}
                      placeholder={t("orders.search")}
                      placeholderOnly
                      icon={Search}
                      type="search"
                      value={search}
                      onChange={setSearch}
                      clearable
                      testId="my-orders-search"
                    />
                  }
                />
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
                      <ExitsTableHead colSize="flex">{t("orders.branch")}</ExitsTableHead>
                      <ExitsTableHead>{t("orders.statusLabel")}</ExitsTableHead>
                      <ExitsTableHead>{t("orders.paymentStatus")}</ExitsTableHead>
                      <ExitsTableHead>{t("orders.paymentMethod")}</ExitsTableHead>
                      <ExitsTableHead cellAlign="center" colSize="numeric">
                        {t("orders.itemsColumn")}
                      </ExitsTableHead>
                      <ExitsTableHead>{t("orders.fulfillmentType")}</ExitsTableHead>
                      <ExitsTableHead>{t("orders.placedAt")}</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    {filteredItems.map((order) => {
                      const tone = orderStatusChipTone(order);
                      const statusLabel = t(displayOrderStatusKey(order) as MessageKey);
                      const href = `/personal/orders/${order.orderId}`;
                      return (
                        <ExitsTableRow key={order.orderId} data-testid={`my-order-table-row-${order.orderId}`}>
                          <ExitsTableCell emphasis="semibold" truncate title={order.branchNameSnapshot}>
                            <Link className="text-foreground no-underline hover:underline" to={href}>
                              {order.branchNameSnapshot}
                            </Link>
                          </ExitsTableCell>
                          <ExitsTableCell>
                            <StatusChip tone={tone} appearance="emphasis">
                              {statusLabel}
                            </StatusChip>
                          </ExitsTableCell>
                          <ExitsTableCell truncate>
                            {paymentStatusLabel(order.paymentStatus, t)}
                          </ExitsTableCell>
                          <ExitsTableCell truncate>
                            {paymentMethodLabel(order.paymentMethod, t)}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="center" colSize="numeric">
                            {order.lineCount}
                          </ExitsTableCell>
                          <ExitsTableCell truncate>
                            {fulfillmentLabel(order.fulfillmentType, t)}
                          </ExitsTableCell>
                          <ExitsTableCell truncate>
                            {new Date(order.createdAtUtc).toLocaleString()}
                          </ExitsTableCell>
                        </ExitsTableRow>
                      );
                    })}
                  </ExitsTableBody>
                </ExitsTable>
                <div
                  className="my-orders-table-footer"
                  data-testid="my-orders-table-footer"
                  data-appearance={TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE}
                  data-intent={TABLE_GLOBAL_DEFAULT_FOOTER_INTENT}
                  style={
                    {
                      "--qfooter-bg": "var(--exits-surface-elevated, #fff)",
                      "--qfooter-ink": "var(--exits-text, #000)",
                    } as CSSProperties
                  }
                >
                  <span className="my-orders-table-footer__range" data-testid="my-orders-table-range">
                    {filteredItems.length}
                    {search.trim() ? ` / ${items.length}` : ""}
                  </span>
                  {query.hasNextPage ? (
                    <CommerceLoadMore
                      label={t("inventory.loadMore")}
                      loadingLabel={t("loading.label")}
                      busy={query.isFetchingNextPage}
                      testId="my-orders-load-more"
                      onClick={() => void query.fetchNextPage()}
                    />
                  ) : null}
                </div>
              </ExitsTableContainer>
            }
            list={
              <div className="my-orders-list-shell flex min-w-0 flex-col gap-3">
                <ExitsTableToolbar
                  className="my-orders-list-toolbar"
                  search={
                    <ExitsOutlineInput
                      label={t("orders.search")}
                      placeholder={t("orders.search")}
                      placeholderOnly
                      icon={Search}
                      type="search"
                      value={search}
                      onChange={setSearch}
                      clearable
                      testId="my-orders-search-list"
                    />
                  }
                />
                <ul
                  className="utang-relationship-list pc-orders-list m-0 flex list-none flex-col p-0"
                  data-testid="my-orders-list"
                >
                  {filteredItems.map((order) => {
                    const tone = orderStatusChipTone(order);
                    const statusLabel = t(displayOrderStatusKey(order) as MessageKey);
                    return (
                      <li key={order.orderId}>
                        <Link
                          to={`/personal/orders/${order.orderId}`}
                          className={cn("pc-order-card text-foreground no-underline", orderCardToneClass(tone))}
                          data-testid="my-order-card"
                          data-shell="soft-glass"
                        >
                          <span className="pc-order-card__ribbon">
                            <span className="pc-order-card__ribbon-label truncate uppercase">{statusLabel}</span>
                          </span>
                          <div className="pc-order-card__body min-w-0 flex-1">
                            <p className="pc-order-card__title m-0 truncate">{order.branchNameSnapshot}</p>
                            <PersonalOrderFactChips
                              paymentStatus={order.paymentStatus}
                              paymentMethod={order.paymentMethod}
                              itemCount={order.lineCount}
                              fulfillmentType={order.fulfillmentType}
                              itemsLabel={t("orders.items")}
                              t={t}
                            />
                            <p className="pc-order-card__schedule m-0 truncate">
                              {new Date(order.createdAtUtc).toLocaleString()}
                            </p>
                          </div>
                        </Link>
                      </li>
                    );
                  })}
                </ul>
                <div
                  className="my-orders-table-footer"
                  data-testid="my-orders-list-footer"
                  data-appearance={TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE}
                  data-intent={TABLE_GLOBAL_DEFAULT_FOOTER_INTENT}
                  style={
                    {
                      "--qfooter-bg": "var(--exits-surface-elevated, #fff)",
                      "--qfooter-ink": "var(--exits-text, #000)",
                    } as CSSProperties
                  }
                >
                  <span className="my-orders-table-footer__range">
                    {filteredItems.length}
                    {search.trim() ? ` / ${items.length}` : ""}
                  </span>
                  {query.hasNextPage ? (
                    <CommerceLoadMore
                      label={t("inventory.loadMore")}
                      loadingLabel={t("loading.label")}
                      busy={query.isFetchingNextPage}
                      testId="my-orders-load-more-list"
                      onClick={() => void query.fetchNextPage()}
                    />
                  ) : null}
                </div>
              </div>
            }
          />
        </section>
      )}
      </PersonalCommerceNav>
    </div>
  );
}

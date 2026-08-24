import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList, Package, RefreshCw, Store, Truck, UserRoundCheck } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listMyCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { CommerceLoadMore } from "@/features/customer-ordering/personal-commerce-ui";
import {
  displayOrderStatusKey,
  orderStatusChipTone,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

const BUYER_SCOPE_ORG = "00000000-0000-4000-8000-000000000001";
const ORDERS_PAGE_SIZE = 40;

function fulfillmentLabel(type: string, t: (key: MessageKey) => string): string {
  if (type.localeCompare("Delivery", undefined, { sensitivity: "accent" }) === 0) {
    return t("orders.delivery");
  }
  if (type.localeCompare("Pickup", undefined, { sensitivity: "accent" }) === 0) {
    return t("orders.pickup");
  }
  return type;
}

function FulfillmentIcon({ type }: { type: string }) {
  const isDelivery = type.localeCompare("Delivery", undefined, { sensitivity: "accent" }) === 0;
  const Icon = isDelivery ? Truck : Package;
  return <Icon className="size-3.5 shrink-0" aria-hidden />;
}

export function MyOrdersPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const [tokenReady, setTokenReady] = useState(false);

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
  const pageShell =
    "personal-page personal-commerce-page my-orders-page exits-page flex min-w-0 flex-col gap-3";

  if (!tokenReady || (online && query.isLoading)) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.myOrdersTitle")}
          description={t("personal.myOrdersLede")}
          backTo={personalPageBackNav.home.to}
          backLabel={t(personalPageBackNav.home.labelKey)}
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
          backTo={personalPageBackNav.home.to}
          backLabel={t(personalPageBackNav.home.labelKey)}
          backTestId="page-header-back-my-orders"
        />
        <EmptyState
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
          backTo={personalPageBackNav.home.to}
          backLabel={t(personalPageBackNav.home.labelKey)}
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

      <ExitsChipBar
        variant="actions"
        className="pc-commerce-toolbar exits-animate-toolbar"
        ariaLabel={t("personal.home.quickActions")}
        testId="my-orders-toolbar"
        items={[
          {
            key: "stores",
            label: t("personal.merchantsTitle"),
            icon: <Store className="size-4 shrink-0" />,
            href: "/personal/linked-merchants",
            testId: "my-orders-open-stores",
          },
          {
            key: "orders",
            label: t("personal.myOrdersLink"),
            icon: <ClipboardList className="size-4 shrink-0" />,
            state: "active",
            testId: "my-orders-tab-orders",
          },
          {
            key: "links",
            label: t("personal.customerLinks.title"),
            icon: <UserRoundCheck className="size-4 shrink-0" />,
            href: "/personal/customer-links",
            testId: "my-orders-open-customer-links",
          },
        ]}
      />

      {items.length === 0 ? (
        <div className="pc-empty-panel exits-animate-panel flex flex-col gap-3">
          <EmptyState title={t("orders.emptyTitle")} detail={t("orders.emptyBuyerDetail")} />
          <Button asChild className="min-h-11 w-full gap-2" data-testid="my-orders-open-stores-empty">
            <Link to="/personal/linked-merchants">
              <Store className="size-4 shrink-0" aria-hidden />
              {t("personal.merchantsTitle")}
            </Link>
          </Button>
        </div>
      ) : (
        <section className="exits-animate-panel flex flex-col gap-3" aria-label={t("personal.myOrdersTitle")}>
          <h2 className="pc-section-heading">{t("personal.myOrdersTitle")}</h2>
          <ul className="flex flex-col gap-2 m-0 p-0 list-none" data-testid="my-orders-list">
            {items.map((order) => (
              <li key={order.orderId}>
                <Link
                  className="pc-order-card"
                  to={`/personal/orders/${order.orderId}`}
                  data-testid="my-order-card"
                >
                  <div className="pc-order-card__main">
                    <p className="pc-order-card__store">{order.branchNameSnapshot}</p>
                    <p className="pc-order-card__ref">
                      #{order.orderNumber} · {new Date(order.createdAtUtc).toLocaleString()}
                    </p>
                    <div className="pc-order-card__meta">
                      <StatusChip tone={orderStatusChipTone(order)}>
                        {t(displayOrderStatusKey(order) as MessageKey)}
                      </StatusChip>
                      <span className="inline-flex items-center gap-1">
                        <FulfillmentIcon type={order.fulfillmentType} />
                        {fulfillmentLabel(order.fulfillmentType, t)}
                      </span>
                      <span>
                        {order.lineCount} {t("orders.items")}
                      </span>
                    </div>
                  </div>
                  <div className="pc-order-card__aside">
                    <MoneyDisplay amount={order.total} className="pc-order-card__total" />
                    <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                  </div>
                </Link>
              </li>
            ))}
          </ul>

          {query.hasNextPage ? (
            <CommerceLoadMore
              label={t("inventory.loadMore")}
              loadingLabel={t("loading.label")}
              busy={query.isFetchingNextPage}
              testId="my-orders-load-more"
              onClick={() => void query.fetchNextPage()}
            />
          ) : null}
        </section>
      )}
    </div>
  );
}

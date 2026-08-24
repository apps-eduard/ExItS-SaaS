import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Package, RefreshCw, Store, Truck, UserRoundCheck } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listMyCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  displayOrderStatusKey,
  orderStatusChipTone,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

/** Buyer list uses a synthetic workspace org header (first seller on items or placeholder). */
const BUYER_SCOPE_ORG = "00000000-0000-4000-8000-000000000001";

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
  return <Icon className="customer-order-row__fulfillment-icon size-3.5 shrink-0" aria-hidden />;
}

export function MyOrdersPage() {
  const { t } = useI18n();
  const [tokenReady, setTokenReady] = useState(false);

  useEffect(() => {
    void ensurePersonalBuyerPosToken().then((r) => setTokenReady(r.ok));
  }, []);

  const query = useQuery({
    queryKey: ["personal", "my-orders"],
    enabled: tokenReady,
    queryFn: ({ signal }) =>
      listMyCustomerOrders(
        sellerWorkspace(BUYER_SCOPE_ORG),
        { partyType: "Personal", pageSize: 40 },
        signal,
      ),
  });

  if (!tokenReady || query.isLoading) {
    return (
      <div className="personal-page my-orders-page exits-page flex min-w-0 flex-col gap-3">
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

  if (query.isError) {
    return (
      <div className="personal-page my-orders-page exits-page flex min-w-0 flex-col gap-3">
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

  const items = query.data?.items ?? [];

  return (
    <div
      className="personal-page my-orders-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="my-orders-page"
    >
      <PageHeader
        title={t("personal.myOrdersTitle")}
        description={t("personal.myOrdersLede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-my-orders"
      />

      <div className="exits-animate-toolbar" data-testid="my-orders-toolbar">
        <ActionTileGrid
          tiles={[
            {
              key: "stores",
              label: t("personal.merchantsTitle"),
              icon: Store,
              to: "/personal/linked-merchants",
              testId: "my-orders-open-stores",
            },
            {
              key: "customer-links",
              label: t("personal.customerLinks.title"),
              icon: UserRoundCheck,
              to: "/personal/customer-links",
              testId: "my-orders-open-customer-links",
            },
          ]}
        />
      </div>

      {items.length === 0 ? (
        <div className="exits-animate-panel flex flex-col gap-3">
          <EmptyState title={t("orders.emptyTitle")} detail={t("orders.emptyBuyerDetail")} />
          <ActionTileGrid
            tiles={[
              {
                key: "stores-empty",
                label: t("personal.merchantsTitle"),
                icon: Store,
                to: "/personal/linked-merchants",
                testId: "my-orders-open-stores-empty",
                primary: true,
              },
            ]}
          />
        </div>
      ) : (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          aria-label={t("personal.myOrdersTitle")}
        >
          <h2 className="catalog-form-section__title text-muted">{t("personal.myOrdersTitle")}</h2>
          <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="my-orders-list">
            {items.map((order) => (
              <li key={order.orderId}>
                <Link
                  className="exits-list__card customer-order-row min-w-0 text-foreground no-underline"
                  to={`/personal/orders/${order.orderId}`}
                  data-testid="my-order-card"
                >
                  <div className="customer-order-row__main min-w-0">
                    <div className="customer-order-row__title-row">
                      <strong className="exits-list__name block min-w-0 truncate font-semibold">
                        #{order.orderNumber}
                      </strong>
                      <StatusChip tone={orderStatusChipTone(order)}>
                        {t(displayOrderStatusKey(order) as MessageKey)}
                      </StatusChip>
                    </div>
                    <p className="customer-order-row__meta mb-0 mt-1 flex min-w-0 items-center gap-1.5 truncate text-[length:var(--exits-text-sm)] text-muted">
                      <FulfillmentIcon type={order.fulfillmentType} />
                      <span className="min-w-0 truncate">
                        {order.branchNameSnapshot} · {fulfillmentLabel(order.fulfillmentType, t)} ·{" "}
                        {order.lineCount} {t("orders.items")}
                      </span>
                    </p>
                  </div>
                  <div className="customer-order-row__aside">
                    <MoneyDisplay amount={order.total} className="customer-order-row__total" />
                    <ChevronRight
                      className="customer-order-row__chevron size-4 shrink-0 text-muted"
                      aria-hidden
                    />
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}

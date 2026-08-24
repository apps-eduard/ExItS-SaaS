import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Ban,
  CreditCard,
  Info,
  Loader2,
  MapPin,
  Package,
  Truck,
  Wallet,
} from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import {
  cancelMyCustomerOrder,
  getMyCustomerOrder,
  sellerWorkspace,
} from "@/api/pos/pos-customer-orders-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  displayOrderStatusKey,
  orderStatusChipTone,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

function fulfillmentLabel(type: string, t: (key: MessageKey) => string): string {
  if (type.localeCompare("Delivery", undefined, { sensitivity: "accent" }) === 0) {
    return t("orders.delivery");
  }
  if (type.localeCompare("Pickup", undefined, { sensitivity: "accent" }) === 0) {
    return t("orders.pickup");
  }
  return type;
}

function isDelivery(type: string): boolean {
  return type.localeCompare("Delivery", undefined, { sensitivity: "accent" }) === 0;
}

export function MyOrderDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { orderId = "" } = useParams();
  const [tokenReady, setTokenReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    void ensurePersonalBuyerPosToken().then((r) => setTokenReady(r.ok));
  }, []);

  const query = useQuery({
    queryKey: ["personal", "my-order", orderId],
    enabled: tokenReady && Boolean(orderId) && online,
    queryFn: ({ signal }) =>
      getMyCustomerOrder(
        sellerWorkspace("00000000-0000-4000-8000-000000000001"),
        orderId,
        { partyType: "Personal" },
        signal,
      ),
  });

  async function cancelOrder() {
    if (!query.data) return;
    setBusy(true);
    setActionError(null);
    try {
      await cancelMyCustomerOrder(
        sellerWorkspace(query.data.sellerOrganizationId),
        query.data.sellerOrganizationId,
        orderId,
      );
      await query.refetch();
    } catch (err) {
      setActionError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : err instanceof Error
            ? err.message
            : t("orders.error"),
      );
    } finally {
      setBusy(false);
    }
  }

  const pageShell =
    "personal-page personal-commerce-page my-order-detail-page exits-page flex min-w-0 flex-col gap-3";

  if (!tokenReady || (online && query.isLoading)) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.myOrdersTitle")}
          backTo={personalPageBackNav.orders.to}
          backLabel={t("orders.backToQueue")}
          backTestId="page-header-back-order-detail"
        />
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (!online) {
    return (
      <div className={pageShell} data-testid="my-order-detail-offline">
        <PageHeader
          title={t("personal.myOrdersTitle")}
          backTo={personalPageBackNav.orders.to}
          backLabel={t("orders.backToQueue")}
          backTestId="page-header-back-order-detail"
        />
        <EmptyState
          title={t("offline.internetRequiredTitle")}
          detail={t("offline.internetRequiredDetail")}
        />
      </div>
    );
  }

  if (query.isError || !query.data) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.myOrdersTitle")}
          backTo={personalPageBackNav.orders.to}
          backLabel={t("orders.backToQueue")}
          backTestId="page-header-back-order-detail"
        />
        <ErrorState title={t("orders.notFound")} detail={t("orders.notFoundHelp")} />
      </div>
    );
  }

  const order = query.data;
  const canCancel =
    order.status.localeCompare("Submitted", undefined, { sensitivity: "accent" }) === 0;
  const FulfillmentIcon = isDelivery(order.fulfillmentType) ? Truck : Package;

  return (
    <div className={pageShell} data-testid="my-order-detail-page">
      <PageHeader
        title={`#${order.orderNumber}`}
        backTo={personalPageBackNav.orders.to}
        backLabel={t("orders.backToQueue")}
        backTestId="page-header-back-order-detail"
      />

      <header className="pc-order-summary-hero exits-animate-panel">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="pc-order-summary-hero__store">{order.branchNameSnapshot}</p>
            <p className="pc-order-summary-hero__meta">
              {fulfillmentLabel(order.fulfillmentType, t)} ·{" "}
              {new Date(order.createdAtUtc).toLocaleString()}
            </p>
          </div>
          <StatusChip tone={orderStatusChipTone(order)}>
            {t(displayOrderStatusKey(order) as MessageKey)}
          </StatusChip>
        </div>
      </header>

      {actionError ? <ErrorState title={t("orders.error")} detail={actionError} /> : null}

      <section className="pc-checkout-section exits-animate-panel" data-testid="order-facts">
        <h2 className="pc-checkout-section__title">{t("orders.fulfillmentType")}</h2>
        <div className="pc-facts-grid">
          <div className="pc-fact-tile">
            <span className="pc-fact-tile__label">{t("orders.branch")}</span>
            <span className="pc-fact-tile__value">{order.branchNameSnapshot}</span>
          </div>
          <div className="pc-fact-tile">
            <span className="pc-fact-tile__label">{t("orders.fulfillmentType")}</span>
            <span className="pc-fact-tile__value inline-flex items-center gap-1">
              <FulfillmentIcon className="size-3.5 shrink-0" aria-hidden />
              {fulfillmentLabel(order.fulfillmentType, t)}
            </span>
          </div>
          <div className="pc-fact-tile">
            <span className="pc-fact-tile__label">{t("orders.paymentMethod")}</span>
            <span className="pc-fact-tile__value inline-flex items-center gap-1">
              <Wallet className="size-3.5 shrink-0" aria-hidden />
              {order.paymentMethod}
            </span>
          </div>
          <div className="pc-fact-tile">
            <span className="pc-fact-tile__label">{t("orders.paymentStatus")}</span>
            <span className="pc-fact-tile__value inline-flex items-center gap-1">
              <CreditCard className="size-3.5 shrink-0" aria-hidden />
              {order.paymentStatus}
            </span>
          </div>
        </div>
      </section>

      <section className="pc-checkout-section exits-animate-panel" data-testid="order-lines">
        <h2 className="pc-checkout-section__title flex items-center gap-2">
          <Package className="size-4 shrink-0 text-muted" aria-hidden />
          {t("orders.viewDetails")}
        </h2>
        {order.lines.map((line) => (
          <div key={line.lineId} className="pc-checkout-line">
            <span className="pc-checkout-line__name">
              {line.nameSnapshot}
              <span className="pc-checkout-line__qty"> × {line.quantity}</span>
            </span>
            <MoneyDisplay amount={line.lineTotal} className="pc-checkout-line__amount" />
          </div>
        ))}
        <div className="pc-checkout-totals">
          <div className="pc-checkout-totals__row">
            <span>{t("orders.subtotal")}</span>
            <MoneyDisplay amount={order.merchandiseSubtotal} />
          </div>
          <div className="pc-checkout-totals__row">
            <span>{t("orders.deliveryFee")}</span>
            <MoneyDisplay amount={order.deliveryFee} />
          </div>
          <div className="pc-checkout-totals__row pc-checkout-totals__row--grand">
            <span>{t("orders.total")}</span>
            <MoneyDisplay amount={order.total} />
          </div>
        </div>
      </section>

      {order.delivery ? (
        <section className="pc-checkout-section exits-animate-panel" data-testid="order-delivery">
          <h2 className="pc-checkout-section__title flex items-center gap-2">
            <MapPin className="size-4 shrink-0 text-muted" aria-hidden />
            {t("orders.deliveryAddress")}
          </h2>
          <p className="m-0 font-semibold">{order.delivery.recipientName}</p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">{order.delivery.addressLine1}</p>
          {order.delivery.addressLine2 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)]">{order.delivery.addressLine2}</p>
          ) : null}
        </section>
      ) : null}

      <p className="exits-animate-toolbar m-0 flex items-start gap-2 text-[length:var(--exits-text-sm)] text-muted">
        <Info className="mt-0.5 size-4 shrink-0" aria-hidden />
        <span>{t("orders.noLiveTracking")}</span>
      </p>

      {canCancel ? (
        <div className="exits-animate-toolbar">
          <Button
            type="button"
            variant="destructive"
            className="pc-order-cancel gap-2"
            data-testid="cancel-order"
            disabled={busy}
            onClick={() => void cancelOrder()}
          >
            {busy ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Ban className="size-4 shrink-0" aria-hidden />
            )}
            {t("orders.cancel")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}

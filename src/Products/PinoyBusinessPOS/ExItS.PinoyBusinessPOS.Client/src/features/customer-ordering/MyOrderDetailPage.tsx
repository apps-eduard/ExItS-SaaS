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
  Store,
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

  if (!tokenReady || (online && query.isLoading)) {
    return (
      <div className="personal-page my-order-detail-page exits-page flex min-w-0 flex-col gap-3">
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
      <div
        className="personal-page my-order-detail-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="my-order-detail-offline"
      >
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
      <div className="personal-page my-order-detail-page exits-page flex min-w-0 flex-col gap-3">
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
    <div
      className="personal-page my-order-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="my-order-detail-page"
    >
      <PageHeader
        title={`#${order.orderNumber}`}
        subtitle={order.branchNameSnapshot}
        description={fulfillmentLabel(order.fulfillmentType, t)}
        backTo={personalPageBackNav.orders.to}
        backLabel={t("orders.backToQueue")}
        backTestId="page-header-back-order-detail"
        trailing={
          <StatusChip tone={orderStatusChipTone(order)}>
            {t(displayOrderStatusKey(order) as MessageKey)}
          </StatusChip>
        }
      />

      {actionError ? <ErrorState title={t("orders.error")} detail={actionError} /> : null}

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-2"
        data-testid="order-facts"
      >
        <h2 className="catalog-form-section__title text-muted">{t("orders.fulfillmentType")}</h2>
        <div className="customer-order-fact">
          <Store className="customer-order-fact__icon size-4 shrink-0" aria-hidden />
          <div className="customer-order-fact__body min-w-0">
            <span className="customer-order-fact__label">{t("orders.branch")}</span>
            <strong className="customer-order-fact__value truncate">{order.branchNameSnapshot}</strong>
          </div>
        </div>
        <div className="customer-order-fact">
          <FulfillmentIcon className="customer-order-fact__icon size-4 shrink-0" aria-hidden />
          <div className="customer-order-fact__body min-w-0">
            <span className="customer-order-fact__label">{t("orders.fulfillmentType")}</span>
            <strong className="customer-order-fact__value">
              {fulfillmentLabel(order.fulfillmentType, t)}
            </strong>
          </div>
        </div>
        <div className="customer-order-fact">
          <Wallet className="customer-order-fact__icon size-4 shrink-0" aria-hidden />
          <div className="customer-order-fact__body min-w-0">
            <span className="customer-order-fact__label">{t("orders.paymentMethod")}</span>
            <strong className="customer-order-fact__value truncate">{order.paymentMethod}</strong>
          </div>
        </div>
        <div className="customer-order-fact">
          <CreditCard className="customer-order-fact__icon size-4 shrink-0" aria-hidden />
          <div className="customer-order-fact__body min-w-0">
            <span className="customer-order-fact__label">{t("orders.paymentStatus")}</span>
            <strong className="customer-order-fact__value truncate">{order.paymentStatus}</strong>
          </div>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-2"
        data-testid="order-lines"
      >
        <h2 className="catalog-form-section__title flex items-center gap-2">
          <Package className="size-4 shrink-0 text-muted" aria-hidden />
          {t("orders.viewDetails")}
        </h2>
        {order.lines.map((line) => (
          <div key={line.lineId} className="customer-order-line">
            <span className="min-w-0 truncate">
              {line.nameSnapshot} × {line.quantity}
            </span>
            <MoneyDisplay amount={line.lineTotal} />
          </div>
        ))}
        <div className="customer-order-line">
          <span>{t("orders.subtotal")}</span>
          <MoneyDisplay amount={order.merchandiseSubtotal} />
        </div>
        <div className="customer-order-line">
          <span>{t("orders.deliveryFee")}</span>
          <MoneyDisplay amount={order.deliveryFee} />
        </div>
        <div className="customer-order-line customer-order-line--total">
          <span>{t("orders.total")}</span>
          <MoneyDisplay amount={order.total} />
        </div>
      </section>

      {order.delivery ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          data-testid="order-delivery"
        >
          <h2 className="catalog-form-section__title flex items-center gap-2">
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

      <p className="customer-order-note exits-animate-toolbar m-0 flex items-start gap-2 text-[length:var(--exits-text-sm)] text-muted">
        <Info className="mt-0.5 size-4 shrink-0" aria-hidden />
        <span>{t("orders.noLiveTracking")}</span>
      </p>

      {canCancel ? (
        <div className="exits-animate-toolbar customer-order-detail-actions">
          <Button
            type="button"
            variant="destructive"
            className="min-h-11 w-fit"
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

import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Ban,
  Banknote,
  CheckCircle2,
  CircleAlert,
  Clock3,
  HandCoins,
  Info,
  Loader2,
  Package,
  ShoppingBag,
  Smartphone,
  Truck,
  Users,
  type LucideIcon,
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
import { useBrowserOnline } from "@/connectivity/browser-online";
import { orderStatusChipTone } from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";

function orderCardToneClass(tone: ReturnType<typeof orderStatusChipTone>): string {
  if (tone === "success") return "pc-order-card--success";
  if (tone === "danger" || tone === "warning") return "pc-order-card--warn";
  return "pc-order-card--primary";
}

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

function paymentMethodLabel(code: string, t: (key: MessageKey) => string): string {
  if (code === "Cash") return t("orders.paymentCash");
  if (code === "ManualGCash") return t("orders.paymentGCashShort");
  return t("orders.paymentUtang");
}

function PaymentMethodIcon({ code }: { code: string }) {
  let Icon: LucideIcon = Banknote;
  if (code === "ManualGCash") Icon = Smartphone;
  else if (code === "Utang") Icon = HandCoins;
  return <Icon className="pc-order-card__fact-icon" aria-hidden />;
}

function paymentStatusLabel(status: string, t: (key: MessageKey) => string): string {
  if (status === "Paid") return t("orders.paymentPaid");
  if (status === "Pending") return t("orders.paymentAwaitingVerification");
  return t("orders.paymentUnpaid");
}

function PaymentStatusIcon({ status }: { status: string }) {
  let Icon: LucideIcon = CircleAlert;
  if (status === "Paid") Icon = CheckCircle2;
  else if (status === "Pending") Icon = Clock3;
  return <Icon className="pc-order-card__fact-icon" aria-hidden />;
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
              variant="setup"
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
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
  const tone = orderStatusChipTone(order);

  return (
    <div className={pageShell} data-testid="my-order-detail-page">
      <PageHeader
        title={`#${order.orderNumber}`}
        backTo={personalPageBackNav.orders.to}
        backLabel={t("orders.backToQueue")}
        backTestId="page-header-back-order-detail"
      />

      {actionError ? <ErrorState title={t("orders.error")} detail={actionError} /> : null}

      <section
        className={cn("pc-order-card exits-animate-panel", orderCardToneClass(tone))}
        data-testid="order-facts"
      >
        <div className="pc-order-card__body min-w-0 flex-1">
          <p className="pc-order-card__title m-0 truncate">{order.branchNameSnapshot}</p>
          <div className="pc-order-card__meta-row" data-testid="my-order-card-facts">
            <span className="pc-order-card__fact" data-testid="order-payment-status">
              <PaymentStatusIcon status={order.paymentStatus} />
              <span className="pc-order-card__fact-text truncate">
                {paymentStatusLabel(order.paymentStatus, t)}
              </span>
            </span>
            <span className="pc-order-card__fact" data-testid="order-payment-method">
              <PaymentMethodIcon code={order.paymentMethod} />
              <span className="pc-order-card__fact-text truncate">
                {paymentMethodLabel(order.paymentMethod, t)}
              </span>
            </span>
            <span className="pc-order-card__fact">
              <ShoppingBag className="pc-order-card__fact-icon" aria-hidden />
              <span className="pc-order-card__fact-text truncate">
                {order.lines.length} {t("orders.items")}
              </span>
            </span>
          </div>
          <div className="pc-order-card__fulfill">
            <span className="pc-order-card__fact">
              <FulfillmentIcon className="pc-order-card__fact-icon" aria-hidden />
              <span className="pc-order-card__fact-text truncate">
                {fulfillmentLabel(order.fulfillmentType, t)}
              </span>
            </span>
            <span className="pc-order-card__when truncate">
              {new Date(order.createdAtUtc).toLocaleString()}
            </span>
          </div>
          {order.paymentReference ? (
            <div className="pc-order-kv" data-testid="order-payment-reference">
              <span className="pc-order-kv__label">{t("orders.paymentRefShort")}</span>
              <span className="pc-order-kv__value">{order.paymentReference}</span>
            </div>
          ) : null}
          {order.requestedPickupLocal ? (
            <div className="pc-order-kv" data-testid="order-requested-pickup">
              <span className="pc-order-kv__label">{t("orders.requestedPickupTime")}</span>
              <span className="pc-order-kv__value">{order.requestedPickupLocal}</span>
            </div>
          ) : null}
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

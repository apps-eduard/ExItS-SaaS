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
  MapPin,
  Package,
  ShoppingBag,
  Smartphone,
  Truck,
  Users,
  type LucideIcon,
} from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import {
  acceptMyCustomerOrderScheduleProposal,
  cancelMyCustomerOrder,
  declineMyCustomerOrderScheduleProposal,
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
import { formatPreferredFulfillmentLabel } from "@/features/customer-ordering/preferred-fulfillment-display";
import {
  displayOrderStatusKey,
  orderStatusChipTone,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { usePosRealtimeOrderWatch } from "@/realtime/usePosRealtime";

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

  usePosRealtimeOrderWatch(query.data?.sellerOrganizationId, orderId);

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

  async function acceptScheduleProposal() {
    if (!query.data?.pendingScheduleProposal) return;
    setBusy(true);
    setActionError(null);
    try {
      await acceptMyCustomerOrderScheduleProposal(
        sellerWorkspace(query.data.sellerOrganizationId),
        query.data.sellerOrganizationId,
        orderId,
        { proposalId: query.data.pendingScheduleProposal.proposalId },
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

  async function declineScheduleProposal() {
    if (!query.data?.pendingScheduleProposal) return;
    setBusy(true);
    setActionError(null);
    try {
      await declineMyCustomerOrderScheduleProposal(
        sellerWorkspace(query.data.sellerOrganizationId),
        query.data.sellerOrganizationId,
        orderId,
        { proposalId: query.data.pendingScheduleProposal.proposalId },
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
    !order.pendingScheduleProposal
    && (order.status.localeCompare("Submitted", undefined, { sensitivity: "accent" }) === 0
      || (order.status.localeCompare("Accepted", undefined, { sensitivity: "accent" }) === 0
        && order.fulfillmentStatus.localeCompare("Pending", undefined, { sensitivity: "accent" }) === 0));
  const FulfillmentIcon = isDelivery(order.fulfillmentType) ? Truck : Package;
  const tone = orderStatusChipTone(order);
  const requestedLocal =
    order.requestedPickupLocal ?? order.requestedDeliveryLocal ?? order.effectiveFulfillmentLocal;
  const adjustmentEvents = (order.activity ?? []).filter(
    (e) =>
      e.action === "QuantityReduced"
      || e.action === "ItemCancelled"
      || e.action === "OrderTotalAdjusted"
      || e.action === "OrderCancelled"
      || e.action === "CustomerRefused"
      || e.action === "PaymentConfirmed",
  );

  return (
    <div className={pageShell} data-testid="my-order-detail-page">
      <PageHeader
        title={`#${order.orderNumber}`}
        backTo={personalPageBackNav.orders.to}
        backLabel={t("orders.backToQueue")}
        backTestId="page-header-back-order-detail"
      />

      {actionError ? <ErrorState title={t("orders.error")} detail={actionError} /> : null}

      <div className="exits-animate-toolbar" data-testid="my-order-status">
        <StatusChip tone={tone}>{t(displayOrderStatusKey(order) as MessageKey)}</StatusChip>
      </div>

      {order.hasLineAdjustments ? (
        <section
          className="pc-checkout-section exits-animate-panel"
          data-testid="order-updated-notice"
          data-shell="soft-glass"
        >
          <h2 className="pc-checkout-section__title">{t("orders.orderUpdated")}</h2>
          <ul className="m-0 list-none space-y-1 p-0 text-[length:var(--exits-text-sm)]">
            {order.lines
              .filter((line) => (line.originalQuantity ?? 0) > line.quantity)
              .map((line) => (
                <li key={line.lineId}>
                  {line.isCancelled
                    ? t("orders.lineCancelledSummary")
                        .replace("{name}", line.nameSnapshot)
                        .replace("{reason}", line.adjustmentReason ?? "")
                    : t("orders.lineAdjustedSummary")
                        .replace("{name}", line.nameSnapshot)
                        .replace("{from}", `${line.originalQuantity} ${line.unitSnapshot}`)
                        .replace("{to}", `${line.quantity} ${line.unitSnapshot}`)
                        .replace("{reason}", (line.adjustmentReason ?? "").replace(/([A-Z])/g, " $1").trim().toLowerCase())}
                </li>
              ))}
          </ul>
          {order.originalTotal > order.total ? (
            <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)]" data-testid="order-updated-total">
              {t("orders.newTotal")}: <MoneyDisplay amount={order.total} />
              <span className="text-muted">
                {" "}
                ({t("orders.originalAmount")}: <MoneyDisplay amount={order.originalTotal} />)
              </span>
            </p>
          ) : null}
        </section>
      ) : null}

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
          {order.preferredFulfillmentDate && order.preferredFulfillmentPeriod ? (
            <div className="pc-order-kv" data-testid="order-preferred-fulfillment">
              <span className="pc-order-kv__label">
                {order.fulfillmentType === "Delivery"
                  ? t("orders.preferredDeliverySummary")
                  : t("orders.preferredPickupSummary")}
              </span>
              <span className="pc-order-kv__value">
                {formatPreferredFulfillmentLabel({
                  date: order.preferredFulfillmentDate,
                  period: order.preferredFulfillmentPeriod,
                  timeZoneId: order.preferredFulfillmentTimeZoneId,
                  todayLabel: t("orders.preferredDayToday"),
                  tomorrowLabel: t("orders.preferredDayTomorrow"),
                })}
              </span>
            </div>
          ) : null}
          {!order.preferredFulfillmentPeriod && order.requestedPickupLocal ? (
            <div className="pc-order-kv" data-testid="order-requested-pickup">
              <span className="pc-order-kv__label">{t("orders.requestedPickupTime")}</span>
              <span className="pc-order-kv__value">{order.requestedPickupLocal}</span>
            </div>
          ) : null}
          {!order.preferredFulfillmentPeriod && order.requestedDeliveryLocal ? (
            <div className="pc-order-kv" data-testid="order-requested-delivery">
              <span className="pc-order-kv__label">{t("orders.requestedDeliveryTime")}</span>
              <span className="pc-order-kv__value">{order.requestedDeliveryLocal}</span>
            </div>
          ) : null}
          {order.confirmedFulfillmentLocal ? (
            <div className="pc-order-kv" data-testid="order-confirmed-schedule">
              <span className="pc-order-kv__label">{t("orders.confirmedScheduleLabel")}</span>
              <span className="pc-order-kv__value">{order.confirmedFulfillmentLocal}</span>
            </div>
          ) : null}
        </div>
      </section>

      {order.pendingScheduleProposal ? (
        <section
          className="pc-checkout-section exits-animate-panel"
          data-testid="schedule-proposal-panel"
          data-shell="soft-glass"
        >
          <h2 className="pc-checkout-section__title flex items-center gap-2">
            <Clock3 className="size-4 shrink-0 text-muted" aria-hidden />
            {t("orders.sellerProposedSchedule")}
          </h2>
          {requestedLocal ? (
            <div className="pc-order-kv" data-testid="proposal-requested">
              <span className="pc-order-kv__label">{t("orders.requestedScheduleLabel")}</span>
              <span className="pc-order-kv__value">{requestedLocal}</span>
            </div>
          ) : null}
          <div className="pc-order-kv" data-testid="proposal-proposed">
            <span className="pc-order-kv__label">{t("orders.proposedScheduleLabel")}</span>
            <span className="pc-order-kv__value">{order.pendingScheduleProposal.local}</span>
          </div>
          {order.pendingScheduleProposal.note ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="proposal-note">
              {order.pendingScheduleProposal.note}
            </p>
          ) : null}
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              type="button"
              intent="primary"
              appearance="glossy"
              data-testid="accept-schedule-proposal"
              disabled={busy}
              onClick={() => void acceptScheduleProposal()}
            >
              {busy ? (
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
              ) : null}
              {t("orders.acceptNewSchedule")}
            </Button>
            <Button
              type="button"
              intent="danger"
              appearance="glossy"
              emphasis="strong"
              data-testid="decline-schedule-proposal"
              disabled={busy}
              onClick={() => void declineScheduleProposal()}
            >
              {t("orders.declineNewSchedule")}
            </Button>
          </div>
        </section>
      ) : null}

      <section className="pc-checkout-section exits-animate-panel" data-testid="order-lines">
        <h2 className="pc-checkout-section__title flex items-center gap-2">
          <Package className="size-4 shrink-0 text-muted" aria-hidden />
          {t("orders.viewDetails")}
        </h2>
        {order.lines.map((line) => (
          <div key={line.lineId} className="pc-checkout-line">
            <span className="pc-checkout-line__name">
              {line.nameSnapshot}
              {line.isCancelled ? (
                <span className="pc-checkout-line__qty text-muted"> · {t("orders.itemCancelled")}</span>
              ) : (
                <span className="pc-checkout-line__qty">
                  {" "}
                  × {line.quantity} {line.unitSnapshot}
                </span>
              )}
              {(line.originalQuantity ?? 0) > line.quantity ? (
                <span className="block text-[length:var(--exits-text-xs)] text-muted">
                  {t("orders.originalQtySecondary")
                    .replace("{qty}", String(line.originalQuantity))
                    .replace("{unit}", line.unitSnapshot)}
                </span>
              ) : null}
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

      {adjustmentEvents.length > 0 ? (
        <section
          className="pc-checkout-section exits-animate-panel"
          data-testid="order-activity-timeline"
          data-shell="soft-glass"
        >
          <h2 className="pc-checkout-section__title">{t("orders.activityTimeline")}</h2>
          <ol className="m-0 list-none space-y-2 p-0">
            {adjustmentEvents.map((event) => (
              <li key={event.eventId} className="text-[length:var(--exits-text-sm)]">
                <p className="m-0 font-medium">{event.summary ?? event.action}</p>
                <p className="mb-0 mt-0.5 text-muted">
                  {new Date(event.atUtc).toLocaleString()}
                  {event.note ? ` · ${event.note}` : ""}
                </p>
              </li>
            ))}
          </ol>
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

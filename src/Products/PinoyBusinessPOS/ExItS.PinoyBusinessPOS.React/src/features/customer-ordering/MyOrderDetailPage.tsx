import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Ban, Clock3, Info, Loader2, MapPin, Users } from "lucide-react";
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
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  isDeliveryFulfillment,
  orderCardToneClass,
  PersonalOrderFactChips,
  PersonalOrderTimelineRow,
} from "@/features/customer-ordering/personal-order-card";
import {
  formatOrderEventDateTime,
  formatPreferredFulfillmentLabel,
} from "@/features/customer-ordering/preferred-fulfillment-display";
import {
  displayOrderStatusKey,
  orderStatusChipTone,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { usePosRealtimeOrderWatch } from "@/realtime/usePosRealtime";

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
  const tone = orderStatusChipTone(order);
  const isDeliveryOrder = isDeliveryFulfillment(order.fulfillmentType);
  const eventTimeZone =
    order.preferredFulfillmentTimeZoneId
    ?? order.confirmedFulfillmentTimeZoneId
    ?? order.effectiveFulfillmentTimeZoneId
    ?? undefined;
  const requestedLocal =
    order.requestedPickupLocal ?? order.requestedDeliveryLocal ?? order.effectiveFulfillmentLocal;
  const preferredValue =
    formatPreferredFulfillmentLabel({
      date: order.preferredFulfillmentDate,
      period: order.preferredFulfillmentPeriod,
      timeZoneId: order.preferredFulfillmentTimeZoneId,
    })
    ?? (!order.preferredFulfillmentPeriod
      ? order.requestedPickupLocal ?? order.requestedDeliveryLocal ?? null
      : null);
  const preferredLabel = isDeliveryOrder
    ? t("orders.preferredDeliverySummary")
    : t("orders.preferredPickupSummary");
  // Ready = when the seller marked the order ready (not the preferred window).
  const readyAtLabel = order.readyAtUtc
    ? formatOrderEventDateTime(order.readyAtUtc, eventTimeZone)
    : null;
  // Pickup / Delivered = when the customer collected or delivery completed.
  const completedAtIso = isDeliveryOrder ? order.deliveredAtUtc : order.collectedAtUtc;
  const completedAtLabel = completedAtIso
    ? formatOrderEventDateTime(completedAtIso, eventTimeZone)
    : null;
  const completedLabel = isDeliveryOrder
    ? t("orders.fulfillmentDeliveredLabel")
    : t("orders.fulfillmentPickedUpLabel");
  const hasSchedule = Boolean(preferredValue || readyAtLabel || completedAtLabel);
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
        data-shell="soft-glass"
      >
        <span className="pc-order-card__ribbon" data-testid="my-order-status">
          <span className="pc-order-card__ribbon-label truncate uppercase">
            {t(displayOrderStatusKey(order) as MessageKey)}
          </span>
        </span>
        <div className="pc-order-card__body min-w-0 flex-1">
          <p className="pc-order-card__title m-0 truncate">{order.branchNameSnapshot}</p>
          <PersonalOrderFactChips
            paymentStatus={order.paymentStatus}
            paymentMethod={order.paymentMethod}
            itemCount={order.lines.length}
            fulfillmentType={order.fulfillmentType}
            itemsLabel={t("orders.items")}
            t={t}
          />
          {hasSchedule ? (
            <>
              <div className="pc-order-card__divider" aria-hidden />
              <div className="pc-order-card__schedule-stack" data-testid="order-fulfillment-schedule">
                {preferredValue ? (
                  <PersonalOrderTimelineRow
                    icon="preferred"
                    label={preferredLabel}
                    value={preferredValue}
                    tone="muted"
                    testId="order-preferred-fulfillment"
                  />
                ) : null}
                {readyAtLabel ? (
                  <PersonalOrderTimelineRow
                    icon="ready"
                    label={t("orders.readyForPickup")}
                    value={readyAtLabel}
                    tone="ready"
                    testId="order-ready-at"
                  />
                ) : null}
                {completedAtLabel ? (
                  <PersonalOrderTimelineRow
                    icon="completed"
                    label={completedLabel}
                    value={completedAtLabel}
                    tone="completed"
                    testId="order-completed-at"
                  />
                ) : null}
              </div>
            </>
          ) : null}
          {order.paymentReference ? (
            <div className="pc-order-kv" data-testid="order-payment-reference">
              <span className="pc-order-kv__label">{t("orders.paymentRefShort")}</span>
              <span className="pc-order-kv__value">{order.paymentReference}</span>
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

      <section
        className="pc-order-items exits-animate-panel"
        data-testid="order-lines"
        data-shell="soft-glass"
      >
        <h2 className="pc-order-items__title">
          <span className="pc-order-items__title-text">{t("orders.orderItems")}</span>
          <span className="pc-order-items__title-rule" aria-hidden />
        </h2>
        <ul className="pc-order-items__list">
          {order.lines.map((line) => (
            <li key={line.lineId} className="pc-order-items__line">
              <span className="pc-order-items__name">
                {line.isCancelled ? (
                  <>
                    {line.nameSnapshot}
                    <span className="pc-order-items__meta"> · {t("orders.itemCancelled")}</span>
                  </>
                ) : (
                  <>
                    <span className="pc-order-items__qty">{line.quantity}x</span>{" "}
                    {line.nameSnapshot}
                    {line.unitSnapshot ? (
                      <span className="pc-order-items__meta"> ({line.unitSnapshot})</span>
                    ) : null}
                  </>
                )}
                {(line.originalQuantity ?? 0) > line.quantity ? (
                  <span className="pc-order-items__adjusted">
                    {t("orders.originalQtySecondary")
                      .replace("{qty}", String(line.originalQuantity))
                      .replace("{unit}", line.unitSnapshot)}
                  </span>
                ) : null}
              </span>
              <MoneyDisplay amount={line.lineTotal} className="pc-order-items__amount" />
            </li>
          ))}
        </ul>
        <div className="pc-order-items__totals">
          <div className="pc-order-items__row">
            <span>{t("orders.subtotal")}</span>
            <MoneyDisplay amount={order.merchandiseSubtotal} className="pc-order-items__amount" />
          </div>
          <div className="pc-order-items__row">
            <span>{t("orders.deliveryFeeLabel")}</span>
            <MoneyDisplay amount={order.deliveryFee} className="pc-order-items__amount" />
          </div>
          <div className="pc-order-items__row pc-order-items__row--grand">
            <span>{t("orders.total")}</span>
            <MoneyDisplay amount={order.total} className="pc-order-items__grand" />
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

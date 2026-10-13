import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageCustomerOrders } from "@/access/pos-capabilities";
import {
  acceptSellerCustomerOrder,
  completeSellerCustomerOrder,
  confirmSellerCustomerOrderPayment,
  declineSellerCustomerOrderPayment,
  getSellerCustomerOrder,
  markCollectedSellerCustomerOrder,
  markDeliveredSellerCustomerOrder,
  markOutForDeliverySellerCustomerOrder,
  markReadySellerCustomerOrder,
  rejectSellerCustomerOrder,
  sellerWorkspace,
  startPreparingSellerCustomerOrder,
} from "@/api/pos/pos-customer-orders-client";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  availableSellerActions,
  displayOrderStatusKey,
  type SellerOrderAction,
} from "@/features/customer-ordering/seller-order-actions";
import { ActorName } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

function paymentMethodLabel(code: string, t: (key: MessageKey) => string): string {
  if (code === "Cash") return t("orders.paymentCash");
  if (code === "ManualGCash") return t("orders.paymentGCashShort");
  return t("orders.paymentUtang");
}

function paymentStatusLabel(status: string, t: (key: MessageKey) => string): string {
  if (status === "Paid") return t("orders.paymentPaid");
  if (status === "Pending") return t("orders.paymentAwaitingVerification");
  return t("orders.paymentUnpaid");
}

function parseCashReceived(value: string): number | null {
  const cleaned = value.replace(/[₱,\s]/g, "");
  if (!/^\d+(\.\d{1,2})?$/.test(cleaned)) return null;
  return Number(cleaned);
}

const REJECT_REASONS = [
  "OutOfStock",
  "StoreTooBusy",
  "DeliveryUnavailable",
  "UnableToFulfill",
  "Other",
] as const;

type OrderActivityEvent = {
  key: string;
  labelKey: MessageKey;
  atUtc: string;
  actorId?: string | null;
};

function buildOrderActivityEvents(order: {
  createdAtUtc: string;
  acceptedAtUtc?: string | null;
  acceptedBy?: string | null;
  rejectedAtUtc?: string | null;
  rejectedBy?: string | null;
  cancelledAtUtc?: string | null;
  cancelledBy?: string | null;
  readyAtUtc?: string | null;
  readyBy?: string | null;
  outForDeliveryAtUtc?: string | null;
  outForDeliveryBy?: string | null;
  deliveredAtUtc?: string | null;
  deliveredBy?: string | null;
  collectedAtUtc?: string | null;
  collectedBy?: string | null;
  completedAtUtc?: string | null;
  completedBy?: string | null;
}): OrderActivityEvent[] {
  const candidates: Array<OrderActivityEvent | null> = [
    {
      key: "received",
      labelKey: "orders.activityReceived",
      atUtc: order.createdAtUtc,
      actorId: null,
    },
    order.acceptedAtUtc
      ? {
          key: "accepted",
          labelKey: "orders.activityAccepted",
          atUtc: order.acceptedAtUtc,
          actorId: order.acceptedBy,
        }
      : null,
    order.rejectedAtUtc
      ? {
          key: "rejected",
          labelKey: "orders.activityRejected",
          atUtc: order.rejectedAtUtc,
          actorId: order.rejectedBy,
        }
      : null,
    order.cancelledAtUtc
      ? {
          key: "cancelled",
          labelKey: "orders.activityCancelled",
          atUtc: order.cancelledAtUtc,
          actorId: order.cancelledBy,
        }
      : null,
    order.readyAtUtc
      ? {
          key: "ready",
          labelKey: "orders.activityReady",
          atUtc: order.readyAtUtc,
          actorId: order.readyBy,
        }
      : null,
    order.outForDeliveryAtUtc
      ? {
          key: "out-for-delivery",
          labelKey: "orders.activityOutForDelivery",
          atUtc: order.outForDeliveryAtUtc,
          actorId: order.outForDeliveryBy,
        }
      : null,
    order.deliveredAtUtc
      ? {
          key: "delivered",
          labelKey: "orders.activityDelivered",
          atUtc: order.deliveredAtUtc,
          actorId: order.deliveredBy,
        }
      : null,
    order.collectedAtUtc
      ? {
          key: "collected",
          labelKey: "orders.activityCollected",
          atUtc: order.collectedAtUtc,
          actorId: order.collectedBy,
        }
      : null,
    order.completedAtUtc
      ? {
          key: "completed",
          labelKey: "orders.activityCompleted",
          atUtc: order.completedAtUtc,
          actorId: order.completedBy,
        }
      : null,
  ];
  return candidates.filter((event): event is OrderActivityEvent => event != null);
}

export function SellerOrderDetailPage() {
  const { t } = useI18n();
  const { orderId = "" } = useParams();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageCustomerOrders(sessionGrant);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showReject, setShowReject] = useState(false);
  const [rejectReason, setRejectReason] = useState<string>("UnableToFulfill");
  const [rejectNotes, setRejectNotes] = useState("");
  const [cashReceived, setCashReceived] = useState("");

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? sellerWorkspace(boundWorkspace.organizationId, boundWorkspace.branchId)
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["seller-order", workspace?.organizationId, orderId],
    enabled: Boolean(workspace) && Boolean(orderId),
    queryFn: ({ signal }) => getSellerCustomerOrder(workspace!, orderId, signal),
  });

  const activityEvents = useMemo(
    () => (query.data ? buildOrderActivityEvents(query.data) : []),
    [query.data],
  );
  const actors = useActorDirectory(workspace?.organizationId, [
    ...activityEvents.map((event) => event.actorId),
    query.data?.paymentConfirmedBy,
  ]);

  async function runAction(action: SellerOrderAction) {
    if (!workspace || !canManage || busy) return;
    setBusy(true);
    setError(null);
    try {
      const runners: Record<Exclude<SellerOrderAction, "Reject">, () => Promise<unknown>> = {
        Accept: () => acceptSellerCustomerOrder(workspace, orderId),
        StartPreparing: () => startPreparingSellerCustomerOrder(workspace, orderId),
        MarkReady: () => markReadySellerCustomerOrder(workspace, orderId),
        OutForDelivery: () => markOutForDeliverySellerCustomerOrder(workspace, orderId),
        MarkDelivered: () => markDeliveredSellerCustomerOrder(workspace, orderId),
        MarkCollected: () => markCollectedSellerCustomerOrder(workspace, orderId),
        Complete: () => completeSellerCustomerOrder(workspace, orderId),
      };
      if (action === "Reject") {
        setShowReject(true);
        setBusy(false);
        return;
      }
      await runners[action]();
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  async function confirmCash() {
    if (!workspace || !canManage || !query.data || busy) return;
    const amount = parseCashReceived(cashReceived);
    if (amount === null || amount < query.data.total) {
      setError(t("orders.cashReceivedRequired"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await confirmSellerCustomerOrderPayment(workspace, orderId, amount);
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  async function confirmGCash() {
    if (!workspace || !canManage || !query.data || busy) return;
    setBusy(true);
    setError(null);
    try {
      await confirmSellerCustomerOrderPayment(workspace, orderId, query.data.total);
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  async function markPaymentNotReceived() {
    if (!workspace || !canManage || busy) return;
    setBusy(true);
    setError(null);
    try {
      await declineSellerCustomerOrderPayment(workspace, orderId);
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  async function confirmReject() {
    if (!workspace || !canManage) return;
    setBusy(true);
    setError(null);
    try {
      await rejectSellerCustomerOrder(workspace, orderId, {
        reason: rejectReason,
        notes: rejectNotes.trim() || null,
      });
      setShowReject(false);
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  function actionLabel(action: SellerOrderAction): string {
    switch (action) {
      case "Accept":
        return t("orders.accept");
      case "Reject":
        return t("orders.reject");
      case "StartPreparing":
        return t("orders.startPreparing");
      case "MarkReady":
        return query.data?.fulfillmentType.toLowerCase() === "pickup"
          ? t("orders.readyForPickup")
          : t("orders.markReady");
      case "OutForDelivery":
        return t("orders.outForDelivery");
      case "MarkDelivered":
        return t("orders.markDelivered");
      case "MarkCollected":
        return t("orders.markCollected");
      case "Complete":
        return t("orders.complete");
      default:
        return action;
    }
  }

  if (!workspace || query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
        <PageHeader
          title={t("orders.notFound")}
          description={t("orders.notFoundHelp")}
          backTo={pageBackNav.orders.to}
          backLabel={t(pageBackNav.orders.labelKey)}
          backTestId="page-header-back-orders"
        />
        <ErrorState title={t("orders.notFound")} detail={t("orders.notFoundHelp")} />
      </div>
    );
  }

  const order = query.data;
  const actions = availableSellerActions(order);

  return (
    <div
      className="customer-order-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="seller-order-detail-page"
    >
      <PageHeader
        title={`#${order.orderNumber}`}
        description={`${order.customerDisplayName} · ${order.fulfillmentType}`}
        backTo={pageBackNav.orders.to}
        backLabel={t(pageBackNav.orders.labelKey)}
        backTestId="page-header-back-orders"
      />
      <div className="customer-order-detail__status exits-animate-toolbar">
        <StatusChip tone="info">{t(displayOrderStatusKey(order) as MessageKey)}</StatusChip>
      </div>
      {error ? <ErrorState title={t("orders.error")} detail={error} /> : null}

      <section
        className="catalog-form-section exits-animate-panel gap-2 text-[length:var(--exits-text-sm)]"
        data-testid="order-facts"
      >
        <div>
          {t("orders.branch")}: <strong>{order.branchNameSnapshot}</strong>
        </div>
        {order.requestedPickupLocal ? (
          <div data-testid="order-requested-pickup">
            {t("orders.requestedPickupTime")}: <strong>{order.requestedPickupLocal}</strong>
            <p className="m-0 text-muted">{t("orders.requestedPickupNotGuaranteed")}</p>
          </div>
        ) : null}
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-2"
        data-testid="seller-order-payment"
      >
        <h2 className="catalog-form-section__title">{t("orders.paymentSection")}</h2>
        <div>
          {t("orders.paymentMethod")}: <strong>{paymentMethodLabel(order.paymentMethod, t)}</strong>
        </div>
        <div>
          {t("orders.amountDue")}: <strong><MoneyDisplay amount={order.total} /></strong>
        </div>
        {order.paymentReference ? (
          <div data-testid="order-payment-reference">
            {t("checkout.paymentReference")}: <strong>{order.paymentReference}</strong>
          </div>
        ) : null}
        <div>
          {t("orders.paymentStatus")}:{" "}
          <strong data-testid="seller-payment-status">{paymentStatusLabel(order.paymentStatus, t)}</strong>
        </div>
        {order.paymentMethod === "ManualGCash" && order.paymentStatus === "Paid" ? (
          <p className="m-0 text-muted" data-testid="gcash-manual-confirmation">
            {t("orders.gcashManuallyConfirmed")}
          </p>
        ) : null}
        {order.paymentMethod === "Utang" ? (
          <p className="m-0 text-muted">{t("orders.utangSettledOnComplete")}</p>
        ) : null}
        {order.paymentStatus === "Paid" && order.amountReceived != null ? (
          <div data-testid="seller-amount-received">
            {t("orders.cashReceived")}: <strong><MoneyDisplay amount={order.amountReceived} /></strong>
            {order.changeAmount != null ? (
              <div data-testid="seller-change">
                {t("orders.change")}: <strong><MoneyDisplay amount={order.changeAmount} /></strong>
              </div>
            ) : null}
            {order.paymentConfirmedAtUtc ? (
              <p className="mb-0 mt-1 text-muted" data-testid="seller-payment-confirmed-by">
                {t("orders.paymentConfirmed")}
                {" · "}
                {new Date(order.paymentConfirmedAtUtc).toLocaleString()}
                {order.paymentConfirmedBy ? (
                  <>
                    {" · "}
                    <ActorName
                      actorId={order.paymentConfirmedBy}
                      resolved={actors.resolve(order.paymentConfirmedBy)}
                      isLoading={actors.isResolving}
                    />
                  </>
                ) : null}
              </p>
            ) : null}
          </div>
        ) : null}
        {canManage && order.paymentMethod === "Cash" && order.paymentStatus === "Unpaid" && order.status !== "Rejected" && order.status !== "Cancelled" ? (
          <div className="flex flex-col gap-2" data-testid="cash-payment-form">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("orders.cashReceived")}</span>
              <input
                className="catalog-form-select"
                inputMode="decimal"
                data-testid="cash-received-input"
                value={cashReceived}
                onChange={(e) => setCashReceived(e.target.value)}
              />
            </label>
            {parseCashReceived(cashReceived) !== null && parseCashReceived(cashReceived)! >= order.total ? (
              <div data-testid="cash-change-preview">
                {t("orders.change")}: <strong><MoneyDisplay amount={parseCashReceived(cashReceived)! - order.total} /></strong>
              </div>
            ) : null}
            <Button
              type="button"
              data-testid="confirm-cash-received"
              disabled={busy}
              onClick={() => void confirmCash()}
            >
              {t("orders.confirmCashReceived")}
            </Button>
          </div>
        ) : null}
        {canManage && order.paymentMethod === "ManualGCash" && order.paymentStatus !== "Paid" && order.status !== "Rejected" && order.status !== "Cancelled" ? (
          <div className="catalog-form-actions customer-order-detail-actions">
            <div className="catalog-form-actions__primary">
              <Button
                type="button"
                data-testid="confirm-gcash-received"
                disabled={busy}
                onClick={() => void confirmGCash()}
              >
                {t("orders.confirmGCashReceived")}
              </Button>
            </div>
            {order.paymentStatus === "Pending" ? (
              <div className="catalog-form-actions__secondary">
                <Button
                  type="button"
                  variant="ghost"
                  data-testid="payment-not-received"
                  disabled={busy}
                  onClick={() => void markPaymentNotReceived()}
                >
                  {t("orders.paymentNotReceived")}
                </Button>
              </div>
            ) : null}
          </div>
        ) : null}
      </section>

      {order.delivery ? (
        <section className="catalog-form-section exits-animate-panel gap-2" data-testid="seller-delivery">
          <p className="m-0 font-semibold">{t("orders.deliveryAddress")}</p>
          <p className="m-0">{order.delivery.recipientName}</p>
          <p className="m-0">{order.delivery.addressLine1}</p>
          <p className="m-0 text-muted">
            {t("orders.deliveryFee")}: {money(order.delivery.finalDeliveryFee)}
          </p>
        </section>
      ) : null}

      <section className="catalog-form-section exits-animate-panel gap-2" data-testid="seller-order-lines">
        <h2 className="catalog-form-section__title">{t("orders.items")}</h2>
        <ul className="m-0 list-none space-y-2 p-0">
          {order.lines.map((line) => (
            <li key={line.lineId} className="customer-order-line">
              <span className="min-w-0 truncate">
                {line.nameSnapshot} × {line.quantity}
              </span>
              <strong>
                <MoneyDisplay amount={line.lineTotal} />
              </strong>
            </li>
          ))}
        </ul>
        <div className="customer-order-line customer-order-line--total">
          <span>{t("orders.total")}</span>
          <strong>
            <MoneyDisplay amount={order.total} />
          </strong>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-2"
        data-testid="seller-order-activity"
      >
        <h2 className="catalog-form-section__title">{t("orders.activity")}</h2>
        <ol className="m-0 list-none space-y-2 p-0">
          {activityEvents.map((event) => (
            <li
              key={event.key}
              className="text-[length:var(--exits-text-sm)]"
              data-testid={`seller-order-activity-${event.key}`}
            >
              <p className="m-0 font-medium">{t(event.labelKey)}</p>
              <p className="mb-0 mt-0.5 text-muted">
                {new Date(event.atUtc).toLocaleString()}
                {event.actorId ? (
                  <>
                    {" · "}
                    <ActorName
                      actorId={event.actorId}
                      resolved={actors.resolve(event.actorId)}
                      isLoading={actors.isResolving}
                    />
                  </>
                ) : null}
              </p>
            </li>
          ))}
        </ol>
      </section>

      {showReject ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="reject-panel">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.rejectReason")}</span>
            <select
              className="catalog-form-select"
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
            >
              {REJECT_REASONS.map((r) => (
                <option key={r} value={r}>
                  {t(`orders.reject${r}` as MessageKey)}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.rejectNotes")}</span>
            <input
              className="catalog-form-select"
              value={rejectNotes}
              onChange={(e) => setRejectNotes(e.target.value)}
            />
          </label>
          <div className="catalog-form-actions customer-order-detail-actions">
            <div className="catalog-form-actions__primary">
              <Button
                type="button"
                className="catalog-form-actions__save"
                data-testid="confirm-reject"
                disabled={busy || !canManage}
                onClick={() => void confirmReject()}
              >
                {t("orders.reject")}
              </Button>
            </div>
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__danger"
                disabled={busy}
                onClick={() => setShowReject(false)}
              >
                {t("orders.cancel")}
              </Button>
            </div>
          </div>
        </section>
      ) : (
        <>
        {order.fulfillmentType === "Pickup"
        && order.fulfillmentStatus === "ReadyForPickup"
        && (order.paymentMethod === "Cash" || order.paymentMethod === "ManualGCash")
        && order.paymentStatus !== "Paid" ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="collect-needs-payment">
            {t("orders.collectNeedsPayment")}
          </p>
        ) : null}
        <div className="catalog-form-actions customer-order-detail-actions" data-testid="seller-order-actions">
          <div className="catalog-form-actions__primary">
            {actions
              .filter((action) => action !== "Reject")
              .map((action) => (
                <Button
                  key={action}
                  type="button"
                  className="catalog-form-actions__save"
                  data-testid={`seller-action-${action.toLowerCase()}`}
                  disabled={busy || !canManage}
                  onClick={() => void runAction(action)}
                >
                  {actionLabel(action)}
                </Button>
              ))}
          </div>
          {actions.includes("Reject") ? (
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__danger"
                data-testid="seller-action-reject"
                disabled={busy || !canManage}
                onClick={() => void runAction("Reject")}
              >
                {actionLabel("Reject")}
              </Button>
            </div>
          ) : null}
        </div>
        </>
      )}
    </div>
  );
}

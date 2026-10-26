import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageCustomerOrders } from "@/access/pos-capabilities";
import {
  acceptSellerCustomerOrder,
  adjustSellerCustomerOrderLines,
  cancelSellerCustomerOrder,
  completeSellerCustomerOrder,
  confirmSellerCustomerOrderPayment,
  declineSellerCustomerOrderPayment,
  getSellerCustomerOrder,
  markCollectedSellerCustomerOrder,
  markDeliveredSellerCustomerOrder,
  markOutForDeliverySellerCustomerOrder,
  markReadySellerCustomerOrder,
  proposeSellerCustomerOrderSchedule,
  rejectSellerCustomerOrder,
  sellerWorkspace,
  startPreparingSellerCustomerOrder,
  type CustomerOrderDto,
} from "@/api/pos/pos-customer-orders-client";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
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
import { formatPreferredFulfillmentLabel } from "@/features/customer-ordering/preferred-fulfillment-display";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { usePosRealtimeOrderWatch } from "@/realtime/usePosRealtime";
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

const ADJUST_REASONS = [
  "InsufficientStock",
  "ProductUnavailable",
  "DamagedProduct",
  "QualityIssue",
  "CustomerRequested",
  "Other",
] as const;

type AdjustDraft = {
  lineId: string;
  newQuantity: number;
  reason: (typeof ADJUST_REASONS)[number];
  note: string;
};

/** Stage allows Adjust Items through Ready / ReadyForPickup (pickup handoff). */
function isAdjustableFulfillmentStage(order: CustomerOrderDto): boolean {
  if (["Completed", "Rejected", "Cancelled", "Draft"].includes(order.status)) return false;
  if (["Collected", "Delivered", "OutForDelivery"].includes(order.fulfillmentStatus)) {
    return false;
  }
  return order.status === "Submitted" || order.status === "Accepted";
}

function canAdjustOrder(order: CustomerOrderDto): boolean {
  // Paid is allowed: overpayment stays as ChangeAmount / refund-credit obligation.
  if (!isAdjustableFulfillmentStage(order)) return false;
  return order.lines.some((l) => !l.isCancelled && l.quantity > 0);
}

/** Whole-order cancel during Accepted / Preparing (not Ready refusal). */
function canSellerCancelActiveOrder(order: CustomerOrderDto): boolean {
  if (["Completed", "Rejected", "Cancelled"].includes(order.status)) return false;
  if (order.status !== "Accepted" && order.status !== "Submitted") return false;
  if (["Ready", "ReadyForPickup", "Collected", "Delivered", "OutForDelivery"].includes(order.fulfillmentStatus)) {
    return false;
  }
  return true;
}

/** Customer refusal / authorized exception once Ready. */
function canSellerRefuseAtReady(order: CustomerOrderDto): boolean {
  if (["Completed", "Rejected", "Cancelled"].includes(order.status)) return false;
  return (
    order.fulfillmentStatus === "ReadyForPickup"
    || order.fulfillmentStatus === "Ready"
  );
}

function adjustReasonLabel(reason: string, t: (key: MessageKey) => string): string {
  switch (reason) {
    case "OutOfStock":
      return t("orders.adjustReasonOutOfStock");
    case "ProductUnavailable":
      return t("orders.adjustReasonProductUnavailable");
    case "InsufficientStock":
      return t("orders.adjustReasonInsufficientStock");
    case "DamagedProduct":
      return t("orders.adjustReasonDamagedProduct");
    case "QualityIssue":
      return t("orders.adjustReasonQualityIssue");
    case "CustomerRequested":
      return t("orders.adjustReasonCustomerRequested");
    default:
      return t("orders.adjustReasonOther");
  }
}

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
  const [showPropose, setShowPropose] = useState(false);
  const [rejectReason, setRejectReason] = useState<string>("UnableToFulfill");
  const [rejectNotes, setRejectNotes] = useState("");
  const [proposeDate, setProposeDate] = useState("");
  const [proposeTime, setProposeTime] = useState("");
  const [proposeNote, setProposeNote] = useState("");
  const [cashReceived, setCashReceived] = useState("");
  const [gcashReference, setGcashReference] = useState("");
  const [gcashAmount, setGcashAmount] = useState("");
  const [showAdjust, setShowAdjust] = useState(false);
  const [adjustDrafts, setAdjustDrafts] = useState<Record<string, AdjustDraft>>({});
  const [showConfirmAdjust, setShowConfirmAdjust] = useState(false);
  const [showSellerCancel, setShowSellerCancel] = useState(false);
  const [sellerCancelReason, setSellerCancelReason] = useState("CustomerRefusedPickup");
  const [sellerCancelNotes, setSellerCancelNotes] = useState("");

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

  usePosRealtimeOrderWatch(workspace?.organizationId, orderId);

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
      const runners: Record<
        Exclude<SellerOrderAction, "Reject" | "ProposeSchedule">,
        () => Promise<unknown>
      > = {
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
        setShowPropose(false);
        setBusy(false);
        return;
      }
      if (action === "ProposeSchedule") {
        setShowPropose(true);
        setShowReject(false);
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
    const reference = gcashReference.trim();
    if (!reference) {
      setError(t("orders.gcashReferenceRequired"));
      return;
    }
    const amount = parseCashReceived(gcashAmount) ?? query.data.total;
    if (amount === null || amount <= 0) {
      setError(t("orders.gcashAmountRequired"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await confirmSellerCustomerOrderPayment(workspace, orderId, amount, reference);
      setGcashReference("");
      setGcashAmount("");
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  function openAdjustPanel() {
    if (!query.data) return;
    const drafts: Record<string, AdjustDraft> = {};
    for (const line of query.data.lines) {
      if (line.isCancelled) continue;
      drafts[line.lineId] = {
        lineId: line.lineId,
        newQuantity: line.quantity,
        reason: "InsufficientStock",
        note: "",
      };
    }
    setAdjustDrafts(drafts);
    setShowAdjust(true);
    setShowReject(false);
    setShowPropose(false);
    setShowSellerCancel(false);
    setShowConfirmAdjust(false);
  }

  function changedAdjustments(order: CustomerOrderDto): AdjustDraft[] {
    return Object.values(adjustDrafts).filter((draft) => {
      const line = order.lines.find((l) => l.lineId === draft.lineId);
      return line != null && draft.newQuantity !== line.quantity;
    });
  }

  function estimateLineTotal(
    line: CustomerOrderDto["lines"][number],
    qty: number,
  ): number {
    if (qty <= 0) return 0;
    const unit = line.unitPrice;
    const ratio = line.originalQuantity > 0 ? qty / line.originalQuantity : 0;
    const discount = Math.min(unit * qty, (line.discount ?? 0) * ratio);
    return Math.round((unit * qty - discount) * 100) / 100;
  }

  /** Preview-only totals for Confirm Order Adjustment (server remains authoritative). */
  function estimateAdjustedTotals(
    order: CustomerOrderDto,
    drafts: Record<string, AdjustDraft>,
  ): { merchandise: number; total: number } {
    let merchandise = 0;
    for (const line of order.lines) {
      if (line.isCancelled) continue;
      const draft = drafts[line.lineId];
      const qty = draft ? draft.newQuantity : line.quantity;
      merchandise += estimateLineTotal(line, qty);
    }
    const delivery = merchandise > 0 ? order.deliveryFee : 0;
    return {
      merchandise: Math.round(merchandise * 100) / 100,
      total: Math.round((merchandise + delivery) * 100) / 100,
    };
  }

  async function saveAdjustments() {
    if (!workspace || !canManage || !query.data || busy) return;
    const changed = changedAdjustments(query.data);
    if (changed.length === 0) {
      setShowAdjust(false);
      return;
    }
    for (const draft of changed) {
      if (draft.reason === "Other" && !draft.note.trim()) {
        setError(t("orders.adjustmentNoteRequired"));
        return;
      }
      if (draft.newQuantity < 0) {
        setError(t("orders.error"));
        return;
      }
    }
    setShowConfirmAdjust(true);
  }

  async function confirmAdjustments() {
    if (!workspace || !canManage || !query.data || busy) return;
    const changed = changedAdjustments(query.data);
    setBusy(true);
    setError(null);
    try {
      await adjustSellerCustomerOrderLines(workspace, orderId, {
        lines: changed.map((d) => ({
          lineId: d.lineId,
          newQuantity: d.newQuantity,
          reason: d.reason,
          note: d.note.trim() || null,
        })),
      });
      setShowAdjust(false);
      setShowConfirmAdjust(false);
      await query.refetch();
    } catch (err) {
      setError(describePosApiError(err, t, "orders.error"));
    } finally {
      setBusy(false);
    }
  }

  async function confirmSellerCancel() {
    if (!workspace || !canManage) return;
    if (
      (sellerCancelReason === "CustomerRefusedPickup"
        || sellerCancelReason === "CustomerRefusedDelivery"
        || sellerCancelReason === "Other")
      && !sellerCancelNotes.trim()
    ) {
      setError(t("orders.sellerCancelNotesRequired"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await cancelSellerCustomerOrder(workspace, orderId, {
        reason: sellerCancelReason,
        notes: sellerCancelNotes.trim() || null,
      });
      setShowSellerCancel(false);
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

  async function confirmPropose() {
    if (!workspace || !canManage) return;
    if (!proposeDate.trim() || !proposeTime.trim()) {
      setError(t("orders.scheduleDateTimeRequired"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await proposeSellerCustomerOrderSchedule(workspace, orderId, {
        date: proposeDate.trim(),
        time: proposeTime.trim(),
        note: proposeNote.trim() || null,
        clientProposalId: crypto.randomUUID(),
      });
      setShowPropose(false);
      setProposeNote("");
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
      case "ProposeSchedule":
        return t("orders.proposeNewSchedule");
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
        <StatusChip tone="info">{t(displayOrderStatusKey(order, "seller") as MessageKey)}</StatusChip>
      </div>
      {error ? <ErrorState title={t("orders.error")} detail={error} /> : null}

      <section
        className="catalog-form-section exits-animate-panel gap-2 text-[length:var(--exits-text-sm)]"
        data-testid="order-facts"
      >
        <div>
          {t("orders.branch")}: <strong>{order.branchNameSnapshot}</strong>
        </div>
        {order.preferredFulfillmentDate && order.preferredFulfillmentPeriod ? (
          <div data-testid="order-preferred-fulfillment">
            <div>
              {t("orders.customerPreferredDate")}:{" "}
              <strong>
                {formatPreferredFulfillmentLabel({
                  date: order.preferredFulfillmentDate,
                  period: order.preferredFulfillmentPeriod,
                  timeZoneId: order.preferredFulfillmentTimeZoneId,
                })?.split(" — ")[0] ?? order.preferredFulfillmentDate}
              </strong>
            </div>
            <div>
              {t("orders.customerPreferredPeriod")}:{" "}
              <strong>{order.preferredFulfillmentPeriod}</strong>
            </div>
            <p className="m-0 text-muted">{t("orders.preferredFulfillmentHint")}</p>
          </div>
        ) : null}
        {!order.preferredFulfillmentPeriod && order.requestedPickupLocal ? (
          <div data-testid="order-requested-pickup">
            {t("orders.requestedPickupTime")}: <strong>{order.requestedPickupLocal}</strong>
            <p className="m-0 text-muted">{t("orders.requestedPickupNotGuaranteed")}</p>
          </div>
        ) : null}
        {!order.preferredFulfillmentPeriod && order.requestedDeliveryLocal ? (
          <div data-testid="order-requested-delivery">
            {t("orders.requestedDeliveryTime")}: <strong>{order.requestedDeliveryLocal}</strong>
            <p className="m-0 text-muted">{t("orders.requestedDeliveryNotGuaranteed")}</p>
          </div>
        ) : null}
        {order.confirmedFulfillmentLocal ? (
          <div data-testid="order-confirmed-schedule">
            {t("orders.confirmedScheduleLabel")}: <strong>{order.confirmedFulfillmentLocal}</strong>
          </div>
        ) : null}
        {order.pendingScheduleProposal ? (
          <div data-testid="order-pending-proposal">
            <p className="m-0 font-semibold">{t("orders.pendingProposalBlocksAccept")}</p>
            <p className="m-0">
              {t("orders.proposedScheduleLabel")}:{" "}
              <strong>{order.pendingScheduleProposal.local}</strong>
            </p>
            {order.pendingScheduleProposal.note ? (
              <p className="m-0 text-muted">{order.pendingScheduleProposal.note}</p>
            ) : null}
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
          <div className="flex flex-col gap-2" data-testid="gcash-payment-form">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("orders.sellerGcashReference")}</span>
              <input
                className="catalog-form-select"
                data-testid="gcash-reference-input"
                value={gcashReference}
                onChange={(e) => setGcashReference(e.target.value)}
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("orders.sellerGcashAmount")}</span>
              <input
                className="catalog-form-select"
                inputMode="decimal"
                data-testid="gcash-amount-input"
                value={gcashAmount}
                placeholder={String(order.total)}
                onChange={(e) => setGcashAmount(e.target.value)}
              />
            </label>
            <div className="catalog-form-actions customer-order-detail-actions">
              <div className="catalog-form-actions__primary">
                <Button
                  type="button"
                  data-testid="confirm-gcash-received"
                  disabled={busy}
                  onClick={() => void confirmGCash()}
                >
                  {t("orders.confirmGcashPayment")}
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
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="catalog-form-section__title m-0">{t("orders.items")}</h2>
          {canManage && canAdjustOrder(order) && !showAdjust ? (
            <Button
              type="button"
              intent="primary"
              appearance="outline"
              data-testid="adjust-items"
              disabled={busy}
              onClick={openAdjustPanel}
            >
              {t("orders.adjustItems")}
            </Button>
          ) : null}
        </div>
        {canManage && canAdjustOrder(order) && !showAdjust && order.paymentStatus === "Paid" ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="adjust-items-paid-hint">
            {t("orders.adjustItemsPaidHint")}
          </p>
        ) : null}
        {canManage && canAdjustOrder(order) && !showAdjust && order.paymentStatus !== "Paid" ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="adjust-items-unpaid-hint">
            {t("orders.adjustItemsUnpaidHint")}
          </p>
        ) : null}

        {showAdjust ? (
          <div className="flex flex-col gap-3" data-testid="adjust-items-panel">
            {order.lines
              .filter((line) => !line.isCancelled)
              .map((line) => {
                const draft = adjustDrafts[line.lineId];
                if (!draft) return null;
                return (
                  <div
                    key={line.lineId}
                    className="flex flex-col gap-2 rounded border p-2"
                    data-testid={`adjust-line-${line.lineId}`}
                  >
                    <div className="flex w-full items-center justify-between gap-2">
                      <p className="m-0 min-w-0 truncate font-medium">
                        {line.nameSnapshot} · {line.unitSnapshot}
                      </p>
                      <strong>
                        <MoneyDisplay amount={estimateLineTotal(line, draft.newQuantity)} />
                      </strong>
                    </div>
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("orders.orderedQuantity")}: {line.originalQuantity || line.quantity}
                      {" · "}
                      {t("orders.fulfillableQuantity")}: {draft.newQuantity}
                      {" · "}
                      {t("orders.cancelledQuantity")}:{" "}
                      {Math.max(0, (line.originalQuantity || line.quantity) - draft.newQuantity)}
                    </p>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      <span>{t("orders.fulfillableQuantity")}</span>
                      <QuantityStepper
                        variant="outline"
                        min={0}
                        max={line.quantity}
                        step={1}
                        precision={0}
                        value={draft.newQuantity}
                        valueTestId={`adjust-qty-${line.lineId}`}
                        ariaLabel={t("orders.fulfillableQuantity")}
                        decreaseLabel={t("orders.reduceQuantity")}
                        increaseLabel={t("orders.fulfillableQuantity")}
                        disabled={busy}
                        onChange={(next) => {
                          setAdjustDrafts((prev) => ({
                            ...prev,
                            [line.lineId]: {
                              ...draft,
                              newQuantity: Math.min(line.quantity, Math.max(0, next)),
                            },
                          }));
                        }}
                      />
                    </label>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        intent="danger"
                        appearance="ghost"
                        data-testid={`cancel-item-${line.lineId}`}
                        disabled={busy || draft.newQuantity === 0}
                        onClick={() =>
                          setAdjustDrafts((prev) => ({
                            ...prev,
                            [line.lineId]: { ...draft, newQuantity: 0 },
                          }))
                        }
                      >
                        {t("orders.cancelItem")}
                      </Button>
                    </div>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      <span>{t("orders.adjustmentReason")}</span>
                      <select
                        className="catalog-form-select"
                        value={draft.reason}
                        onChange={(e) =>
                          setAdjustDrafts((prev) => ({
                            ...prev,
                            [line.lineId]: {
                              ...draft,
                              reason: e.target.value as AdjustDraft["reason"],
                            },
                          }))
                        }
                      >
                        {ADJUST_REASONS.map((r) => (
                          <option key={r} value={r}>
                            {adjustReasonLabel(r, t)}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      <span>{t("orders.adjustmentNote")}</span>
                      <input
                        className="catalog-form-select"
                        value={draft.note}
                        onChange={(e) =>
                          setAdjustDrafts((prev) => ({
                            ...prev,
                            [line.lineId]: { ...draft, note: e.target.value },
                          }))
                        }
                      />
                    </label>
                  </div>
                );
              })}

            {showConfirmAdjust ? (
              <div className="flex flex-col gap-2 rounded border p-2" data-testid="confirm-adjustments-panel">
                <p className="m-0 font-semibold">{t("orders.confirmAdjustmentsTitle")}</p>
                {changedAdjustments(order).map((draft) => {
                  const line = order.lines.find((l) => l.lineId === draft.lineId)!;
                  const cancelledQty = Math.max(0, line.quantity - draft.newQuantity);
                  return (
                    <p key={draft.lineId} className="m-0 text-[length:var(--exits-text-sm)]">
                      {line.nameSnapshot}: {line.quantity} → {draft.newQuantity} {line.unitSnapshot}
                      {draft.newQuantity === 0
                        ? ` (${t("orders.itemCancelled")})`
                        : ` (${t("orders.quantityReduced")}; −${cancelledQty} ${line.unitSnapshot})`}
                    </p>
                  );
                })}
                {(() => {
                  const preview = estimateAdjustedTotals(order, adjustDrafts);
                  const difference = preview.total - order.total;
                  return (
                    <>
                      <p className="m-0 text-[length:var(--exits-text-sm)]">
                        {t("orders.adjustmentOriginalTotal")}:{" "}
                        <MoneyDisplay amount={order.originalTotal || order.total} />
                      </p>
                      <p className="m-0 text-[length:var(--exits-text-sm)]">
                        {t("orders.adjustmentRevisedTotal")}: <MoneyDisplay amount={preview.total} />
                      </p>
                      <p className="m-0 text-[length:var(--exits-text-sm)]">
                        {t("orders.adjustmentDifference")}: <MoneyDisplay amount={difference} />
                      </p>
                    </>
                  );
                })()}
                <div className="catalog-form-actions customer-order-detail-actions">
                  <div className="catalog-form-actions__primary">
                    <Button
                      type="button"
                      intent="primary"
                      appearance="glossy"
                      data-testid="confirm-save-adjustments"
                      disabled={busy || !canManage}
                      onClick={() => void confirmAdjustments()}
                    >
                      {t("orders.confirmAdjustmentsConfirm")}
                    </Button>
                  </div>
                  <div className="catalog-form-actions__secondary">
                    <Button
                      type="button"
                      intent="danger"
                      appearance="ghost"
                      disabled={busy}
                      onClick={() => setShowConfirmAdjust(false)}
                    >
                      {t("orders.confirmAdjustmentsBack")}
                    </Button>
                  </div>
                </div>
              </div>
            ) : (
              <div className="catalog-form-actions customer-order-detail-actions">
                <div className="catalog-form-actions__primary">
                  <Button
                    type="button"
                    intent="primary"
                    appearance="glossy"
                    data-testid="save-adjustments"
                    disabled={busy || !canManage}
                    onClick={() => void saveAdjustments()}
                  >
                    {t("orders.saveAdjustments")}
                  </Button>
                </div>
                <div className="catalog-form-actions__secondary">
                  <Button
                    type="button"
                    intent="danger"
                    appearance="ghost"
                    className="catalog-form-actions__danger"
                    disabled={busy}
                    onClick={() => {
                      setShowAdjust(false);
                      setShowConfirmAdjust(false);
                    }}
                  >
                    {t("orders.cancel")}
                  </Button>
                </div>
              </div>
            )}
          </div>
        ) : (
          <>
            <ul className="m-0 list-none space-y-2 p-0">
              {order.lines.map((line) => (
                <li key={line.lineId} className="customer-order-line flex flex-col gap-0.5">
                  <div className="flex w-full items-center justify-between gap-2">
                    <span className="min-w-0 truncate">
                      {line.nameSnapshot}
                      {line.isCancelled ? (
                        <span className="text-muted"> · {t("orders.itemCancelled")}</span>
                      ) : (
                        <span>
                          {" "}
                          × {line.quantity} {line.unitSnapshot}
                        </span>
                      )}
                    </span>
                    <strong>
                      <MoneyDisplay amount={line.lineTotal} />
                    </strong>
                  </div>
                  {(line.originalQuantity ?? 0) > line.quantity ? (
                    <span className="text-[length:var(--exits-text-xs)] text-muted">
                      {t("orders.orderedQuantity")}: {line.originalQuantity} {line.unitSnapshot}
                      {line.cancelledQuantity > 0
                        ? ` · ${t("orders.cancelledQuantity")}: ${line.cancelledQuantity}`
                        : ""}
                      {line.adjustmentReason
                        ? ` · ${adjustReasonLabel(line.adjustmentReason, t)}`
                        : ""}
                    </span>
                  ) : null}
                </li>
              ))}
            </ul>
            <div className="customer-order-line customer-order-line--total">
              <span>{t("orders.total")}</span>
              <strong>
                <MoneyDisplay amount={order.total} />
              </strong>
            </div>
            {order.hasLineAdjustments && order.originalTotal > order.total ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="seller-original-total">
                {t("orders.originalAmount")}: <MoneyDisplay amount={order.originalTotal} />
              </p>
            ) : null}
          </>
        )}
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-2"
        data-testid="seller-order-activity"
      >
        <h2 className="catalog-form-section__title">{t("orders.activityTimeline")}</h2>
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
          {(order.activity ?? []).map((event) => (
            <li
              key={event.eventId}
              className="text-[length:var(--exits-text-sm)]"
              data-testid={`seller-order-activity-event-${event.eventId}`}
            >
              <p className="m-0 font-medium">{event.summary ?? event.action}</p>
              <p className="mb-0 mt-0.5 text-muted">
                {new Date(event.atUtc).toLocaleString()}
                {event.reasonCode ? ` · ${event.reasonCode}` : ""}
                {event.note ? ` · ${event.note}` : ""}
              </p>
            </li>
          ))}
        </ol>
      </section>

      {showSellerCancel ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="seller-cancel-panel">
          <h2 className="catalog-form-section__title">
            {sellerCancelReason === "CustomerRefusedPickup"
              || sellerCancelReason === "CustomerRefusedDelivery"
              ? t("orders.cancelCustomerRefused")
              : t("orders.cancelOrder")}
          </h2>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.sellerCancelReason")}</span>
            <select
              className="catalog-form-select"
              value={sellerCancelReason}
              onChange={(e) => setSellerCancelReason(e.target.value)}
            >
              <option value="CustomerRefusedPickup">{t("orders.cancelReasonCustomerRefusedPickup")}</option>
              <option value="CustomerRefusedDelivery">{t("orders.cancelReasonCustomerRefusedDelivery")}</option>
              <option value="SellerUnableToFulfill">{t("orders.cancelReasonSellerUnableToFulfill")}</option>
              <option value="Other">{t("orders.cancelReasonOther")}</option>
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.sellerCancelNotes")}</span>
            <input
              className="catalog-form-select"
              data-testid="seller-cancel-notes"
              value={sellerCancelNotes}
              onChange={(e) => setSellerCancelNotes(e.target.value)}
            />
          </label>
          <div className="catalog-form-actions customer-order-detail-actions">
            <div className="catalog-form-actions__primary">
              <Button
                type="button"
                data-testid="confirm-seller-cancel"
                disabled={busy || !canManage}
                onClick={() => void confirmSellerCancel()}
              >
                {t("orders.confirmCancelOrder")}
              </Button>
            </div>
            <div className="catalog-form-actions__secondary">
              <Button type="button" variant="ghost" disabled={busy} onClick={() => setShowSellerCancel(false)}>
                {t("orders.cancel")}
              </Button>
            </div>
          </div>
        </section>
      ) : null}

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
      ) : null}

      {showPropose ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="propose-schedule-panel">
          <h2 className="catalog-form-section__title">{t("orders.proposeScheduleTitle")}</h2>
          {(order.requestedPickupLocal || order.requestedDeliveryLocal || order.effectiveFulfillmentLocal) ? (
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {t("orders.requestedScheduleLabel")}:{" "}
              <strong>
                {order.requestedPickupLocal
                  ?? order.requestedDeliveryLocal
                  ?? order.effectiveFulfillmentLocal}
              </strong>
            </p>
          ) : null}
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.pickupDate")}</span>
            <input
              className="catalog-form-select"
              type="date"
              data-testid="propose-schedule-date"
              value={proposeDate}
              onChange={(e) => setProposeDate(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.pickupTime")}</span>
            <input
              className="catalog-form-select"
              type="time"
              data-testid="propose-schedule-time"
              value={proposeTime}
              onChange={(e) => setProposeTime(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.proposeScheduleNote")}</span>
            <input
              className="catalog-form-select"
              data-testid="propose-schedule-note"
              value={proposeNote}
              onChange={(e) => setProposeNote(e.target.value)}
            />
          </label>
          <div className="catalog-form-actions customer-order-detail-actions">
            <div className="catalog-form-actions__primary">
              <Button
                type="button"
                className="catalog-form-actions__save"
                data-testid="confirm-propose-schedule"
                disabled={busy || !canManage}
                onClick={() => void confirmPropose()}
              >
                {t("orders.proposeScheduleSubmit")}
              </Button>
            </div>
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__danger"
                disabled={busy}
                onClick={() => setShowPropose(false)}
              >
                {t("orders.cancel")}
              </Button>
            </div>
          </div>
        </section>
      ) : null}

      {!showReject && !showPropose && !showAdjust && !showSellerCancel ? (
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
          <div className="catalog-form-actions__secondary flex flex-wrap gap-2">
            {actions.includes("Reject") ? (
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
            ) : null}
            {canManage && canSellerCancelActiveOrder(order) ? (
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__danger"
                data-testid="seller-action-cancel-order"
                disabled={busy}
                onClick={() => {
                  setSellerCancelReason("SellerUnableToFulfill");
                  setShowSellerCancel(true);
                }}
              >
                {t("orders.cancelOrder")}
              </Button>
            ) : null}
            {canManage && canSellerRefuseAtReady(order) ? (
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__danger"
                data-testid="seller-action-customer-refused"
                disabled={busy}
                onClick={() => {
                  setSellerCancelReason(
                    order.fulfillmentType === "Delivery" || order.fulfillmentStatus === "Ready"
                      ? "CustomerRefusedDelivery"
                      : "CustomerRefusedPickup",
                  );
                  setShowSellerCancel(true);
                }}
              >
                {t("orders.cancelCustomerRefused")}
              </Button>
            ) : null}
          </div>
        </div>
        </>
      ) : null}
    </div>
  );
}

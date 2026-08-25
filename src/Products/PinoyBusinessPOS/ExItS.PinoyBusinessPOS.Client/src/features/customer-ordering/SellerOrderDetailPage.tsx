import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageCustomerOrders } from "@/access/pos-capabilities";
import {
  acceptSellerCustomerOrder,
  completeSellerCustomerOrder,
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
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

const REJECT_REASONS = [
  "OutOfStock",
  "StoreTooBusy",
  "DeliveryUnavailable",
  "UnableToFulfill",
  "Other",
] as const;

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
        <div>
          {t("orders.paymentMethod")}: <strong>{order.paymentMethod}</strong>
        </div>
        <div>
          {t("orders.paymentStatus")}: <strong>{order.paymentStatus}</strong>
        </div>
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
      )}
    </div>
  );
}

import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  acceptIncomingOrder,
  declineIncomingOrder,
  fulfillIncomingOrder,
  getIncomingOrder,
  prepareIncomingOrder,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatIncomingLineMath,
  incomingOrderStatusTone,
} from "@/features/purchasing/incoming-orders-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const DECLINE_REASONS = [
  "OutOfStock",
  "CannotFulfillQuantity",
  "PriceOrOrderIssue",
  "UnableToFulfill",
  "Other",
] as const;

function statusLabel(t: (key: MessageKey) => string, status: string, displayStatus: string): string {
  switch (status) {
    case "New":
      return t("incomingOrders.statusPending");
    case "Accepted":
      return t("incomingOrders.statusAccepted");
    case "Preparing":
      return t("incomingOrders.statusPreparing");
    case "Fulfilled":
      return t("incomingOrders.statusCompleted");
    case "Declined":
      return t("incomingOrders.statusDeclined");
    case "Withdrawn":
      return t("incomingOrders.statusWithdrawn");
    case "ChangesProposed":
      return t("incomingOrders.statusChangesProposed");
    default:
      return displayStatus || status;
  }
}

function declineReasonLabel(t: (key: MessageKey) => string, reason: string): string {
  switch (reason) {
    case "OutOfStock":
      return t("incomingOrders.declineReason.outOfStock");
    case "CannotFulfillQuantity":
      return t("incomingOrders.declineReason.cannotFulfillQuantity");
    case "PriceOrOrderIssue":
      return t("incomingOrders.declineReason.priceOrOrderIssue");
    case "UnableToFulfill":
      return t("incomingOrders.declineReason.unableToFulfill");
    case "Other":
      return t("incomingOrders.declineReason.other");
    default:
      return reason;
  }
}

export function IncomingOrderDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { connectedPurchaseOrderId } = useParams<{ connectedPurchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [showDecline, setShowDecline] = useState(false);
  const [declineReason, setDeclineReason] = useState("");
  const [declineNote, setDeclineNote] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);
  const allowManage = canManagePurchasing(sessionGrant);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming-order", connectedPurchaseOrderId],
    enabled: Boolean(workspace) && online && allowView && Boolean(connectedPurchaseOrderId),
    queryFn: ({ signal }) => getIncomingOrder(workspace!, connectedPurchaseOrderId!, signal),
  });

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "incoming-orders"] });
    await queryClient.invalidateQueries({
      queryKey: ["connected-suppliers", "incoming-order", connectedPurchaseOrderId],
    });
  }

  const acceptMutation = useMutation({
    mutationFn: () => acceptIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      setShowDecline(false);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const declineMutation = useMutation({
    mutationFn: () =>
      declineIncomingOrder(workspace!, connectedPurchaseOrderId!, {
        declineReason: declineReason || null,
        declineNote: declineNote.trim() || null,
      }),
    onSuccess: async () => {
      setActionError(null);
      setShowDecline(false);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const prepareMutation = useMutation({
    mutationFn: () => prepareIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const fulfillMutation = useMutation({
    mutationFn: () => fulfillIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const busy =
    acceptMutation.isPending ||
    declineMutation.isPending ||
    prepareMutation.isPending ||
    fulfillMutation.isPending;

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="incoming-order-detail-page">
        <PageHeader
          title={t("incomingOrders.detailTitle")}
          backTo="/purchasing/incoming-orders"
          backLabel={t("incomingOrders.backList")}
        />
        <ErrorState
          title={t("error.title")}
          detail={
            query.error instanceof PosApiError
              ? (query.error.problem.detail ?? query.error.message)
              : t("incomingOrders.notFound")
          }
        />
      </div>
    );
  }

  const order = query.data;
  const isNew = order.status === "New";
  const isAccepted = order.status === "Accepted";
  const isPreparing = order.status === "Preparing";
  const canAct = allowManage && online && !busy;

  return (
    <div
      className="incoming-order-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="incoming-order-detail-page"
    >
      <PageHeader
        title={order.buyerPoNumber ?? t("incomingOrders.unnamedPo")}
        description={t("incomingOrders.detailLede")}
        backTo="/purchasing/incoming-orders"
        backLabel={t("incomingOrders.backList")}
        backTestId="page-header-back-incoming-order-detail"
      />

      <Card className="grid gap-2 p-3" data-testid="incoming-order-summary">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("incomingOrders.buyer")}</p>
            <p className="m-0 font-semibold">
              {order.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
            </p>
          </div>
          <StatusChip tone={incomingOrderStatusTone(order.status)}>
            {statusLabel(t, order.status, order.displayStatus)}
          </StatusChip>
        </div>
        {order.supplierBranchName ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            <span className="text-muted">{t("incomingOrders.deliverTo")}: </span>
            {order.supplierBranchName}
          </p>
        ) : null}
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("incomingOrders.orderDate")}: </span>
          {order.orderDate}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("purchasing.paymentTerm")}: </span>
          {order.paymentTermLabel || order.paymentTerm}
        </p>
        {order.buyerReceivingStatus ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="incoming-order-receiving">
            {order.buyerReceivingStatus}
          </p>
        ) : null}
      </Card>

      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert" data-testid="incoming-order-action-error">
          {actionError}
        </p>
      ) : null}

      {order.lines.length === 0 ? (
        <EmptyState title={t("purchasing.linesEmpty")} detail={t("purchasing.linesRequired")} />
      ) : (
        <ul className="m-0 grid list-none gap-2 p-0" data-testid="incoming-order-lines">
          {order.lines.map((line) => (
            <li key={line.productId}>
              <Card className="grid gap-1 p-3" data-testid={`incoming-order-line-${line.productId}`}>
                <p className="m-0 font-semibold">{line.nameSnapshot}</p>
                {line.skuSnapshot ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{line.skuSnapshot}</p>
                ) : null}
                <p className="m-0 text-[length:var(--exits-text-sm)] tabular-nums">
                  {formatIncomingLineMath(line.qty, line.unitPriceSnapshot, line.lineTotal)}
                </p>
              </Card>
            </li>
          ))}
        </ul>
      )}

      <Card className="flex items-center justify-between gap-3 p-3" data-testid="incoming-order-total">
        <span className="font-medium">{t("incomingOrders.total")}</span>
        <MoneyDisplay amount={order.totalAmount} testId="incoming-order-total-amount" />
      </Card>

      {isNew && showDecline ? (
        <Card className="grid gap-3 p-3" data-testid="incoming-order-decline-form">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("incomingOrders.declineReason")}
            <select
              className="rounded-md border border-border bg-background px-3"
              value={declineReason}
              onChange={(e) => setDeclineReason(e.target.value)}
              data-testid="incoming-order-decline-reason"
            >
              <option value="">{t("incomingOrders.declineReasonOptional")}</option>
              {DECLINE_REASONS.map((reason) => (
                <option key={reason} value={reason}>
                  {declineReasonLabel(t, reason)}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("incomingOrders.declineNote")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={declineNote}
              onChange={(e) => setDeclineNote(e.target.value)}
              data-testid="incoming-order-decline-note"
            />
          </label>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              disabled={busy}
              onClick={() => setShowDecline(false)}
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={!canAct}
              data-testid="incoming-order-decline-confirm"
              onClick={() => declineMutation.mutate()}
            >
              {t("incomingOrders.decline")}
            </Button>
          </div>
        </Card>
      ) : null}

      {isNew && !showDecline ? (
        <div className="flex flex-wrap gap-2" data-testid="incoming-order-pending-actions">
          <Button
            type="button"
            variant="destructive"
            className="flex-1"
            disabled={!canAct}
            data-testid="incoming-order-decline"
            onClick={() => setShowDecline(true)}
          >
            {t("incomingOrders.decline")}
          </Button>
          <Button
            type="button"
            className="flex-1"
            disabled={!canAct}
            data-testid="incoming-order-accept"
            onClick={() => acceptMutation.mutate()}
          >
            {t("incomingOrders.accept")}
          </Button>
        </div>
      ) : null}

      {isAccepted ? (
        <Button
          type="button"
          className="w-full"
          disabled={!canAct}
          data-testid="incoming-order-prepare"
          onClick={() => prepareMutation.mutate()}
        >
          {t("incomingOrders.startPreparing")}
        </Button>
      ) : null}

      {isPreparing ? (
        <Button
          type="button"
          className="w-full"
          disabled={!canAct}
          data-testid="incoming-order-fulfill"
          onClick={() => fulfillMutation.mutate()}
        >
          {t("incomingOrders.markReady")}
        </Button>
      ) : null}

      {!allowManage ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("incomingOrders.viewOnly")}</p>
      ) : null}

      <Button asChild variant="ghost">
        <Link to="/purchasing/incoming-orders">{t("incomingOrders.backList")}</Link>
      </Button>
    </div>
  );
}

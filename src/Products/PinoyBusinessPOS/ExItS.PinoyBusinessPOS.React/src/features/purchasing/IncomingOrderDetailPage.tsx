import { Ban, Check, AlertTriangle, ClipboardList, PackageCheck, Play, Building2, ArrowLeft } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  acceptIncomingOrder,
  closeIncomingOrderRemaining,
  confirmIncomingOrderReceiptSettlement,
  declineIncomingOrder,
  fulfillIncomingOrder,
  getIncomingOrder,
  prepareIncomingOrder,
  proposeIncomingOrderChanges,
  withdrawIncomingOrderProposal,
  type ConnectedPurchaseOrderLine,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { incomingOrderStatusTone, isIncomingOrderAwaitingPayment } from "@/features/purchasing/incoming-orders-helpers";
import {
  buildIncomingOrderExportModel,
  downloadIncomingOrderCsv,
  downloadIncomingOrderPdf,
  downloadIncomingOrderXlsx,
  printIncomingOrderDocument,
} from "@/features/purchasing/incoming-order-table-output";
import {
  countShortageLines,
  defaultConfirmQty,
  formatStockQtyLabel,
  hasMaterialProposalChanges,
  lineHasShortage,
} from "@/features/purchasing/incoming-order-stock-review";
import { buildProposalRevisionFromConnectedOrder } from "@/features/purchasing/po-proposal-revision";
import { PoProposalRevisionPanel } from "@/features/purchasing/PoProposalRevisionPanel";
import { PoDocumentExportActions } from "@/features/purchasing/PoDocumentExportActions";
import { IncomingOrderFulfillmentProgress } from "@/features/purchasing/IncomingOrderFulfillmentProgress";
import { IncomingOrderBuyerReceipts } from "@/features/purchasing/IncomingOrderBuyerReceipts";
import { PoDocumentLineItems } from "@/features/purchasing/PoDocumentLineItems";
import { PoDocumentSummary } from "@/features/purchasing/PoDocumentSummary";
import { PoDocumentTotals } from "@/features/purchasing/PoDocumentTotals";
import type { PoDocumentLine } from "@/features/purchasing/po-document-types";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { roundMoneyAmount } from "@/lib/money-input";
import { maxQuantityDecimals } from "@/lib/quantity-rules";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function lineQtyLabel(line: ConnectedPurchaseOrderLine): string {
  return formatStockQtyLabel(line.qty, line.unitOfMeasureCode);
}

function confirmedLineTotal(unitPrice: number, confirmQty: number): number {
  return roundMoneyAmount(unitPrice * Math.max(0, confirmQty));
}

const DECLINE_REASONS = [
  "OutOfStock",
  "CannotFulfillQuantity",
  "PriceOrOrderIssue",
  "UnableToFulfill",
  "Other",
] as const;

function statusLabel(t: (key: MessageKey) => string, status: string, displayStatus: string): string {
  const key = displayStatus || status;
  switch (key) {
    case "Completed":
    case "ReceivedByBuyer":
    case "Received":
      return t("incomingOrders.statusCompleted");
    case "CompletedRemainingCancelled":
      return t("incomingOrders.statusCompletedRemainingCancelled");
    case "ReceivedAwaitingPayment":
      return t("incomingOrders.statusReceivedAwaitingPayment");
    case "ReceivedWithIssues":
      return t("incomingOrders.statusReceivedWithIssues");
    case "PartiallyReceived":
      return t("incomingOrders.statusPartiallyReceived");
    case "AwaitingBuyerReceipt":
    case "Shipped":
      return t("incomingOrders.statusAwaitingReceipt");
    case "Ready":
      return t("incomingOrders.statusReady");
    case "Preparing":
      return t("incomingOrders.statusPreparing");
    case "Accepted":
      return t("incomingOrders.statusAccepted");
    case "New":
      return t("incomingOrders.statusPending");
    case "Declined":
      return t("incomingOrders.statusDeclined");
    case "Withdrawn":
      return t("incomingOrders.statusWithdrawn");
    case "ChangesProposed":
      return t("incomingOrders.statusChangesProposed");
    default:
      break;
  }
  switch (status) {
    case "New":
      return t("incomingOrders.statusPending");
    case "Accepted":
      return t("incomingOrders.statusAccepted");
    case "Preparing":
      return t("incomingOrders.statusPreparing");
    case "Fulfilled":
      return t("incomingOrders.statusAwaitingReceipt");
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

function toDocumentLines(lines: ConnectedPurchaseOrderLine[]): PoDocumentLine[] {
  return lines.map((line) => ({
    id: line.productId,
    productName: line.nameSnapshot,
    sku: line.skuSnapshot,
    quantityLabel: lineQtyLabel(line),
    unitCost: line.unitPriceSnapshot,
    lineTotal: line.lineTotal,
  }));
}

function ShortageQty({
  label,
  warning,
  testId,
}: {
  label: string;
  warning: boolean;
  testId?: string;
}) {
  return (
    <span
      className={cn(
        "incoming-order-stock-qty inline-flex items-center gap-1 tabular-nums",
        warning && "incoming-order-stock-qty--warning",
      )}
      data-testid={testId}
    >
      <span>{label}</span>
      {warning ? <AlertTriangle className="size-3.5 shrink-0" strokeWidth={2} aria-hidden /> : null}
    </span>
  );
}

export function IncomingOrderDetailPage() {
  const { t } = useI18n();
  const smartBack = usePageSmartBack({
    fallback: "incomingOrders",
    backLabel: t("shell.back"),
    backTestId: "page-header-back-incoming-order-detail",
  });
  const { showToast } = useToast();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { connectedPurchaseOrderId } = useParams<{ connectedPurchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [showDecline, setShowDecline] = useState(false);
  const [declineReason, setDeclineReason] = useState("");
  const [declineNote, setDeclineNote] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);
  const [proposeValidation, setProposeValidation] = useState<string | null>(null);
  const [confirmQtys, setConfirmQtys] = useState<Record<string, number>>({});
  const [showMarkRemainingConfirm, setShowMarkRemainingConfirm] = useState(false);
  const [showCloseRemaining, setShowCloseRemaining] = useState(false);
  const [closeRemainingReason, setCloseRemainingReason] = useState("");
  const [showConfirmPayment, setShowConfirmPayment] = useState(false);
  const [settlementAmount, setSettlementAmount] = useState("");
  const [settlementMethod, setSettlementMethod] = useState("Cash");
  const [settlementReference, setSettlementReference] = useState("");
  const [settlementSellerRemarks, setSettlementSellerRemarks] = useState("");
  const [settlementCheckCleared, setSettlementCheckCleared] = useState(false);

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

  useEffect(() => {
    if (!query.data) {
      return;
    }
    setConfirmQtys((prev) => {
      const next: Record<string, number> = { ...prev };
      for (const line of query.data.lines) {
        if (next[line.productId] == null) {
          next[line.productId] = defaultConfirmQty(line);
        }
      }
      return next;
    });
  }, [query.data]);

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "incoming-orders"] });
    await queryClient.invalidateQueries({
      queryKey: ["connected-suppliers", "incoming-order", connectedPurchaseOrderId],
    });
    await queryClient.invalidateQueries({ queryKey: ["purchasing-hub"] });
  }

  const acceptMutation = useMutation({
    mutationFn: () => acceptIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      setProposeValidation(null);
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
      setProposeValidation(null);
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
      setShowMarkRemainingConfirm(false);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const closeRemainingMutation = useMutation({
    mutationFn: () =>
      closeIncomingOrderRemaining(workspace!, connectedPurchaseOrderId!, {
        reason: closeRemainingReason.trim(),
      }),
    onSuccess: async () => {
      setActionError(null);
      setShowCloseRemaining(false);
      setCloseRemainingReason("");
      setShowMarkRemainingConfirm(false);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const confirmPaymentMutation = useMutation({
    mutationFn: () => {
      const parsed = Number.parseFloat(settlementAmount);
      return confirmIncomingOrderReceiptSettlement(workspace!, connectedPurchaseOrderId!, {
        settledAmount: Number.isFinite(parsed) ? parsed : null,
        paymentMethod: settlementMethod,
        reference: settlementReference.trim() || null,
        sellerRemarks: settlementSellerRemarks.trim() || null,
        checkClearingStatus:
          settlementMethod === "Check" ? (settlementCheckCleared ? "Cleared" : "PendingClearing") : null,
      });
    },
    onSuccess: async () => {
      setActionError(null);
      setShowConfirmPayment(false);
      setSettlementReference("");
      setSettlementSellerRemarks("");
      setSettlementCheckCleared(false);
      showToast({
        title: t("incomingOrders.settlementConfirmed"),
        tone: "success",
      });
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const proposeMutation = useMutation({
    mutationFn: (lines: Array<{ productId: string; proposedQty: number; unavailable: boolean }>) =>
      proposeIncomingOrderChanges(workspace!, connectedPurchaseOrderId!, { lines }),
    onSuccess: async () => {
      setActionError(null);
      setProposeValidation(null);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const withdrawProposalMutation = useMutation({
    mutationFn: () => withdrawIncomingOrderProposal(workspace!, connectedPurchaseOrderId!),
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
    fulfillMutation.isPending ||
    closeRemainingMutation.isPending ||
    confirmPaymentMutation.isPending ||
    proposeMutation.isPending ||
    withdrawProposalMutation.isPending;

  function buildExportModel() {
    if (!query.data) {
      throw new Error("Order is not loaded");
    }
    return buildIncomingOrderExportModel(query.data, query.data.lines, new Set());
  }

  async function runOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const model = buildExportModel();
      if (action === "csv") {
        downloadIncomingOrderCsv(model);
        return;
      }
      if (action === "xlsx") {
        downloadIncomingOrderXlsx(model);
        return;
      }
      if (action === "pdf") {
        downloadIncomingOrderPdf(model);
        return;
      }
      printIncomingOrderDocument();
    } catch {
      showToast({
        title: t("exitsTable.outputFailed"),
        tone: "error",
      });
    }
  }

  function BackActionButton() {
    return (
      <Button
        type="button"
        variant="ghost"
        onClick={smartBack.onBack}
        aria-label={t("shell.back")}
        data-testid="incoming-order-footer-back"
      >
        <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
        {t("incomingOrders.backList")}
      </Button>
    );
  }

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
          {...smartBack}
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
  const isChangesProposed = order.status === "ChangesProposed";
  const isAccepted = order.status === "Accepted";
  const isPreparing = order.status === "Preparing";
  const isFulfilled = order.status === "Fulfilled";
  const isDeclined = order.status === "Declined";
  const outstandingQty = order.buyerOutstandingQty ?? 0;
  const needsPrepareRemaining =
    (isAccepted || isFulfilled) &&
    Boolean(order.fulfilledAtUtc) &&
    outstandingQty > 0 &&
    (order.displayStatus === "PartiallyReceived" ||
      order.buyerReceivingStatus === "PartiallyReceived");
  const showFulfillmentProgress =
    order.displayStatus === "PartiallyReceived" ||
    order.displayStatus === "Completed" ||
    order.displayStatus === "CompletedRemainingCancelled" ||
    order.displayStatus === "ReceivedWithIssues" ||
    (order.buyerReceipts?.length ?? 0) > 0 ||
    order.lines.some((line) => (line.goodReceivedQty ?? 0) > 0 || (line.outstandingQty ?? 0) > 0);
  const remainingLines = order.lines.filter((line) => (line.outstandingQty ?? 0) > 0);
  const remainingTotal = remainingLines.reduce((sum, line) => sum + (line.outstandingQty ?? 0), 0);
  const isMarkRemainingReady = isPreparing && Boolean(order.fulfilledAtUtc);
  const canAct = allowManage && online && !busy;
  const hasShortage = order.lines.some(lineHasShortage);
  const shortageCount = countShortageLines(order.lines);
  const hasMaterialChanges = hasMaterialProposalChanges(order.lines, confirmQtys);
  const confirmedOrderTotal = order.lines.reduce((sum, line) => {
    const confirm = confirmQtys[line.productId] ?? defaultConfirmQty(line);
    return sum + confirmedLineTotal(line.unitPriceSnapshot, confirm);
  }, 0);
  const proposalRevision = isChangesProposed
    ? buildProposalRevisionFromConnectedOrder(order)
    : null;
  const printModel = buildIncomingOrderExportModel(order, order.lines, new Set());
  const documentLines = toDocumentLines(order.lines);
  const resolvedStatusLabel = statusLabel(t, order.status, order.displayStatus);
  const statusTone = incomingOrderStatusTone(order.status, order.displayStatus);

  const summaryFields = [
    ...(order.supplierBranchName
      ? [
          {
            key: "fulfill",
            label: t("incomingOrders.deliverTo"),
            value: order.supplierBranchName,
          },
        ]
      : []),
    {
      key: "payment",
      label: t("purchasing.paymentTerm"),
      value: order.paymentTermLabel || order.paymentTerm || "—",
    },
    {
      key: "orderDate",
      label: t("incomingOrders.orderDate"),
      value: order.orderDate,
    },
  ];

  function tryProposeChanges() {
    if (!hasMaterialChanges) {
      setProposeValidation(t("incomingOrders.proposeNoMaterialChanges"));
      return;
    }
    setProposeValidation(null);
    const lines = order.lines.map((line) => {
      const proposedQty = confirmQtys[line.productId] ?? defaultConfirmQty(line);
      return {
        productId: line.productId,
        proposedQty,
        unavailable: proposedQty <= 0,
      };
    });
    proposeMutation.mutate(lines);
  }

  return (
    <div
      className="incoming-order-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="incoming-order-detail-page"
    >
      <div className="incoming-order-print-root" data-testid="incoming-order-print-root" aria-hidden>
        <h1>{printModel.poNumber}</h1>
        <p>Buyer: {printModel.buyer}</p>
        {printModel.branch ? <p>Fulfill from: {printModel.branch}</p> : null}
        <p>Order date: {printModel.orderDate}</p>
        {printModel.paymentTerm ? <p>Payment term: {printModel.paymentTerm}</p> : null}
        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>SKU</th>
              <th>Quantity</th>
              <th>Unit cost</th>
              <th>Line total</th>
            </tr>
          </thead>
          <tbody>
            {printModel.lines.map((line) => (
              <tr key={line.productId}>
                <td>{line.product}</td>
                <td>{line.sku || "—"}</td>
                <td>{line.unit ? `${line.quantity} ${line.unit}` : line.quantity}</td>
                <td>{formatPeso(line.unitCost)}</td>
                <td>{formatPeso(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <p>
          <strong>{t("incomingOrders.orderTotal")}</strong> {formatPeso(printModel.orderTotal)}
        </p>
      </div>

      <PageHeader
        title={order.buyerPoNumber ?? t("incomingOrders.unnamedPo")}
        {...smartBack}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip tone={statusTone}>{resolvedStatusLabel}</StatusChip>
            {order.displayStatus === "PartiallyReceived" && outstandingQty > 0 ? (
              <span className="text-[length:var(--exits-text-sm)] text-muted" data-testid="incoming-order-outstanding">
                {t("purchasing.outstanding")}: {outstandingQty}
              </span>
            ) : null}
            <PoDocumentExportActions
              printLabel={t("exitsTable.print")}
              exportLabel={t("purchasing.export")}
              csvLabel={t("exitsTable.exportCsv")}
              xlsxLabel={t("exitsTable.exportExcel")}
              pdfLabel={t("exitsTable.exportPdf")}
              onPrint={() => runOutput("print")}
              onCsv={() => runOutput("csv")}
              onXlsx={() => runOutput("xlsx")}
              onPdf={() => runOutput("pdf")}
            />
          </div>
        }
      />

      {isNew ? (
        <Notice tone="info" testId="incoming-order-accept-notice">
          {t("incomingOrders.detailLede")}
        </Notice>
      ) : null}

      {order.displayStatus === "CompletedRemainingCancelled" || order.remainingClosedAtUtc ? (
        <Card className="flex flex-col gap-2 p-3" data-testid="incoming-order-short-close-summary">
          <div className="flex flex-wrap items-center gap-2">
            <p className="m-0 font-medium">{t("incomingOrders.shortClosed")}</p>
            <StatusChip tone="success">
              {t("incomingOrders.statusCompletedRemainingCancelled")}
            </StatusChip>
          </div>
          <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)] tabular-nums">
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.finalAcceptedValue")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay amount={order.finalAcceptedValue ?? 0} />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.cancelledRemaining")}</dt>
              <dd className="m-0">
                <MoneyDisplay amount={order.cancelledRemainingValue ?? 0} />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.paymentReceived")}</dt>
              <dd className="m-0">
                <MoneyDisplay amount={order.amountPaid ?? 0} />
              </dd>
            </div>
            {(order.refundDueAmount ?? 0) > 0 ? (
              <div className="flex justify-between gap-2">
                <dt>{t("incomingOrders.refundDue")}</dt>
                <dd className="m-0 font-medium">
                  <MoneyDisplay amount={order.refundDueAmount ?? 0} />
                </dd>
              </div>
            ) : (order.balanceDue ?? 0) > 0 ? (
              <div className="flex justify-between gap-2">
                <dt>{t("incomingOrders.balanceDue")}</dt>
                <dd className="m-0 font-medium">
                  <MoneyDisplay amount={order.balanceDue ?? 0} />
                </dd>
              </div>
            ) : null}
          </dl>
          {order.remainingClosedReason?.trim() ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("incomingOrders.closeRemainingReason")}: {order.remainingClosedReason.trim()}
            </p>
          ) : null}
        </Card>
      ) : null}

      {isIncomingOrderAwaitingPayment(order) ? (
        <Card className="flex flex-col gap-3 p-3" data-testid="incoming-order-awaiting-payment">
          <div className="flex flex-wrap items-center gap-2">
            <p className="m-0 font-medium">{t("incomingOrders.awaitingPaymentTitle")}</p>
            <StatusChip tone="warning">{t("incomingOrders.statusReceivedAwaitingPayment")}</StatusChip>
          </div>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("incomingOrders.awaitingPaymentSellerBody")}
          </p>
          <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)] tabular-nums">
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.orderTotal")}</dt>
              <dd className="m-0">
                <MoneyDisplay
                  amount={
                    order.confirmedTotalAmount > 0 ? order.confirmedTotalAmount : order.totalAmount
                  }
                />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.goodReceivedValue")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay amount={order.finalAcceptedValue ?? 0} />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.paymentReceived")}</dt>
              <dd className="m-0">
                <MoneyDisplay amount={order.amountPaid ?? 0} />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.remainingDue")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay
                  amount={order.remainingDueAmount ?? order.balanceDue ?? 0}
                />
              </dd>
            </div>
            {(order.cancelledRemainingValue ?? 0) > 0 ? (
              <div className="flex justify-between gap-2">
                <dt>{t("incomingOrders.cancelledRemaining")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={order.cancelledRemainingValue ?? 0} />
                </dd>
              </div>
            ) : null}
          </dl>
          {order.buyerReceiptRemarks?.trim() ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("incomingOrders.buyerReceiptRemarks")}: {order.buyerReceiptRemarks.trim()}
            </p>
          ) : null}
          {canAct && !showConfirmPayment ? (
            <Button
              type="button"
              data-testid="incoming-order-confirm-payment-btn"
              onClick={() => {
                setSettlementAmount(
                  String(order.remainingDueAmount ?? order.balanceDue ?? 0),
                );
                setSettlementMethod(
                  order.paymentTerm === "ManualGCash"
                    ? "ManualGCash"
                    : order.paymentTerm === "BankTransfer"
                      ? "BankTransfer"
                      : order.paymentTerm === "BankDeposit"
                        ? "BankDeposit"
                        : order.paymentTerm === "Check"
                          ? "Check"
                          : "Cash",
                );
                setShowConfirmPayment(true);
              }}
            >
              {t("incomingOrders.confirmPayment")}
            </Button>
          ) : null}
        </Card>
      ) : null}

      {showConfirmPayment ? (
        <Card className="flex flex-col gap-3 p-4" data-testid="incoming-order-confirm-payment-dialog">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("incomingOrders.confirmPaymentTitle")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("incomingOrders.confirmPaymentBody")}
          </p>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("incomingOrders.settlementAmount")}</span>
            <input
              className="rounded border border-[color:var(--exits-border)] bg-transparent px-2 py-1.5"
              inputMode="decimal"
              data-testid="incoming-order-settlement-amount"
              value={settlementAmount}
              onChange={(e) => setSettlementAmount(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("incomingOrders.settlementMethod")}</span>
            <select
              className="rounded border border-[color:var(--exits-border)] bg-transparent px-2 py-1.5"
              data-testid="incoming-order-settlement-method"
              value={settlementMethod}
              onChange={(e) => setSettlementMethod(e.target.value)}
            >
              <option value="Cash">{t("purchasing.paymentMethod.cod")}</option>
              <option value="ManualGCash">{t("purchasing.paymentMethod.gcash")}</option>
              <option value="BankTransfer">{t("purchasing.paymentMethod.bankTransfer")}</option>
              <option value="BankDeposit">{t("purchasing.paymentMethod.bankDeposit")}</option>
              <option value="Check">{t("purchasing.paymentMethod.check")}</option>
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("incomingOrders.settlementReference")}</span>
            <input
              className="rounded border border-[color:var(--exits-border)] bg-transparent px-2 py-1.5"
              data-testid="incoming-order-settlement-reference"
              value={settlementReference}
              onChange={(e) => setSettlementReference(e.target.value)}
            />
          </label>
          {settlementMethod === "Check" ? (
            <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <input
                type="checkbox"
                data-testid="incoming-order-settlement-check-cleared"
                checked={settlementCheckCleared}
                onChange={(e) => setSettlementCheckCleared(e.target.checked)}
              />
              <span>{t("incomingOrders.settlementCheckCleared")}</span>
            </label>
          ) : null}
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("incomingOrders.sellerRemarks")}</span>
            <textarea
              className="min-h-20 rounded border border-[color:var(--exits-border)] bg-transparent px-2 py-1.5"
              data-testid="incoming-order-settlement-remarks"
              value={settlementSellerRemarks}
              onChange={(e) => setSettlementSellerRemarks(e.target.value)}
            />
          </label>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="secondary"
              disabled={confirmPaymentMutation.isPending}
              onClick={() => setShowConfirmPayment(false)}
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              data-testid="incoming-order-settlement-submit"
              disabled={
                !canAct ||
                confirmPaymentMutation.isPending ||
                (settlementMethod === "Check" && !settlementCheckCleared)
              }
              onClick={() => confirmPaymentMutation.mutate()}
            >
              {t("incomingOrders.confirmPayment")}
            </Button>
          </div>
        </Card>
      ) : null}

      <PoDocumentSummary
        counterpartyLabel={t("incomingOrders.buyer")}
        counterpartyIcon={<Building2 className="size-5" strokeWidth={1.75} />}
        counterpartyName={order.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
        status={{ label: resolvedStatusLabel, tone: statusTone }}
        fields={summaryFields}
        testId="incoming-order-summary"
        footer={
          <>
            {order.buyerReceivingStatus ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="incoming-order-receiving"
              >
                {statusLabel(t, order.status, order.buyerReceivingStatus)}
              </p>
            ) : null}
            {isDeclined && (order.declineReason || order.declineNote) ? (
              <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="incoming-order-decline-info">
                {order.declineReason
                  ? declineReasonLabel(t, order.declineReason)
                  : null}
                {order.declineReason && order.declineNote ? " — " : null}
                {order.declineNote}
              </p>
            ) : null}
          </>
        }
      />

      {actionError ? (
        <Notice tone="danger" testId="incoming-order-action-error">
          {actionError}
        </Notice>
      ) : null}

      {order.lines.length === 0 ? (
        <EmptyState
          variant="setup"
          align="center"
          icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
          title={t("purchasing.linesEmpty")}
          detail={t("purchasing.linesRequired")}
        />
      ) : isNew ? (
        <>
          <div
            className={cn(
              "incoming-order-stock-summary",
              hasShortage
                ? "incoming-order-stock-summary--adjustment"
                : "incoming-order-stock-summary--available",
            )}
            data-testid="incoming-order-stock-summary"
            data-tone={hasShortage ? "adjustment" : "available"}
          >
            <p className="incoming-order-stock-summary__title m-0">
              {hasShortage
                ? t("incomingOrders.stockAdjustmentRequired")
                : t("incomingOrders.stockAvailable")}
            </p>
            <p className="incoming-order-stock-summary__detail m-0">
              {hasShortage
                ? t("incomingOrders.stockAdjustmentDetail")
                    .replace("{shortageCount}", String(shortageCount))
                    .replace("{totalCount}", String(order.lines.length))
                : t("incomingOrders.stockAvailableDetail")}
            </p>
            <p className="incoming-order-stock-summary__hint m-0">
              {hasShortage
                ? t("incomingOrders.stockAdjustmentHint")
                : t("incomingOrders.stockAvailableHint")}
            </p>
          </div>

          <section
            className="incoming-order-stock-lines flex flex-col gap-2"
            data-testid="incoming-order-stock-lines"
          >
            <div className="po-document-lines__table-wrap incoming-order-stock-lines__table-wrap">
              <table className="po-document-lines__table incoming-order-stock-lines__table">
                <thead>
                  <tr>
                    <th scope="col">{t("purchasing.colProduct")}</th>
                    <th scope="col" className="po-document-lines__num">
                      {t("incomingOrders.colRequestedQty")}
                    </th>
                    <th scope="col" className="po-document-lines__num">
                      {t("incomingOrders.colReserved")}
                    </th>
                    <th scope="col" className="po-document-lines__num">
                      {t("incomingOrders.colAvailableStock")}
                    </th>
                    <th scope="col" className="po-document-lines__num">
                      {t("incomingOrders.colConfirmQty")}
                    </th>
                    <th scope="col" className="po-document-lines__num">
                      {t("purchasing.unitCost")}
                    </th>
                    <th scope="col" className="po-document-lines__num">
                      {t("purchasing.lineTotal")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {order.lines.map((line) => {
                    const confirm = confirmQtys[line.productId] ?? defaultConfirmQty(line);
                    const shortage = lineHasShortage(line);
                    const confirmEditable = shortage;
                    const lineTotal = confirmedLineTotal(line.unitPriceSnapshot, confirm);
                    return (
                      <tr
                        key={line.productId}
                        className={cn(shortage && "incoming-order-stock-line--shortage")}
                        data-testid={`incoming-order-stock-line-${line.productId}`}
                        data-shortage={shortage ? "true" : "false"}
                      >
                        <td>
                          <div className="font-medium">{line.nameSnapshot}</div>
                          {line.skuSnapshot ? (
                            <div className="text-[length:var(--exits-text-xs)] text-muted">{line.skuSnapshot}</div>
                          ) : null}
                        </td>
                        <td className="po-document-lines__num">
                          <ShortageQty
                            label={formatStockQtyLabel(line.qty, line.unitOfMeasureCode)}
                            warning={shortage}
                            testId={`incoming-order-requested-qty-${line.productId}`}
                          />
                        </td>
                        <td className="po-document-lines__num">
                          {line.reservedQuantity == null
                            ? "—"
                            : formatStockQtyLabel(line.reservedQuantity, line.unitOfMeasureCode)}
                        </td>
                        <td className="po-document-lines__num">
                          {line.availableToPromise == null ? (
                            "—"
                          ) : (
                            <ShortageQty
                              label={formatStockQtyLabel(
                                line.availableToPromise,
                                line.unitOfMeasureCode,
                              )}
                              warning={shortage}
                              testId={`incoming-order-available-stock-${line.productId}`}
                            />
                          )}
                        </td>
                        <td className="po-document-lines__num">
                          {confirmEditable ? (
                            <div className="incoming-order-confirm-qty">
                              <QuantityStepper
                                compact
                                value={confirm}
                                min={0}
                                precision={maxQuantityDecimals(line.unitOfMeasureCode)}
                                unitOfMeasure={line.unitOfMeasureCode ?? undefined}
                                sellingMode="PerItem"
                                disabled={!canAct}
                                decreaseLabel={t("purchasing.decreaseQty")}
                                increaseLabel={t("purchasing.increaseQty")}
                                ariaLabel={t("incomingOrders.colConfirmQty")}
                                valueTestId={`incoming-order-confirm-qty-${line.productId}`}
                                onChange={(next) => {
                                  setProposeValidation(null);
                                  setConfirmQtys((prev) => ({
                                    ...prev,
                                    [line.productId]: next,
                                  }));
                                }}
                              />
                            </div>
                          ) : (
                            <span
                              className="tabular-nums"
                              data-testid={`incoming-order-confirm-qty-${line.productId}`}
                            >
                              {formatStockQtyLabel(confirm, line.unitOfMeasureCode)}
                            </span>
                          )}
                        </td>
                        <td className="po-document-lines__num">
                          <MoneyDisplay amount={line.unitPriceSnapshot} />
                        </td>
                        <td className="po-document-lines__num font-semibold">
                          <MoneyDisplay
                            amount={lineTotal}
                            testId={`incoming-order-stock-line-total-${line.productId}`}
                          />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <ul
              className="po-document-lines__mobile incoming-order-stock-lines__mobile m-0 list-none flex-col gap-2 p-0"
              data-testid="incoming-order-stock-lines-mobile"
            >
              {order.lines.map((line) => {
                const confirm = confirmQtys[line.productId] ?? defaultConfirmQty(line);
                const shortage = lineHasShortage(line);
                const confirmEditable = shortage;
                const lineTotal = confirmedLineTotal(line.unitPriceSnapshot, confirm);
                return (
                  <li
                    key={line.productId}
                    className={cn(
                      "po-document-lines__mobile-row",
                      shortage && "incoming-order-stock-line--shortage",
                    )}
                    data-testid={`incoming-order-stock-line-mobile-${line.productId}`}
                    data-shortage={shortage ? "true" : "false"}
                  >
                    <div className="po-document-lines__mobile-title-row">
                      <p className="m-0 font-medium">{line.nameSnapshot}</p>
                      <p className="m-0 tabular-nums font-semibold">
                        <MoneyDisplay
                          amount={lineTotal}
                          testId={`incoming-order-stock-line-total-mobile-${line.productId}`}
                        />
                      </p>
                    </div>
                    {line.skuSnapshot ? (
                      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                        {line.skuSnapshot}
                      </p>
                    ) : null}
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("incomingOrders.colRequestedQty")}:{" "}
                      <ShortageQty
                        label={formatStockQtyLabel(line.qty, line.unitOfMeasureCode)}
                        warning={shortage}
                      />
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("incomingOrders.colReserved")}:{" "}
                      {line.reservedQuantity == null
                        ? "—"
                        : formatStockQtyLabel(line.reservedQuantity, line.unitOfMeasureCode)}
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("incomingOrders.colAvailableStock")}:{" "}
                      {line.availableToPromise == null ? (
                        "—"
                      ) : (
                        <ShortageQty
                          label={formatStockQtyLabel(
                            line.availableToPromise,
                            line.unitOfMeasureCode,
                          )}
                          warning={shortage}
                        />
                      )}
                    </p>
                    <div className="incoming-order-stock-mobile-confirm mt-2">
                      <p className="m-0 mb-1 text-[length:var(--exits-text-sm)] text-muted">
                        {t("incomingOrders.colConfirmQty")}
                      </p>
                      {confirmEditable ? (
                        <QuantityStepper
                          compact
                          value={confirm}
                          min={0}
                          precision={maxQuantityDecimals(line.unitOfMeasureCode)}
                          unitOfMeasure={line.unitOfMeasureCode ?? undefined}
                          sellingMode="PerItem"
                          disabled={!canAct}
                          decreaseLabel={t("purchasing.decreaseQty")}
                          increaseLabel={t("purchasing.increaseQty")}
                          ariaLabel={t("incomingOrders.colConfirmQty")}
                          valueTestId={`incoming-order-confirm-qty-mobile-${line.productId}`}
                          onChange={(next) => {
                            setProposeValidation(null);
                            setConfirmQtys((prev) => ({
                              ...prev,
                              [line.productId]: next,
                            }));
                          }}
                        />
                      ) : (
                        <span
                          className="tabular-nums"
                          data-testid={`incoming-order-confirm-qty-mobile-${line.productId}`}
                        >
                          {formatStockQtyLabel(confirm, line.unitOfMeasureCode)}
                        </span>
                      )}
                    </div>
                    <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                      {t("purchasing.unitCost")}: <MoneyDisplay amount={line.unitPriceSnapshot} />
                    </p>
                  </li>
                );
              })}
            </ul>
          </section>

          <PoDocumentTotals
            rows={[
              {
                key: "orderTotal",
                label: t("incomingOrders.orderTotal"),
                amount: confirmedOrderTotal,
                emphasis: "strong",
                testId: "incoming-order-total-amount",
              },
            ]}
            testId="incoming-order-total"
          />
        </>
      ) : isChangesProposed && proposalRevision ? (
        <PoProposalRevisionPanel
          revision={proposalRevision}
          audience="supplier"
          testId="incoming-order-proposal"
        />
      ) : (
        <>
          {showFulfillmentProgress ? (
            <IncomingOrderFulfillmentProgress
              lines={order.lines}
              title={t("incomingOrders.fulfillmentProgress")}
              productLabel={t("purchasing.colProduct")}
              orderedLabel={t("incomingOrders.colOrdered")}
              goodLabel={t("incomingOrders.colGoodReceived")}
              damagedLabel={t("incomingOrders.colDamaged")}
              missingLabel={t("incomingOrders.colMissing")}
              outstandingLabel={t("purchasing.outstanding")}
              unitCostLabel={t("purchasing.unitCost")}
              remainingValueLabel={t("incomingOrders.colRemainingValue")}
            />
          ) : (
            <PoDocumentLineItems
              title={t("purchasing.orderItems")}
              emptyTitle={t("purchasing.linesEmpty")}
              emptyDetail={t("purchasing.linesRequired")}
              lines={documentLines}
              productColLabel={t("purchasing.colProduct")}
              skuColLabel={t("catalog.sku")}
              qtyColLabel={t("purchasing.qty")}
              unitCostColLabel={t("purchasing.unitCost")}
              lineTotalColLabel={t("purchasing.lineTotal")}
              testId="incoming-order-lines"
              lineTestIdPrefix="incoming-order-line"
            />
          )}

          {(order.buyerReceipts?.length ?? 0) > 0 || showFulfillmentProgress ? (
            <IncomingOrderBuyerReceipts
              receipts={order.buyerReceipts ?? []}
              buyerName={order.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
              buyerLabel={t("incomingOrders.buyer")}
              remainingOutstanding={outstandingQty}
              connectedPurchaseOrderId={order.connectedPurchaseOrderId}
              latestTitle={t("incomingOrders.latestReceipt")}
              historyTitle={t("incomingOrders.receiptHistory")}
              viewDetailsLabel={t("incomingOrders.viewReceiptDetails")}
              goodLabel={t("incomingOrders.colGoodReceived")}
              damagedLabel={t("incomingOrders.colDamaged")}
              missingLabel={t("incomingOrders.colMissing")}
              outstandingLabel={t("purchasing.outstanding")}
              deliveryRefLabel={t("purchasing.deliveryReference")}
              notesLabel={t("purchasing.notes")}
              loadMoreLabel={t("incomingOrders.loadMoreReceipts")}
              postedLabel={t("incomingOrders.receiptPosted")}
              voidedLabel={t("incomingOrders.receiptVoided")}
              emptyLabel={t("incomingOrders.noBuyerReceipts")}
            />
          ) : null}

          <PoDocumentTotals
            rows={[
              {
                key: "orderTotal",
                label: t("incomingOrders.orderTotal"),
                amount: order.totalAmount,
                emphasis: "strong",
                testId: "incoming-order-total-amount",
              },
            ]}
            testId="incoming-order-total"
          />
        </>
      )}

      {proposeValidation ? (
        <Notice tone="warning" testId="incoming-order-propose-validation">
          {proposeValidation}
        </Notice>
      ) : null}

      {isNew && showDecline ? (
        <Card className="grid gap-3 p-3" data-testid="incoming-order-decline-form">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("incomingOrders.declineReason")}
            <select
              className="exits-select"
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
          <div className="po-document-actions__cluster">
            <BackActionButton />
            <Button type="button" variant="ghost" disabled={busy} onClick={() => setShowDecline(false)}>
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={!canAct}
              data-testid="incoming-order-decline-confirm"
              onClick={() => declineMutation.mutate()}
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.decline")}
            </Button>
          </div>
        </Card>
      ) : null}

      {isNew && !showDecline ? (
        <div className="po-document-actions" data-testid="incoming-order-pending-actions">
          <div className="po-document-actions__cluster">
            <BackActionButton />
            <Button
              type="button"
              variant="destructive"
              disabled={!canAct}
              data-testid="incoming-order-decline"
              onClick={() => setShowDecline(true)}
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.decline")}
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={!canAct || !hasShortage}
              data-testid="incoming-order-propose"
              onClick={tryProposeChanges}
            >
              {t("incomingOrders.proposeChanges")}
            </Button>
            <Button
              type="button"
              disabled={!canAct || hasShortage}
              data-testid="incoming-order-accept"
              title={hasShortage ? t("incomingOrders.acceptBlockedShortage") : undefined}
              onClick={() => acceptMutation.mutate()}
            >
              <Check className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.accept")}
            </Button>
          </div>
        </div>
      ) : null}

      {isAccepted && !needsPrepareRemaining ? (
        <div className="po-document-actions">
          <div className="po-document-actions__cluster">
            <BackActionButton />
            <Button
              type="button"
              disabled={!canAct}
              data-testid="incoming-order-prepare"
              onClick={() => prepareMutation.mutate()}
            >
              <Play className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.startPreparing")}
            </Button>
          </div>
        </div>
      ) : null}

      {needsPrepareRemaining ? (
        <div className="po-document-actions" data-testid="incoming-order-prepare-remaining-actions">
          <div className="po-document-actions__cluster">
            <BackActionButton />
            <Button
              type="button"
              variant="outline"
              disabled={!canAct}
              data-testid="incoming-order-close-remaining"
              onClick={() => setShowCloseRemaining(true)}
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.closeRemaining")}
            </Button>
            <Button
              type="button"
              disabled={!canAct}
              data-testid="incoming-order-prepare-remaining"
              onClick={() => prepareMutation.mutate()}
            >
              <Play className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.prepareRemaining")}
            </Button>
          </div>
        </div>
      ) : null}

      {isChangesProposed ? (
        <div className="po-document-actions" data-testid="incoming-order-proposal-actions">
          <div className="po-document-actions__cluster">
            <BackActionButton />
            <Button
              type="button"
              variant="outline"
              disabled={!canAct}
              data-testid="incoming-order-withdraw-proposal"
              onClick={() => withdrawProposalMutation.mutate()}
            >
              {t("incomingOrders.withdrawProposal")}
            </Button>
          </div>
        </div>
      ) : null}

      {isPreparing && !showMarkRemainingConfirm && !showCloseRemaining ? (
        <div className="po-document-actions">
          <div className="po-document-actions__cluster">
            <BackActionButton />
            {isMarkRemainingReady ? (
              <Button
                type="button"
                variant="outline"
                disabled={!canAct}
                data-testid="incoming-order-close-remaining"
                onClick={() => setShowCloseRemaining(true)}
              >
                <Ban className="size-4 shrink-0" aria-hidden />
                {t("incomingOrders.closeRemaining")}
              </Button>
            ) : null}
            <Button
              type="button"
              disabled={!canAct}
              data-testid="incoming-order-fulfill"
              onClick={() => {
                if (isMarkRemainingReady) {
                  setShowMarkRemainingConfirm(true);
                  return;
                }
                fulfillMutation.mutate();
              }}
            >
              <PackageCheck className="size-4 shrink-0" aria-hidden />
              {isMarkRemainingReady
                ? t("incomingOrders.markReadyRemaining")
                : t("incomingOrders.markReady")}
            </Button>
          </div>
        </div>
      ) : null}

      {showMarkRemainingConfirm ? (
        <Card
          className="flex flex-col gap-3 p-4"
          data-testid="incoming-order-mark-remaining-confirm"
        >
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("incomingOrders.prepareRemainingItems")}
          </h2>
          <ul className="m-0 list-none flex-col gap-1 p-0">
            {remainingLines.map((line) => (
              <li
                key={line.productId}
                className="flex flex-wrap items-center justify-between gap-2 tabular-nums"
                data-testid={`incoming-order-remaining-line-${line.productId}`}
              >
                <span className="font-medium">{line.nameSnapshot}</span>
                <span>
                  {t("purchasing.outstanding")}{" "}
                  {formatStockQtyLabel(line.outstandingQty ?? 0, line.unitOfMeasureCode)}
                </span>
              </li>
            ))}
          </ul>
          <p className="m-0 font-semibold tabular-nums" data-testid="incoming-order-remaining-total">
            {t("incomingOrders.totalRemaining")}: {formatStockQtyLabel(remainingTotal)}
          </p>
          <div className="po-document-actions__cluster">
            <Button
              type="button"
              variant="ghost"
              disabled={busy}
              data-testid="incoming-order-mark-remaining-cancel"
              onClick={() => setShowMarkRemainingConfirm(false)}
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              disabled={!canAct || remainingTotal <= 0}
              data-testid="incoming-order-mark-remaining-confirm-btn"
              onClick={() => fulfillMutation.mutate()}
            >
              <PackageCheck className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.markReadyRemaining")}
            </Button>
          </div>
        </Card>
      ) : null}

      {showCloseRemaining ? (
        <Card className="flex flex-col gap-3 p-4" data-testid="incoming-order-close-remaining-dialog">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("incomingOrders.closeRemainingTitle")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("incomingOrders.closeRemainingBody")}
          </p>
          <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)] tabular-nums">
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.goodReceivedValue")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay
                  amount={
                    order.finalAcceptedValue ??
                    order.lines.reduce(
                      (sum, line) =>
                        sum +
                        (line.goodReceivedQty ?? 0) *
                          (line.confirmedUnitPrice ?? line.unitPriceSnapshot),
                      0,
                    )
                  }
                />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.remainingCancelledValue")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay
                  amount={
                    order.cancelledRemainingValue ??
                    remainingLines.reduce(
                      (sum, line) =>
                        sum +
                        (line.outstandingQty ?? 0) *
                          (line.confirmedUnitPrice ?? line.unitPriceSnapshot),
                      0,
                    )
                  }
                />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.paymentReceived")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay amount={order.amountPaid ?? 0} />
              </dd>
            </div>
            {(order.refundDueAmount ?? 0) > 0 ? (
              <div className="flex justify-between gap-2">
                <dt>{t("incomingOrders.refundDue")}</dt>
                <dd className="m-0 font-medium" data-testid="incoming-order-close-refund-due">
                  <MoneyDisplay amount={order.refundDueAmount ?? 0} />
                </dd>
              </div>
            ) : null}
          </dl>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>
              {t("incomingOrders.closeRemainingReason")}{" "}
              <span className="text-danger" aria-hidden>
                *
              </span>
            </span>
            <textarea
              className="exits-input min-h-20"
              value={closeRemainingReason}
              onChange={(e) => setCloseRemainingReason(e.target.value)}
              data-testid="incoming-order-close-remaining-reason"
              required
            />
          </label>
          <div className="po-document-actions__cluster">
            <Button
              type="button"
              variant="ghost"
              disabled={busy}
              data-testid="incoming-order-close-remaining-cancel"
              onClick={() => {
                setShowCloseRemaining(false);
                setCloseRemainingReason("");
              }}
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              disabled={!canAct || closeRemainingReason.trim().length === 0}
              data-testid="incoming-order-close-remaining-confirm"
              onClick={() => closeRemainingMutation.mutate()}
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {t("incomingOrders.closeRemainingConfirm")}
            </Button>
          </div>
        </Card>
      ) : null}

      {!allowManage ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("incomingOrders.viewOnly")}</p>
      ) : null}

      {!isNew &&
      !isAccepted &&
      !isPreparing &&
      !isChangesProposed &&
      !needsPrepareRemaining &&
      !showMarkRemainingConfirm &&
      !showCloseRemaining ? (
        <div className="po-document-actions">
          <div className="po-document-actions__cluster">
            <BackActionButton />
          </div>
        </div>
      ) : null}
    </div>
  );
}

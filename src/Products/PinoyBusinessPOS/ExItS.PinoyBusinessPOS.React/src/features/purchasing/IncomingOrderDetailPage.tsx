import { ClipboardList } from "lucide-react";
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
  type ConnectedPurchaseOrderLine,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { incomingOrderStatusTone } from "@/features/purchasing/incoming-orders-helpers";
import {
  buildIncomingOrderExportModel,
  downloadIncomingOrderCsv,
  downloadIncomingOrderPdf,
  downloadIncomingOrderXlsx,
  printIncomingOrderDocument,
} from "@/features/purchasing/incoming-order-table-output";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";
import { PoDocumentExportActions } from "@/features/purchasing/PoDocumentExportActions";
import { PoDocumentLineItems } from "@/features/purchasing/PoDocumentLineItems";
import { PoDocumentSummary } from "@/features/purchasing/PoDocumentSummary";
import { PoDocumentTotals } from "@/features/purchasing/PoDocumentTotals";
import type { PoDocumentLine } from "@/features/purchasing/po-document-types";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function lineQtyLabel(line: ConnectedPurchaseOrderLine): string {
  const uom = line.unitOfMeasureCode ? formatUnitOfMeasureLabel(line.unitOfMeasureCode) : "";
  return uom ? `${line.qty} ${uom}` : String(line.qty);
}

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

export function IncomingOrderDetailPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
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
    await queryClient.invalidateQueries({ queryKey: ["purchasing-hub"] });
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
  const isDeclined = order.status === "Declined";
  const canAct = allowManage && online && !busy;
  const printModel = buildIncomingOrderExportModel(order, order.lines, new Set());
  const documentLines = toDocumentLines(order.lines);
  const resolvedStatusLabel = statusLabel(t, order.status, order.displayStatus);
  const statusTone = incomingOrderStatusTone(order.status);

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
        backTo="/purchasing/incoming-orders"
        backLabel={t("incomingOrders.backList")}
        backTestId="page-header-back-incoming-order-detail"
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip tone={statusTone}>{resolvedStatusLabel}</StatusChip>
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

      <PoDocumentSummary
        counterpartyLabel={t("incomingOrders.buyer")}
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
                {order.buyerReceivingStatus}
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
      ) : (
        <>
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
          <div className="flex flex-wrap gap-2">
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
              {t("incomingOrders.decline")}
            </Button>
          </div>
        </Card>
      ) : null}

      {isNew && !showDecline ? (
        <div className="po-document-actions" data-testid="incoming-order-pending-actions">
          <Button
            type="button"
            variant="destructive"
            disabled={!canAct}
            data-testid="incoming-order-decline"
            onClick={() => setShowDecline(true)}
          >
            {t("incomingOrders.decline")}
          </Button>
          <div className="po-document-actions__primary">
            <Button
              type="button"
              disabled={!canAct}
              data-testid="incoming-order-accept"
              onClick={() => acceptMutation.mutate()}
            >
              {t("incomingOrders.accept")}
            </Button>
          </div>
        </div>
      ) : null}

      {isAccepted ? (
        <div className="po-document-actions">
          <div className="po-document-actions__primary">
            <Button
              type="button"
              disabled={!canAct}
              data-testid="incoming-order-prepare"
              onClick={() => prepareMutation.mutate()}
            >
              {t("incomingOrders.startPreparing")}
            </Button>
          </div>
        </div>
      ) : null}

      {isPreparing ? (
        <div className="po-document-actions">
          <div className="po-document-actions__primary">
            <Button
              type="button"
              disabled={!canAct}
              data-testid="incoming-order-fulfill"
              onClick={() => fulfillMutation.mutate()}
            >
              {t("incomingOrders.markReady")}
            </Button>
          </div>
        </div>
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

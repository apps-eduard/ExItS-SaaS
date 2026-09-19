import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { History, Store } from "lucide-react";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import { listConnectedPoReturnsByPurchaseOrder } from "@/api/pos/pos-connected-po-returns-client";
import { getBuyerConnectedSupplierCommerceReadiness } from "@/api/pos/pos-connected-suppliers-client";
import { SupplierNotReadyForPoBanner } from "@/features/purchasing/SupplierNotReadyForPoBanner";
import {
  acceptConnectedPurchaseOrderChanges,
  cancelPurchaseOrder,
  declineConnectedPurchaseOrderChanges,
  getPurchaseOrder,
  isPurchaseOrderReceivable,
  listGoodsReceiptsForPurchaseOrder,
  submitPurchaseOrder,
  voidGoodsReceipt,
  type PosGoodsReceiptDto,
  type PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import {
  isConnectedSupplier,
  listSuppliers,
} from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  sumGoodsReceiptValue,
  sumPurchaseOrderLineTotals,
} from "@/features/purchasing/purchase-cost-display";
import { buildPurchaseOrderActivityEvents } from "@/features/purchasing/purchase-order-activity";
import { PurchaseOrderTimelineDrawer } from "@/features/purchasing/PurchaseOrderTimelineDrawer";
import { PoDocumentLineItems } from "@/features/purchasing/PoDocumentLineItems";
import { PoDocumentSummary } from "@/features/purchasing/PoDocumentSummary";
import { PoDocumentTotals } from "@/features/purchasing/PoDocumentTotals";
import type { PoDocumentLine } from "@/features/purchasing/po-document-types";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import { DocumentActions } from "@/features/documents/DocumentActions";
import { PurchaseOrderBusinessDocument } from "@/features/documents/PurchasingBusinessDocuments";
import { useBusinessDocumentIdentity } from "@/features/documents/use-business-document-identity";
import { useOrganizationDocumentSettings } from "@/features/documents/use-organization-document-settings";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { receiptReverseErrorMessage } from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { buildProposalRevisionFromBuyerPo } from "@/features/purchasing/po-proposal-revision";
import { PoProposalRevisionPanel } from "@/features/purchasing/PoProposalRevisionPanel";

const RECEIPT_VOID_REASON_MAX = 512;

function buyerStatusTone(status: string, displayStatus: string): "success" | "warning" | "info" | "danger" {
  const key = displayStatus || status;
  switch (key) {
    case "Ordered":
    case "Received":
    case "Completed":
    case "CompletedRemainingCancelled":
    case "Ready":
    case "Shipped":
    case "AwaitingBuyerReceipt":
      return "success";
    case "PartiallyReceived":
    case "ChangesNeedApproval":
    case "New":
    case "ReceivedWithIssues":
    case "ReceivedAwaitingPayment":
      return "warning";
    case "Cancelled":
    case "Declined":
      return "danger";
    case "Draft":
    default:
      return "info";
  }
}

/** Map raw API/display status to human labels (e.g. New → Pending). */
function buyerStatusLabel(
  t: (key: MessageKey) => string,
  status: string,
  displayStatus: string,
): string {
  const key = displayStatus || status;
  switch (key) {
    case "New":
      return "Pending";
    case "PartiallyReceived":
      return "Partially received";
    case "Received":
    case "Completed":
      return "Fully received";
    case "CompletedRemainingCancelled":
      return t("incomingOrders.statusCompletedRemainingCancelled");
    case "ReceivedAwaitingPayment":
      return t("incomingOrders.statusReceivedAwaitingPayment");
    case "ReceivedWithIssues":
      return t("incomingOrders.statusReceivedWithIssues");
    case "Shipped":
    case "AwaitingBuyerReceipt":
      return "Shipped — awaiting receipt";
    case "Ready":
      return "Ready for pickup";
    case "ChangesNeedApproval":
    case "ChangesProposed":
      return t("incomingOrders.statusChangesProposed");
    default:
      return key === "ChangesProposed" ? t("incomingOrders.statusChangesProposed") : key;
  }
}

function resolveOrderTotal(po: PosPurchaseOrderDto): {
  amount: number;
  labelKey: "purchasing.orderTotal" | "purchasing.confirmedTotal" | "purchasing.proposedTotal";
} {
  if (po.confirmedTotalAmount != null) {
    return { amount: po.confirmedTotalAmount, labelKey: "purchasing.confirmedTotal" };
  }
  if (po.proposedTotalAmount != null && po.displayStatus === "ChangesNeedApproval") {
    return { amount: po.proposedTotalAmount, labelKey: "purchasing.proposedTotal" };
  }
  return {
    amount: sumPurchaseOrderLineTotals(po.lines),
    labelKey: "purchasing.orderTotal",
  };
}

function toBuyerDocumentLines(po: PosPurchaseOrderDto): PoDocumentLine[] {
  return po.lines.map((line) => {
    const uom = line.uomSnapshot ?? "";
    return {
      id: line.lineId,
      productName: line.nameSnapshot ?? line.productId ?? "—",
      sku: line.skuSnapshot,
      quantityLabel: uom ? `${line.orderedQty} ${uom}` : String(line.orderedQty),
      unitCost: line.unitPurchaseCost,
      lineTotal: line.lineTotal,
      receivedLabel: uom ? `${line.receivedQty} ${uom}` : String(line.receivedQty),
      outstandingLabel: uom ? `${line.outstandingQty} ${uom}` : String(line.outstandingQty),
    };
  });
}

function GoodsReceiptCard({
  receipt,
  workspace,
  resolveActor,
  isResolving,
  allowManage,
  online,
  onReversed,
}: {
  receipt: PosGoodsReceiptDto;
  workspace: { organizationId: string; branchId: string };
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
  allowManage: boolean;
  online: boolean;
  onReversed: (updated: PosGoodsReceiptDto) => Promise<void>;
}) {
  const { t } = useI18n();
  const receiptValue = sumGoodsReceiptValue(receipt.lines);
  const delivery = receipt.deliveryReference?.trim();
  const notes = receipt.notes?.trim();
  const isPosted = (receipt.status ?? "Posted") === "Posted";
  const isVoided = receipt.status === "Voided";
  const [voidOpen, setVoidOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");
  const [voiding, setVoiding] = useState(false);
  const [voidError, setVoidError] = useState<string | null>(null);

  async function onVoid() {
    const reason = voidReason.trim();
    if (!allowManage || !online || voiding || !isPosted) {
      return;
    }
    if (!reason) {
      setVoidError(t("purchasing.reverseReasonRequired"));
      return;
    }
    setVoiding(true);
    setVoidError(null);
    try {
      const updated = await voidGoodsReceipt(workspace, receipt.goodsReceiptId, { reason });
      await onReversed(updated);
      setVoidOpen(false);
      setVoidReason("");
    } catch (err) {
      setVoidError(
        receiptReverseErrorMessage(
          err,
          t("purchasing.reverseFailed"),
          t("supplierPayables.reverseBlockedByPayments"),
        ),
      );
    } finally {
      setVoiding(false);
    }
  }

  return (
    <Card className="flex flex-col gap-3 p-3" data-testid={`po-receipt-${receipt.grnNumber}`}>
      <div className="flex flex-wrap items-center gap-2">
        <p className="m-0 font-medium">{receipt.grnNumber}</p>
        <StatusChip tone={isVoided ? "danger" : "success"}>
          {isVoided ? t("purchasing.receiptStatus.voided") : t("purchasing.receiptStatus.posted")}
        </StatusChip>
      </div>
      <ActorAttribution
        labelKey="common.receivedBy"
        actorId={receipt.receivedBy}
        occurredAtUtc={receipt.receivedAtUtc}
        resolved={resolveActor(receipt.receivedBy)}
        isLoading={isResolving}
        testId={`po-receipt-received-by-${receipt.goodsReceiptId}`}
      />
      {isVoided && receipt.voidedByUserId ? (
        <ActorAttribution
          labelKey="purchasing.reversedBy"
          actorId={receipt.voidedByUserId}
          occurredAtUtc={receipt.voidedAtUtc}
          resolved={resolveActor(receipt.voidedByUserId)}
          isLoading={isResolving}
          testId={`po-receipt-reversed-by-${receipt.goodsReceiptId}`}
        />
      ) : null}
      {isVoided && receipt.voidReason ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid={`po-receipt-void-reason-${receipt.goodsReceiptId}`}>
          {t("purchasing.reverseReason")}: {receipt.voidReason}
        </p>
      ) : null}
      {delivery ? (
        <div>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.deliveryReference")}
          </p>
          <p className="mt-0.5 mb-0 text-[length:var(--exits-text-sm)]">{delivery}</p>
        </div>
      ) : null}
      <div className="flex flex-wrap items-baseline justify-between gap-2 text-[length:var(--exits-text-sm)]">
        <span className="text-muted">{t("purchasing.receiptValue")}</span>
        <MoneyDisplay amount={receiptValue} testId={`po-receipt-value-${receipt.goodsReceiptId}`} />
      </div>
      <ul className="m-0 flex list-none flex-col gap-3 border-t border-border pt-3 p-0">
        {receipt.lines.map((line) => {
          const goodQty = line.quantityReceived;
          const damaged = line.damagedQty ?? 0;
          const rejected = line.rejectedQty ?? 0;
          const shortClosed = line.shortClosedQty ?? 0;
          const discrepancyNote = line.discrepancyNote?.trim();
          const showExpiryLot = Boolean(line.expiryDate) || Boolean(line.lotNumber);
          return (
            <li
              key={line.lineId}
              className="text-[length:var(--exits-text-sm)]"
              data-testid={`po-receipt-line-${line.lineId}`}
            >
              <p className="m-0 font-medium">{line.nameSnapshot}</p>
              <p className="mt-1 mb-0 text-muted">
                {t("purchasing.receivedGood")}: {goodQty} {line.uomSnapshot}
              </p>
              <p className="mt-1 mb-0 flex flex-wrap items-baseline justify-between gap-2">
                <span className="text-muted">{t("purchasing.unitPurchaseCost")}</span>
                <span>
                  <MoneyDisplay amount={line.unitPurchaseCostSnapshot} />
                  <span className="text-muted"> / {line.uomSnapshot}</span>
                </span>
              </p>
              <p className="mt-1 mb-0 flex flex-wrap items-baseline justify-between gap-2">
                <span className="text-muted">{t("purchasing.lineTotal")}</span>
                <MoneyDisplay amount={line.lineTotalSnapshot} />
              </p>
              {showExpiryLot ? (
                <>
                  <p className="mt-1 mb-0 flex flex-wrap justify-between gap-2">
                    <span className="text-muted">{t("purchasing.expiryDate")}</span>
                    <span>{line.expiryDate ?? "—"}</span>
                  </p>
                  <p className="mt-1 mb-0 flex flex-wrap justify-between gap-2">
                    <span className="text-muted">{t("purchasing.lotNumber")}</span>
                    <span>{line.lotNumber?.trim() || "—"}</span>
                  </p>
                </>
              ) : null}
              {damaged > 0 ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.damaged")}: {damaged} {line.uomSnapshot}
                </p>
              ) : null}
              {rejected > 0 ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.rejected")}: {rejected} {line.uomSnapshot}
                </p>
              ) : null}
              {shortClosed > 0 ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.cancelRemaining")}: {shortClosed} {line.uomSnapshot}
                </p>
              ) : null}
              {line.discrepancyKind && line.discrepancyKind !== "None" ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.discrepancy")}:{" "}
                  {line.discrepancyKind === "Short"
                    ? t("purchasing.cancelRemaining")
                    : line.discrepancyKind}
                </p>
              ) : null}
              {discrepancyNote ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.discrepancyNote")}: {discrepancyNote}
                </p>
              ) : null}
            </li>
          );
        })}
      </ul>
      {notes ? (
        <p className="m-0 border-t border-border pt-2 text-[length:var(--exits-text-sm)] text-muted">
          {t("purchasing.notes")}: {notes}
        </p>
      ) : null}

      {voidError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={voidError} />
      ) : null}

      {allowManage && isPosted && online ? (
        <Button
          type="button"
          variant="outline"
          className="w-fit"
          onClick={() => {
            setVoidOpen(true);
            setVoidError(null);
          }}
          data-testid={`po-receipt-reverse-${receipt.goodsReceiptId}`}
        >
          {t("purchasing.reverseReceipt")}
        </Button>
      ) : null}

      {voidOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby={`po-receipt-reverse-title-${receipt.goodsReceiptId}`}
          data-testid={`po-receipt-reverse-dialog-${receipt.goodsReceiptId}`}
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2
              id={`po-receipt-reverse-title-${receipt.goodsReceiptId}`}
              className="m-0 text-[length:var(--exits-text-lg)] font-semibold"
            >
              {t("purchasing.reverseTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.reverseLede")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {receipt.grnNumber} · {receipt.receivedDate}
            </p>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("purchasing.reverseReason")}</span>
              <textarea
                className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 py-2"
                value={voidReason}
                maxLength={RECEIPT_VOID_REASON_MAX}
                onChange={(e) => setVoidReason(e.target.value)}
                data-testid={`po-receipt-reverse-reason-${receipt.goodsReceiptId}`}
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="destructive"
                disabled={voiding || !voidReason.trim()}
                onClick={() => void onVoid()}
                data-testid={`po-receipt-reverse-confirm-${receipt.goodsReceiptId}`}
              >
                {voiding ? t("purchasing.reversing") : t("purchasing.reverseConfirm")}
              </Button>
              <Button
                type="button"
                variant="outline"
                disabled={voiding}
                onClick={() => {
                  setVoidOpen(false);
                  setVoidReason("");
                  setVoidError(null);
                }}
                data-testid={`po-receipt-reverse-cancel-${receipt.goodsReceiptId}`}
              >
                {t("purchasing.reverseCancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </Card>
  );
}

export function PurchaseOrderDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { purchaseOrderId } = useParams<{ purchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const { settings: documentSettings } = useOrganizationDocumentSettings(organizationId);
  const { identity, headerVisibility } = useBusinessDocumentIdentity(organizationId);
  const allowManage = canManagePurchasing(sessionGrant);
  const smartBack = usePageSmartBack({
    fallback: "purchaseOrders",
    backLabel: t("purchasing.backOrders"),
    backTestId: "page-header-back-purchasing",
  });
  const [busy, setBusy] = useState(false);
  const [banner, setBanner] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["purchase-order", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: ({ signal }) => getPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });

  const receiptsQuery = useQuery({
    queryKey: ["purchase-order-receipts", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: ({ signal }) =>
      listGoodsReceiptsForPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });

  const po = query.data;
  const receipts = receiptsQuery.data ?? [];

  const suppliersQuery = useQuery({
    queryKey: ["suppliers", "po-detail", workspace?.organizationId],
    enabled: Boolean(workspace) && online && Boolean(po?.supplierId),
    queryFn: ({ signal }) => listSuppliers(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const connectedRelationshipId = useMemo(() => {
    if (!po?.supplierId) {
      return null;
    }
    const supplier = (suppliersQuery.data?.items ?? []).find((s) => s.supplierId === po.supplierId);
    if (!supplier || !isConnectedSupplier(supplier)) {
      return null;
    }
    return supplier.connectedRelationshipId ?? null;
  }, [po?.supplierId, suppliersQuery.data]);

  const connectedReturnsQuery = useQuery({
    queryKey: ["connected-po-returns", workspace?.organizationId, purchaseOrderId],
    enabled:
      Boolean(workspace) &&
      Boolean(purchaseOrderId) &&
      online &&
      Boolean(connectedRelationshipId) &&
      po?.status === "Received",
    queryFn: ({ signal }) =>
      listConnectedPoReturnsByPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });
  const connectedReturns = connectedReturnsQuery.data ?? [];

  const commerceReadinessQuery = useQuery({
    queryKey: ["connected-suppliers", "commerce-readiness", connectedRelationshipId],
    enabled:
      Boolean(workspace) &&
      online &&
      Boolean(connectedRelationshipId) &&
      po?.status === "Draft",
    queryFn: ({ signal }) =>
      getBuyerConnectedSupplierCommerceReadiness(workspace!, connectedRelationshipId!, signal),
    refetchOnWindowFocus: true,
  });

  const supplierCommerceReady =
    !connectedRelationshipId || commerceReadinessQuery.data?.isReady !== false;
  const supportedPickupAvailable =
    commerceReadinessQuery.data?.supportedFulfillmentMethods?.includes("Pickup") === true;
  const actors = useActorDirectory(workspace?.organizationId, [
    po?.orderedBy,
    po?.cancelledByUserId,
    po?.remainingClosedByUserId,
    ...receipts.map((receipt) => receipt.receivedBy),
    ...receipts.map((receipt) => receipt.voidedByUserId),
  ]);
  const displayStatus = po?.displayStatus || po?.status || "";
  const needsApproval = displayStatus === "ChangesNeedApproval";
  const canSubmit =
    allowManage &&
    online &&
    po?.status === "Draft" &&
    supplierCommerceReady &&
    !commerceReadinessQuery.isFetching;
  const canEditDraft = allowManage && online && po?.status === "Draft";
  const canCancel =
    allowManage && online && (po?.status === "Draft" || po?.canWithdrawConnected === true);
  const canReceive =
    allowManage && online && po != null && isPurchaseOrderReceivable(po) && !needsApproval;
  const needsProductSetup =
    allowManage &&
    online &&
    po != null &&
    (po.needsProductSetup === true || (po.productSetupRequiredCount ?? 0) > 0) &&
    (po.canReceiveConnected ?? true) &&
    !needsApproval &&
    (po.status === "Ordered" || po.status === "PartiallyReceived");
  const canAcceptChanges = allowManage && online && needsApproval;
  const orderTotal = po ? resolveOrderTotal(po) : null;
  const hasTimeline = useMemo(
    () => (po ? buildPurchaseOrderActivityEvents({ po, receipts }).length > 0 : false),
    [po, receipts],
  );

  async function runAction(
    action: () => Promise<unknown>,
    successKey: "purchasing.submitted" | "purchasing.cancelled" | "purchasing.changesAccepted",
    options?: { reconcile?: () => Promise<boolean> },
  ) {
    if (!workspace || !purchaseOrderId || busy) {
      return;
    }
    setBusy(true);
    setError(null);
    setBanner(null);
    try {
      await action();
      setBanner(t(successKey));
      await queryClient.invalidateQueries({
        queryKey: ["purchase-order", workspace.organizationId, purchaseOrderId],
      });
      await queryClient.invalidateQueries({ queryKey: ["purchasing-hub"] });
      await queryClient.invalidateQueries({ queryKey: ["purchase-orders"] });
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
      await queryClient.invalidateQueries({ queryKey: ["supplier-payable-summary"] });
      await queryClient.invalidateQueries({ queryKey: ["supplier-payables"] });
      await query.refetch();
    } catch (err) {
      if (options?.reconcile) {
        setError(t("checkout.confirmingTransaction"));
        const outcome = await resolveAmbiguousMutationOutcome({
          error: err,
          lookup: async () => {
            const ok = await options.reconcile!();
            if (!ok) {
              throw err;
            }
            return true;
          },
        });
        if (outcome.kind === "confirmed") {
          setError(null);
          setBanner(t(successKey));
          await queryClient.invalidateQueries({
            queryKey: ["purchase-order", workspace.organizationId, purchaseOrderId],
          });
          await queryClient.invalidateQueries({ queryKey: ["purchasing-hub"] });
          await queryClient.invalidateQueries({ queryKey: ["purchase-orders"] });
          await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
          await queryClient.invalidateQueries({ queryKey: ["supplier-payable-summary"] });
          await queryClient.invalidateQueries({ queryKey: ["supplier-payables"] });
          await query.refetch();
          return;
        }
        if (outcome.kind === "still_unknown") {
          setError(t("checkout.transactionStatusUnknown"));
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? err.errorCode === "pos.connected_supplier.commerce_not_ready"
            ? t("purchasing.supplierNotReadySubmitBlocked")
            : (err.problem.detail ?? t("purchasing.actionFailed"))
          : t("purchasing.actionFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!purchaseOrderId) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }
  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !po) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }

  const resolvedStatusLabel = buyerStatusLabel(t, po.status, displayStatus);
  const statusTone = buyerStatusTone(po.status, displayStatus);
  const sellerName = po.supplierBranchName
    ? `${po.supplierName ?? t("purchasing.unknownSupplier")} — ${po.supplierBranchName}`
    : (po.supplierName ?? t("purchasing.unknownSupplier"));
  const documentLines = toBuyerDocumentLines(po);
  const proposalRevision = needsApproval ? buildProposalRevisionFromBuyerPo(po) : null;
  const showReceiveProgress =
    po.status === "Ordered" ||
    po.status === "PartiallyReceived" ||
    po.status === "Received" ||
    displayStatus === "Ready" ||
    displayStatus === "Shipped" ||
    displayStatus === "AwaitingBuyerReceipt";
  const isShortClosed =
    Boolean(po.remainingClosedAtUtc) ||
    displayStatus === "CompletedRemainingCancelled" ||
    po.lines.some((line) => (line.closedShortQty ?? 0) > 0);
  const shortCloseSummary = (() => {
    const posted = receipts.filter((r) => (r.status ?? "Posted") === "Posted");
    const goodQty = po.lines.reduce((sum, line) => sum + line.receivedQty, 0);
    const orderedQty = po.lines.reduce((sum, line) => sum + line.orderedQty, 0);
    const cancelledQty = po.lines.reduce((sum, line) => sum + (line.closedShortQty ?? 0), 0);
    const damagedQty = posted.reduce(
      (sum, r) => sum + r.lines.reduce((lineSum, line) => lineSum + (line.damagedQty ?? 0), 0),
      0,
    );
    const notDeliveredQty = posted.reduce(
      (sum, r) => sum + r.lines.reduce((lineSum, line) => lineSum + (line.rejectedQty ?? 0), 0),
      0,
    );
    const finalAccepted =
      po.finalAcceptedValue ??
      po.lines.reduce((sum, line) => sum + line.receivedQty * line.unitPurchaseCost, 0);
    const paid = po.amountPaidSnapshot ?? 0;
    const refundDue = po.refundDueAmount ?? Math.max(0, paid - finalAccepted);
    const balanceDue = Math.max(0, finalAccepted - paid);
    return {
      orderedQty,
      goodQty,
      damagedQty,
      notDeliveredQty,
      cancelledQty,
      finalAccepted,
      paid,
      refundDue,
      balanceDue,
    };
  })();

  const purchaseOrderDocument = (
    <PurchaseOrderBusinessDocument
      po={po}
      supplierName={sellerName}
      settings={documentSettings}
      identity={identity}
      headerVisibility={headerVisibility(documentSettings.header)}
      deliveryAddress={boundWorkspace?.branchName ?? null}
      preview={documentPreviewOpen}
    />
  );

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-order-detail-page">
      <PageHeader
        title={po.poNumber ?? t("purchasing.detailTitle")}
        {...smartBack}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip tone={statusTone}>{resolvedStatusLabel}</StatusChip>
            {hasTimeline ? (
              <Button
                type="button"
                intent="neutral"
                appearance="outline"
                onClick={() => setTimelineOpen(true)}
                data-testid="po-timeline-open"
              >
                <History className="size-4 shrink-0" aria-hidden />
                {t("purchasing.timeline")}
              </Button>
            ) : null}
            <DocumentActions
              previewLabel={t("summary.preview")}
              printLabel={t("exitsTable.print")}
              pdfLabel={t("exitsTable.exportPdf")}
              onPreview={() => setDocumentPreviewOpen(true)}
              testId="po-business-document-actions"
            />
          </div>
        }
      />

      {!online ? (
        <Notice tone="warning">{t("purchasing.offline")}</Notice>
      ) : null}
      {connectedRelationshipId &&
      po.status === "Draft" &&
      commerceReadinessQuery.isSuccess &&
      !supplierCommerceReady ? (
        <SupplierNotReadyForPoBanner
          blockerCategories={commerceReadinessQuery.data?.blockerCategories}
        />
      ) : null}
      {needsApproval ? (
        <Notice tone="warning" testId="po-needs-approval">
          <span className="font-medium">{t("incomingOrders.changesProposedTitle")}</span>
          <span className="mt-1 block">{t("incomingOrders.awaitingBuyerReview")}</span>
          {po.inventoryReservationExpiresAtUtc ? (
            <span className="mt-1 block" data-testid="po-reserved-until">
              {t("purchasing.reservedUntil").replace(
                "{datetime}",
                new Date(po.inventoryReservationExpiresAtUtc).toLocaleString(),
              )}
            </span>
          ) : null}
        </Notice>
      ) : null}
      {needsProductSetup ? (
        <Card className="p-3" data-testid="po-prepare-products-banner">
          <p className="m-0 font-medium">{t("purchasing.prepareProductsTitle")}</p>
          <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.prepareProductsHelp").replace(
              "{count}",
              String(po.productSetupRequiredCount ?? po.lines.filter((l) => l.needsProductSetup).length ?? 0),
            )}
          </p>
          <Button asChild className="mt-3" data-testid="po-prepare-products">
            <Link to={`/purchasing/${purchaseOrderId}/prepare-products`}>
              {t("purchasing.prepareProductsAction")}
            </Link>
          </Button>
        </Card>
      ) : null}
      {canReceive &&
      (displayStatus === "Ready" ||
        displayStatus === "Shipped" ||
        displayStatus === "AwaitingBuyerReceipt") ? (
        <Notice tone="success" testId="po-ready-receive">
          {t("purchasing.readyToReceive")}
        </Notice>
      ) : null}
      {po.canReceiveConnected === false ? (
        <Notice tone="info" testId="po-receive-gated">
          {t("purchasing.connectedReceiveBlocked")}
        </Notice>
      ) : null}
      {displayStatus === "ReceivedAwaitingPayment" ||
      po.financialSettlementStatus === "AwaitingPayment" ? (
        <Notice tone="warning" testId="po-awaiting-payment">
          {t("incomingOrders.awaitingPaymentBuyerBody")}
        </Notice>
      ) : null}
      {connectedRelationshipId && po.status === "Received" && po.financialSettlementStatus !== "AwaitingPayment" ? (
        <Card className="p-3" data-testid="po-connected-returns-card">
          <p className="m-0 font-medium">{t("returns.connectedPo.sectionTitle")}</p>
          {connectedReturns.length > 0 ? (
            <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {connectedReturns.some((batch) => batch.status !== "Finalized")
                ? t("returns.connectedPo.statusReturnsPending")
                : t("returns.connectedPo.statusReturnsProcessed")}
            </p>
          ) : null}
          <ul className="mb-0 mt-2 list-none space-y-1 p-0">
            {connectedReturns.map((batch) => (
              <li
                key={batch.returnBatchId}
                className="text-[length:var(--exits-text-sm)] text-muted"
                data-testid={`po-connected-return-${batch.returnBatchId}`}
              >
                {batch.batchNumber} ·{" "}
                {batch.status === "AwaitingSellerReceipt"
                  ? t("returns.connectedPo.awaitingSellerReceipt")
                  : batch.status}
              </li>
            ))}
          </ul>
          <Button asChild className="mt-3" data-testid="po-connected-return-items">
            <Link to={`/returns/connected-po/${purchaseOrderId}`}>
              {t("returns.connectedPo.returnItems")}
            </Link>
          </Button>
        </Card>
      ) : null}
      {banner ? (
        <Notice tone="success" testId="po-banner">
          {banner}
        </Notice>
      ) : null}
      {error ? (
        <Notice tone="danger" testId="po-detail-error">
          <div className="flex flex-col gap-2">
            <span>{error}</span>
            {error.toLowerCase().includes("pickup") ||
            error.toLowerCase().includes("delivery") ||
            error.toLowerCase().includes("receiving") ? (
              <div className="flex flex-wrap gap-2">
                {supportedPickupAvailable ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    data-testid="po-submit-choose-pickup"
                    onClick={() => navigate(`/purchasing/${purchaseOrderId}/edit`)}
                  >
                    {t("purchasing.choosePickup")}
                  </Button>
                ) : null}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  data-testid="po-submit-manage-receiving"
                  onClick={() => navigate("/branches")}
                >
                  {t("purchasing.manageReceivingBranch")}
                </Button>
                {connectedRelationshipId ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    data-testid="po-submit-view-fulfillment"
                    onClick={() => navigate(`/suppliers/connected/${connectedRelationshipId}`)}
                  >
                    {t("purchasing.viewFulfillmentSetup")}
                  </Button>
                ) : null}
              </div>
            ) : null}
          </div>
        </Notice>
      ) : null}
      {canSubmit ? (
        <Notice tone="info" testId="po-draft-notice">
          {t("purchasing.ordersNoStock")}
        </Notice>
      ) : null}

      <PoDocumentSummary
        counterpartyLabel={t("purchasing.seller")}
        counterpartyIcon={<Store className="size-5" strokeWidth={1.75} />}
        counterpartyName={sellerName}
        status={{ label: resolvedStatusLabel, tone: statusTone }}
        fields={[
          {
            key: "receiving",
            label: t("purchasing.receivingAt"),
            value: boundWorkspace?.branchName ?? boundWorkspace?.branchId ?? "—",
          },
          {
            key: "payment",
            label: t("purchasing.paymentTerm"),
            value: po.paymentTermLabel || po.paymentTerm || "Cash",
          },
          ...(po.paymentTiming
            ? [
                {
                  key: "paymentTiming",
                  label: t("purchasing.paymentTiming"),
                  value:
                    po.paymentTimingLabel ||
                    (po.paymentTiming === "PayOnDeliveryOrReceipt"
                      ? t("connectedCommerce.timing.payOnDelivery")
                      : po.paymentTiming === "SupplierCredit"
                        ? t("connectedCommerce.timing.supplierCredit")
                        : t("connectedCommerce.timing.payBefore")),
                },
              ]
            : []),
          {
            key: "orderDate",
            label: t("purchasing.fieldOrderDate"),
            value: po.orderDate,
          },
          ...(po.notes?.trim()
            ? [{ key: "notes", label: t("purchasing.notes"), value: po.notes.trim() }]
            : []),
        ]}
        testId="po-document-summary"
        footer={
          po.orderedAtUtc || po.orderedBy ? (
            <ActorAttribution
              labelKey="common.orderedBy"
              actorId={po.orderedBy}
              occurredAtUtc={po.orderedAtUtc}
              resolved={actors.resolve(po.orderedBy)}
              isLoading={actors.isResolving}
              testId="po-ordered-by"
            />
          ) : null
        }
      />

      {isShortClosed ? (
        <Card className="flex flex-col gap-3 p-3" data-testid="po-short-close-summary">
          <div className="flex flex-wrap items-center gap-2">
            <p className="m-0 font-medium">{t("incomingOrders.shortClosed")}</p>
            <StatusChip tone="success">
              {t("incomingOrders.statusCompletedRemainingCancelled")}
            </StatusChip>
          </div>
          <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)] tabular-nums">
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.ordered")}</dt>
              <dd className="m-0">{shortCloseSummary.orderedQty}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.colGoodReceived")}</dt>
              <dd className="m-0">{shortCloseSummary.goodQty}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.colDamaged")}</dt>
              <dd className="m-0">{shortCloseSummary.damagedQty}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.colMissing")}</dt>
              <dd className="m-0">{shortCloseSummary.notDeliveredQty}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.cancelledRemaining")}</dt>
              <dd className="m-0">{shortCloseSummary.cancelledQty}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.finalAcceptedValue")}</dt>
              <dd className="m-0 font-medium">
                <MoneyDisplay amount={shortCloseSummary.finalAccepted} />
              </dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>{t("incomingOrders.paymentReceived")}</dt>
              <dd className="m-0">
                <MoneyDisplay amount={shortCloseSummary.paid} />
              </dd>
            </div>
            {shortCloseSummary.refundDue > 0 ? (
              <div className="flex justify-between gap-2">
                <dt>{t("incomingOrders.refundDue")}</dt>
                <dd className="m-0 font-medium" data-testid="po-short-close-refund-due">
                  <MoneyDisplay amount={shortCloseSummary.refundDue} />
                </dd>
              </div>
            ) : shortCloseSummary.balanceDue > 0 ? (
              <div className="flex justify-between gap-2">
                <dt>{t("incomingOrders.balanceDue")}</dt>
                <dd className="m-0 font-medium" data-testid="po-short-close-balance-due">
                  <MoneyDisplay amount={shortCloseSummary.balanceDue} />
                </dd>
              </div>
            ) : null}
          </dl>
          {po.remainingClosedReason?.trim() ? (
            <p className="m-0 border-t border-border pt-2 text-[length:var(--exits-text-sm)] text-muted">
              {t("incomingOrders.closeRemainingReason")}: {po.remainingClosedReason.trim()}
            </p>
          ) : null}
          {po.remainingClosedAtUtc && po.remainingClosedByUserId ? (
            <ActorAttribution
              labelKey="common.closedBy"
              actorId={po.remainingClosedByUserId}
              occurredAtUtc={po.remainingClosedAtUtc}
              resolved={actors.resolve(po.remainingClosedByUserId)}
              isLoading={actors.isResolving}
              testId="po-remaining-closed-by"
            />
          ) : null}
        </Card>
      ) : null}

      {/* Preserve supplier display test id for existing tests */}
      <span className="sr-only" data-testid="po-supplier-display">
        {sellerName}
      </span>
      <span className="sr-only" data-testid="po-receiving-branch">
        {boundWorkspace?.branchName ?? boundWorkspace?.branchId ?? "—"}
      </span>

      {proposalRevision ? (
        <PoProposalRevisionPanel
          revision={proposalRevision}
          audience="buyer"
          testId="po-proposal-revision"
        />
      ) : (
        <>
          <PoDocumentLineItems
            title={t("purchasing.orderItems")}
            emptyTitle={t("purchasing.linesEmpty")}
            emptyDetail={t("purchasing.linesRequired")}
            lines={documentLines}
            showReceiveProgress={showReceiveProgress}
            productColLabel={t("purchasing.colProduct")}
            skuColLabel={t("purchasing.colSku")}
            qtyColLabel={t("purchasing.ordered")}
            unitCostColLabel={t("purchasing.unitPurchaseCost")}
            lineTotalColLabel={t("purchasing.orderedValue")}
            receivedColLabel={t("purchasing.received")}
            outstandingColLabel={t("purchasing.outstanding")}
            testId="po-lines-table"
            lineTestIdPrefix="po-line"
          />

          {orderTotal ? (
            <PoDocumentTotals
              rows={[
                {
                  key: "orderTotal",
                  label: t(orderTotal.labelKey),
                  amount: orderTotal.amount,
                  emphasis: "strong",
                  testId: "po-order-total",
                },
              ]}
            />
          ) : null}
        </>
      )}

      <PurchaseOrderTimelineDrawer
        open={timelineOpen}
        onOpenChange={setTimelineOpen}
        po={po}
        receipts={receipts}
        resolveActor={actors.resolve}
        isResolving={actors.isResolving}
        receiptsLoading={receiptsQuery.isLoading}
        renderReceiptDetail={(receiptId) => {
          const receipt = receipts.find((r) => r.goodsReceiptId === receiptId);
          if (!receipt || !workspace) {
            return null;
          }
          return (
            <GoodsReceiptCard
              receipt={receipt}
              workspace={workspace}
              resolveActor={actors.resolve}
              isResolving={actors.isResolving}
              allowManage={allowManage}
              online={online}
              onReversed={async (updated) => {
                queryClient.setQueryData(
                  ["purchase-order-receipts", workspace.organizationId, purchaseOrderId],
                  (prev: PosGoodsReceiptDto[] | undefined) =>
                    (prev ?? []).map((r) =>
                      r.goodsReceiptId === updated.goodsReceiptId ? updated : r,
                    ),
                );
                await queryClient.invalidateQueries({
                  queryKey: ["purchase-order", workspace.organizationId, purchaseOrderId],
                });
                await queryClient.invalidateQueries({
                  queryKey: [
                    "purchase-order-receipts",
                    workspace.organizationId,
                    purchaseOrderId,
                  ],
                });
                await queryClient.invalidateQueries({ queryKey: ["inventory"] });
              }}
            />
          );
        }}
      />

      <div className="po-document-actions" data-testid="po-detail-actions">
        <div className="po-document-actions__primary">
          {canAcceptChanges ? (
            <>
              <Button
                type="button"
                disabled={busy}
                onClick={() =>
                  void runAction(
                    () => acceptConnectedPurchaseOrderChanges(workspace, purchaseOrderId),
                    "purchasing.changesAccepted",
                  )
                }
                data-testid="po-accept-changes"
              >
                {t("purchasing.acceptChanges")}
              </Button>
              <Button
                type="button"
                variant="ghost"
                disabled={busy}
                onClick={() =>
                  void runAction(
                    () => declineConnectedPurchaseOrderChanges(workspace, purchaseOrderId),
                    "purchasing.changesDeclined",
                  )
                }
                data-testid="po-decline-changes"
              >
                {t("purchasing.declineChanges")}
              </Button>
            </>
          ) : null}
          {canEditDraft ? (
            <Button asChild variant="outline" data-testid="po-edit-order">
              <Link to={`/purchasing/${purchaseOrderId}/edit`}>{t("purchasing.editOrder")}</Link>
            </Button>
          ) : null}
          {canReceive ? (
            <Button asChild data-testid="po-receive">
              <Link to={`/purchasing/${purchaseOrderId}/receive`}>{t("purchasing.receive")}</Link>
            </Button>
          ) : null}
          {canCancel ? (
            <Button
              type="button"
              intent="danger"
              appearance="solid"
              disabled={busy}
              onClick={() =>
                void runAction(
                  () => cancelPurchaseOrder(workspace, purchaseOrderId),
                  "purchasing.cancelled",
                )
              }
              data-testid="po-cancel"
            >
              {t("purchasing.cancel")}
            </Button>
          ) : null}
          {canSubmit ? (
            <Button
              type="button"
              disabled={busy}
              onClick={() =>
                void (async () => {
                  if (connectedRelationshipId) {
                    try {
                      const readiness = await getBuyerConnectedSupplierCommerceReadiness(
                        workspace,
                        connectedRelationshipId,
                      );
                      await queryClient.invalidateQueries({
                        queryKey: [
                          "connected-suppliers",
                          "commerce-readiness",
                          connectedRelationshipId,
                        ],
                      });
                      if (!readiness.isReady) {
                        setError(t("purchasing.supplierNotReadySubmitBlocked"));
                        return;
                      }
                    } catch {
                      setError(t("purchasing.supplierNotReadySubmitBlocked"));
                      return;
                    }
                  }
                  await runAction(
                    () => submitPurchaseOrder(workspace, purchaseOrderId),
                    "purchasing.submitted",
                    {
                      reconcile: async () => {
                        const latest = await getPurchaseOrder(workspace, purchaseOrderId);
                        return latest.status.toLowerCase() === "ordered";
                      },
                    },
                  );
                })()
              }
              data-testid="po-submit"
            >
              {t("purchasing.submit")}
            </Button>
          ) : po?.status === "Draft" &&
            allowManage &&
            online &&
            connectedRelationshipId &&
            !supplierCommerceReady ? (
            <Button type="button" disabled data-testid="po-submit">
              {t("purchasing.submit")}
            </Button>
          ) : null}
        </div>
      </div>

      {documentPreviewOpen ? (
        <BusinessDocumentPreview
          open={documentPreviewOpen}
          onClose={() => setDocumentPreviewOpen(false)}
          title={documentSettings.purchaseOrder.title || t("purchasing.detailTitle")}
          closeLabel={t("summary.closePreview")}
          printLabel={t("exitsTable.print")}
          pdfLabel={t("exitsTable.exportPdf")}
          testId="po-document-preview"
        >
          {purchaseOrderDocument}
        </BusinessDocumentPreview>
      ) : (
        <div className="exits-bizdoc-print-host" aria-hidden data-testid="po-print-host">
          {purchaseOrderDocument}
        </div>
      )}
    </div>
  );
}

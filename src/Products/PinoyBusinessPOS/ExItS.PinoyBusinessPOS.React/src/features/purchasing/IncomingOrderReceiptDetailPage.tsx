import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canViewPurchasing } from "@/access/pos-capabilities";
import { getIncomingOrder } from "@/api/pos/pos-connected-suppliers-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { Card } from "@/components/ui/card";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import { formatActivityDateTime } from "@/features/purchasing/purchase-order-activity";
import { useI18n } from "@/i18n/I18nProvider";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Seller view of a single buyer goods receipt for a connected incoming PO.
 * Smart Back returns to the incoming order detail (return context from navigateWithReturn).
 */
export function IncomingOrderReceiptDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const { connectedPurchaseOrderId, goodsReceiptId } = useParams<{
    connectedPurchaseOrderId: string;
    goodsReceiptId: string;
  }>();

  const smartBack = usePageSmartBack({
    fallback: connectedPurchaseOrderId
      ? `/purchasing/incoming-orders/${connectedPurchaseOrderId}`
      : "incomingOrders",
    backLabel: t("shell.back"),
    backTestId: "page-header-back-incoming-order-receipt",
  });

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming-order", connectedPurchaseOrderId],
    enabled: Boolean(workspace) && online && allowView && Boolean(connectedPurchaseOrderId),
    queryFn: ({ signal }) => getIncomingOrder(workspace!, connectedPurchaseOrderId!, signal),
  });

  const receipt = query.data?.buyerReceipts?.find((r) => r.goodsReceiptId === goodsReceiptId);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }
  if (query.isError || !query.data) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="incoming-order-receipt-detail-page">
        <PageHeader title={t("incomingOrders.receiptDetailTitle")} {...smartBack} />
        <ErrorState title={t("error.title")} detail={t("incomingOrders.notFound")} />
      </div>
    );
  }
  if (!receipt) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="incoming-order-receipt-detail-page">
        <PageHeader title={t("incomingOrders.receiptDetailTitle")} {...smartBack} />
        <ErrorState title={t("error.title")} detail={t("incomingOrders.receiptNotFound")} />
      </div>
    );
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="incoming-order-receipt-detail-page">
      <PageHeader
        title={receipt.grnNumber}
        {...smartBack}
        actions={
          <StatusChip tone={receipt.status === "Voided" ? "danger" : "success"}>
            {receipt.status === "Voided"
              ? t("incomingOrders.receiptVoided")
              : t("incomingOrders.receiptPosted")}
          </StatusChip>
        }
      />

      <Card className="flex flex-col gap-3 p-4" data-testid="incoming-order-receipt-detail-summary">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {(() => {
            const { date, time } = formatActivityDateTime(receipt.receivedAtUtc);
            return time ? `${date} ${time}` : date;
          })()}
        </p>
        <p className="m-0">
          {t("incomingOrders.buyer")}:{" "}
          <span className="font-medium">
            {query.data.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
          </span>
        </p>
        {receipt.deliveryReference?.trim() ? (
          <p className="m-0">
            {t("purchasing.deliveryReference")}: {receipt.deliveryReference.trim()}
          </p>
        ) : null}
        {receipt.notes?.trim() ? (
          <p className="m-0" data-testid="incoming-order-receipt-notes">
            {t("purchasing.notes")}: {receipt.notes.trim()}
          </p>
        ) : null}
        <p className="m-0 tabular-nums">
          {t("incomingOrders.colGoodReceived")}: {formatStockQtyLabel(receipt.goodQtyTotal)}
          {" · "}
          {t("incomingOrders.colDamaged")}: {formatStockQtyLabel(receipt.damagedQtyTotal)}
          {receipt.missingQtyTotal > 0
            ? ` · ${t("incomingOrders.colMissing")}: ${formatStockQtyLabel(receipt.missingQtyTotal)}`
            : ""}
        </p>
      </Card>

      <Card className="overflow-hidden p-0" data-testid="incoming-order-receipt-detail-lines">
        <div className="po-document-lines__table-wrap">
          <table className="po-document-lines__table">
            <thead>
              <tr>
                <th scope="col">{t("purchasing.colProduct")}</th>
                <th scope="col" className="po-document-lines__num">
                  {t("incomingOrders.colGoodReceived")}
                </th>
                <th scope="col" className="po-document-lines__num">
                  {t("incomingOrders.colDamaged")}
                </th>
                <th scope="col" className="po-document-lines__num">
                  {t("incomingOrders.colMissing")}
                </th>
                <th scope="col">{t("purchasing.remainingDecisionTitle")}</th>
              </tr>
            </thead>
            <tbody>
              {receipt.lines.map((line) => (
                <tr key={`${line.productId}-${line.nameSnapshot}`}>
                  <td className="font-medium">{line.nameSnapshot}</td>
                  <td className="po-document-lines__num tabular-nums">
                    {formatStockQtyLabel(line.goodQty, line.uomSnapshot)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">
                    {formatStockQtyLabel(line.damagedQty, line.uomSnapshot)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">
                    {formatStockQtyLabel(line.missingQty, line.uomSnapshot)}
                  </td>
                  <td className="text-[length:var(--exits-text-sm)]">
                    {line.remainingAction === "CancelRemaining"
                      ? t("purchasing.remainingActionCancel")
                      : line.remainingAction === "DeliverLater"
                        ? t("purchasing.remainingActionDeliverLater")
                        : "—"}
                    {line.discrepancyNote?.trim() ? (
                      <div className="text-muted">{line.discrepancyNote}</div>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {receipt.lines.some((l) => l.discrepancyNote?.trim()) ? (
          <ul className="m-0 list-none flex-col gap-2 border-t border-border p-4">
            {receipt.lines
              .filter((l) => l.discrepancyNote?.trim())
              .map((line) => (
                <li key={`note-${line.productId}`} className="text-[length:var(--exits-text-sm)]">
                  <span className="font-medium">{line.nameSnapshot}</span>: {line.discrepancyNote}
                </li>
              ))}
          </ul>
        ) : null}
      </Card>
    </div>
  );
}

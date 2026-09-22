import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import { canViewPurchasing } from "@/access/pos-capabilities";
import { getIncomingOrder } from "@/api/pos/pos-connected-suppliers-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Card } from "@/components/ui/card";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import { PoProcessHeaderActions } from "@/features/purchasing/PoProcessHeaderActions";
import {
  buildConnectedPurchaseOrderActivityEvents,
  formatActivityDateTime,
} from "@/features/purchasing/purchase-order-activity";
import { PurchaseOrderTimelineDrawer } from "@/features/purchasing/PurchaseOrderTimelineDrawer";
import { useI18n } from "@/i18n/I18nProvider";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";
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
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);

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

  const actors = useActorDirectory(workspace?.organizationId, []);
  const receipt = query.data?.buyerReceipts?.find((r) => r.goodsReceiptId === goodsReceiptId);
  const timelineEvents = useMemo(
    () => (query.data ? buildConnectedPurchaseOrderActivityEvents(query.data) : []),
    [query.data],
  );
  const hasTimeline = timelineEvents.length > 0;

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

  const order = query.data;
  const statusLabel =
    receipt.status === "Voided"
      ? t("incomingOrders.receiptVoided")
      : t("incomingOrders.receiptPosted");
  const statusTone = receipt.status === "Voided" ? "danger" : "success";

  function runReceiptOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    const filenameBase = sanitizeCsvFilenamePart(receipt.grnNumber || "grn");
    const rows = receipt.lines.map((line) => [
      line.nameSnapshot,
      line.goodQty,
      line.damagedQty,
      line.missingQty,
      line.remainingAction ?? "",
      line.discrepancyNote ?? "",
    ]);

    if (action === "csv") {
      const text = buildCsvWithMetadata(
        [
          ["Goods receipt", receipt.grnNumber],
          ["PO", order.buyerPoNumber ?? ""],
          ["Status", statusLabel],
          ["Exported at", new Date().toISOString()],
        ],
        {
          headers: ["Product", "Good", "Damaged", "Missing", "Remaining", "Note"],
          rows,
        },
      );
      downloadCsvFile(`${filenameBase}.csv`, text);
      return;
    }

    if (action === "xlsx") {
      const workbook = XLSX.utils.book_new();
      const sheet = XLSX.utils.aoa_to_sheet([
        ["Goods receipt", receipt.grnNumber],
        ["PO", order.buyerPoNumber ?? ""],
        ["Status", statusLabel],
        [],
        ["Product", "Good", "Damaged", "Missing", "Remaining", "Note"],
        ...rows,
      ]);
      XLSX.utils.book_append_sheet(workbook, sheet, "Receipt");
      const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
      downloadBlob(
        `${filenameBase}.xlsx`,
        buffer,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      );
      return;
    }

    if (action === "pdf") {
      const doc = new jsPDF({ orientation: "portrait", unit: "pt", format: "a4" });
      doc.setFontSize(14);
      doc.text(receipt.grnNumber, 40, 40);
      doc.setFontSize(10);
      doc.text(`${statusLabel} · ${new Date().toLocaleString()}`, 40, 58);
      autoTable(doc, {
        startY: 72,
        head: [["Product", "Good", "Damaged", "Missing", "Remaining", "Note"]],
        body: rows.map((row) => row.map((cell) => String(cell))),
        styles: { fontSize: 9, cellPadding: 4 },
        headStyles: { fillColor: [55, 75, 60] },
      });
      downloadBlob(
        `${filenameBase}.pdf`,
        new Blob([new Uint8Array(doc.output("arraybuffer") as ArrayBuffer)], {
          type: "application/pdf",
        }),
        "application/pdf",
      );
      return;
    }

    const previous = document.body.classList.contains("exits-printing");
    document.body.classList.add("exits-printing");
    const cleanup = () => {
      if (!previous) {
        document.body.classList.remove("exits-printing");
      }
      window.removeEventListener("afterprint", cleanup);
    };
    window.addEventListener("afterprint", cleanup);
    window.print();
    window.setTimeout(cleanup, 1000);
  }

  const printDocument = (
    <div className="incoming-order-print-root" data-testid="incoming-order-receipt-print-root">
      <h1>{receipt.grnNumber}</h1>
      <p>{statusLabel}</p>
      <p>
        {t("incomingOrders.buyer")}:{" "}
        {order.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
      </p>
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th>Good</th>
            <th>Damaged</th>
            <th>Missing</th>
          </tr>
        </thead>
        <tbody>
          {receipt.lines.map((line) => (
            <tr key={`${line.productId}-${line.nameSnapshot}`}>
              <td>{line.nameSnapshot}</td>
              <td>{formatStockQtyLabel(line.goodQty, line.uomSnapshot)}</td>
              <td>{formatStockQtyLabel(line.damagedQty, line.uomSnapshot)}</td>
              <td>{formatStockQtyLabel(line.missingQty, line.uomSnapshot)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="incoming-order-receipt-detail-page">
      <div className="exits-bizdoc-print-host" aria-hidden>
        {printDocument}
      </div>

      <PageHeader
        title={receipt.grnNumber}
        {...smartBack}
        actions={
          <PoProcessHeaderActions
            statusLabel={statusLabel}
            statusTone={statusTone}
            timelineEnabled={hasTimeline}
            onTimeline={() => setTimelineOpen(true)}
            onPreview={() => setDocumentPreviewOpen(true)}
            onPrint={() => runReceiptOutput("print")}
            onCsv={() => runReceiptOutput("csv")}
            onXlsx={() => runReceiptOutput("xlsx")}
            onPdf={() => runReceiptOutput("pdf")}
          />
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
            {order.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
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
                <th scope="col" className="po-document-lines__num po-document-lines__num--start">
                  {t("incomingOrders.colGoodReceived")}
                </th>
                <th scope="col" className="po-document-lines__num po-document-lines__num--start">
                  {t("incomingOrders.colDamaged")}
                </th>
                <th scope="col" className="po-document-lines__num po-document-lines__num--start">
                  {t("incomingOrders.colMissing")}
                </th>
                <th scope="col">{t("purchasing.remainingDecisionTitle")}</th>
              </tr>
            </thead>
            <tbody>
              {receipt.lines.map((line) => (
                <tr key={`${line.productId}-${line.nameSnapshot}`}>
                  <td className="font-medium">{line.nameSnapshot}</td>
                  <td className="po-document-lines__num po-document-lines__num--start tabular-nums">
                    {formatStockQtyLabel(line.goodQty, line.uomSnapshot)}
                  </td>
                  <td className="po-document-lines__num po-document-lines__num--start tabular-nums">
                    {formatStockQtyLabel(line.damagedQty, line.uomSnapshot)}
                  </td>
                  <td className="po-document-lines__num po-document-lines__num--start tabular-nums">
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

      <PurchaseOrderTimelineDrawer
        open={timelineOpen}
        onOpenChange={setTimelineOpen}
        titleHint={order.buyerPoNumber}
        events={timelineEvents}
        resolveActor={actors.resolve}
        isResolving={actors.isResolving}
      />

      {documentPreviewOpen ? (
        <BusinessDocumentPreview
          open={documentPreviewOpen}
          onClose={() => setDocumentPreviewOpen(false)}
          title={receipt.grnNumber}
          closeLabel={t("summary.closePreview")}
          printLabel={t("exitsTable.print")}
          pdfLabel={t("exitsTable.exportPdf")}
          showPdf={false}
          testId="incoming-order-receipt-document-preview"
        >
          {printDocument}
        </BusinessDocumentPreview>
      ) : null}
    </div>
  );
}

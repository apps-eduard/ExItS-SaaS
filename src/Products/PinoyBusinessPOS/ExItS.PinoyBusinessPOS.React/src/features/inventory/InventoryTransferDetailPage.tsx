import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowRight,
  Ban,
  FilePlus2,
  PackageCheck,
  PackageOpen,
  Truck,
} from "lucide-react";
import * as XLSX from "xlsx";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  cancelInventoryTransfer,
  dispatchInventoryTransfer,
  getInventoryTransfer,
  receiveInventoryTransfer,
  type InventoryTransferDto,
} from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import { InventoryTransferActivityTimeline } from "@/features/inventory/InventoryTransferActivityTimeline";
import { buildTransferActivityEvents } from "@/features/inventory/inventory-transfer-activity";
import { InventoryTransferReceiveMode } from "@/features/inventory/InventoryTransferReceiveMode";
import {
  branchDisplayName,
  formatTransferQty,
  inventoryTransferDiscrepancyLabelKey,
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusTone,
  isReceiveLineReady,
  parseReceivedQuantity,
} from "@/features/inventory/inventory-transfer-labels";
import { PoProcessHeaderActions } from "@/features/purchasing/PoProcessHeaderActions";
import { useI18n } from "@/i18n/I18nProvider";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Mode = "detail" | "receive";
type ConfirmKind = "dispatch" | "cancel" | "receive" | null;
type LocalError = { title: string; detail: string };

function resolveTransferActionError(err: unknown, fallback: string): string {
  if (err instanceof PosApiError) {
    const detail = err.problem.detail?.trim();
    if (detail) {
      return detail;
    }
    const title = err.problem.title?.trim();
    if (title) {
      return title;
    }
    if (err.message.trim()) {
      return err.message.trim();
    }
  }
  return fallback;
}

function inventoryTransferStatusIcon(status: string) {
  switch (status) {
    case "InTransit":
      return <Truck aria-hidden />;
    case "PartiallyReceived":
      return <PackageOpen aria-hidden />;
    case "Received":
      return <PackageCheck aria-hidden />;
    case "Cancelled":
      return <Ban aria-hidden />;
    case "Draft":
    default:
      return <FilePlus2 aria-hidden />;
  }
}

export function InventoryTransferDetailPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { showToast } = useToast();
  const { transferId = "" } = useParams();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const smartBack = usePageSmartBack({
    fallback: "transfers",
    backLabel: t("transfer.backList"),
    backTestId: "page-header-back-transfers",
  });

  const [localError, setLocalError] = useState<LocalError | null>(null);
  const [busy, setBusy] = useState(false);
  const busyRef = useRef(false);
  const [mode, setMode] = useState<Mode>("detail");
  const [confirmKind, setConfirmKind] = useState<ConfirmKind>(null);
  const [receivedByLine, setReceivedByLine] = useState<Record<string, string>>({});
  const [reasonByLine, setReasonByLine] = useState<Record<string, string>>({});
  const [noteByLine, setNoteByLine] = useState<Record<string, string>>({});
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const actingBranchId = boundWorkspace?.branchId ?? null;

  const query = useQuery({
    queryKey: ["inventory-transfer", workspace?.organizationId, transferId],
    enabled: Boolean(workspace) && Boolean(transferId) && online,
    queryFn: ({ signal }) => getInventoryTransfer(workspace!, transferId, signal),
  });

  const transfer = query.data;

  const activityEvents = useMemo(
    () => (transfer ? buildTransferActivityEvents(transfer) : []),
    [transfer],
  );
  const actorIds = useMemo(
    () =>
      activityEvents
        .map((event) => event.actorId)
        .filter((id): id is string => Boolean(id)),
    [activityEvents],
  );
  const actors = useActorDirectory(workspace?.organizationId, actorIds);

  useEffect(() => {
    const flash = (location.state as { flash?: string } | null)?.flash;
    if (flash === "created") {
      showToast(t("transfer.createdSuccess"), "success");
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.pathname, location.state, navigate, showToast, t]);

  useEffect(() => {
    if (!transfer || transfer.status !== "InTransit") {
      return;
    }
    const next: Record<string, string> = {};
    for (const line of transfer.lines) {
      next[line.lineId] = String(line.sentQty);
    }
    setReceivedByLine(next);
    setReasonByLine({});
    setNoteByLine({});
    setMode("detail");
  }, [transfer?.transferId, transfer?.status, transfer?.updatedAtUtc]);

  async function refreshAfter(
    mutation: () => Promise<InventoryTransferDto>,
    successMessage: string,
    failureTitle: string,
  ) {
    if (!workspace || busyRef.current) {
      return;
    }
    busyRef.current = true;
    setBusy(true);
    setLocalError(null);
    try {
      const updated = await mutation();
      queryClient.setQueryData(
        ["inventory-transfer", workspace.organizationId, transferId],
        updated,
      );
      await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      showToast(successMessage, "success");
      setMode("detail");
    } catch (err) {
      const detail = resolveTransferActionError(err, t("transfer.actionFailed"));
      setLocalError({ title: failureTitle, detail });
      showToast(detail, "error");
    } finally {
      busyRef.current = false;
      setBusy(false);
      setConfirmKind(null);
    }
  }

  async function onDispatch() {
    if (!workspace || !transfer || busyRef.current) {
      return;
    }
    const { transferId: id, destinationBranchName, destinationBranchId } = transfer;
    const dest = branchDisplayName(destinationBranchName, destinationBranchId);
    await refreshAfter(
      () => dispatchInventoryTransfer(workspace, id),
      t("transfer.dispatchedSuccess").replace("{destination}", dest),
      t("transfer.dispatchFailedTitle"),
    );
  }

  async function onCancel() {
    if (!workspace || !transfer || busyRef.current) {
      return;
    }
    const successMessage =
      transfer.status === "InTransit"
        ? t("transfer.cancelledRestoredSuccess").replace(
            "{source}",
            branchDisplayName(transfer.sourceBranchName, transfer.sourceBranchId),
          )
        : t("transfer.cancelledSuccess");
    await refreshAfter(
      () => cancelInventoryTransfer(workspace, transfer.transferId),
      successMessage,
      t("transfer.cancelFailedTitle"),
    );
  }

  async function onReceive() {
    if (!workspace || !transfer || busyRef.current) {
      return;
    }
    const lines: Array<{
      productId: string;
      receivedQty: number;
      lineId: string;
      discrepancyReason?: string | null;
      discrepancyNote?: string | null;
    }> = [];
    for (const line of transfer.lines) {
      const parsed = parseReceivedQuantity(receivedByLine[line.lineId] ?? "", line.sentQty);
      if (parsed === "empty" || parsed === "invalid") {
        setLocalError({
          title: t("transfer.receiveFailedTitle"),
          detail: t("transfer.invalidReceivedQuantity"),
        });
        setConfirmKind(null);
        return;
      }
      if (parsed === "exceeds") {
        setLocalError({
          title: t("transfer.receiveFailedTitle"),
          detail: t("transfer.receivedExceedsSent").replace(
            "{sent}",
            formatTransferQty(line.sentQty),
          ),
        });
        setConfirmKind(null);
        return;
      }
      const entry: (typeof lines)[number] = {
        productId: line.productId,
        receivedQty: parsed,
        lineId: line.lineId,
      };
      if (parsed < line.sentQty) {
        const reason = reasonByLine[line.lineId]?.trim();
        if (!reason) {
          setLocalError({
            title: t("transfer.receiveFailedTitle"),
            detail: t("transfer.discrepancyReasonRequired"),
          });
          setConfirmKind(null);
          return;
        }
        entry.discrepancyReason = reason;
        const note = noteByLine[line.lineId]?.trim();
        if (note) {
          entry.discrepancyNote = note;
        }
      }
      lines.push(entry);
    }

    busyRef.current = true;
    setBusy(true);
    setLocalError(null);
    try {
      const updated = await receiveInventoryTransfer(workspace, transfer.transferId, { lines });
      queryClient.setQueryData(
        ["inventory-transfer", workspace.organizationId, transferId],
        updated,
      );
      await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      const dest = branchDisplayName(updated.destinationBranchName, updated.destinationBranchId);
      showToast(
        updated.status === "PartiallyReceived"
          ? t("transfer.partiallyReceivedSuccess")
          : t("transfer.receivedSuccess").replace("{destination}", dest),
        "success",
      );
      setMode("detail");
    } catch (err) {
      const detail = resolveTransferActionError(err, t("transfer.actionFailed"));
      setLocalError({ title: t("transfer.receiveFailedTitle"), detail });
      showToast(detail, "error");
    } finally {
      busyRef.current = false;
      setBusy(false);
      setConfirmKind(null);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (query.isLoading) {
    return <LoadingState label={t("transfer.loading")} />;
  }

  if (query.isError || !transfer) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-detail-missing">
        <PageHeader
          title={t("transfer.title")}
          {...smartBack}
        />
        <ErrorState title={t("transfer.errorTitle")} detail={t("transfer.notFound")} />
      </div>
    );
  }

  const sourceName = branchDisplayName(transfer.sourceBranchName, transfer.sourceBranchId);
  const destName = branchDisplayName(transfer.destinationBranchName, transfer.destinationBranchId);
  const isSource = actingBranchId === transfer.sourceBranchId;
  const isDestination = actingBranchId === transfer.destinationBranchId;
  const isDraft = transfer.status === "Draft";
  const isInTransit = transfer.status === "InTransit";
  const isFinal =
    transfer.status === "Received" ||
    transfer.status === "PartiallyReceived" ||
    transfer.status === "Cancelled";
  const canMutate = allowManage && online && !busy;
  const canDispatch = canMutate && isSource && isDraft;
  const canCancel = canMutate && isSource && (isDraft || isInTransit);
  const canReceive = canMutate && isDestination && isInTransit;
  const receiveFormReady = transfer.lines.every((line) =>
    isReceiveLineReady(
      receivedByLine[line.lineId] ?? "",
      line.sentQty,
      reasonByLine[line.lineId],
    ),
  );
  const canSubmitReceive = canReceive && receiveFormReady;

  const statusLabel = t(inventoryTransferStatusLabelKey(transfer.status));
  const statusTone = inventoryTransferStatusTone(transfer.status);
  const transferTitle =
    transfer.transferNumber?.trim() || t("transfer.summaryTitle");

  async function runTransferOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const stamp = new Date().toISOString().slice(0, 10);
      const numberPart = sanitizeCsvFilenamePart(
        transfer.transferNumber?.trim() || transfer.transferId.slice(0, 8),
      );
      if (action === "csv") {
        const csv = buildCsvWithMetadata(
          [
            ["Transfer", transfer.transferNumber ?? transfer.transferId],
            ["Status", statusLabel],
            ["Route", `${sourceName} → ${destName}`],
            ["Generated", stamp],
          ],
          {
            headers: [
              t("purchasing.colProduct"),
              t("transfer.sent"),
              t("transfer.received"),
              t("transfer.difference"),
              t("transfer.lot"),
              t("transfer.expiry"),
            ],
            rows: transfer.lines.map((line) => [
              line.productName,
              formatTransferQty(line.sentQty),
              formatTransferQty(line.receivedQty),
              formatTransferQty(line.differenceQty),
              line.lotNumber ?? "",
              line.expirationDate ?? "",
            ]),
          },
        );
        downloadCsvFile(`inventory-transfer-${numberPart}-${stamp}.csv`, csv);
        return;
      }
      if (action === "xlsx") {
        const sheet = XLSX.utils.aoa_to_sheet([
          ["Transfer", transfer.transferNumber ?? transfer.transferId],
          ["Status", statusLabel],
          ["Route", `${sourceName} → ${destName}`],
          [],
          [
            t("purchasing.colProduct"),
            t("transfer.sent"),
            t("transfer.received"),
            t("transfer.difference"),
            t("transfer.lot"),
            t("transfer.expiry"),
          ],
          ...transfer.lines.map((line) => [
            line.productName,
            formatTransferQty(line.sentQty),
            formatTransferQty(line.receivedQty),
            formatTransferQty(line.differenceQty),
            line.lotNumber ?? "",
            line.expirationDate ?? "",
          ]),
        ]);
        const book = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(book, sheet, "Transfer");
        const buffer = XLSX.write(book, { bookType: "xlsx", type: "array" });
        downloadBlob(
          `inventory-transfer-${numberPart}-${stamp}.xlsx`,
          buffer,
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        );
        return;
      }
      window.print();
    } catch {
      showToast(t("exitsTable.outputFailed"), "error");
    }
  }

  const printDocument = (
    <div className="incoming-order-print-document">
      <h1>{t("transfer.summaryTitle")}</h1>
      {transfer.transferNumber?.trim() ? <p>{transfer.transferNumber.trim()}</p> : null}
      <p>
        {sourceName} → {destName}
      </p>
      <p>
        {t("purchasing.fieldStatus")}: {statusLabel}
      </p>
      <table>
        <thead>
          <tr>
            <th>{t("purchasing.colProduct")}</th>
            <th>{t("transfer.sent")}</th>
            <th>{t("transfer.received")}</th>
            <th>{t("transfer.difference")}</th>
          </tr>
        </thead>
        <tbody>
          {transfer.lines.map((line) => (
            <tr key={line.lineId}>
              <td>{line.productName}</td>
              <td>
                {formatTransferQty(line.sentQty)} {line.unitOfMeasure}
              </td>
              <td>
                {formatTransferQty(line.receivedQty)} {line.unitOfMeasure}
              </td>
              <td>{formatTransferQty(line.differenceQty)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  const dialogCancelIcon = <Ban className="size-4 shrink-0" aria-hidden />;

  const localErrorAlert = localError ? (
    <Notice
      tone="danger"
      testId="transfer-local-error"
      title={localError.title}
      action={
        <Button
          type="button"
          variant="ghost"
          className="h-auto min-h-0 shrink-0 px-1 py-0 text-[length:var(--exits-text-xs)] text-muted"
          onClick={() => setLocalError(null)}
          data-testid="transfer-local-error-dismiss"
        >
          {t("transfer.dialogCancel")}
        </Button>
      }
    >
      <p className="text-[length:var(--exits-text-xs)] text-muted wrap-break-word">{localError.detail}</p>
    </Notice>
  ) : null;

  const confirmDialog =
    confirmKind === "dispatch" ? (
      <ConfirmationDialog
        open
        title={t("transfer.dispatchConfirmTitle")}
        detail={t("transfer.dispatchConfirmDetail")
          .replace("{source}", sourceName)
          .replace("{destination}", destName)
          .replace("{count}", String(transfer.lines.length))}
        confirmLabel={t("transfer.dispatch")}
        confirmPendingLabel={t("transfer.dispatching")}
        confirmIcon={<Truck className="size-4 shrink-0" aria-hidden />}
        cancelLabel={t("transfer.dialogCancel")}
        cancelIcon={dialogCancelIcon}
        cancelTone="danger-outline"
        busy={busy}
        testId="transfer-dispatch-confirm"
        onCancel={() => {
          if (!busy) {
            setConfirmKind(null);
          }
        }}
        onConfirm={() => void onDispatch()}
      />
    ) : confirmKind === "cancel" ? (
      <ConfirmationDialog
        open
        title={t("transfer.cancelConfirmTitle")}
        detail={
          transfer.status === "InTransit"
            ? t("transfer.cancelInTransitConfirmDetail")
            : t("transfer.cancelDraftConfirmDetail")
        }
        confirmLabel={t("transfer.cancel")}
        confirmPendingLabel={t("transfer.cancelling")}
        confirmIcon={<Ban className="size-4 shrink-0" aria-hidden />}
        cancelLabel={t("transfer.dialogCancel")}
        cancelIcon={dialogCancelIcon}
        cancelTone="danger-outline"
        confirmTone="danger"
        busy={busy}
        testId="transfer-cancel-confirm"
        onCancel={() => {
          if (!busy) {
            setConfirmKind(null);
          }
        }}
        onConfirm={() => void onCancel()}
      />
    ) : confirmKind === "receive" ? (
      <ConfirmationDialog
        open
        title={t("transfer.receiveConfirmTitle")}
        detail={t("transfer.receiveFinalConfirmDetail")}
        confirmLabel={t("transfer.receive")}
        confirmPendingLabel={t("transfer.receiving")}
        confirmIcon={<PackageCheck className="size-4 shrink-0" aria-hidden />}
        cancelLabel={t("transfer.dialogCancel")}
        cancelIcon={dialogCancelIcon}
        cancelTone="danger-outline"
        busy={busy}
        testId="transfer-receive-confirm"
        onCancel={() => {
          if (!busy) {
            setConfirmKind(null);
          }
        }}
        onConfirm={() => void onReceive()}
      />
    ) : null;

  if (mode === "receive" && isInTransit) {
    return (
      <InventoryTransferReceiveMode
        transfer={transfer}
        sourceName={sourceName}
        destName={destName}
        statusLabel={statusLabel}
        statusIcon={inventoryTransferStatusIcon(transfer.status)}
        receivedByLine={receivedByLine}
        reasonByLine={reasonByLine}
        noteByLine={noteByLine}
        setReceivedByLine={setReceivedByLine}
        setReasonByLine={setReasonByLine}
        setNoteByLine={setNoteByLine}
        canSubmitReceive={canSubmitReceive}
        busy={busy}
        online={online}
        localErrorAlert={localErrorAlert}
        confirmDialog={confirmDialog}
        onBack={() => setMode("detail")}
        onSubmit={() => {
          setLocalError(null);
          setConfirmKind("receive");
        }}
      />
    );
  }

  return (
    <div
      className="inventory-transfer-detail-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="inventory-transfer-detail-page"
      data-status={transfer.status}
    >
      <div className="exits-bizdoc-print-host" aria-hidden>
        {printDocument}
      </div>

      <PageHeader
        title={t("transfer.summaryTitle")}
        {...smartBack}
        actions={
          <PoProcessHeaderActions
            statusLabel={statusLabel}
            statusTone={statusTone}
            statusIcon={inventoryTransferStatusIcon(transfer.status)}
            timelineEnabled={activityEvents.length > 0}
            onTimeline={() => setTimelineOpen(true)}
            onPreview={() => setDocumentPreviewOpen(true)}
            onPrint={() => void runTransferOutput("print")}
            onCsv={() => void runTransferOutput("csv")}
            onXlsx={() => void runTransferOutput("xlsx")}
            onPdf={() => void runTransferOutput("pdf")}
            timelineTestId="transfer-timeline-open"
            previewTestId="transfer-document-preview-open"
          />
        }
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}
      {localErrorAlert}

      <Card
        className="flex min-w-0 flex-col gap-3 p-3"
        treatment="bordered"
        data-testid="transfer-route-summary"
      >
        <div className="flex min-w-0 flex-wrap items-center justify-center gap-2 sm:justify-start sm:gap-3">
          <span className="truncate text-[length:var(--exits-text-md)] font-semibold text-foreground">
            {sourceName}
          </span>
          <ArrowRight className="size-4 shrink-0 text-primary" aria-hidden />
          <span className="truncate text-[length:var(--exits-text-md)] font-semibold text-foreground">
            {destName}
          </span>
        </div>

        <div
          className="grid grid-cols-1 gap-2 sm:grid-cols-3"
          data-testid="transfer-qty-summary"
        >
          <Card
            className="flex flex-col gap-0.5 p-3"
            treatment="bordered"
            data-testid="transfer-number-summary"
          >
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("transfer.colNumber")}
            </p>
            <p className="m-0 truncate text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
              {transfer.transferNumber?.trim() || "—"}
            </p>
          </Card>
          <Card className="flex flex-col gap-0.5 p-3" treatment="bordered">
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.sent")}</p>
            <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
              {formatTransferQty(transfer.totalSentQty)}
            </p>
          </Card>
          <Card className="flex flex-col gap-0.5 p-3" treatment="bordered">
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.received")}</p>
            <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
              {formatTransferQty(transfer.totalReceivedQty)}
            </p>
          </Card>
        </div>
      </Card>

      {transfer.stockRequestId ? (
        <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="transfer-stock-request-link">
          <span className="text-muted">{t("transfer.requestedBy")}: {destName}. </span>
          <Link className="underline" to={`/inventory/stock-requests/${transfer.stockRequestId}`}>
            {t("transfer.stockRequest")}
          </Link>
        </p>
      ) : null}

      {transfer.notes ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="transfer-notes-display"
        >
          {transfer.notes}
        </p>
      ) : null}

      <section className="flex flex-col gap-1.5">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("transfer.items")}
        </h2>
        <ExitsTableContainer data-testid="transfer-lines">
          <ExitsTable data-testid="transfer-lines-desktop">
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text" colSize="flex">
                  {t("purchasing.colProduct")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text" colSize="sku">
                  {t("purchasing.colSku")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.colUnit")}</ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.sent")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.received")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.difference")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("transfer.lot")}</ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {transfer.lines.map((line) => {
                const showReceived = isFinal || isInTransit;
                return (
                  <ExitsTableRow
                    key={line.lineId}
                    data-testid={`transfer-line-${line.lineId}`}
                  >
                    <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                      <div>{line.productName}</div>
                      {line.discrepancyReason ? (
                        <div className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                          {t("transfer.discrepancy")}:{" "}
                          {t(inventoryTransferDiscrepancyLabelKey(line.discrepancyReason))}
                          {line.discrepancyNote ? ` — ${line.discrepancyNote}` : ""}
                        </div>
                      ) : null}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text" colSize="sku" className="text-muted tabular-nums">
                      {line.sku?.trim() || "—"}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">{line.unitOfMeasure}</ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(line.sentQty)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {showReceived ? formatTransferQty(line.receivedQty) : "—"}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {line.differenceQty !== 0 ? formatTransferQty(line.differenceQty) : "—"}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text" className="text-muted">
                      {line.lotNumber || line.expirationDate
                        ? `${line.lotNumber ?? "—"} · ${line.expirationDate ?? "—"}`
                        : "—"}
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>

          <ExitsTableMobile data-testid="transfer-lines-mobile">
            {transfer.lines.map((line) => {
              const showReceived = isFinal || isInTransit;
              return (
                <ExitsTableMobileRow
                  key={line.lineId}
                  data-testid={`transfer-line-${line.lineId}`}
                >
                  <div className="exits-table-mobile__title-row">
                    <span className="exits-table-mobile__title">{line.productName}</span>
                  </div>
                  <p className="exits-table-mobile__meta m-0">
                    {t("purchasing.colSku")}: {line.sku?.trim() || "—"}
                    {" · "}
                    {t("purchasing.colUnit")}: {line.unitOfMeasure}
                  </p>
                  <p className="exits-table-mobile__math mt-1 mb-0">
                    {t("transfer.sent")}: {formatTransferQty(line.sentQty)}
                    {showReceived
                      ? ` · ${t("transfer.received")}: ${formatTransferQty(line.receivedQty)}`
                      : ""}
                    {line.differenceQty !== 0
                      ? ` · ${t("transfer.difference")}: ${formatTransferQty(line.differenceQty)}`
                      : ""}
                  </p>
                  {line.lotNumber || line.expirationDate ? (
                    <p className="exits-table-mobile__meta m-0">
                      {t("transfer.lot")}: {line.lotNumber ?? "—"} · {t("transfer.expiry")}:{" "}
                      {line.expirationDate ?? "—"}
                    </p>
                  ) : null}
                  {line.discrepancyReason ? (
                    <p className="mt-1 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("transfer.discrepancy")}:{" "}
                      {t(inventoryTransferDiscrepancyLabelKey(line.discrepancyReason))}
                      {line.discrepancyNote ? ` — ${line.discrepancyNote}` : ""}
                    </p>
                  ) : null}
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>
        </ExitsTableContainer>
      </section>

      {canCancel || canDispatch || canReceive ? (
        <div className="receive-stock-actions" data-testid="transfer-detail-actions">
          {isDraft ? (
            <p className="m-0 me-auto text-[length:var(--exits-text-xs)] text-muted">
              {t("transfer.draftNoEdit")}
            </p>
          ) : null}
          <div className="receive-stock-actions__primary">
            <Button
              type="button"
              intent="primary"
              appearance="ghost"
              className="font-semibold"
              onClick={smartBack.onBack}
              data-testid="transfer-detail-back"
            >
              <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
              {smartBack.backLabel}
            </Button>
            {canCancel ? (
              <Button
                type="button"
                intent="danger"
                appearance="solid"
                disabled={!canMutate}
                onClick={() => {
                  setLocalError(null);
                  setConfirmKind("cancel");
                }}
                data-testid="transfer-cancel"
              >
                {t("transfer.cancel")}
              </Button>
            ) : null}
            {canReceive ? (
              <Button
                type="button"
                disabled={!canMutate}
                onClick={() => {
                  setLocalError(null);
                  setMode("receive");
                }}
                data-testid="transfer-receive"
              >
                <PackageCheck className="size-4 shrink-0" aria-hidden />
                {t("transfer.receive")}
              </Button>
            ) : null}
            {canDispatch ? (
              <Button
                type="button"
                disabled={!canMutate}
                onClick={() => {
                  setLocalError(null);
                  setConfirmKind("dispatch");
                }}
                data-testid="transfer-dispatch"
              >
                <Truck className="size-4 shrink-0" aria-hidden />
                {t("transfer.dispatch")}
              </Button>
            ) : null}
          </div>
        </div>
      ) : isDraft ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.draftNoEdit")}</p>
      ) : null}
      {confirmDialog}

      <SideDrawer
        open={timelineOpen}
        onClose={() => setTimelineOpen(false)}
        title={t("transfer.timelineTitle")}
        description={transfer.transferNumber?.trim() || undefined}
        testId="transfer-timeline-drawer"
        closeLabel={t("purchasing.timelineClose")}
        closeTestId="transfer-timeline-drawer-close"
        panelClassName="exits-form-drawer__panel exits-form-drawer__panel--lg"
      >
        <div className="exits-form-drawer" data-testid="transfer-timeline-drawer-content">
          <div className="exits-form-drawer__body">
            <InventoryTransferActivityTimeline
              events={activityEvents}
              resolveActor={actors.resolve}
              isResolving={actors.isResolving}
            />
          </div>
        </div>
      </SideDrawer>

      {documentPreviewOpen ? (
        <BusinessDocumentPreview
          open={documentPreviewOpen}
          onClose={() => setDocumentPreviewOpen(false)}
          title={transferTitle}
          closeLabel={t("summary.closePreview")}
          printLabel={t("exitsTable.print")}
          pdfLabel={t("exitsTable.exportPdf")}
          showPdf={false}
          testId="transfer-document-preview"
        >
          {printDocument}
        </BusinessDocumentPreview>
      ) : null}
    </div>
  );
}

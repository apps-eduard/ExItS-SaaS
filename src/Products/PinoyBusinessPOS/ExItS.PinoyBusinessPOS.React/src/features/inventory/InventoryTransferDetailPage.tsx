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
  closeRemainderInventoryTransfer,
  dispatchInventoryTransfer,
  dispatchInventoryTransferDamageReturn,
  getInventoryTransfer,
  inspectInventoryTransferDamageCustody,
  receiveInventoryTransfer,
  receiveInventoryTransferDamageReturn,
  type InventoryTransferDto,
  type ReceiveInventoryTransferRequest,
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
} from "@/features/inventory/inventory-transfer-labels";
import {
  canDestinationCloseRemainder,
  canDestinationReceiveTransfer,
  isTransferTerminalStatus,
  lineOutstandingQty,
} from "@/features/inventory/inventory-transfer-receive-helpers";
import { TransferCloseRemainderDialog } from "@/features/inventory/TransferCloseRemainderDialog";
import { PoProcessHeaderActions } from "@/features/purchasing/PoProcessHeaderActions";
import { useI18n } from "@/i18n/I18nProvider";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Mode = "detail" | "receive";
type ConfirmKind = "dispatch" | "cancel" | null;
type CloseRemainderOpen = boolean;
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
    case "ClosedWithDiscrepancy":
      return <PackageOpen aria-hidden />;
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
  const [closeRemainderOpen, setCloseRemainderOpen] = useState<CloseRemainderOpen>(false);
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);
  const [inspectCustodyId, setInspectCustodyId] = useState<string | null>(null);
  const [inspectRecoveredText, setInspectRecoveredText] = useState("0");
  const [inspectConfirmedText, setInspectConfirmedText] = useState("0");

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
    if (!transfer || !canDestinationReceiveTransfer(transfer)) {
      return;
    }
    setMode("detail");
  }, [transfer?.transferId, transfer?.status, transfer?.updatedAtUtc, transfer?.totalReceivedQty]);

  async function refreshAfter(
    mutation: () => Promise<unknown>,
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
      if (
        updated &&
        typeof updated === "object" &&
        "transferId" in updated &&
        "status" in updated &&
        "lines" in updated
      ) {
        queryClient.setQueryData(
          ["inventory-transfer", workspace.organizationId, transferId],
          updated,
        );
      } else {
        await queryClient.invalidateQueries({
          queryKey: ["inventory-transfer", workspace.organizationId, transferId],
        });
      }
      await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-request"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-request-activity"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-requests"] });
      await queryClient.invalidateQueries({ queryKey: ["wh-dash"] });
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

  async function onCloseRemainder(
    closeLines: Array<{
      lineId: string;
      productId: string;
      discrepancyReason: string;
      discrepancyNote?: string | null;
    }>,
  ) {
    if (!workspace || !transfer || busyRef.current) {
      return;
    }
    await refreshAfter(
      () => closeRemainderInventoryTransfer(workspace, transfer.transferId, { lines: closeLines }),
      t("transfer.closeRemainderSuccess"),
      t("transfer.closeRemainderFailedTitle"),
    );
    setCloseRemainderOpen(false);
  }

  async function onReceive(body: ReceiveInventoryTransferRequest) {
    if (!workspace || !transfer || busyRef.current) {
      return;
    }

    busyRef.current = true;
    setBusy(true);
    setLocalError(null);
    try {
      const updated = await receiveInventoryTransfer(workspace, transfer.transferId, body);
      queryClient.setQueryData(
        ["inventory-transfer", workspace.organizationId, transferId],
        updated,
      );
      await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-request"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-request-activity"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-requests"] });
      await queryClient.invalidateQueries({ queryKey: ["wh-dash"] });
      const dest = branchDisplayName(updated.destinationBranchName, updated.destinationBranchId);
      showToast(
        updated.status === "Received"
          ? t("transfer.receivedSuccess").replace("{destination}", dest)
          : t("transfer.receiveWaveSuccess"),
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
    }
  }

  async function onDispatchDamageReturn(custodyId: string) {
    if (!workspace || busyRef.current) {
      return;
    }
    await refreshAfter(
      () => dispatchInventoryTransferDamageReturn(workspace, custodyId),
      "Damage return dispatched",
      t("transfer.actionFailed"),
    );
  }

  async function onReceiveDamageReturn(custodyId: string) {
    if (!workspace || busyRef.current) {
      return;
    }
    await refreshAfter(
      () => receiveInventoryTransferDamageReturn(workspace, custodyId),
      "Damage return received",
      t("transfer.actionFailed"),
    );
  }

  async function onInspectDamageCustody() {
    if (!workspace || !inspectCustodyId || busyRef.current) {
      return;
    }
    const recovered = Number(inspectRecoveredText);
    const confirmed = Number(inspectConfirmedText);
    if (!Number.isFinite(recovered) || recovered < 0 || !Number.isFinite(confirmed) || confirmed < 0) {
      showToast("Enter valid inspection quantities", "error");
      return;
    }
    await refreshAfter(
      () =>
        inspectInventoryTransferDamageCustody(workspace, inspectCustodyId, {
          recoveredSellableQty: recovered,
          confirmedDamagedQty: confirmed,
        }),
      "Damage custody inspected",
      t("transfer.actionFailed"),
    );
    setInspectCustodyId(null);
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
  const isPartiallyReceived = transfer.status === "PartiallyReceived";
  const isFinal = isTransferTerminalStatus(transfer.status);
  const showReceiptProgress = isInTransit || isPartiallyReceived || isFinal;
  const canMutate = allowManage && online && !busy;
  const canDispatch = canMutate && isSource && isDraft;
  const canCancel = canMutate && isSource && (isDraft || isInTransit);
  const canReceive = canMutate && isDestination && canDestinationReceiveTransfer(transfer);
  const canCloseRemainder = canMutate && isDestination && canDestinationCloseRemainder(transfer);
  const receiveButtonLabel =
    isPartiallyReceived ? t("transfer.receiveRemaining") : t("transfer.receive");

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
    ) : null;

  if (mode === "receive" && canReceive) {
    return (
      <InventoryTransferReceiveMode
        transfer={transfer}
        sourceName={sourceName}
        destName={destName}
        statusLabel={statusLabel}
        statusIcon={inventoryTransferStatusIcon(transfer.status)}
        busy={busy}
        online={online}
        localErrorAlert={localErrorAlert}
        onBack={() => setMode("detail")}
        onSubmitReceive={(body) => {
          setLocalError(null);
          void onReceive(body);
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

      {transfer.rootTransferId ? (
        <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="transfer-replacement-banner">
          Replacement for{" "}
          <Link className="underline" to={`/inventory/transfers/${transfer.rootTransferId}`}>
            root transfer
          </Link>
          {transfer.replacementReason ? ` — ${transfer.replacementReason}` : null}
        </p>
      ) : null}

      {(transfer.familyMembers?.length ?? 0) > 1 ||
      (transfer.satisfiedAtDestinationQty ?? 0) > 0 ||
      (transfer.remainingToDispatchQty ?? 0) > 0 ? (
        <Card
          className="flex min-w-0 flex-col gap-2 p-3"
          treatment="bordered"
          data-testid="transfer-family-coverage"
        >
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">Satisfied</p>
              <p className="m-0 font-semibold tabular-nums">
                {formatTransferQty(transfer.satisfiedAtDestinationQty ?? 0)}
              </p>
            </div>
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">Still in transit</p>
              <p className="m-0 font-semibold tabular-nums">
                {formatTransferQty(transfer.openInTransitQty ?? 0)}
              </p>
            </div>
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">Needs fulfillment</p>
              <p className="m-0 font-semibold tabular-nums">
                {formatTransferQty(transfer.remainingToDispatchQty ?? 0)}
              </p>
            </div>
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">Waived</p>
              <p className="m-0 font-semibold tabular-nums">
                {formatTransferQty(transfer.waivedQty ?? 0)}
              </p>
            </div>
          </div>
          {(transfer.familyMembers?.length ?? 0) > 0 ? (
            <ul className="m-0 list-none p-0" data-testid="transfer-family-members">
              {transfer.familyMembers!.map((member) => (
                <li key={member.transferId} className="text-[length:var(--exits-text-sm)]">
                  <Link className="underline" to={`/inventory/transfers/${member.transferId}`}>
                    {member.isRoot
                      ? `Original ${member.transferNumber ?? member.transferId.slice(0, 8)}`
                      : `Replacement ${member.transferNumber ?? `R${member.replacementSequence}`}`}
                  </Link>{" "}
                  <span className="text-muted">({member.status})</span>
                </li>
              ))}
            </ul>
          ) : null}
          {(transfer.damageCustodies?.length ?? 0) > 0 ? (
            <ul className="m-0 list-none p-0" data-testid="transfer-damage-custodies">
              {inspectCustodyId ? (
                <li className="mb-2 flex flex-col gap-2 rounded-md border border-border p-2">
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
                    Inspect damage custody
                  </p>
                  <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                    Recovered sellable
                    <input
                      className="rounded-md border border-border px-2 py-1"
                      inputMode="decimal"
                      value={inspectRecoveredText}
                      onChange={(e) => setInspectRecoveredText(e.target.value)}
                      data-testid="transfer-custody-inspect-recovered"
                    />
                  </label>
                  <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                    Confirmed damaged
                    <input
                      className="rounded-md border border-border px-2 py-1"
                      inputMode="decimal"
                      value={inspectConfirmedText}
                      onChange={(e) => setInspectConfirmedText(e.target.value)}
                      data-testid="transfer-custody-inspect-confirmed"
                    />
                  </label>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      disabled={busy}
                      onClick={() => void onInspectDamageCustody()}
                      data-testid="transfer-custody-inspect-confirm"
                    >
                      Confirm inspection
                    </Button>
                    <Button
                      type="button"
                      appearance="ghost"
                      disabled={busy}
                      onClick={() => setInspectCustodyId(null)}
                      data-testid="transfer-custody-inspect-cancel"
                    >
                      Cancel
                    </Button>
                  </div>
                </li>
              ) : null}
              {transfer.damageCustodies!.map((c) => {
                const canDispatchReturn =
                  canMutate &&
                  isDestination &&
                  c.decision === "ReturnToSource" &&
                  (c.status === "AwaitingReturn" || c.status === "HeldAtDestination");
                const canReceiveReturn =
                  canMutate && isSource && c.status === "ReturnInTransit";
                const canInspect =
                  canMutate &&
                  ((isDestination &&
                    c.decision === "KeepAtDestination" &&
                    (c.status === "HeldAtDestination" || c.status === "AwaitingInspection")) ||
                    (isSource &&
                      c.decision === "ReturnToSource" &&
                      (c.status === "ReceivedAtSource" || c.status === "AwaitingInspection")));
                return (
                  <li
                    key={c.custodyId}
                    className="flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)]"
                  >
                    <span className="text-muted">
                      Damage custody {c.status}: {formatTransferQty(c.quantity)} ({c.decision})
                    </span>
                    {canDispatchReturn ? (
                      <Button
                        type="button"
                        appearance="ghost"
                        disabled={busy}
                        onClick={() => void onDispatchDamageReturn(c.custodyId)}
                        data-testid={`transfer-custody-dispatch-return-${c.custodyId}`}
                      >
                        Dispatch return
                      </Button>
                    ) : null}
                    {canReceiveReturn ? (
                      <Button
                        type="button"
                        appearance="ghost"
                        disabled={busy}
                        onClick={() => void onReceiveDamageReturn(c.custodyId)}
                        data-testid={`transfer-custody-receive-return-${c.custodyId}`}
                      >
                        Receive return
                      </Button>
                    ) : null}
                    {canInspect ? (
                      <Button
                        type="button"
                        appearance="ghost"
                        disabled={busy}
                        onClick={() => {
                          setInspectCustodyId(c.custodyId);
                          setInspectRecoveredText("0");
                          setInspectConfirmedText(String(c.quantity));
                        }}
                        data-testid={`transfer-custody-inspect-${c.custodyId}`}
                      >
                        Inspect
                      </Button>
                    ) : null}
                  </li>
                );
              })}
            </ul>
          ) : null}
        </Card>
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
                {showReceiptProgress ? (
                  <ExitsTableHead cellAlign="center" colSize="numeric">
                    {t("transfer.outstanding")}
                  </ExitsTableHead>
                ) : null}
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.difference")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("transfer.lot")}</ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {transfer.lines.map((line) => {
                const showReceived = showReceiptProgress;
                const outstanding = lineOutstandingQty(line);
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
                    {showReceiptProgress ? (
                      <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                        {formatTransferQty(outstanding)}
                      </ExitsTableCell>
                    ) : null}
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
              const showReceived = showReceiptProgress;
              const outstanding = lineOutstandingQty(line);
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
                    {showReceived
                      ? ` · ${t("transfer.outstanding")}: ${formatTransferQty(outstanding)}`
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

      {canCancel || canDispatch || canReceive || canCloseRemainder ? (
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
                {receiveButtonLabel}
              </Button>
            ) : null}
            {canCloseRemainder ? (
              <Button
                type="button"
                intent="danger"
                appearance="outline"
                disabled={!canMutate}
                onClick={() => {
                  setLocalError(null);
                  setCloseRemainderOpen(true);
                }}
                data-testid="transfer-close-remainder"
              >
                {t("transfer.closeRemainder")}
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

      {closeRemainderOpen ? (
        <TransferCloseRemainderDialog
          open
          transfer={transfer}
          busy={busy}
          onCancel={() => {
            if (!busy) {
              setCloseRemainderOpen(false);
            }
          }}
          onConfirm={(lines) => void onCloseRemainder(lines)}
        />
      ) : null}

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

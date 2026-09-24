import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowRight,
  ArrowUpRight,
  Ban,
  ChevronRight,
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
  dispatchInventoryTransferExceptionReturn,
  getInventoryTransfer,
  inspectInventoryTransferDamageCustody,
  inspectInventoryTransferExceptionCustody,
  prepareInventoryTransferRemaining,
  receiveInventoryTransfer,
  receiveInventoryTransferDamageReturn,
  receiveInventoryTransferExceptionReturn,
  type InventoryTransferDto,
  type ReceiveInventoryTransferRequest,
} from "@/api/pos/pos-inventory-transfer-client";
import { prepareStockRequestTransfer } from "@/api/pos/pos-stock-requests-client";
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
import { StatusChip } from "@/components/exits/StatusChip";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import { InventoryMovementTransactionDrawer } from "@/features/inventory/InventoryMovementTransactionDrawer";
import { InventoryTransferActivityTimeline } from "@/features/inventory/InventoryTransferActivityTimeline";
import { buildTransferActivityEvents } from "@/features/inventory/inventory-transfer-activity";
import { InventoryTransferReceiveMode } from "@/features/inventory/InventoryTransferReceiveMode";
import {
  branchDisplayName,
  formatTransferQty,
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusTone,
} from "@/features/inventory/inventory-transfer-labels";
import {
  canDestinationCloseRemainder,
  canDestinationReceiveTransfer,
  isTransferTerminalStatus,
  lineDamagedQty,
  lineNeedsFulfillmentQty,
  lineOutstandingQty,
} from "@/features/inventory/inventory-transfer-receive-helpers";
import {
  computeThisShipmentTotals,
  familyFulfillmentTargetQty,
  familyMemberDamagedQty,
  lineFollowUpDisplay,
  lineMissingQty,
  lineOtherExceptionSecondaryText,
  lineOtherQty,
  resolveTransferProductDisplayName,
  resolveExceptionCustodyItemLabel,
  looksLikeTransferProductIdFragment,
} from "@/features/inventory/inventory-transfer-summary-presentation";
import { restoresDirectlyToSellableOnSourceReceive } from "@/features/inventory/transfer-exception-custody-policy";
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

function isExceptionReturnPendingSend(status: string): boolean {
  return status === "AwaitingReturn" || status === "HeldAtDestination";
}

function isExceptionReturnInTransit(status: string): boolean {
  return status === "ReturnInTransit";
}

function isExceptionReturnAlreadyReceived(status: string): boolean {
  return (
    status === "ReceivedAtSource" ||
    status === "AwaitingInspection" ||
    status === "Inspected"
  );
}

function isExceptionReturnWorkflowStatus(status: string): boolean {
  return (
    isExceptionReturnPendingSend(status) ||
    isExceptionReturnInTransit(status) ||
    isExceptionReturnAlreadyReceived(status)
  );
}

function ExceptionSendBackButtonContent({
  label,
  branchName,
}: {
  label: string;
  branchName: string;
}) {
  return (
    <>
      <ArrowUpRight className="size-4 shrink-0" aria-hidden />
      <span className="min-w-0 truncate">{label}</span>
      <ArrowRight className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
      <span className="min-w-0 truncate font-medium">{branchName}</span>
    </>
  );
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
  const [familyTransactionOpen, setFamilyTransactionOpen] = useState<{
    transferId: string;
    transferNumber: string | null;
  } | null>(null);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);
  const [inspectCustodyId, setInspectCustodyId] = useState<string | null>(null);
  const [inspectRecoveredText, setInspectRecoveredText] = useState("0");
  const [inspectConfirmedText, setInspectConfirmedText] = useState("0");
  const [inspectExceptionCustodyId, setInspectExceptionCustodyId] = useState<string | null>(null);
  const [inspectExceptionRecoveredText, setInspectExceptionRecoveredText] = useState("0");
  const [inspectExceptionNonSellableText, setInspectExceptionNonSellableText] = useState("0");

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
        // Custody mutations return custody DTOs (not full transfer). Always refetch transfer detail.
        await queryClient.invalidateQueries({
          queryKey: ["inventory-transfer", workspace.organizationId, transferId],
        });
      }
      await queryClient.invalidateQueries({
        queryKey: ["inventory-transfer", workspace.organizationId],
        predicate: (query) => {
          const cachedId = query.queryKey[2];
          return typeof cachedId === "string" && cachedId !== transferId;
        },
      });
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
      // Refetch sibling family pages (root / R1…) without clobbering the just-updated DTO.
      await queryClient.invalidateQueries({
        queryKey: ["inventory-transfer", workspace.organizationId],
        predicate: (query) => {
          const cachedId = query.queryKey[2];
          return typeof cachedId === "string" && cachedId !== transferId;
        },
      });
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
      t("transfer.exceptionReturnReceiveSuccess"),
      t("transfer.actionFailed"),
    );
  }

  async function onDispatchExceptionReturn(custodyId: string) {
    if (!workspace || busyRef.current) {
      return;
    }
    await refreshAfter(
      () => dispatchInventoryTransferExceptionReturn(workspace, custodyId),
      t("transfer.exceptionReturnDispatched"),
      t("transfer.actionFailed"),
    );
  }

  async function onReceiveExceptionReturn(custodyId: string) {
    if (!workspace || busyRef.current) {
      return;
    }
    await refreshAfter(
      () => receiveInventoryTransferExceptionReturn(workspace, custodyId),
      t("transfer.exceptionReturnReceiveSuccess"),
      t("transfer.actionFailed"),
    );
  }

  async function onInspectExceptionCustody() {
    if (!workspace || !inspectExceptionCustodyId || busyRef.current) {
      return;
    }
    const recovered = Number(inspectExceptionRecoveredText);
    const confirmed = Number(inspectExceptionNonSellableText);
    if (!Number.isFinite(recovered) || recovered < 0 || !Number.isFinite(confirmed) || confirmed < 0) {
      showToast(t("transfer.exceptionInspectInvalidQty"), "error");
      return;
    }
    await refreshAfter(
      () =>
        inspectInventoryTransferExceptionCustody(workspace, inspectExceptionCustodyId, {
          recoveredSellableQty: recovered,
          confirmedNonSellableQty: confirmed,
        }),
      t("transfer.exceptionInspected"),
      t("transfer.actionFailed"),
    );
    setInspectExceptionCustodyId(null);
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

  async function onFulfillRemaining() {
    if (!workspace || !transfer || busyRef.current) {
      return;
    }
    busyRef.current = true;
    setBusy(true);
    setLocalError(null);
    try {
      const draft = transfer.stockRequestId
        ? await prepareStockRequestTransfer(workspace, transfer.stockRequestId)
        : await prepareInventoryTransferRemaining(workspace, transfer.transferId);
      await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-request"] });
      await queryClient.invalidateQueries({ queryKey: ["stock-requests"] });
      navigate(`/inventory/transfers/${draft.transferId}`);
    } catch (err) {
      const detail = resolveTransferActionError(err, t("transfer.actionFailed"));
      setLocalError({ title: t("transfer.actionFailed"), detail });
      showToast(detail, "error");
    } finally {
      busyRef.current = false;
      setBusy(false);
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
  // GUID strings may differ by case across session grant vs transfer DTO.
  const sameBranch = (a: string | null | undefined, b: string | null | undefined) =>
    Boolean(a && b && a.toLowerCase() === b.toLowerCase());
  const isSource = sameBranch(actingBranchId, transfer.sourceBranchId);
  const isDestination = sameBranch(actingBranchId, transfer.destinationBranchId);
  const isDraft = transfer.status === "Draft";
  const isInTransit = transfer.status === "InTransit";
  const isPartiallyReceived = transfer.status === "PartiallyReceived";
  const isFinal = isTransferTerminalStatus(transfer.status);
  const canMutate = allowManage && online && !busy;
  const canDispatch = canMutate && isSource && isDraft;
  const canCancel = canMutate && isSource && (isDraft || isInTransit);
  const canReceive = canMutate && isDestination && canDestinationReceiveTransfer(transfer);
  const canCloseRemainder = canMutate && isDestination && canDestinationCloseRemainder(transfer);
  const remainingToDispatchQty = transfer.remainingToDispatchQty ?? 0;
  // Match stock-request detail: gate on allowManage + source, not busy/online.
  // Never offer fulfill on a lone Draft — that qty is the first dispatch, not a replacement.
  const canFulfillRemaining =
    allowManage && isSource && remainingToDispatchQty > 0 && !isDraft;
  const familyMembers = transfer.familyMembers ?? [];
  const damageCustodies = transfer.damageCustodies ?? [];
  const exceptionCustodies = transfer.exceptionCustodies ?? [];
  const thisShipment = computeThisShipmentTotals(transfer);
  const fulfillmentTargetQty = familyFulfillmentTargetQty(transfer);
  // Fulfillment coverage is for receive / replacement waves — not a lone Draft
  // where Remaining simply equals the yet-to-dispatch send qty.
  const showFulfillmentCoverage =
    familyMembers.length > 1 ||
    (transfer.satisfiedAtDestinationQty ?? 0) > 0 ||
    (transfer.openInTransitQty ?? 0) > 0 ||
    (transfer.waivedQty ?? 0) > 0 ||
    damageCustodies.length > 0 ||
    exceptionCustodies.length > 0 ||
    (remainingToDispatchQty > 0 && transfer.status !== "Draft");
  const thisTransferCustodies = damageCustodies.filter(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
  const thisTransferExceptionCustodies = exceptionCustodies.filter(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
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
              t("transfer.good"),
              t("transfer.damaged"),
              t("transfer.inTransit"),
              t("transfer.needsFulfillment"),
            ],
            rows: transfer.lines.map((line) => [
              line.productName,
              formatTransferQty(line.sentQty),
              formatTransferQty(line.receivedQty),
              formatTransferQty(lineDamagedQty(transfer, line.lineId)),
              formatTransferQty(lineOutstandingQty(line)),
              formatTransferQty(lineNeedsFulfillmentQty(line)),
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
            t("transfer.good"),
            t("transfer.damaged"),
            t("transfer.inTransit"),
            t("transfer.needsFulfillment"),
          ],
          ...transfer.lines.map((line) => [
            line.productName,
            formatTransferQty(line.sentQty),
            formatTransferQty(line.receivedQty),
            formatTransferQty(lineDamagedQty(transfer, line.lineId)),
            formatTransferQty(lineOutstandingQty(line)),
            formatTransferQty(lineNeedsFulfillmentQty(line)),
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
            <th>{t("transfer.good")}</th>
            <th>{t("transfer.damaged")}</th>
            <th>{t("transfer.inTransit")}</th>
            <th>{t("transfer.needsFulfillment")}</th>
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
              <td>
                {formatTransferQty(lineDamagedQty(transfer, line.lineId))} {line.unitOfMeasure}
              </td>
              <td>
                {formatTransferQty(lineOutstandingQty(line))} {line.unitOfMeasure}
              </td>
              <td>
                {formatTransferQty(lineNeedsFulfillmentQty(line))} {line.unitOfMeasure}
              </td>
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

  const receiveTitle =
    isPartiallyReceived ? t("transfer.receiveRemainingTitle") : t("transfer.receiveTitle");
  const headerTitle = mode === "receive" ? receiveTitle : t("transfer.summaryTitle");
  const headerBack =
    mode === "receive"
      ? {
          backTo: `/inventory/transfers/${transfer.transferId}`,
          backLabel: t("transfer.backToTransfer"),
          backTestId: "page-header-back-transfer",
          onBack: () => setMode("detail"),
        }
      : smartBack;

  return (
    <div
      className="inventory-transfer-detail-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid={
        mode === "receive" ? "inventory-transfer-receive-page" : "inventory-transfer-detail-page"
      }
      data-status={transfer.status}
      data-is-source={isSource ? "true" : "false"}
      data-can-fulfill-remaining={canFulfillRemaining ? "true" : "false"}
      data-remaining-to-dispatch={String(remainingToDispatchQty)}
    >
      <div className="exits-bizdoc-print-host" aria-hidden>
        {printDocument}
      </div>

      <PageHeader
        title={headerTitle}
        {...headerBack}
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
            trailing={
              canFulfillRemaining ? (
                <Button
                  type="button"
                  disabled={!online || busy}
                  onClick={() => void onFulfillRemaining()}
                  data-testid="transfer-fulfill-remaining-header"
                >
                  <Truck className="size-4 shrink-0" aria-hidden />
                  {t("stockRequest.fulfillRemaining").replace(
                    "{qty}",
                    formatTransferQty(remainingToDispatchQty),
                  )}
                </Button>
              ) : null
            }
          />
        }
      />

      {mode === "receive" && canReceive ? (
        <InventoryTransferReceiveMode
          workspace={workspace}
          transfer={transfer}
          sourceName={sourceName}
          destName={destName}
          statusLabel={statusLabel}
          statusIcon={inventoryTransferStatusIcon(transfer.status)}
          busy={busy}
          online={online}
          localErrorAlert={localErrorAlert}
          embedded
          onBack={() => setMode("detail")}
          onSubmitReceive={(body) => {
            setLocalError(null);
            void onReceive(body);
          }}
        />
      ) : (
        <>
      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}
      {localErrorAlert}

      {canFulfillRemaining ? (
        <Notice tone="info" testId="transfer-fulfill-remaining-notice">
          {t("transfer.fulfillRemainingHint").replace(
            "{qty}",
            formatTransferQty(remainingToDispatchQty),
          )}
        </Notice>
      ) : null}
      {!canFulfillRemaining &&
      isDestination &&
      remainingToDispatchQty > 0 &&
      transfer.stockRequestId ? (
        <Notice tone="info" testId="transfer-fulfill-at-source-notice">
          {t("transfer.fulfillAtSourceHint").replace(
            "{qty}",
            formatTransferQty(remainingToDispatchQty),
          )}{" "}
          <Link
            className="underline"
            to={`/inventory/stock-requests/${transfer.stockRequestId}`}
          >
            {t("transfer.viewStockRequest")}
          </Link>
        </Notice>
      ) : null}

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

        <div data-testid="transfer-qty-summary">
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

      <div
        className="grid grid-cols-1 gap-3 lg:grid-cols-2"
        data-testid="transfer-summary-body-top"
      >
        <Card
          className="flex min-w-0 flex-col gap-2 p-3 lg:col-span-2"
          treatment="bordered"
          data-testid="transfer-this-shipment"
        >
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("transfer.thisShipment")}
          </h2>
          <div className="flex flex-col gap-1.5 lg:grid lg:grid-cols-5 lg:gap-2">
            {(
              [
                {
                  key: "sent",
                  label: t("transfer.sent"),
                  value: thisShipment.sent,
                  testId: "this-shipment-sent",
                },
                {
                  key: "good",
                  label: t("transfer.goodReceived"),
                  value: thisShipment.goodReceived,
                  testId: "this-shipment-good",
                },
                {
                  key: "damaged",
                  label: t("transfer.damaged"),
                  value: thisShipment.damaged,
                  testId: "this-shipment-damaged",
                },
                {
                  key: "missing",
                  label: t("transfer.missing"),
                  value: thisShipment.missing,
                  testId: "this-shipment-missing",
                },
                {
                  key: "other",
                  label: t("transfer.other"),
                  value: thisShipment.other,
                  testId: "this-shipment-other",
                },
              ] as const
            ).map((metric) => (
              <div
                key={metric.key}
                className="flex items-baseline justify-between gap-4 text-[length:var(--exits-text-sm)] lg:flex-col lg:items-start lg:justify-start lg:gap-1 lg:rounded-[var(--exits-radius-soft)] lg:border lg:border-border lg:bg-[color-mix(in_srgb,var(--exits-surface)_92%,var(--exits-border))] lg:p-2.5"
              >
                <span className="text-muted">{metric.label}</span>
                <span
                  className="font-semibold tabular-nums lg:text-[length:var(--exits-text-md)]"
                  data-testid={metric.testId}
                >
                  {formatTransferQty(metric.value)}
                </span>
              </div>
            ))}
          </div>
        </Card>

        {thisTransferCustodies.length > 0 || thisTransferExceptionCustodies.length > 0 ? (
          <div className="flex min-w-0 flex-col gap-2" data-testid="transfer-custody-actions">
            {thisTransferCustodies.length > 0 ? (
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
                {thisTransferCustodies.map((c) => {
                  const canInspect =
                    canMutate &&
                    isSource &&
                    c.decision === "ReturnToSource" &&
                    (c.status === "ReceivedAtSource" || c.status === "AwaitingInspection");
                  if (!canInspect) {
                    return null;
                  }
                  return (
                    <li
                      key={c.custodyId}
                      className="flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)]"
                    >
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
                    </li>
                  );
                })}
              </ul>
            ) : null}
            {thisTransferExceptionCustodies.length > 0 ? (
              <ul className="m-0 list-none p-0" data-testid="transfer-exception-custodies">
                {inspectExceptionCustodyId ? (
                  <li className="mb-2 flex flex-col gap-2 rounded-md border border-border p-2">
                    <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
                      {t("transfer.exceptionInspectTitle")}
                    </p>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("transfer.exceptionRecoveredSellable")}
                      <input
                        className="rounded-md border border-border px-2 py-1"
                        inputMode="decimal"
                        value={inspectExceptionRecoveredText}
                        onChange={(e) => setInspectExceptionRecoveredText(e.target.value)}
                        data-testid="transfer-exception-custody-inspect-recovered"
                      />
                    </label>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("transfer.exceptionConfirmedNonSellable")}
                      <input
                        className="rounded-md border border-border px-2 py-1"
                        inputMode="decimal"
                        value={inspectExceptionNonSellableText}
                        onChange={(e) => setInspectExceptionNonSellableText(e.target.value)}
                        data-testid="transfer-exception-custody-inspect-non-sellable"
                      />
                    </label>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        disabled={busy}
                        onClick={() => void onInspectExceptionCustody()}
                        data-testid="transfer-exception-custody-inspect-confirm"
                      >
                        {t("transfer.exceptionInspectConfirm")}
                      </Button>
                      <Button
                        type="button"
                        appearance="ghost"
                        disabled={busy}
                        onClick={() => setInspectExceptionCustodyId(null)}
                        data-testid="transfer-exception-custody-inspect-cancel"
                      >
                        {t("transfer.dialogCancel")}
                      </Button>
                    </div>
                  </li>
                ) : null}
                {thisTransferExceptionCustodies.map((c) => {
                  const pendingInspect =
                    c.decision === "ReturnToSource" &&
                    !restoresDirectlyToSellableOnSourceReceive(c.reasonCode) &&
                    (c.status === "ReceivedAtSource" || c.status === "AwaitingInspection");
                  if (!pendingInspect) {
                    return null;
                  }

                  const canInspect = canMutate && isSource && pendingInspect;

                  return (
                    <li
                      key={c.custodyId}
                      className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
                      data-testid={`transfer-exception-custody-actions-${c.custodyId}`}
                    >
                      <div className="flex flex-wrap items-center gap-2">
                        <Button
                          type="button"
                          appearance="ghost"
                          disabled={!canInspect || busy}
                          onClick={() => {
                            setInspectExceptionCustodyId(c.custodyId);
                            setInspectExceptionRecoveredText("0");
                            setInspectExceptionNonSellableText(String(c.quantity));
                          }}
                          data-testid={`transfer-exception-custody-inspect-${c.custodyId}`}
                        >
                          {t("transfer.exceptionInspect")}
                        </Button>
                      </div>
                    </li>
                  );
                })}
              </ul>
            ) : null}
          </div>
        ) : null}
      </div>

      {showFulfillmentCoverage ? (
        <Card
          className="flex min-w-0 flex-col gap-3 p-3"
          treatment="bordered"
          data-testid="transfer-family-coverage"
        >
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("transfer.fulfillment")}
          </h2>
          <div className="flex flex-col gap-1.5 lg:grid lg:grid-cols-5 lg:gap-2">
            {(
              [
                {
                  key: "target",
                  label: t("transfer.targetRequested"),
                  value: fulfillmentTargetQty,
                  testId: "transfer-fulfillment-target",
                },
                {
                  key: "good",
                  label: t("transfer.goodReceived"),
                  value: transfer.satisfiedAtDestinationQty ?? 0,
                  testId: "transfer-fulfillment-good",
                },
                {
                  key: "inTransit",
                  label: t("transfer.stillInTransit"),
                  value: transfer.openInTransitQty ?? 0,
                  testId: "transfer-fulfillment-in-transit",
                },
                {
                  key: "needs",
                  label: t("transfer.needsFulfillment"),
                  value: remainingToDispatchQty,
                  testId: "transfer-fulfillment-needs-replacement",
                },
                {
                  key: "waived",
                  label: t("transfer.acceptedWaived"),
                  value: transfer.waivedQty ?? 0,
                  testId: "transfer-fulfillment-waived",
                },
              ] as const
            ).map((metric) => (
              <div
                key={metric.key}
                className="flex items-baseline justify-between gap-4 text-[length:var(--exits-text-sm)] lg:flex-col lg:items-start lg:justify-start lg:gap-1 lg:rounded-[var(--exits-radius-soft)] lg:border lg:border-border lg:bg-[color-mix(in_srgb,var(--exits-surface)_92%,var(--exits-border))] lg:p-2.5"
              >
                <span className="text-muted">{metric.label}</span>
                <span
                  className="font-semibold tabular-nums lg:text-[length:var(--exits-text-md)]"
                  data-testid={metric.testId}
                >
                  {formatTransferQty(metric.value)}
                </span>
              </div>
            ))}
          </div>
          {canFulfillRemaining ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] font-medium"
              data-testid="transfer-fulfillment-needs-banner"
            >
              {t("transfer.fulfillRemainingBanner").replace(
                "{qty}",
                formatTransferQty(remainingToDispatchQty),
              )}
            </p>
          ) : null}
          {canFulfillRemaining || transfer.stockRequestId ? (
            <div className="flex flex-wrap gap-2" data-testid="transfer-fulfillment-actions">
              {canFulfillRemaining ? (
                <Button
                  type="button"
                  disabled={!online || busy}
                  onClick={() => void onFulfillRemaining()}
                  data-testid="transfer-fulfill-remaining"
                >
                  {t("stockRequest.fulfillRemaining").replace(
                    "{qty}",
                    formatTransferQty(remainingToDispatchQty),
                  )}
                </Button>
              ) : null}
              {transfer.stockRequestId ? (
                <Button asChild variant="outline" data-testid="transfer-view-stock-request">
                  <Link to={`/inventory/stock-requests/${transfer.stockRequestId}`}>
                    {t("transfer.viewStockRequest")}
                  </Link>
                </Button>
              ) : null}
            </div>
          ) : null}
          {familyMembers.length > 0 ? (
            <ul
              className="m-0 grid list-none grid-cols-1 gap-2 p-0 sm:grid-cols-2"
              data-testid="transfer-family-members"
            >
              {familyMembers.map((member) => {
                const damaged = familyMemberDamagedQty(member);
                const isCurrent =
                  member.transferId.toLowerCase() === transfer.transferId.toLowerCase();
                const roleLabel = member.isRoot
                  ? member.transferNumber
                    ? t("transfer.family.original")
                    : t("transfer.family.originalDraft")
                  : member.transferNumber
                    ? t("transfer.family.replacementN").replace(
                        "{n}",
                        String(member.replacementSequence ?? ""),
                      )
                    : t("transfer.family.replacementDraft").replace(
                        "{n}",
                        String(member.replacementSequence ?? ""),
                      );
                const memberExceptionCustodies = exceptionCustodies.filter(
                  (c) =>
                    c.transferId.toLowerCase() === member.transferId.toLowerCase(),
                );
                const memberReturnCustodies = memberExceptionCustodies.filter(
                  (c) =>
                    c.decision === "ReturnToSource" &&
                    isExceptionReturnWorkflowStatus(c.status),
                );
                const memberDamageReturnCustodies = damageCustodies.filter(
                  (c) =>
                    c.transferId.toLowerCase() === member.transferId.toLowerCase() &&
                    c.decision === "ReturnToSource" &&
                    isExceptionReturnWorkflowStatus(c.status),
                );
                const otherQty = member.totalOtherQty ?? 0;
                const qtyParts = [
                  `${t("transfer.sent")} ${formatTransferQty(member.totalSentQty)}`,
                  `${t("transfer.good")} ${formatTransferQty(member.totalReceivedQty)}`,
                ];
                if (damaged > 1e-9) {
                  qtyParts.push(`${t("transfer.damaged")} ${formatTransferQty(damaged)}`);
                }
                const missing = member.totalMissingQty ?? 0;
                if (missing > 1e-9) {
                  qtyParts.push(`${t("transfer.missing")} ${formatTransferQty(missing)}`);
                }
                if (otherQty > 1e-9) {
                  qtyParts.push(`${t("transfer.other")} ${formatTransferQty(otherQty)}`);
                }
                const viewDetailsButton = (
                  <button
                    type="button"
                    className="inline-flex shrink-0 items-center gap-0.5 border-0 bg-transparent p-0 text-[length:var(--exits-text-sm)] font-medium text-primary no-underline hover:text-[color-mix(in_srgb,var(--exits-primary)_82%,black)] hover:underline"
                    onClick={() =>
                      setFamilyTransactionOpen({
                        transferId: member.transferId,
                        transferNumber: member.transferNumber?.trim() || null,
                      })
                    }
                    data-testid={`transfer-family-member-open-${member.transferId}`}
                  >
                    {t("transfer.viewDetails")}
                    <ChevronRight className="size-3.5 shrink-0" aria-hidden />
                  </button>
                );
                const renderExceptionReturnUi = (
                  c: (typeof memberReturnCustodies)[number],
                ) => {
                  const productName = resolveExceptionCustodyItemLabel(transfer, c);
                  const hasProduct = productName !== "—" && c.quantity > 1e-9;
                  const pendingSend = isExceptionReturnPendingSend(c.status);
                  const inTransit = isExceptionReturnInTransit(c.status);
                  const received = isExceptionReturnAlreadyReceived(c.status);
                  const canDispatchReturn = canMutate && isDestination && pendingSend;
                  const canReceiveReturn = canMutate && isSource && inTransit;
                  const sendBackButtonLabel = hasProduct
                    ? t("transfer.exceptionDispatchReturnWithProduct")
                        .replace("{qty}", formatTransferQty(c.quantity))
                        .replace("{product}", productName)
                    : t("transfer.exceptionDispatchReturn");
                  const toSourceLine = hasProduct
                    ? t("transfer.exceptionReturnInTransitLine")
                        .replace("{qty}", formatTransferQty(c.quantity))
                        .replace("{product}", productName)
                        .replace("{branch}", sourceName)
                    : sourceName;
                  const fromDestLine = hasProduct
                    ? t("transfer.exceptionIncomingReturnLine")
                        .replace("{qty}", formatTransferQty(c.quantity))
                        .replace("{product}", productName)
                        .replace("{branch}", destName)
                    : destName;
                  const receivedFromLine = hasProduct
                    ? t("transfer.exceptionReceivedFromBranch")
                        .replace("{qty}", formatTransferQty(c.quantity))
                        .replace("{product}", productName)
                        .replace("{branch}", destName)
                    : null;

                  return (
                    <div
                      key={c.custodyId}
                      className="flex min-w-0 flex-col items-start gap-1"
                      data-testid={`transfer-family-member-send-back-${c.custodyId}`}
                    >
                      {isDestination && pendingSend && canDispatchReturn ? (
                        <Button
                          type="button"
                          intent="primary"
                          appearance="solid"
                          className="w-auto self-start"
                          disabled={busy}
                          onClick={() => void onDispatchExceptionReturn(c.custodyId)}
                          data-testid={`transfer-family-member-dispatch-return-${c.custodyId}`}
                        >
                          <ExceptionSendBackButtonContent
                            label={sendBackButtonLabel}
                            branchName={sourceName}
                          />
                        </Button>
                      ) : null}
                      {isDestination && pendingSend && !canDispatchReturn ? (
                        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                          {!online
                            ? t("transfer.exceptionSendBackOffline")
                            : !allowManage
                              ? t("transfer.exceptionSendBackNoPermission")
                              : t("transfer.custody.awaitingReturn")}
                        </p>
                      ) : null}
                      {isDestination && (inTransit || received) ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                          data-testid={`transfer-family-member-return-status-${c.custodyId}`}
                        >
                          {toSourceLine}
                          {" · "}
                          {received
                            ? t("transfer.exceptionReturnedToSource")
                            : t("transfer.exceptionReturnInTransitStatus")}
                        </p>
                      ) : null}
                      {isSource && pendingSend ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                          data-testid={`transfer-family-member-return-status-${c.custodyId}`}
                        >
                          {t("transfer.exceptionWaitingForReturn")}
                        </p>
                      ) : null}
                      {isSource && inTransit ? (
                        <>
                          <div
                            className="flex min-w-0 flex-col gap-0.5"
                            data-testid={`transfer-family-member-return-status-${c.custodyId}`}
                          >
                            <span className="font-medium text-foreground">
                              {t("transfer.exceptionIncomingReturn")}
                            </span>
                            <span className="text-muted">{fromDestLine}</span>
                          </div>
                          {canReceiveReturn ? (
                            <Button
                              type="button"
                              intent="primary"
                              appearance="solid"
                              className="w-auto self-start"
                              disabled={busy}
                              onClick={() => void onReceiveExceptionReturn(c.custodyId)}
                              data-testid={`transfer-family-member-receive-return-${c.custodyId}`}
                            >
                              <PackageOpen className="size-4 shrink-0" aria-hidden />
                              {t("transfer.exceptionReceiveReturn")}
                            </Button>
                          ) : (
                            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                              {!online
                                ? t("transfer.exceptionSendBackOffline")
                                : !allowManage
                                  ? t("transfer.exceptionSendBackNoPermission")
                                  : null}
                            </p>
                          )}
                        </>
                      ) : null}
                      {isSource && received ? (
                        <div
                          className="flex min-w-0 flex-col gap-0.5"
                          data-testid={`transfer-family-member-return-status-${c.custodyId}`}
                        >
                          <span className="font-medium text-foreground">
                            {t("transfer.exceptionReturnedToSource")}
                          </span>
                          {receivedFromLine ? (
                            <span className="text-muted">{receivedFromLine}</span>
                          ) : null}
                        </div>
                      ) : null}
                    </div>
                  );
                };
                const renderDamageReturnUi = (
                  c: (typeof memberDamageReturnCustodies)[number],
                ) => {
                  const productName = resolveTransferProductDisplayName(transfer, c.productId);
                  const hasProduct =
                    !!productName &&
                    !looksLikeTransferProductIdFragment(productName, c.productId) &&
                    c.quantity > 1e-9;
                  const qtyProduct = hasProduct
                    ? `${formatTransferQty(c.quantity)} ${productName}`
                    : c.quantity > 1e-9
                      ? formatTransferQty(c.quantity)
                      : null;
                  const pendingSend = isExceptionReturnPendingSend(c.status);
                  const inTransit = isExceptionReturnInTransit(c.status);
                  const received = isExceptionReturnAlreadyReceived(c.status);
                  const canDispatchReturn = canMutate && isDestination && pendingSend;
                  const canReceiveReturn = canMutate && isSource && inTransit;
                  const sendBackButtonLabel = hasProduct
                    ? t("transfer.exceptionDispatchReturnWithProduct")
                        .replace("{qty}", formatTransferQty(c.quantity))
                        .replace("{product}", productName)
                    : t("transfer.exceptionDispatchReturn");

                  return (
                    <div
                      key={c.custodyId}
                      className="flex min-w-0 flex-col items-start gap-1"
                      data-testid={`transfer-family-member-damage-send-back-${c.custodyId}`}
                    >
                      {isDestination && pendingSend && canDispatchReturn ? (
                        <Button
                          type="button"
                          intent="primary"
                          appearance="solid"
                          className="w-auto self-start"
                          disabled={busy}
                          onClick={() => void onDispatchDamageReturn(c.custodyId)}
                          data-testid={`transfer-family-member-damage-dispatch-return-${c.custodyId}`}
                        >
                          <ExceptionSendBackButtonContent
                            label={sendBackButtonLabel}
                            branchName={sourceName}
                          />
                        </Button>
                      ) : null}
                      {isDestination && pendingSend && !canDispatchReturn ? (
                        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                          {!online
                            ? t("transfer.exceptionSendBackOffline")
                            : !allowManage
                              ? t("transfer.exceptionSendBackNoPermission")
                              : t("transfer.custody.awaitingReturn")}
                        </p>
                      ) : null}
                      {isDestination && (inTransit || received) ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                          data-testid={`transfer-family-member-damage-return-status-${c.custodyId}`}
                        >
                          {hasProduct
                            ? t("transfer.exceptionReturnInTransitLine")
                                .replace("{qty}", formatTransferQty(c.quantity))
                                .replace("{product}", productName)
                                .replace("{branch}", sourceName)
                            : sourceName}
                          {" · "}
                          {received
                            ? t("transfer.exceptionReturnedToSource")
                            : t("transfer.exceptionReturnInTransitStatus")}
                        </p>
                      ) : null}
                      {isSource && pendingSend ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                          data-testid={`transfer-family-member-damage-return-status-${c.custodyId}`}
                        >
                          {t("transfer.exceptionWaitingForReturn")}
                        </p>
                      ) : null}
                      {isSource && inTransit ? (
                        <>
                          <div
                            className="flex min-w-0 flex-col gap-0.5"
                            data-testid={`transfer-family-member-damage-return-status-${c.custodyId}`}
                          >
                            <span className="font-medium text-foreground">
                              {t("transfer.exceptionIncomingReturn")}
                            </span>
                            {hasProduct ? (
                              <span className="text-muted">
                                {t("transfer.exceptionIncomingReturnLine")
                                  .replace("{qty}", formatTransferQty(c.quantity))
                                  .replace("{product}", productName)
                                  .replace("{branch}", destName)}
                              </span>
                            ) : null}
                          </div>
                          {canReceiveReturn ? (
                            <Button
                              type="button"
                              intent="primary"
                              appearance="solid"
                              className="w-auto self-start"
                              disabled={busy}
                              onClick={() => void onReceiveDamageReturn(c.custodyId)}
                              data-testid={`transfer-family-member-damage-receive-return-${c.custodyId}`}
                            >
                              <PackageOpen className="size-4 shrink-0" aria-hidden />
                              {t("transfer.exceptionReceiveReturn")}
                            </Button>
                          ) : null}
                        </>
                      ) : null}
                      {isSource && received ? (
                        <div
                          className="flex min-w-0 flex-col gap-0.5"
                          data-testid={`transfer-family-member-damage-return-status-${c.custodyId}`}
                        >
                          <span className="font-medium text-foreground">
                            {t("transfer.exceptionReturnedToSource")}
                          </span>
                          {qtyProduct && hasProduct ? (
                            <span className="text-muted">
                              {t("transfer.exceptionReceivedFromBranch")
                                .replace("{qty}", formatTransferQty(c.quantity))
                                .replace("{product}", productName)
                                .replace("{branch}", destName)}
                            </span>
                          ) : null}
                        </div>
                      ) : null}
                    </div>
                  );
                };
                return (
                  <li key={member.transferId} className="min-w-0">
                    <Card
                      className={`flex h-full flex-col gap-1.5 p-3 ${
                        isCurrent
                          ? "ring-1 ring-[color-mix(in_srgb,var(--exits-border)_80%,transparent)]"
                          : ""
                      }`}
                      treatment="bordered"
                      padding="compact"
                      data-testid={`transfer-family-member-${member.transferId}`}
                    >
                      <div className="flex min-w-0 items-start justify-between gap-2">
                        <div className="flex min-w-0 flex-wrap items-center gap-2">
                          <span
                            className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground"
                            data-testid={`transfer-family-member-link-${member.transferId}`}
                          >
                            {roleLabel}
                          </span>
                          {qtyParts.map((part) => (
                            <StatusChip
                              key={part}
                              tone="info"
                              appearance="outline"
                              shape="soft"
                            >
                              {part}
                            </StatusChip>
                          ))}
                        </div>
                        <StatusChip
                          tone={inventoryTransferStatusTone(member.status)}
                          appearance="soft"
                          shape="soft"
                        >
                          {t(inventoryTransferStatusLabelKey(member.status))}
                        </StatusChip>
                      </div>

                      <span
                        className="min-w-0 truncate font-mono text-[length:var(--exits-text-xs)] font-semibold text-primary"
                        data-testid={`transfer-family-member-number-${member.transferId}`}
                      >
                        {member.transferNumber?.trim() || "—"}
                      </span>

                      <div className="flex min-w-0 items-end justify-between gap-x-3 gap-y-2">
                        <div className="flex min-w-0 flex-col items-start gap-2">
                          {memberReturnCustodies.map((c) => renderExceptionReturnUi(c))}
                          {memberDamageReturnCustodies.map((c) => renderDamageReturnUi(c))}
                        </div>
                        {viewDetailsButton}
                      </div>
                    </Card>
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
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.sent")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.good")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.damaged")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.missing")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.other")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text" colSize="flex">
                  {t("transfer.followUp.decisionCol")}
                </ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {transfer.lines.map((line) => {
                const damaged = lineDamagedQty(transfer, line.lineId);
                const missing = lineMissingQty(transfer, line.lineId);
                const other = lineOtherQty(transfer, line.lineId);
                const followUp = lineFollowUpDisplay(transfer, line);
                const otherSecondary = lineOtherExceptionSecondaryText(transfer, line);
                const followUpText = followUp
                  ? followUp.qty != null
                    ? t(followUp.labelKey).replace("{qty}", formatTransferQty(followUp.qty))
                    : t(followUp.labelKey)
                  : t("transfer.followUp.none");
                return (
                  <ExitsTableRow
                    key={line.lineId}
                    data-testid={`transfer-line-${line.lineId}`}
                  >
                    <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                      <div>{line.productName}</div>
                      {otherSecondary ? (
                        <div className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                          {otherSecondary}
                        </div>
                      ) : null}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(line.sentQty)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(line.receivedQty)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(damaged)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(missing)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(other)}
                    </ExitsTableCell>
                    <ExitsTableCell
                      cellAlign="text"
                      colSize="flex"
                      data-testid={`transfer-line-follow-up-${line.lineId}`}
                    >
                      {followUpText}
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>

          <ExitsTableMobile data-testid="transfer-lines-mobile">
            {transfer.lines.map((line) => {
              const damaged = lineDamagedQty(transfer, line.lineId);
              const missing = lineMissingQty(transfer, line.lineId);
              const other = lineOtherQty(transfer, line.lineId);
              const followUp = lineFollowUpDisplay(transfer, line);
              const otherSecondary = lineOtherExceptionSecondaryText(transfer, line);
              const followUpText = followUp
                ? followUp.qty != null
                  ? t(followUp.labelKey).replace("{qty}", formatTransferQty(followUp.qty))
                  : t(followUp.labelKey)
                : t("transfer.followUp.none");
              return (
                <ExitsTableMobileRow
                  key={line.lineId}
                  data-testid={`transfer-line-${line.lineId}`}
                >
                  <div className="exits-table-mobile__title-row">
                    <span className="exits-table-mobile__title">{line.productName}</span>
                  </div>
                  {otherSecondary ? (
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{otherSecondary}</p>
                  ) : null}
                  <p className="exits-table-mobile__math mt-1 mb-0">
                    {t("transfer.sent")}: {formatTransferQty(line.sentQty)}
                    {` · ${t("transfer.good")}: ${formatTransferQty(line.receivedQty)}`}
                    {` · ${t("transfer.damaged")}: ${formatTransferQty(damaged)}`}
                    {` · ${t("transfer.missing")}: ${formatTransferQty(missing)}`}
                    {` · ${t("transfer.other")}: ${formatTransferQty(other)}`}
                  </p>
                  <p className="mt-1 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                    {t("transfer.followUp.decisionCol")}: {followUpText}
                  </p>
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>
        </ExitsTableContainer>
      </section>

      {canCancel || canDispatch || canReceive || canCloseRemainder || canFulfillRemaining ? (
        <div className="receive-stock-actions" data-testid="transfer-detail-actions">
          {isDraft ? (
            <p className="m-0 me-auto text-[length:var(--exits-text-xs)] text-muted">
              {t("transfer.draftNoEdit")}
            </p>
          ) : null}
          {canFulfillRemaining ? (
            <p
              className="m-0 me-auto text-[length:var(--exits-text-sm)] font-medium"
              data-testid="transfer-actions-needs-fulfillment"
            >
              {t("stockRequest.needsFulfillmentSummary").replace(
                "{qty}",
                formatTransferQty(remainingToDispatchQty),
              )}
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
            {canFulfillRemaining ? (
              <Button
                type="button"
                disabled={!online || busy}
                onClick={() => void onFulfillRemaining()}
                data-testid="transfer-fulfill-remaining-actions"
              >
                <Truck className="size-4 shrink-0" aria-hidden />
                {t("stockRequest.fulfillRemaining").replace(
                  "{qty}",
                  formatTransferQty(remainingToDispatchQty),
                )}
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
        </>
      )}
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

      <InventoryMovementTransactionDrawer
        open={familyTransactionOpen != null}
        onOpenChange={(open) => {
          if (!open) {
            setFamilyTransactionOpen(null);
          }
        }}
        movement={null}
        transferContext={familyTransactionOpen}
        unitOfMeasure=""
        workspace={workspace}
        resolveActor={actors.resolve}
        actorsLoading={actors.isResolving}
      />

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

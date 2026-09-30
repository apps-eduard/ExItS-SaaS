import {
  ArrowLeft,
  ArrowLeftRight,
  ArrowRight,
  Ban,
  ClipboardCheck,
  FilePlus2,
  PackageCheck,
  Pencil,
  Truck,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as XLSX from "xlsx";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  approveStockRequest,
  cancelStockRequest,
  getStockRequest,
  getStockRequestActivity,
  prepareStockRequest,
  prepareStockRequestTransfer,
  rejectStockRequest,
} from "@/api/pos/pos-stock-requests-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsDataRecordCard } from "@/components/exits/ExitsDataRecordCard";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { RESPONSIVE_DATA_TABLE_MIN_LG } from "@/components/exits/responsive-data-view";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { useToast } from "@/components/exits/ToastProvider";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import { InventoryMovementTransactionDrawer } from "@/features/inventory/InventoryMovementTransactionDrawer";
import {
  formatTransferQty,
  inventoryTransferStatusLabelKey,
  stockRequestLinkedTransferQtyLabelKey,
} from "@/features/inventory/inventory-transfer-labels";
import { PoProcessHeaderActions } from "@/features/purchasing/PoProcessHeaderActions";
import { StockRequestActivityTimeline } from "@/features/replenishment/StockRequestActivityTimeline";
import {
  canCancelStockRequestAsDestination,
  canPrepareTransfer,
  displayApprovedQuantity,
  findLinkedDraftTransfer,
  formatCommittedRemainingLabel,
  isWarehousePreparingOrDispatchedStatus,
  listReceivableTransfers,
  normalizeStockRequestStatus,
  prepareTransferPrimaryLabelKey,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
  totalRemainingToDispatch,
  transferReceiveHref,
} from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Translate = (key: MessageKey) => string;

function StockRequestApproveQtyStepper({
  productId,
  requestedQuantity,
  unitOfMeasure,
  value,
  onChange,
  t,
}: {
  productId: string;
  requestedQuantity: number;
  unitOfMeasure: string;
  value: string | undefined;
  onChange: (next: string) => void;
  t: Translate;
}) {
  const qty = Number(value ?? requestedQuantity);
  const safeQty = Number.isFinite(qty) ? qty : 0;
  const isEmpty = !Number.isFinite(qty) || qty <= 0;
  return (
    <QuantityStepper
      compact
      variant="auto"
      editOnClick
      value={safeQty}
      min={0}
      max={requestedQuantity}
      precision={4}
      step={1}
      unitOfMeasure={unitOfMeasure}
      sellingMode="PerItem"
      invalid={isEmpty}
      decreaseLabel={t("transfer.decreaseQuantity")}
      increaseLabel={t("transfer.increaseQuantity")}
      ariaLabel={t("stockRequest.approvedQty")}
      valueTestId={`stock-request-approve-qty-${productId}`}
      className="stock-request-approve-qty justify-center"
      onChange={(next) => onChange(String(next))}
    />
  );
}

function stockRequestStatusIcon(status: string) {
  switch (normalizeStockRequestStatus(status)) {
    case "InTransit":
      return <Truck aria-hidden />;
    case "Fulfilled":
      return <PackageCheck aria-hidden />;
    case "PartiallyFulfilled":
      return <ClipboardCheck aria-hidden />;
    case "Rejected":
    case "Cancelled":
      return <Ban aria-hidden />;
    case "Approved":
    case "Preparing":
      return <ClipboardCheck aria-hidden />;
    case "Pending":
    default:
      return <FilePlus2 aria-hidden />;
  }
}

export function StockRequestDetailPage() {
  const { stockRequestId = "" } = useParams();
  const { t } = useI18n();
  const location = useLocation();
  const isWarehouseRequestPath = location.pathname.startsWith("/warehouse/");
  const smartBack = usePageSmartBack({
    fallback: isWarehouseRequestPath ? "/warehouse/my-requests" : "stockRequests",
    backLabel: isWarehouseRequestPath
      ? t("retailWarehouse.nav.myRequests")
      : t("stockRequest.listTitle"),
    backTestId: isWarehouseRequestPath
      ? "page-header-back-warehouse-requests"
      : "page-header-back-stock-requests",
  });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { layout } = useResponsiveDataLayout({ tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_LG });
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [rejectReason, setRejectReason] = useState("");
  const [declineOpen, setDeclineOpen] = useState(false);
  const [approvedQtys, setApprovedQtys] = useState<Record<string, string>>({});
  const [actionError, setActionError] = useState<string | null>(null);

  function resolveStockRequestActionError(err: unknown): string {
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
    if (err instanceof Error && err.message.trim() && err.message !== "missing" && err.message !== "qty") {
      return err.message.trim();
    }
    return t("stockRequest.actionError");
  }
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [documentPreviewOpen, setDocumentPreviewOpen] = useState(false);
  const [transferDrawer, setTransferDrawer] = useState<{
    transferId: string;
    transferNumber?: string | null;
  } | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: [
      "stock-request",
      stockRequestId,
      workspace?.organizationId,
      workspace?.branchId,
    ],
    enabled: Boolean(workspace && stockRequestId),
    queryFn: ({ signal }) => getStockRequest(workspace!, stockRequestId, signal),
  });

  const activityQuery = useQuery({
    queryKey: [
      "stock-request-activity",
      stockRequestId,
      workspace?.organizationId,
      workspace?.branchId,
    ],
    enabled: Boolean(workspace && stockRequestId),
    queryFn: ({ signal }) => getStockRequestActivity(workspace!, stockRequestId, signal),
  });

  const dto = query.data;
  const isSource = dto?.requestedSourceLocationId === workspace?.branchId;
  const isDestination = dto?.destinationLocationId === workspace?.branchId;

  useEffect(() => {
    if (!dto) return;
    setApprovedQtys((prev) => {
      const next = { ...prev };
      for (const line of dto.lines) {
        if (next[line.productId] == null) {
          next[line.productId] = String(line.approvedQuantity ?? line.requestedQuantity);
        }
      }
      return next;
    });
  }, [dto]);

  const activityActorIds = useMemo(
    () =>
      (activityQuery.data ?? [])
        .map((event) => event.actorId)
        .filter((id): id is string => Boolean(id)),
    [activityQuery.data],
  );

  const actors = useActorDirectory(workspace?.organizationId, [
    dto?.requestedBy,
    dto?.approvedBy,
    dto?.preparingStartedBy,
    dto?.dispatchedBy,
    dto?.rejectedBy,
    dto?.cancelledBy,
    ...activityActorIds,
  ]);

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["stock-request", stockRequestId] });
    await queryClient.invalidateQueries({ queryKey: ["stock-request-activity", stockRequestId] });
    await queryClient.invalidateQueries({ queryKey: ["stock-requests"] });
    await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
    await queryClient.invalidateQueries({ queryKey: ["wh-dash"] });
  };

  const buildLineApprovals = () => {
    if (!dto) return [];
    return dto.lines.map((line) => ({
      productId: line.productId,
      approvedQuantity: Number(approvedQtys[line.productId] ?? line.requestedQuantity),
    }));
  };

  const approveMutation = useMutation({
    mutationFn: async (andPrepare: boolean) => {
      if (!workspace || !dto) throw new Error("missing");
      setActionError(null);
      const approvals = buildLineApprovals();
      if (approvals.some((a) => !Number.isFinite(a.approvedQuantity) || a.approvedQuantity <= 0)) {
        throw new Error("qty");
      }
      await approveStockRequest(workspace, dto.stockRequestId, { lineApprovals: approvals });
      if (!andPrepare) {
        return { phase: "approved" as const };
      }
      try {
        const transfer = await prepareStockRequestTransfer(workspace, dto.stockRequestId);
        return { phase: "prepared" as const, transfer };
      } catch (err) {
        // Approval already committed — surface Approved + prepare retry; do not roll back.
        return {
          phase: "prepareFailed" as const,
          prepareError: resolveStockRequestActionError(err),
        };
      }
    },
    onSuccess: async (result) => {
      await invalidate();
      if (result.phase === "prepared") {
        navigate(`/inventory/transfers/${result.transfer.transferId}`);
        return;
      }
      if (result.phase === "prepareFailed") {
        const detail = result.prepareError?.trim();
        setActionError(
          detail && detail !== t("stockRequest.actionError")
            ? `${t("stockRequest.approvedButPrepareFailed")} ${detail}`
            : t("stockRequest.approvedButPrepareFailed"),
        );
      }
    },
    onError: (err) => setActionError(resolveStockRequestActionError(err)),
  });

  const prepareMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      setActionError(null);
      return prepareStockRequest(workspace, dto.stockRequestId);
    },
    onSuccess: () => void invalidate(),
    onError: (err) => setActionError(resolveStockRequestActionError(err)),
  });

  const prepareTransferMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      setActionError(null);
      return prepareStockRequestTransfer(workspace, dto.stockRequestId);
    },
    onSuccess: async (transfer) => {
      await invalidate();
      navigate(`/inventory/transfers/${transfer.transferId}`);
    },
    onError: (err) => setActionError(resolveStockRequestActionError(err)),
  });

  const rejectMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      const reason = rejectReason.trim();
      if (!reason) throw new Error("reason");
      setActionError(null);
      return rejectStockRequest(workspace, dto.stockRequestId, reason);
    },
    onSuccess: () => {
      setDeclineOpen(false);
      setRejectReason("");
      void invalidate();
    },
    onError: (err) => {
      setActionError(
        err instanceof Error && err.message === "reason"
          ? t("stockRequest.declineReasonRequired")
          : resolveStockRequestActionError(err),
      );
    },
  });

  const cancelMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      setActionError(null);
      return cancelStockRequest(workspace, dto.stockRequestId);
    },
    onSuccess: () => void invalidate(),
    onError: (err) => setActionError(resolveStockRequestActionError(err)),
  });

  if (!workspace) {
    return <EmptyState
              align="center"
              icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />} title={t("stockRequest.detailTitle")} detail={t("stockRequest.needBranch")} />;
  }

  if (query.isLoading) return <LoadingState label={t("stockRequest.loading")} />;
  if (query.isError || !dto) {
    return <ErrorState title={t("stockRequest.loadError")} detail={t("stockRequest.loadError")} />;
  }

  const linkedTransferId =
    dto.linkedInventoryTransferId ?? dto.linkedTransfers[0]?.transferId ?? null;
  const pendingAtSource = allowManage && isSource && dto.status === "Pending";
  const preparingAtSource =
    allowManage &&
    isSource &&
    (dto.status === "Approved" || dto.status === "Preparing" || dto.status === "InProgress");
  const linkedDraftId = findLinkedDraftTransfer(dto.linkedTransfers);
  const canPrepareTransferAction =
    allowManage &&
    isSource &&
    canPrepareTransfer(dto.status, dto.lines);
  const remainingDispatchQty = totalRemainingToDispatch(dto.lines);
  const committedRemainingLabel =
    isSource && isWarehousePreparingOrDispatchedStatus(dto.status)
      ? formatCommittedRemainingLabel(
          remainingDispatchQty,
          t("stockRequest.committedRemaining"),
          formatTransferQty,
        )
      : null;
  const remainingDispatchColumnLabel = isSource
    ? t("stockRequest.colRemainingToDispatchCommitted")
    : t("transfer.needsFulfillment");
  const prepareTransferLabelKey = prepareTransferPrimaryLabelKey(
    dto.status,
    Boolean(linkedDraftId),
  );
  const prepareTransferButtonLabel =
    prepareTransferLabelKey === "stockRequest.fulfillRemaining"
      ? t("stockRequest.fulfillRemaining").replace("{qty}", String(remainingDispatchQty))
      : t(prepareTransferLabelKey as MessageKey);
  const receivableTransfers = listReceivableTransfers(dto.linkedTransfers);
  const canReceive =
    allowManage &&
    isDestination &&
    (dto.status === "InTransit" || dto.status === "PartiallyFulfilled") &&
    receivableTransfers.length > 0;
  const singleReceiveTransferId =
    receivableTransfers.length === 1 ? receivableTransfers[0]!.transferId : null;
  const canCancel =
    allowManage && isDestination && canCancelStockRequestAsDestination(dto.status);
  const canEditRequest =
    allowManage && isDestination && dto.status === "Pending";
  const showSourceFulfillmentSummary =
    allowManage && isSource && remainingDispatchQty > 0 && canPrepareTransferAction;

  const sourceName = dto.requestedSourceLocationName ?? dto.requestedSourceLocationId;
  const destName = dto.destinationLocationName ?? dto.destinationLocationId;
  const statusLabel = t(stockRequestStatusLabelKey(dto.status) as MessageKey);
  const statusTone = stockRequestStatusTone(dto.status);
  const headerTitle = t("stockRequest.summaryTitle");
  const documentTitle = dto.requestNumber?.trim() || t("stockRequest.detailTitle");
  const activityEvents = activityQuery.data ?? [];

  async function runStockRequestOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const stamp = new Date().toISOString().slice(0, 10);
      const numberPart = sanitizeCsvFilenamePart(
        dto.requestNumber?.trim() || dto.stockRequestId.slice(0, 8),
      );
      if (action === "csv") {
        const csv = buildCsvWithMetadata(
          [
            ["Stock request", dto.requestNumber ?? dto.stockRequestId],
            ["Status", statusLabel],
            ["Route", `${sourceName} → ${destName}`],
            ["Generated", stamp],
          ],
          {
            headers: [
              t("purchasing.colProduct"),
              t("stockRequest.approved"),
              t("transfer.dispatched"),
              t("stockRequest.goodReceived"),
              t("transfer.discrepancy"),
              t("transfer.inTransit"),
              remainingDispatchColumnLabel,
            ],
            rows: dto.lines.map((line) => {
              const approved = displayApprovedQuantity(line.approvedQuantity);
              return [
                line.nameSnapshot,
                formatTransferQty(approved),
                formatTransferQty(line.sentQuantity),
                formatTransferQty(line.fulfilledQuantity),
                formatTransferQty(line.damagedQuantity ?? 0),
                formatTransferQty(line.inProgressQuantity),
                formatTransferQty(line.remainingToDispatchQuantity),
              ];
            }),
          },
        );
        downloadCsvFile(`stock-request-${numberPart}-${stamp}.csv`, csv);
        return;
      }
      if (action === "xlsx") {
        const sheet = XLSX.utils.aoa_to_sheet([
          ["Stock request", dto.requestNumber ?? dto.stockRequestId],
          ["Status", statusLabel],
          ["Route", `${sourceName} → ${destName}`],
          [],
          [
            t("purchasing.colProduct"),
            t("stockRequest.approved"),
            t("transfer.dispatched"),
            t("stockRequest.goodReceived"),
            t("transfer.discrepancy"),
            t("transfer.inTransit"),
            remainingDispatchColumnLabel,
          ],
          ...dto.lines.map((line) => {
            const approved = displayApprovedQuantity(line.approvedQuantity);
            return [
              line.nameSnapshot,
              formatTransferQty(approved),
              formatTransferQty(line.sentQuantity),
              formatTransferQty(line.fulfilledQuantity),
              formatTransferQty(line.damagedQuantity ?? 0),
              formatTransferQty(line.inProgressQuantity),
              formatTransferQty(line.remainingToDispatchQuantity),
            ];
          }),
        ]);
        const book = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(book, sheet, "Stock request");
        const buffer = XLSX.write(book, { bookType: "xlsx", type: "array" });
        downloadBlob(
          `stock-request-${numberPart}-${stamp}.xlsx`,
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
      <h1>{t("stockRequest.detailTitle")}</h1>
      {dto.requestNumber?.trim() ? <p>{dto.requestNumber.trim()}</p> : null}
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
            <th>{t("stockRequest.approved")}</th>
            <th>{t("transfer.dispatched")}</th>
            <th>{t("stockRequest.goodReceived")}</th>
            <th>{t("transfer.discrepancy")}</th>
            <th>{t("transfer.inTransit")}</th>
            <th>{remainingDispatchColumnLabel}</th>
          </tr>
        </thead>
        <tbody>
          {dto.lines.map((line) => {
            const approved = displayApprovedQuantity(line.approvedQuantity);
            return (
              <tr key={line.lineId}>
                <td>
                  {line.nameSnapshot}
                  <div>
                    {t("stockRequest.requested")}: {formatTransferQty(line.requestedQuantity)}
                  </div>
                </td>
                <td>
                  {formatTransferQty(approved)} {line.unitOfMeasure}
                </td>
                <td>
                  {formatTransferQty(line.sentQuantity)} {line.unitOfMeasure}
                </td>
                <td>
                  {formatTransferQty(line.fulfilledQuantity)} {line.unitOfMeasure}
                </td>
                <td>
                  {formatTransferQty(line.damagedQuantity ?? 0)} {line.unitOfMeasure}
                </td>
                <td>
                  {formatTransferQty(line.inProgressQuantity)} {line.unitOfMeasure}
                </td>
                <td>
                  {formatTransferQty(line.remainingToDispatchQuantity)} {line.unitOfMeasure}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );

  return (
    <div
      className="inventory-transfer-detail-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="stock-request-detail"
      data-status={dto.status}
    >
      <div className="exits-bizdoc-print-host" aria-hidden>
        {printDocument}
      </div>

      <PageHeader
        title={headerTitle}
        {...smartBack}
        actions={
          <PoProcessHeaderActions
            statusLabel={statusLabel}
            statusTone={statusTone}
            statusIcon={stockRequestStatusIcon(dto.status)}
            timelineEnabled={activityEvents.length > 0}
            onTimeline={() => setTimelineOpen(true)}
            onPreview={() => setDocumentPreviewOpen(true)}
            onPrint={() => void runStockRequestOutput("print")}
            onCsv={() => void runStockRequestOutput("csv")}
            onXlsx={() => void runStockRequestOutput("xlsx")}
            onPdf={() => void runStockRequestOutput("pdf")}
            timelineTestId="stock-request-timeline-open"
            previewTestId="stock-request-document-preview-open"
          />
        }
      />

      <Card
        className="flex min-w-0 flex-col gap-3 p-3"
        treatment="bordered"
        data-testid="stock-request-route-summary"
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
        <div data-testid="stock-request-number-summary">
          <Card className="flex flex-col gap-2 p-3" treatment="bordered">
            <div className="flex flex-col gap-0.5">
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("stockRequest.colNumber")}
              </p>
              <p className="m-0 truncate text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
                {dto.requestNumber?.trim() || "—"}
              </p>
            </div>
            {dto.linkedTransfers.length > 0 || linkedTransferId ? (
              <div
                className="flex min-w-0 flex-col gap-2 border-t border-border pt-2"
                data-testid="stock-request-linked-in-summary"
              >
                <p className="m-0 text-[length:var(--exits-text-xs)] font-medium text-muted">
                  {t("stockRequest.linkedTransfers")}
                </p>
                <ul className="m-0 flex list-none flex-col gap-2 p-0">
                  {dto.linkedTransfers.map((tr) => {
                    const transferNumber = tr.transferNumber?.trim() || "";
                    const transferLabel = transferNumber || t("transfer.draftNumber");
                    const showStatusSeparately = Boolean(transferNumber) || tr.status !== "Draft";
                    return (
                      <li
                        key={tr.transferId}
                        className="flex min-w-0 flex-col gap-1.5"
                        data-testid={`stock-request-linked-transfer-${tr.transferId}`}
                      >
                        <p className="m-0 text-[length:var(--exits-text-sm)]">
                          <span className="font-medium tabular-nums">{transferLabel}</span>
                          <span>
                            {showStatusSeparately ? (
                              <>
                                {" · "}
                                {t(inventoryTransferStatusLabelKey(tr.status) as MessageKey)}
                              </>
                            ) : null}
                            {" · "}
                            {t(stockRequestLinkedTransferQtyLabelKey(tr.status))}:{" "}
                            {formatTransferQty(tr.totalSentQty)}
                            {" · "}
                            {t("stockRequest.linkedTransfer.received")}:{" "}
                            {formatTransferQty(tr.totalReceivedQty)}
                            {" · "}
                            {t("stockRequest.linkedTransfer.closed")}:{" "}
                            {formatTransferQty(tr.totalClosedQty ?? 0)}
                            {" · "}
                            {t("stockRequest.linkedTransfer.outstanding")}:{" "}
                            {formatTransferQty(tr.totalOutstandingQty ?? 0)}
                          </span>
                        </p>
                        <Button
                          type="button"
                          appearance="outline"
                          className="w-fit"
                          onClick={() =>
                            setTransferDrawer({
                              transferId: tr.transferId,
                              transferNumber: tr.transferNumber,
                            })
                          }
                          data-testid={`stock-request-view-transfer-${tr.transferId}`}
                        >
                          {t("inventory.viewTransfer")}
                        </Button>
                      </li>
                    );
                  })}
                  {linkedTransferId &&
                  !dto.linkedTransfers.some((tr) => tr.transferId === linkedTransferId) ? (
                    <li
                      className="flex min-w-0 flex-col gap-1.5"
                      data-testid={`stock-request-linked-transfer-${linkedTransferId}`}
                    >
                      <p className="m-0 text-[length:var(--exits-text-sm)] font-medium tabular-nums">
                        {t("transfer.draftNumber")}
                      </p>
                      <Button
                        type="button"
                        appearance="outline"
                        className="w-fit"
                        onClick={() =>
                          setTransferDrawer({
                            transferId: linkedTransferId,
                            transferNumber: null,
                          })
                        }
                        data-testid={`stock-request-view-transfer-${linkedTransferId}`}
                      >
                        {t("inventory.viewTransfer")}
                      </Button>
                    </li>
                  ) : null}
                </ul>
              </div>
            ) : null}
          </Card>
        </div>
      </Card>

      {dto.notes ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{dto.notes}</p>
      ) : null}

      {committedRemainingLabel && !showSourceFulfillmentSummary ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-medium text-foreground"
          data-testid="stock-request-committed-remaining"
        >
          {committedRemainingLabel}
        </p>
      ) : null}

      {showSourceFulfillmentSummary ? (
        <Card
          className="flex min-w-0 flex-col gap-2 p-3"
          treatment="bordered"
          data-testid="stock-request-fulfillment"
        >
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("transfer.fulfillment")}
          </h2>
          <p
            className="m-0 text-[length:var(--exits-text-sm)]"
            data-testid="stock-request-needs-fulfillment"
          >
            {formatCommittedRemainingLabel(
              remainingDispatchQty,
              t("stockRequest.committedRemaining"),
              formatTransferQty,
            ) ??
              t("stockRequest.needsFulfillmentSummary").replace(
                "{qty}",
                formatTransferQty(remainingDispatchQty),
              )}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              onClick={() => prepareTransferMutation.mutate()}
              disabled={prepareTransferMutation.isPending || prepareMutation.isPending}
              data-testid="stock-request-fulfill-remaining"
            >
              {prepareTransferButtonLabel}
            </Button>
          </div>
        </Card>
      ) : null}

      <ExitsResponsiveDataView
        layout={layout}
        testId="stock-request-lines"
        table={
          layout === "table" ? (
            <ExitsTableContainer data-testid="stock-request-lines-desktop">
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead cellAlign="text" colSize="flex">
                      {t("purchasing.colProduct")}
                    </ExitsTableHead>
                    <ExitsTableHead
                      cellAlign="center"
                      className={
                        pendingAtSource
                          ? "stock-request-approve-qty-col whitespace-nowrap"
                          : "whitespace-nowrap"
                      }
                      colWidth={pendingAtSource ? "9.5rem" : undefined}
                      colSize={pendingAtSource ? undefined : "numeric"}
                    >
                      {t("stockRequest.approved")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="center" colSize="numeric">
                      {t("transfer.dispatched")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="center" colSize="numeric">
                      {t("stockRequest.goodReceived")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="center" colSize="numeric">
                      {t("transfer.discrepancy")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="center" colSize="numeric">
                      {t("transfer.inTransit")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="center" colSize="numeric">
                      {remainingDispatchColumnLabel}
                    </ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {dto.lines.map((line) => {
                    const approved = displayApprovedQuantity(line.approvedQuantity);
                    const approveDraftQty = Number(
                      approvedQtys[line.productId] ?? line.requestedQuantity,
                    );
                    return (
                      <ExitsTableRow
                        key={line.lineId}
                        data-testid={`stock-request-line-${line.productId}`}
                      >
                        <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                          <div>{line.nameSnapshot}</div>
                          <div className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                            {line.unitOfMeasure}
                            {` · ${t("stockRequest.requested")}: ${formatTransferQty(line.requestedQuantity)}`}
                            {pendingAtSource
                              ? ` · ${t("stockRequest.approved")}: ${formatTransferQty(
                                  Number.isFinite(approveDraftQty) ? approveDraftQty : 0,
                                )}`
                              : null}
                            {!pendingAtSource && isSource
                              ? ` · ${t("stockRequest.remainingToDispatch")}: ${formatTransferQty(
                                  line.remainingToDispatchQuantity,
                                )}`
                              : null}
                          </div>
                        </ExitsTableCell>
                        <ExitsTableCell
                          cellAlign="center"
                          className={
                            pendingAtSource
                              ? "stock-request-approve-qty-col"
                              : "tabular-nums"
                          }
                          colSize={pendingAtSource ? undefined : "numeric"}
                        >
                          {pendingAtSource ? (
                            <StockRequestApproveQtyStepper
                              productId={line.productId}
                              requestedQuantity={line.requestedQuantity}
                              unitOfMeasure={line.unitOfMeasure}
                              value={approvedQtys[line.productId]}
                              onChange={(next) =>
                                setApprovedQtys((prev) => ({
                                  ...prev,
                                  [line.productId]: next,
                                }))
                              }
                              t={t}
                            />
                          ) : (
                            <span data-testid={`stock-request-approved-${line.productId}`}>
                              {formatTransferQty(approved)}
                            </span>
                          )}
                        </ExitsTableCell>
                        <ExitsTableCell
                          cellAlign="center"
                          colSize="numeric"
                          className="tabular-nums"
                          data-testid={`stock-request-dispatched-${line.productId}`}
                        >
                          {formatTransferQty(line.sentQuantity)}
                        </ExitsTableCell>
                        <ExitsTableCell
                          cellAlign="center"
                          colSize="numeric"
                          className="tabular-nums"
                          data-testid={`stock-request-received-${line.productId}`}
                        >
                          {formatTransferQty(line.fulfilledQuantity)}
                        </ExitsTableCell>
                        <ExitsTableCell
                          cellAlign="center"
                          colSize="numeric"
                          className="tabular-nums"
                          data-testid={`stock-request-discrepancy-${line.productId}`}
                        >
                          {formatTransferQty(line.damagedQuantity ?? 0)}
                        </ExitsTableCell>
                        <ExitsTableCell
                          cellAlign="center"
                          colSize="numeric"
                          className="tabular-nums"
                          data-testid={`stock-request-in-transit-${line.productId}`}
                        >
                          {formatTransferQty(line.inProgressQuantity)}
                        </ExitsTableCell>
                        <ExitsTableCell
                          cellAlign="center"
                          colSize="numeric"
                          className="tabular-nums"
                          data-testid={`stock-request-remaining-dispatch-${line.productId}`}
                        >
                          {formatTransferQty(line.remainingToDispatchQuantity)}
                        </ExitsTableCell>
                      </ExitsTableRow>
                    );
                  })}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          ) : null
        }
        list={
          layout === "list" ? (
            <ul className="exits-data-record-list" data-testid="stock-request-lines-mobile">
              {dto.lines.map((line) => {
                const approved = displayApprovedQuantity(line.approvedQuantity);
                const approveDraftQty = Number(
                  approvedQtys[line.productId] ?? line.requestedQuantity,
                );
                const subtitleParts = [
                  line.unitOfMeasure,
                  `${t("stockRequest.requested")}: ${formatTransferQty(line.requestedQuantity)}`,
                ];
                if (pendingAtSource) {
                  subtitleParts.push(
                    `${t("stockRequest.approved")}: ${formatTransferQty(
                      Number.isFinite(approveDraftQty) ? approveDraftQty : 0,
                    )}`,
                  );
                } else if (isSource) {
                  subtitleParts.push(
                    `${t("stockRequest.remainingToDispatch")}: ${formatTransferQty(
                      line.remainingToDispatchQuantity,
                    )}`,
                  );
                }
                return (
                  <ExitsDataRecordCard
                    key={line.lineId}
                    as="li"
                    data-testid={`stock-request-line-${line.productId}`}
                    title={line.nameSnapshot}
                    subtitle={subtitleParts.join(" · ")}
                    fields={[
                      {
                        label: t("stockRequest.approved"),
                        value: pendingAtSource ? (
                          <StockRequestApproveQtyStepper
                            productId={line.productId}
                            requestedQuantity={line.requestedQuantity}
                            unitOfMeasure={line.unitOfMeasure}
                            value={approvedQtys[line.productId]}
                            onChange={(next) =>
                              setApprovedQtys((prev) => ({
                                ...prev,
                                [line.productId]: next,
                              }))
                            }
                            t={t}
                          />
                        ) : (
                          <span data-testid={`stock-request-approved-${line.productId}`}>
                            {formatTransferQty(approved)}
                          </span>
                        ),
                        emphasize: true,
                      },
                      {
                        label: t("transfer.dispatched"),
                        value: (
                          <span data-testid={`stock-request-dispatched-${line.productId}`}>
                            {formatTransferQty(line.sentQuantity)}
                          </span>
                        ),
                      },
                      {
                        label: t("stockRequest.goodReceived"),
                        value: (
                          <span data-testid={`stock-request-received-${line.productId}`}>
                            {formatTransferQty(line.fulfilledQuantity)}
                          </span>
                        ),
                      },
                      {
                        label: t("transfer.discrepancy"),
                        value: (
                          <span data-testid={`stock-request-discrepancy-${line.productId}`}>
                            {formatTransferQty(line.damagedQuantity ?? 0)}
                          </span>
                        ),
                      },
                      {
                        label: t("transfer.inTransit"),
                        value: (
                          <span data-testid={`stock-request-in-transit-${line.productId}`}>
                            {formatTransferQty(line.inProgressQuantity)}
                          </span>
                        ),
                      },
                      {
                        label: remainingDispatchColumnLabel,
                        value: (
                          <span data-testid={`stock-request-remaining-dispatch-${line.productId}`}>
                            {formatTransferQty(line.remainingToDispatchQuantity)}
                          </span>
                        ),
                      },
                    ]}
                  />
                );
              })}
            </ul>
          ) : null
        }
      />

      {pendingAtSource ? (
        <div className="po-document-actions" data-testid="stock-request-approve-actions">
          <div className="po-document-actions__cluster">
            <Button
              type="button"
              intent="danger"
              appearance="outline"
              onClick={() => {
                setActionError(null);
                setDeclineOpen(true);
              }}
              disabled={
                approveMutation.isPending ||
                rejectMutation.isPending ||
                prepareMutation.isPending
              }
              data-testid="stock-request-decline"
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {t("stockRequest.decline")}
            </Button>
            <Button
              type="button"
              onClick={() => approveMutation.mutate(false)}
              disabled={approveMutation.isPending || rejectMutation.isPending}
              data-testid="stock-request-approve"
            >
              {t("stockRequest.approve")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() => approveMutation.mutate(true)}
              disabled={approveMutation.isPending || rejectMutation.isPending}
              data-testid="stock-request-approve-prepare"
            >
              {t("stockRequest.approveAndPrepare")}
            </Button>
          </div>
        </div>
      ) : null}

      {preparingAtSource || canPrepareTransferAction ? (
        <div className="flex flex-wrap gap-2" data-testid="stock-request-prepare-transfer-actions">
          {preparingAtSource && dto.status === "Approved" ? (
            <Button
              type="button"
              variant="secondary"
              onClick={() => prepareMutation.mutate()}
              disabled={prepareMutation.isPending || prepareTransferMutation.isPending}
              data-testid="stock-request-start-preparing"
            >
              {t("stockRequest.startPreparing")}
            </Button>
          ) : null}
          {canPrepareTransferAction && !showSourceFulfillmentSummary ? (
            <Button
              type="button"
              onClick={() => prepareTransferMutation.mutate()}
              disabled={prepareTransferMutation.isPending || prepareMutation.isPending}
              data-testid="stock-request-prepare-transfer"
            >
              {prepareTransferButtonLabel}
            </Button>
          ) : null}
        </div>
      ) : null}

      {canReceive && !singleReceiveTransferId ? (
        <div
          className="flex min-w-0 flex-col gap-2"
          data-testid="stock-request-receive-choices"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("stockRequest.readyToReceive")}
          </p>
          <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
            {receivableTransfers.map((tr) => (
              <li key={tr.transferId}>
                <Button
                  asChild
                  variant="outline"
                  data-testid={`stock-request-receive-${tr.transferId}`}
                >
                  <Link to={transferReceiveHref(tr.transferId)}>
                    {tr.transferNumber?.trim() || tr.transferId.slice(0, 8)}
                    {tr.totalOutstandingQty != null
                      ? ` · ${formatTransferQty(tr.totalOutstandingQty)}`
                      : ""}
                  </Link>
                </Button>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <div className="receive-stock-actions" data-testid="stock-request-detail-actions">
        <div className="receive-stock-actions__primary">
          <Button
            type="button"
            intent="primary"
            appearance="ghost"
            className="font-semibold"
            onClick={smartBack.onBack}
            data-testid="stock-request-detail-back"
          >
            <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
            {smartBack.backLabel}
          </Button>
          {canEditRequest ? (
            <Button
              type="button"
              appearance="outline"
              disabled={cancelMutation.isPending}
              onClick={() => {
                navigate("/warehouse/request-stock", {
                  state: {
                    amendFromStockRequest: {
                      stockRequestId: dto.stockRequestId,
                      notes: dto.notes ?? null,
                      lines: dto.lines.map((line) => ({
                        productId: line.productId,
                        name: line.nameSnapshot,
                        unitOfMeasure: line.unitOfMeasure,
                        quantity: line.requestedQuantity,
                      })),
                    },
                  },
                });
              }}
              data-testid="stock-request-edit"
            >
              <Pencil className="size-4 shrink-0" aria-hidden />
              {t("stockRequest.editRequest")}
            </Button>
          ) : null}
          {canCancel ? (
            <Button
              type="button"
              intent="danger"
              appearance="solid"
              onClick={() => cancelMutation.mutate()}
              disabled={cancelMutation.isPending}
              data-testid="stock-request-cancel"
            >
              {t("stockRequest.cancel")}
            </Button>
          ) : null}
          {canReceive && singleReceiveTransferId ? (
            <Button asChild data-testid="stock-request-receive">
              <Link to={transferReceiveHref(singleReceiveTransferId)}>
                <PackageCheck className="size-4 shrink-0" aria-hidden />
                {t("stockRequest.readyToReceive")}
              </Link>
            </Button>
          ) : null}
        </div>
      </div>

      {actionError && !declineOpen ? (
        <p className="m-0 text-danger text-[length:var(--exits-text-sm)]" role="alert">
          {actionError}
        </p>
      ) : null}

      <SideDrawer
        open={timelineOpen}
        onClose={() => setTimelineOpen(false)}
        title={t("stockRequest.timelineTitle")}
        description={dto.requestNumber?.trim() || undefined}
        testId="stock-request-timeline-drawer"
        closeLabel={t("purchasing.timelineClose")}
        closeTestId="stock-request-timeline-drawer-close"
        panelClassName="exits-form-drawer__panel exits-form-drawer__panel--lg"
      >
        <div className="exits-form-drawer" data-testid="stock-request-timeline-drawer-content">
          <div className="exits-form-drawer__body">
            {activityQuery.isLoading ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("stockRequest.loading")}
              </p>
            ) : activityQuery.isError ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-danger">
                {t("stockRequest.activity.loadError")}
              </p>
            ) : (
              <StockRequestActivityTimeline
                events={activityEvents}
                resolveActor={actors.resolve}
                isResolving={actors.isResolving}
              />
            )}
          </div>
        </div>
      </SideDrawer>

      <ExitsModal
        open={declineOpen}
        onOpenChange={(open) => {
          if (!open && !rejectMutation.isPending) {
            setDeclineOpen(false);
            setRejectReason("");
            setActionError(null);
          }
        }}
        title={t("stockRequest.declineConfirmTitle")}
        description={t("stockRequest.declineConfirmDetail")}
        testId="stock-request-decline-dialog"
        size="md"
        footer={
          <div className="flex flex-wrap justify-end gap-2">
            <Button
              type="button"
              intent="neutral"
              appearance="outline"
              disabled={rejectMutation.isPending}
              onClick={() => {
                setDeclineOpen(false);
                setRejectReason("");
                setActionError(null);
              }}
              data-testid="stock-request-decline-cancel"
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              intent="danger"
              disabled={rejectMutation.isPending || !rejectReason.trim()}
              onClick={() => rejectMutation.mutate()}
              data-testid="stock-request-decline-confirm"
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {rejectMutation.isPending
                ? t("stockRequest.declining")
                : t("stockRequest.decline")}
            </Button>
          </div>
        }
      >
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span>{t("stockRequest.rejectReason")}</span>
          <textarea
            className="exits-input min-h-24 resize-y"
            rows={3}
            value={rejectReason}
            onChange={(e) => {
              setRejectReason(e.target.value);
              if (actionError) setActionError(null);
            }}
            placeholder={t("stockRequest.rejectReasonPlaceholder")}
            data-testid="stock-request-decline-reason"
            data-exits-modal-autofocus="true"
          />
        </label>
        {actionError ? (
          <p className="m-0 mt-2 text-danger text-[length:var(--exits-text-sm)]" role="alert">
            {actionError}
          </p>
        ) : null}
      </ExitsModal>

      {transferDrawer ? (
        <InventoryMovementTransactionDrawer
          open
          onOpenChange={(open) => {
            if (!open) {
              setTransferDrawer(null);
            }
          }}
          movement={null}
          transferContext={transferDrawer}
          unitOfMeasure=""
          workspace={workspace}
          resolveActor={actors.resolve}
          actorsLoading={actors.isResolving}
        />
      ) : null}

      {documentPreviewOpen ? (
        <BusinessDocumentPreview
          open={documentPreviewOpen}
          onClose={() => setDocumentPreviewOpen(false)}
          title={documentTitle}
          closeLabel={t("summary.closePreview")}
          printLabel={t("exitsTable.print")}
          pdfLabel={t("exitsTable.exportPdf")}
          showPdf={false}
          testId="stock-request-document-preview"
        >
          {printDocument}
        </BusinessDocumentPreview>
      ) : null}
    </div>
  );
}

import { ArrowLeftRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
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
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { StatusChip } from "@/components/exits/StatusChip";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  formatTransferQty,
  inventoryTransferStatusLabelKey,
} from "@/features/inventory/inventory-transfer-labels";
import { StockRequestActivityTimeline } from "@/features/replenishment/StockRequestActivityTimeline";
import {
  canCancelStockRequestAsDestination,
  canPrepareTransfer,
  findLinkedDraftTransfer,
  findOpenCoveringTransfer,
  prepareTransferPrimaryLabelKey,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
  totalRemainingToDispatch,
} from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function StockRequestDetailPage() {
  const { stockRequestId = "" } = useParams();
  const { t } = useI18n();
  const smartBack = usePageSmartBack({
    fallback: "stockRequests",
    backLabel: t("stockRequest.listTitle"),
    backTestId: "page-header-back-stock-requests",
  });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [rejectReason, setRejectReason] = useState("");
  const [approvedQtys, setApprovedQtys] = useState<Record<string, string>>({});
  const [actionError, setActionError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["stock-request", stockRequestId, workspace?.organizationId],
    enabled: Boolean(workspace && stockRequestId),
    queryFn: ({ signal }) => getStockRequest(workspace!, stockRequestId, signal),
  });

  const activityQuery = useQuery({
    queryKey: ["stock-request-activity", stockRequestId, workspace?.organizationId],
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
      if (andPrepare) {
        await prepareStockRequest(workspace, dto.stockRequestId);
      }
    },
    onSuccess: () => void invalidate(),
    onError: () => setActionError(t("stockRequest.actionError")),
  });

  const prepareMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      setActionError(null);
      return prepareStockRequest(workspace, dto.stockRequestId);
    },
    onSuccess: () => void invalidate(),
    onError: () => setActionError(t("stockRequest.actionError")),
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
    onError: () => setActionError(t("stockRequest.actionError")),
  });

  const rejectMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      const reason = rejectReason.trim();
      if (!reason) throw new Error("reason");
      setActionError(null);
      return rejectStockRequest(workspace, dto.stockRequestId, reason);
    },
    onSuccess: () => void invalidate(),
    onError: (err) => {
      setActionError(
        err instanceof Error && err.message === "reason"
          ? t("stockRequest.declineReasonRequired")
          : t("stockRequest.actionError"),
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
    onError: () => setActionError(t("stockRequest.actionError")),
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
  const openCover = findOpenCoveringTransfer(dto.linkedTransfers);
  const linkedDraftId = findLinkedDraftTransfer(dto.linkedTransfers);
  const canPrepareTransferAction =
    allowManage &&
    isSource &&
    canPrepareTransfer(dto.status, dto.lines);
  const remainingDispatchQty = totalRemainingToDispatch(dto.lines);
  const prepareTransferLabelKey = prepareTransferPrimaryLabelKey(
    dto.status,
    Boolean(linkedDraftId),
  );
  const prepareTransferButtonLabel =
    prepareTransferLabelKey === "stockRequest.fulfillRemaining"
      ? t("stockRequest.fulfillRemaining").replace("{qty}", String(remainingDispatchQty))
      : t(prepareTransferLabelKey as MessageKey);
  const canReceive =
    allowManage &&
    isDestination &&
    (dto.status === "InTransit" || dto.status === "PartiallyFulfilled") &&
    Boolean(
      dto.linkedTransfers.find(
        (tr) => tr.status === "InTransit" || tr.status === "PartiallyReceived",
      )?.transferId ?? linkedTransferId,
    );
  const receiveTransferId =
    dto.linkedTransfers.find(
      (tr) => tr.status === "InTransit" || tr.status === "PartiallyReceived",
    )?.transferId ?? linkedTransferId;
  const canCancel =
    allowManage && isDestination && canCancelStockRequestAsDestination(dto.status);
  const showSourceFulfillmentSummary =
    allowManage && isSource && remainingDispatchQty > 0 && canPrepareTransferAction;

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-detail">
      <PageHeader
        title={dto.requestNumber ?? t("stockRequest.detailTitle")}
        description={`${dto.destinationLocationName ?? dto.destinationLocationId} ← ${dto.requestedSourceLocationName ?? dto.requestedSourceLocationId}`}
        {...smartBack}
        trailing={
          <StatusChip tone={stockRequestStatusTone(dto.status)}>
            {t(stockRequestStatusLabelKey(dto.status) as MessageKey)}
          </StatusChip>
        }
      />

      {dto.notes ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{dto.notes}</p>
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
            {t("stockRequest.needsFulfillmentSummary").replace(
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

      <ExitsTableContainer data-testid="stock-request-lines">
        <ExitsTable data-testid="stock-request-lines-desktop">
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
                {t("transfer.inTransit")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.needsFulfillment")}
              </ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {dto.lines.map((line) => {
              const sent =
                line.sentQuantity > 0
                  ? line.sentQuantity
                  : (line.approvedQuantity ?? line.requestedQuantity);
              return (
                <ExitsTableRow
                  key={line.lineId}
                  data-testid={`stock-request-line-${line.productId}`}
                >
                  <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                    <div>{line.nameSnapshot}</div>
                    <div className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                      {line.unitOfMeasure}
                      {pendingAtSource ? ` · ${t("stockRequest.requested")}: ${line.requestedQuantity}` : ""}
                    </div>
                    {pendingAtSource ? (
                      <label className="mt-2 flex items-center gap-2 text-[length:var(--exits-text-sm)] font-normal">
                        <span>{t("stockRequest.approvedQty")}</span>
                        <input
                          className="exits-input w-24 max-w-[6rem] shrink-0"
                          inputMode="decimal"
                          value={approvedQtys[line.productId] ?? String(line.requestedQuantity)}
                          onChange={(e) =>
                            setApprovedQtys((prev) => ({
                              ...prev,
                              [line.productId]: e.target.value,
                            }))
                          }
                          data-testid={`stock-request-approve-qty-${line.productId}`}
                        />
                      </label>
                    ) : null}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                    {formatTransferQty(sent)}
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
                    data-testid={`stock-request-damaged-${line.productId}`}
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

        <ExitsTableMobile data-testid="stock-request-lines-mobile">
          {dto.lines.map((line) => {
            const sent =
              line.sentQuantity > 0
                ? line.sentQuantity
                : (line.approvedQuantity ?? line.requestedQuantity);
            return (
              <ExitsTableMobileRow
                key={line.lineId}
                data-testid={`stock-request-line-${line.productId}`}
              >
                <div className="exits-table-mobile__title-row">
                  <span className="exits-table-mobile__title">{line.nameSnapshot}</span>
                </div>
                <p className="exits-table-mobile__math mt-1 mb-0">
                  {t("transfer.sent")}: {formatTransferQty(sent)}
                  {` · ${t("transfer.good")}: ${formatTransferQty(line.fulfilledQuantity)}`}
                  {` · ${t("transfer.damaged")}: ${formatTransferQty(line.damagedQuantity ?? 0)}`}
                  {` · ${t("transfer.inTransit")}: ${formatTransferQty(line.inProgressQuantity)}`}
                  {` · ${t("transfer.needsFulfillment")}: ${formatTransferQty(line.remainingToDispatchQuantity)}`}
                </p>
                {pendingAtSource ? (
                  <label className="mt-2 flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <span>{t("stockRequest.approvedQty")}</span>
                    <input
                      className="exits-input w-24 max-w-[6rem] shrink-0"
                      inputMode="decimal"
                      value={approvedQtys[line.productId] ?? String(line.requestedQuantity)}
                      onChange={(e) =>
                        setApprovedQtys((prev) => ({
                          ...prev,
                          [line.productId]: e.target.value,
                        }))
                      }
                      data-testid={`stock-request-approve-qty-${line.productId}`}
                    />
                  </label>
                ) : null}
              </ExitsTableMobileRow>
            );
          })}
        </ExitsTableMobile>
      </ExitsTableContainer>

      {openCover ? (
        <div
          className="m-0 flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-muted/40 p-3 text-[length:var(--exits-text-sm)]"
          data-testid="stock-request-open-transfer-guard"
          role="status"
        >
          <p className="m-0">
            {t("stockRequest.waitingForDestination")
              .replace("{qty}", String(openCover.outstandingQty))
              .replace("{transfer}", openCover.transferLabel)}
          </p>
          <Link className="underline w-fit" to={`/inventory/transfers/${openCover.transferId}`}>
            {t("stockRequest.viewOpenTransfer")}
          </Link>
        </div>
      ) : null}

      <section className="rounded-[var(--exits-radius-md)] border border-border p-3" data-testid="stock-request-activity">
        <h2 className="exits-type-label m-0 mb-2">{t("stockRequest.activity")}</h2>
        {activityQuery.isLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockRequest.loading")}</p>
        ) : activityQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-danger">{t("stockRequest.activity.loadError")}</p>
        ) : (
          <StockRequestActivityTimeline
            events={activityQuery.data ?? []}
            resolveActor={actors.resolve}
            isResolving={actors.isResolving}
          />
        )}
      </section>

      {dto.linkedTransfers.length > 0 || linkedTransferId ? (
        <section>
          <h2 className="exits-type-label">{t("stockRequest.linkedTransfers")}</h2>
          <ul className="m-0 flex list-none flex-col gap-1 p-0">
            {dto.linkedTransfers.map((tr) => (
              <li
                key={tr.transferId}
                className="text-[length:var(--exits-text-sm)]"
                data-testid={`stock-request-linked-transfer-${tr.transferId}`}
              >
                <Link className="underline font-medium" to={`/inventory/transfers/${tr.transferId}`}>
                  {tr.transferNumber ?? tr.transferId.slice(0, 8)}
                </Link>
                {" · "}
                <StatusChip tone="neutral" shape="pill">
                  {t(inventoryTransferStatusLabelKey(tr.status) as MessageKey)}
                </StatusChip>
                <span className="text-muted">
                  {" · "}
                  {t("stockRequest.linkedTransfer.sent")}: {tr.totalSentQty}
                  {" · "}
                  {t("stockRequest.linkedTransfer.received")}: {tr.totalReceivedQty}
                  {" · "}
                  {t("stockRequest.linkedTransfer.closed")}: {tr.totalClosedQty ?? 0}
                  {" · "}
                  {t("stockRequest.linkedTransfer.outstanding")}: {tr.totalOutstandingQty ?? 0}
                </span>
              </li>
            ))}
            {linkedTransferId &&
            !dto.linkedTransfers.some((tr) => tr.transferId === linkedTransferId) ? (
              <li>
                <Link className="underline" to={`/inventory/transfers/${linkedTransferId}`}>
                  {linkedTransferId.slice(0, 8)}
                </Link>
              </li>
            ) : null}
          </ul>
        </section>
      ) : null}

      {pendingAtSource ? (
        <div className="flex flex-col gap-2" data-testid="stock-request-approve-actions">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              onClick={() => approveMutation.mutate(false)}
              disabled={approveMutation.isPending}
              data-testid="stock-request-approve"
            >
              {t("stockRequest.approve")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() => approveMutation.mutate(true)}
              disabled={approveMutation.isPending}
              data-testid="stock-request-approve-prepare"
            >
              {t("stockRequest.approveAndPrepare")}
            </Button>
          </div>
          <textarea
            className="exits-input max-h-20 resize-none"
            rows={2}
            placeholder={t("stockRequest.rejectReason")}
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            data-testid="stock-request-decline-reason"
          />
          <Button
            type="button"
            variant="outline"
            onClick={() => rejectMutation.mutate()}
            disabled={rejectMutation.isPending}
            data-testid="stock-request-decline"
          >
            {t("stockRequest.decline")}
          </Button>
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

      {canReceive && receiveTransferId ? (
        <Button asChild data-testid="stock-request-receive">
          <Link to={`/inventory/transfers/${receiveTransferId}`}>{t("stockRequest.readyToReceive")}</Link>
        </Button>
      ) : null}

      {canCancel ? (
        <Button
          type="button"
          variant="outline"
          onClick={() => cancelMutation.mutate()}
          disabled={cancelMutation.isPending}
          data-testid="stock-request-cancel"
        >
          {t("stockRequest.cancel")}
        </Button>
      ) : null}

      {actionError ? (
        <p className="m-0 text-danger text-[length:var(--exits-text-sm)]" role="alert">
          {actionError}
        </p>
      ) : null}
    </div>
  );
}

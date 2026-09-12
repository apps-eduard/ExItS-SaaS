import { ArrowLeftRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  approveStockRequest,
  cancelStockRequest,
  dispatchStockRequest,
  getStockRequest,
  prepareStockRequest,
  rejectStockRequest,
} from "@/api/pos/pos-stock-requests-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { formatTransferTimestamp } from "@/features/inventory/inventory-transfer-labels";
import {
  canCancelStockRequestAsDestination,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
} from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function StockRequestDetailPage() {
  const { stockRequestId = "" } = useParams();
  const { t } = useI18n();
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

  const actors = useActorDirectory(workspace?.organizationId, [
    dto?.requestedBy,
    dto?.approvedBy,
    dto?.preparingStartedBy,
    dto?.dispatchedBy,
    dto?.rejectedBy,
    dto?.cancelledBy,
  ]);

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["stock-request", stockRequestId] });
    await queryClient.invalidateQueries({ queryKey: ["stock-requests"] });
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

  const dispatchMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      setActionError(null);
      return dispatchStockRequest(workspace, dto.stockRequestId);
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
  const canReceive =
    allowManage && isDestination && dto.status === "InTransit" && Boolean(linkedTransferId);
  const canCancel =
    allowManage && isDestination && canCancelStockRequestAsDestination(dto.status);

  const actorLabel = (id: string | null | undefined) => {
    if (!id) return null;
    return actors.resolve(id)?.displayName ?? id.slice(0, 8);
  };

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-detail">
      <PageHeader
        title={dto.requestNumber ?? t("stockRequest.detailTitle")}
        description={`${dto.destinationLocationName ?? dto.destinationLocationId} ← ${dto.requestedSourceLocationName ?? dto.requestedSourceLocationId}`}
        backTo="/inventory/stock-requests"
        backLabel={t("stockRequest.listTitle")}
        trailing={
          <StatusChip tone={stockRequestStatusTone(dto.status)}>
            {t(stockRequestStatusLabelKey(dto.status) as MessageKey)}
          </StatusChip>
        }
      />

      {dto.notes ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{dto.notes}</p>
      ) : null}

      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {dto.lines.map((line) => (
          <li key={line.lineId} className="rounded-[var(--exits-radius-md)] border border-border p-3">
            <div className="font-medium">{line.nameSnapshot}</div>
            <div className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {t("stockRequest.requested")}: {line.requestedQuantity}
              {" · "}
              {t("stockRequest.approved")}: {line.approvedQuantity ?? "—"}
              {" · "}
              {t("stockRequest.fulfilled")}: {line.fulfilledQuantity}
              {line.inProgressQuantity > 0 ? (
                <>
                  {" · "}
                  {t("stockRequest.inProgress")}: {line.inProgressQuantity}
                </>
              ) : null}
              {" · "}
              {line.unitOfMeasure}
            </div>
            {pendingAtSource ? (
              <label className="mt-2 flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <span>{t("stockRequest.approvedQty")}</span>
                <input
                  className="exits-input w-24 max-w-[6rem] shrink-0"
                  inputMode="decimal"
                  value={approvedQtys[line.productId] ?? String(line.requestedQuantity)}
                  onChange={(e) =>
                    setApprovedQtys((prev) => ({ ...prev, [line.productId]: e.target.value }))
                  }
                  data-testid={`stock-request-approve-qty-${line.productId}`}
                />
              </label>
            ) : null}
          </li>
        ))}
      </ul>

      <section className="rounded-[var(--exits-radius-md)] border border-border p-3" data-testid="stock-request-activity">
        <h2 className="exits-type-label m-0 mb-2">{t("stockRequest.activity")}</h2>
        <ul className="m-0 flex list-none flex-col gap-1 p-0 text-[length:var(--exits-text-sm)] text-muted">
          <li>
            {t("stockRequest.activity.requested")}: {formatTransferTimestamp(dto.createdAtUtc)}
            {actorLabel(dto.requestedBy) ? ` · ${actorLabel(dto.requestedBy)}` : ""}
          </li>
          {dto.approvedAtUtc ? (
            <li>
              {t("stockRequest.activity.approved")}: {formatTransferTimestamp(dto.approvedAtUtc)}
              {actorLabel(dto.approvedBy) ? ` · ${actorLabel(dto.approvedBy)}` : ""}
            </li>
          ) : null}
          {dto.preparingStartedAtUtc ? (
            <li>
              {t("stockRequest.activity.preparing")}:{" "}
              {formatTransferTimestamp(dto.preparingStartedAtUtc)}
              {actorLabel(dto.preparingStartedBy) ? ` · ${actorLabel(dto.preparingStartedBy)}` : ""}
            </li>
          ) : null}
          {dto.dispatchedAtUtc ? (
            <li>
              {t("stockRequest.activity.dispatched")}: {formatTransferTimestamp(dto.dispatchedAtUtc)}
              {actorLabel(dto.dispatchedBy) ? ` · ${actorLabel(dto.dispatchedBy)}` : ""}
            </li>
          ) : null}
          {dto.rejectedAtUtc ? (
            <li>
              {t("stockRequest.activity.rejected")}: {formatTransferTimestamp(dto.rejectedAtUtc)}
              {dto.rejectionReason ? ` · ${dto.rejectionReason}` : ""}
            </li>
          ) : null}
          {dto.cancelledAtUtc ? (
            <li>
              {t("stockRequest.activity.cancelled")}: {formatTransferTimestamp(dto.cancelledAtUtc)}
            </li>
          ) : null}
        </ul>
      </section>

      {dto.linkedTransfers.length > 0 || linkedTransferId ? (
        <section>
          <h2 className="exits-type-label">{t("stockRequest.linkedTransfers")}</h2>
          <ul className="m-0 flex list-none flex-col gap-1 p-0">
            {dto.linkedTransfers.map((tr) => (
              <li key={tr.transferId}>
                <Link className="underline" to={`/inventory/transfers/${tr.transferId}`}>
                  {tr.transferNumber ?? tr.transferId.slice(0, 8)} · {tr.status}
                </Link>
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

      {preparingAtSource ? (
        <div className="flex flex-wrap gap-2" data-testid="stock-request-dispatch-actions">
          {dto.status === "Approved" ? (
            <Button
              type="button"
              variant="secondary"
              onClick={() => prepareMutation.mutate()}
              disabled={prepareMutation.isPending || dispatchMutation.isPending}
              data-testid="stock-request-start-preparing"
            >
              {t("stockRequest.startPreparing")}
            </Button>
          ) : null}
          <Button
            type="button"
            onClick={() => dispatchMutation.mutate()}
            disabled={dispatchMutation.isPending || prepareMutation.isPending}
            data-testid="stock-request-dispatch"
          >
            {t("stockRequest.dispatchStock")}
          </Button>
        </div>
      ) : null}

      {canReceive ? (
        <Button asChild data-testid="stock-request-receive">
          <Link to={`/inventory/transfers/${linkedTransferId}`}>{t("stockRequest.receiveLinked")}</Link>
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

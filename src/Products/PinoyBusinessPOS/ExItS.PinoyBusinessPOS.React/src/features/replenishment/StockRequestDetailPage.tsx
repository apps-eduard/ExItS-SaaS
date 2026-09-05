import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  cancelStockRequest,
  fulfillStockRequestViaTransfer,
  getStockRequest,
  rejectStockRequest,
} from "@/api/pos/pos-stock-requests-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { remainingRequestQty } from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function StockRequestDetailPage() {
  const { stockRequestId = "" } = useParams();
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [rejectReason, setRejectReason] = useState("");
  const [fulfillQtys, setFulfillQtys] = useState<Record<string, string>>({});

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
  const open =
    dto &&
    !["Rejected", "Cancelled", "Fulfilled"].includes(dto.status);

  const fulfillMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      const lines = dto.lines
        .map((line) => ({
          productId: line.productId,
          quantity: Number(fulfillQtys[line.productId] ?? remainingRequestQty(line.requestedQuantity, line.fulfilledQuantity, line.inProgressQuantity)),
        }))
        .filter((l) => l.quantity > 0);
      return fulfillStockRequestViaTransfer(workspace, dto.stockRequestId, { lines });
    },
    onSuccess: async (transfer) => {
      await queryClient.invalidateQueries({ queryKey: ["stock-request", stockRequestId] });
      window.location.assign(`/inventory/transfers/${transfer.transferId}`);
    },
  });

  const rejectMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      return rejectStockRequest(workspace, dto.stockRequestId, rejectReason.trim() || t("stockRequest.rejectDefault"));
    },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["stock-request", stockRequestId] }),
  });

  const cancelMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !dto) throw new Error("missing");
      return cancelStockRequest(workspace, dto.stockRequestId);
    },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["stock-request", stockRequestId] }),
  });

  if (!workspace) {
    return <EmptyState title={t("stockRequest.detailTitle")} detail={t("stockRequest.needBranch")} />;
  }

  if (query.isLoading) return <LoadingState label={t("stockRequest.loading")} />;
  if (query.isError || !dto) {
    return <ErrorState title={t("stockRequest.loadError")} detail={t("stockRequest.loadError")} />;
  }

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-detail">
      <PageHeader
        title={dto.requestNumber ?? t("stockRequest.detailTitle")}
        description={`${dto.destinationLocationName ?? dto.destinationLocationId} ← ${dto.requestedSourceLocationName ?? dto.requestedSourceLocationId}`}
        backTo="/inventory/stock-requests"
        backLabel={t("stockRequest.listTitle")}
      />

      <StatusChip tone="info">{dto.status}</StatusChip>

      <ul className="flex flex-col gap-2">
        {dto.lines.map((line) => {
          const remaining = remainingRequestQty(
            line.requestedQuantity,
            line.fulfilledQuantity,
            line.inProgressQuantity,
          );
          return (
            <li key={line.lineId} className="rounded-[var(--exits-radius-md)] border border-border p-3">
              <div className="font-medium">{line.nameSnapshot}</div>
              <div className="text-[length:var(--exits-text-sm)] text-muted">
                {t("stockRequest.requested")}: {line.requestedQuantity} · {t("stockRequest.fulfilled")}:{" "}
                {line.fulfilledQuantity} · {t("stockRequest.inProgress")}: {line.inProgressQuantity}
              </div>
              {allowManage && isSource && open ? (
                <label className="mt-2 flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <span>{t("stockRequest.fulfillQty")}</span>
                  <input
                    className="exits-input w-24"
                    inputMode="decimal"
                    placeholder={String(remaining)}
                    value={fulfillQtys[line.productId] ?? ""}
                    onChange={(e) =>
                      setFulfillQtys((prev) => ({ ...prev, [line.productId]: e.target.value }))
                    }
                  />
                </label>
              ) : null}
            </li>
          );
        })}
      </ul>

      {dto.linkedTransfers.length > 0 ? (
        <section>
          <h2 className="exits-type-label">{t("stockRequest.linkedTransfers")}</h2>
          <ul className="flex flex-col gap-1">
            {dto.linkedTransfers.map((tr) => (
              <li key={tr.transferId}>
                <Link className="underline" to={`/inventory/transfers/${tr.transferId}`}>
                  {tr.transferNumber ?? tr.transferId.slice(0, 8)} · {tr.status}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {allowManage && isSource && open ? (
        <div className="flex flex-col gap-2">
          <Button type="button" onClick={() => fulfillMutation.mutate()} disabled={fulfillMutation.isPending}>
            {t("stockRequest.createTransfer")}
          </Button>
          <textarea
            className="exits-input"
            rows={2}
            placeholder={t("stockRequest.rejectReason")}
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
          />
          <Button type="button" variant="outline" onClick={() => rejectMutation.mutate()} disabled={rejectMutation.isPending}>
            {t("stockRequest.reject")}
          </Button>
        </div>
      ) : null}

      {allowManage && isDestination && open ? (
        <Button type="button" variant="outline" onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending}>
          {t("stockRequest.cancel")}
        </Button>
      ) : null}

      <Link to={pageBackNav.inventory.to} className="text-[length:var(--exits-text-sm)] underline">
        {t(pageBackNav.inventory.labelKey)}
      </Link>
    </div>
  );
}

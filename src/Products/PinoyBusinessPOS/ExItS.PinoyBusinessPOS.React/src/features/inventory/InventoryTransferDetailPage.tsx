import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRight, Ban, PackageCheck, Truck } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  cancelInventoryTransfer,
  dispatchInventoryTransfer,
  getInventoryTransfer,
  INVENTORY_TRANSFER_DISCREPANCY_REASONS,
  receiveInventoryTransfer,
  type InventoryTransferDto,
} from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  branchDisplayName,
  formatTransferQty,
  formatTransferTimestamp,
  inventoryTransferDiscrepancyLabelKey,
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusTone,
  parseReceivedQuantity,
} from "@/features/inventory/inventory-transfer-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Mode = "detail" | "receive";
type ConfirmKind = "dispatch" | "cancel" | "receive" | null;

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

  const [localError, setLocalError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [mode, setMode] = useState<Mode>("detail");
  const [confirmKind, setConfirmKind] = useState<ConfirmKind>(null);
  const [receivedByLine, setReceivedByLine] = useState<Record<string, string>>({});
  const [reasonByLine, setReasonByLine] = useState<Record<string, string>>({});
  const [noteByLine, setNoteByLine] = useState<Record<string, string>>({});

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
  ) {
    if (!workspace) {
      return;
    }
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
      setLocalError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("transfer.actionFailed"))
          : t("transfer.actionFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function onDispatch() {
    if (!workspace || !transfer) {
      return;
    }
    const { transferId: id, destinationBranchName, destinationBranchId } = transfer;
    setConfirmKind(null);
    const dest = branchDisplayName(destinationBranchName, destinationBranchId);
    await refreshAfter(
      () => dispatchInventoryTransfer(workspace, id),
      t("transfer.dispatchedSuccess").replace("{destination}", dest),
    );
  }

  async function onCancel() {
    if (!workspace || !transfer) {
      return;
    }
    setConfirmKind(null);
    const successMessage =
      transfer.status === "InTransit"
        ? t("transfer.cancelledRestoredSuccess").replace(
            "{source}",
            branchDisplayName(transfer.sourceBranchName, transfer.sourceBranchId),
          )
        : t("transfer.cancelledSuccess");
    await refreshAfter(() => cancelInventoryTransfer(workspace, transfer.transferId), successMessage);
  }

  async function onReceive() {
    if (!workspace || !transfer) {
      return;
    }
    setConfirmKind(null);
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
        setLocalError(t("transfer.invalidReceivedQuantity"));
        return;
      }
      const entry: (typeof lines)[number] = {
        productId: line.productId,
        receivedQty: parsed,
        lineId: line.lineId,
      };
      if (parsed < line.sentQty) {
        const reason = reasonByLine[line.lineId]?.trim();
        if (reason) {
          entry.discrepancyReason = reason;
        }
        const note = noteByLine[line.lineId]?.trim();
        if (note) {
          entry.discrepancyNote = note;
        }
      }
      lines.push(entry);
    }

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
      setLocalError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("transfer.actionFailed"))
          : t("transfer.actionFailed"),
      );
    } finally {
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
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
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

  const dialogCancelIcon = <Ban className="size-4 shrink-0" aria-hidden />;

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
        confirmIcon={<Truck className="size-4 shrink-0" aria-hidden />}
        cancelLabel={t("transfer.dialogCancel")}
        cancelIcon={dialogCancelIcon}
        cancelTone="danger-outline"
        testId="transfer-dispatch-confirm"
        onCancel={() => setConfirmKind(null)}
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
        confirmIcon={<Ban className="size-4 shrink-0" aria-hidden />}
        cancelLabel={t("transfer.dialogCancel")}
        cancelIcon={dialogCancelIcon}
        cancelTone="danger-outline"
        confirmTone="danger"
        testId="transfer-cancel-confirm"
        onCancel={() => setConfirmKind(null)}
        onConfirm={() => void onCancel()}
      />
    ) : confirmKind === "receive" ? (
      <ConfirmationDialog
        open
        title={t("transfer.receiveConfirmTitle")}
        detail={t("transfer.receiveFinalConfirmDetail")}
        confirmLabel={t("transfer.receive")}
        confirmIcon={<PackageCheck className="size-4 shrink-0" aria-hidden />}
        cancelLabel={t("transfer.dialogCancel")}
        cancelIcon={dialogCancelIcon}
        cancelTone="danger-outline"
        testId="transfer-receive-confirm"
        onCancel={() => setConfirmKind(null)}
        onConfirm={() => void onReceive()}
      />
    ) : null;

  if (mode === "receive" && isInTransit) {
    return (
      <div
        className="inventory-transfer-receive-page exits-page flex min-w-0 flex-col gap-3 pb-4"
        data-testid="inventory-transfer-receive-page"
      >
        <PageHeader
          title={t("transfer.receiveTitle")}
          description={`${transfer.transferNumber ?? t("transfer.draftNumber")} · ${sourceName} → ${destName}`}
          backTo={`/inventory/transfers/${transfer.transferId}`}
          backLabel={t("transfer.backToTransfer")}
          backTestId="page-header-back-transfer"
        />
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.receiveFinalHint")}</p>
        {localError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {localError}
          </p>
        ) : null}

        {/* Desktop table */}
        <div className="hidden md:block overflow-x-auto" data-testid="transfer-receive-table">
          <table className="w-full min-w-[40rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
            <thead>
              <tr className="border-b border-border">
                <th className="px-2 py-2">{t("transfer.product")}</th>
                <th className="px-2 py-2">{t("transfer.sent")}</th>
                <th className="px-2 py-2">{t("transfer.received")}</th>
                <th className="px-2 py-2">{t("transfer.difference")}</th>
                <th className="px-2 py-2">{t("transfer.discrepancyReason")}</th>
              </tr>
            </thead>
            <tbody>
              {transfer.lines.map((line) => {
                const text = receivedByLine[line.lineId] ?? "";
                const parsed = parseReceivedQuantity(text, line.sentQty);
                const diff =
                  parsed === "empty" || parsed === "invalid" ? null : line.sentQty - parsed;
                return (
                  <tr key={line.lineId} className="border-b border-border align-top">
                    <td className="px-2 py-2">
                      <div className="font-medium">{line.productName}</div>
                      <div className="text-muted">
                        {line.unitOfMeasure}
                        {line.lotNumber || line.expirationDate
                          ? ` · ${line.lotNumber ?? "—"} · ${line.expirationDate ?? "—"}`
                          : ""}
                      </div>
                    </td>
                    <td className="px-2 py-2">{formatTransferQty(line.sentQty)}</td>
                    <td className="px-2 py-2">
                      <input
                        className="exits-input w-28"
                        inputMode="decimal"
                        value={text}
                        onChange={(e) =>
                          setReceivedByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                        }
                        data-testid={`transfer-receive-qty-${line.lineId}`}
                      />
                    </td>
                    <td className="px-2 py-2">{diff == null ? "—" : formatTransferQty(diff)}</td>
                    <td className="px-2 py-2">
                      {diff != null && diff > 0 ? (
                        <div className="flex flex-col gap-1">
                          <select
                            className="exits-select"
                            value={reasonByLine[line.lineId] ?? ""}
                            onChange={(e) =>
                              setReasonByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                            }
                            data-testid={`transfer-discrepancy-${line.lineId}`}
                          >
                            <option value="">{t("transfer.selectDiscrepancy")}</option>
                            {INVENTORY_TRANSFER_DISCREPANCY_REASONS.map((code) => (
                              <option key={code} value={code}>
                                {t(inventoryTransferDiscrepancyLabelKey(code))}
                              </option>
                            ))}
                          </select>
                          <input
                            className="exits-input"
                            placeholder={t("transfer.discrepancyNote")}
                            value={noteByLine[line.lineId] ?? ""}
                            onChange={(e) =>
                              setNoteByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                            }
                            data-testid={`transfer-discrepancy-note-${line.lineId}`}
                          />
                        </div>
                      ) : (
                        "—"
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Mobile cards */}
        <ul className="m-0 flex list-none flex-col gap-3 p-0 md:hidden" data-testid="transfer-receive-mobile">
          {transfer.lines.map((line) => {
            const text = receivedByLine[line.lineId] ?? "";
            const parsed = parseReceivedQuantity(text, line.sentQty);
            const diff =
              parsed === "empty" || parsed === "invalid" ? null : line.sentQty - parsed;
            return (
              <li key={line.lineId}>
                <Card className="flex flex-col gap-2 p-3" data-testid={`transfer-receive-card-${line.lineId}`}>
                  <p className="m-0 font-semibold">{line.productName}</p>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("transfer.sent")}: {formatTransferQty(line.sentQty)} {line.unitOfMeasure}
                  </p>
                  <label className="flex flex-col gap-1">
                    <span className="text-[length:var(--exits-text-sm)] font-medium">
                      {t("transfer.received")}
                    </span>
                    <input
                      className="exits-input text-[length:var(--exits-text-lg)]"
                      inputMode="decimal"
                      value={text}
                      onChange={(e) =>
                        setReceivedByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                      }
                      data-testid={`transfer-receive-qty-mobile-${line.lineId}`}
                    />
                  </label>
                  <p className="m-0 text-[length:var(--exits-text-sm)]">
                    {t("transfer.difference")}: {diff == null ? "—" : formatTransferQty(diff)}
                  </p>
                  {diff != null && diff > 0 ? (
                    <>
                      <select
                        className="exits-select"
                        value={reasonByLine[line.lineId] ?? ""}
                        onChange={(e) =>
                          setReasonByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                        }
                      >
                        <option value="">{t("transfer.selectDiscrepancy")}</option>
                        {INVENTORY_TRANSFER_DISCREPANCY_REASONS.map((code) => (
                          <option key={code} value={code}>
                            {t(inventoryTransferDiscrepancyLabelKey(code))}
                          </option>
                        ))}
                      </select>
                      <input
                        className="exits-input"
                        placeholder={t("transfer.discrepancyNote")}
                        value={noteByLine[line.lineId] ?? ""}
                        onChange={(e) =>
                          setNoteByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                        }
                      />
                    </>
                  ) : null}
                </Card>
              </li>
            );
          })}
        </ul>

        <StickyActionBar>
          <div className="flex w-full flex-col gap-2 sm:flex-row">
            <Button
              type="button"
              variant="outline"
              className="flex-1"
              disabled={busy}
              onClick={() => setMode("detail")}
            >
              {t("transfer.backToTransfer")}
            </Button>
            <Button
              type="button"
              className="flex-1"
              disabled={!canReceive}
              onClick={() => setConfirmKind("receive")}
              data-testid="transfer-receive-submit"
            >
              {busy ? t("transfer.receiving") : t("transfer.receive")}
            </Button>
          </div>
        </StickyActionBar>
        {confirmDialog}
      </div>
    );
  }

  return (
    <div
      className="inventory-transfer-detail-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="inventory-transfer-detail-page"
      data-status={transfer.status}
    >
      <PageHeader
        title={t("transfer.summaryTitle")}
        subtitle={transfer.transferNumber?.trim() || undefined}
        trailing={
          <StatusChip tone={inventoryTransferStatusTone(transfer.status)}>
            {t(inventoryTransferStatusLabelKey(transfer.status))}
          </StatusChip>
        }
        backTo="/inventory/transfers"
        backLabel={t("transfer.backList")}
        backTestId="page-header-back-transfers"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}
      {localError ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-danger"
          role="alert"
          data-testid="transfer-local-error"
        >
          {localError}
        </p>
      ) : null}

      <div
        className="flex min-w-0 flex-wrap items-center justify-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3 sm:justify-start sm:gap-3"
        data-testid="transfer-route-summary"
      >
        <span className="truncate text-[length:var(--exits-text-md)] font-semibold text-foreground">
          {sourceName}
        </span>
        <ArrowRight className="size-4 shrink-0 text-muted" aria-hidden />
        <span className="truncate text-[length:var(--exits-text-md)] font-semibold text-foreground">
          {destName}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-2" data-testid="transfer-qty-summary">
        <div className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5">
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.sent")}</p>
          <p className="m-0 mt-0.5 text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
            {formatTransferQty(transfer.totalSentQty)}
          </p>
        </div>
        <div className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5">
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.received")}</p>
          <p className="m-0 mt-0.5 text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
            {formatTransferQty(transfer.totalReceivedQty)}
          </p>
        </div>
      </div>

      {(transfer.dispatchedAtUtc || transfer.receivedAtUtc || transfer.cancelledAtUtc) && (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {transfer.dispatchedAtUtc
            ? `${t("transfer.dispatched")}: ${formatTransferTimestamp(transfer.dispatchedAtUtc)}`
            : null}
          {transfer.dispatchedAtUtc && transfer.receivedAtUtc ? " · " : null}
          {transfer.receivedAtUtc
            ? `${t("transfer.receivedAt")}: ${formatTransferTimestamp(transfer.receivedAtUtc)}`
            : null}
          {(transfer.dispatchedAtUtc || transfer.receivedAtUtc) && transfer.cancelledAtUtc
            ? " · "
            : null}
          {transfer.cancelledAtUtc
            ? `${t("transfer.cancelledAt")}: ${formatTransferTimestamp(transfer.cancelledAtUtc)}`
            : null}
        </p>
      )}

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
        <ul
          className="m-0 flex list-none flex-col divide-y divide-border overflow-hidden rounded-[var(--exits-radius-md)] border border-border p-0"
          data-testid="transfer-lines"
        >
          {transfer.lines.map((line) => {
            const showReceived = isFinal || isInTransit;
            const showDiff = line.differenceQty !== 0;
            return (
              <li
                key={line.lineId}
                className="flex min-w-0 items-start gap-3 bg-surface px-3 py-2.5"
                data-testid={`transfer-line-${line.lineId}`}
              >
                <div className="min-w-0 flex-1">
                  <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-medium text-foreground">
                    {line.productName}
                  </p>
                  {line.lotNumber || line.expirationDate ? (
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("transfer.lot")}: {line.lotNumber ?? "—"} · {t("transfer.expiry")}:{" "}
                      {line.expirationDate ?? "—"}
                    </p>
                  ) : null}
                  {line.discrepancyReason ? (
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("transfer.discrepancy")}:{" "}
                      {t(inventoryTransferDiscrepancyLabelKey(line.discrepancyReason))}
                      {line.discrepancyNote ? ` — ${line.discrepancyNote}` : ""}
                    </p>
                  ) : null}
                </div>
                <div className="shrink-0 text-right">
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-medium tabular-nums text-foreground">
                    {formatTransferQty(line.sentQty)} {line.unitOfMeasure}
                  </p>
                  {showReceived ? (
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted tabular-nums">
                      {t("transfer.received")}: {formatTransferQty(line.receivedQty)}
                    </p>
                  ) : null}
                  {showDiff ? (
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted tabular-nums">
                      {t("transfer.difference")} {formatTransferQty(line.differenceQty)}
                    </p>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      </section>

      {canCancel || canDispatch || canReceive ? (
        <StickyActionBar className="flex-col items-stretch gap-2 sm:flex-row sm:items-center sm:justify-end">
          {isDraft ? (
            <p className="m-0 flex-1 text-[length:var(--exits-text-xs)] text-muted sm:mr-auto">
              {t("transfer.draftNoEdit")}
            </p>
          ) : (
            <span className="hidden flex-1 sm:block" aria-hidden />
          )}
          <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:justify-end">
            {canCancel ? (
              <Button
                type="button"
                variant="outline"
                className="border-destructive/40 text-destructive hover:border-destructive/55 hover:bg-[var(--exits-danger-soft)]"
                disabled={!canMutate}
                onClick={() => setConfirmKind("cancel")}
                data-testid="transfer-cancel"
              >
                <Ban className="size-4 shrink-0" aria-hidden />
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
                onClick={() => setConfirmKind("dispatch")}
                data-testid="transfer-dispatch"
              >
                <Truck className="size-4 shrink-0" aria-hidden />
                {t("transfer.dispatch")}
              </Button>
            ) : null}
          </div>
        </StickyActionBar>
      ) : isDraft ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.draftNoEdit")}</p>
      ) : null}
      {confirmDialog}
    </div>
  );
}

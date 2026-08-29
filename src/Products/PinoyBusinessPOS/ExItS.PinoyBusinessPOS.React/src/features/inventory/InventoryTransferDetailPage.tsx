import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
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

export function InventoryTransferDetailPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { transferId = "" } = useParams();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [localError, setLocalError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [mode, setMode] = useState<Mode>("detail");
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
      setSuccess(t("transfer.createdSuccess"));
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.pathname, location.state, navigate, t]);

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
    if (!workspace || busy) {
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
      setSuccess(successMessage);
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
    const source = branchDisplayName(transfer.sourceBranchName, transfer.sourceBranchId);
    const dest = branchDisplayName(transfer.destinationBranchName, transfer.destinationBranchId);
    if (
      !window.confirm(
        t("transfer.dispatchConfirm")
          .replace("{source}", source)
          .replace("{destination}", dest)
          .replace("{count}", String(transfer.lines.length)),
      )
    ) {
      return;
    }
    await refreshAfter(
      () => dispatchInventoryTransfer(workspace, transfer.transferId),
      t("transfer.dispatchedSuccess").replace("{destination}", dest),
    );
  }

  async function onCancel() {
    if (!workspace || !transfer) {
      return;
    }
    const message =
      transfer.status === "InTransit"
        ? t("transfer.cancelInTransitConfirm")
        : t("transfer.cancelDraftConfirm");
    if (!window.confirm(message)) {
      return;
    }
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
    if (!window.confirm(t("transfer.receiveFinalConfirm"))) {
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
      setSuccess(
        updated.status === "PartiallyReceived"
          ? t("transfer.partiallyReceivedSuccess")
          : t("transfer.receivedSuccess").replace("{destination}", dest),
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
                        className="exits-input min-h-11 w-28"
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
                            className="exits-input min-h-11"
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
                            className="exits-input min-h-11"
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
                      className="exits-input min-h-12 text-[length:var(--exits-text-lg)]"
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
                        className="exits-input min-h-11"
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
                        className="exits-input min-h-11"
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
              className="min-h-11 flex-1"
              disabled={busy}
              onClick={() => setMode("detail")}
            >
              {t("transfer.backToTransfer")}
            </Button>
            <Button
              type="button"
              className="min-h-11 flex-1"
              disabled={!canReceive}
              onClick={() => void onReceive()}
              data-testid="transfer-receive-submit"
            >
              {busy ? t("transfer.receiving") : t("transfer.receive")}
            </Button>
          </div>
        </StickyActionBar>
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
        title={transfer.transferNumber?.trim() || t("transfer.draftNumber")}
        description={`${sourceName} → ${destName}`}
        backTo="/inventory/transfers"
        backLabel={t("transfer.backList")}
        backTestId="page-header-back-transfers"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}
      {success ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-success" data-testid="transfer-success">
          {success}
        </p>
      ) : null}
      {localError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert" data-testid="transfer-local-error">
          {localError}
        </p>
      ) : null}

      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone={inventoryTransferStatusTone(transfer.status)}>
          {t(inventoryTransferStatusLabelKey(transfer.status))}
        </StatusChip>
        <span className="text-[length:var(--exits-text-sm)] text-muted">
          {t("transfer.currentBranch")}: {boundWorkspace?.branchName}
        </span>
      </div>

      <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
        <div>
          <dt className="text-muted">{t("transfer.fromBranch")}</dt>
          <dd className="m-0 font-medium">{sourceName}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("transfer.toBranch")}</dt>
          <dd className="m-0 font-medium">{destName}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("transfer.sent")}</dt>
          <dd className="m-0">{formatTransferQty(transfer.totalSentQty)}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("transfer.received")}</dt>
          <dd className="m-0">{formatTransferQty(transfer.totalReceivedQty)}</dd>
        </div>
        {transfer.dispatchedAtUtc ? (
          <div>
            <dt className="text-muted">{t("transfer.dispatched")}</dt>
            <dd className="m-0">{formatTransferTimestamp(transfer.dispatchedAtUtc)}</dd>
          </div>
        ) : null}
        {transfer.receivedAtUtc ? (
          <div>
            <dt className="text-muted">{t("transfer.receivedAt")}</dt>
            <dd className="m-0">{formatTransferTimestamp(transfer.receivedAtUtc)}</dd>
          </div>
        ) : null}
        {transfer.cancelledAtUtc ? (
          <div>
            <dt className="text-muted">{t("transfer.cancelledAt")}</dt>
            <dd className="m-0">{formatTransferTimestamp(transfer.cancelledAtUtc)}</dd>
          </div>
        ) : null}
      </dl>

      {transfer.notes ? (
        <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="transfer-notes-display">
          {transfer.notes}
        </p>
      ) : null}

      {isDraft ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.draftNoEdit")}</p>
      ) : null}

      <section>
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-base)] font-semibold">{t("transfer.items")}</h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="transfer-lines">
          {transfer.lines.map((line) => (
            <li key={line.lineId}>
              <Card className="flex flex-col gap-1 p-3" data-testid={`transfer-line-${line.lineId}`}>
                <p className="m-0 font-medium">{line.productName}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("transfer.sent")}: {formatTransferQty(line.sentQty)} {line.unitOfMeasure}
                  {isFinal || isInTransit
                    ? ` · ${t("transfer.received")}: ${formatTransferQty(line.receivedQty)}`
                    : ""}
                  {line.differenceQty !== 0
                    ? ` · ${t("transfer.difference")}: ${formatTransferQty(line.differenceQty)}`
                    : ""}
                </p>
                {line.lotNumber || line.expirationDate ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("transfer.lot")}: {line.lotNumber ?? "—"} · {t("transfer.expiry")}:{" "}
                    {line.expirationDate ?? "—"}
                  </p>
                ) : null}
                {line.discrepancyReason ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)]">
                    {t("transfer.discrepancy")}:{" "}
                    {t(inventoryTransferDiscrepancyLabelKey(line.discrepancyReason))}
                    {line.discrepancyNote ? ` — ${line.discrepancyNote}` : ""}
                  </p>
                ) : null}
              </Card>
            </li>
          ))}
        </ul>
      </section>

      <div className="flex flex-wrap gap-2">
        {canDispatch ? (
          <Button
            type="button"
            className="min-h-11"
            disabled={!canMutate}
            onClick={() => void onDispatch()}
            data-testid="transfer-dispatch"
          >
            {t("transfer.dispatch")}
          </Button>
        ) : null}
        {canReceive ? (
          <Button
            type="button"
            className="min-h-11"
            disabled={!canMutate}
            onClick={() => {
              setLocalError(null);
              setMode("receive");
            }}
            data-testid="transfer-receive"
          >
            {t("transfer.receive")}
          </Button>
        ) : null}
        {canCancel ? (
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={!canMutate}
            onClick={() => void onCancel()}
            data-testid="transfer-cancel"
          >
            {t("transfer.cancel")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}

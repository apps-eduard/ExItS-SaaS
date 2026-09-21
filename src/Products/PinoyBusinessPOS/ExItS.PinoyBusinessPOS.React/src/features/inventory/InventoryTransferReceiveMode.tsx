import { useEffect, useMemo, useState, Fragment, type Dispatch, type ReactNode, type SetStateAction } from "react";
import { Check, RotateCcw } from "lucide-react";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { QuantityInput } from "@/components/exits/MoneyQuantityInputs";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableEditMenu,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableInlineEditor,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableRow,
  type ExitsTableEditableField,
} from "@/components/exits/ExitsTable";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  formatTransferQty,
  inventoryTransferDiscrepancyLabelKey,
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusTone,
  parseReceivedQuantity,
} from "@/features/inventory/inventory-transfer-labels";
import { TransferReceiveDiscrepancyDialog } from "@/features/inventory/TransferReceiveDiscrepancyDialog";
import { useI18n } from "@/i18n/I18nProvider";

type ReceiveEditFieldKey = "receiveNow";

function ReceiveIconAction({
  label,
  variant = "ghost",
  children,
  onClick,
  className,
  title,
  "data-testid": testId,
}: {
  label: string;
  variant?: "ghost" | "destructive" | "success" | "info";
  children: ReactNode;
  onClick?: () => void;
  className?: string;
  title?: string;
  "data-testid"?: string;
}) {
  return (
    <Button
      type="button"
      variant={variant}
      size="icon"
      shape="round"
      title={title ?? label}
      aria-label={label}
      className={className}
      data-testid={testId}
      onClick={onClick}
    >
      {children}
    </Button>
  );
}

export type InventoryTransferReceiveModeProps = {
  transfer: InventoryTransferDto;
  sourceName: string;
  destName: string;
  statusLabel: string;
  statusIcon: ReactNode;
  receivedByLine: Record<string, string>;
  reasonByLine: Record<string, string>;
  noteByLine: Record<string, string>;
  setReceivedByLine: Dispatch<SetStateAction<Record<string, string>>>;
  setReasonByLine: Dispatch<SetStateAction<Record<string, string>>>;
  setNoteByLine: Dispatch<SetStateAction<Record<string, string>>>;
  canSubmitReceive: boolean;
  busy: boolean;
  online: boolean;
  localErrorAlert: ReactNode;
  confirmDialog: ReactNode;
  onBack: () => void;
  onSubmit: () => void;
};

export function InventoryTransferReceiveMode({
  transfer,
  sourceName,
  destName,
  statusLabel,
  statusIcon,
  receivedByLine,
  reasonByLine,
  noteByLine,
  setReceivedByLine,
  setReasonByLine,
  setNoteByLine,
  canSubmitReceive,
  busy,
  online,
  localErrorAlert,
  confirmDialog,
  onBack,
  onSubmit,
}: InventoryTransferReceiveModeProps) {
  const { t } = useI18n();
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [editingFields, setEditingFields] = useState<Set<ReceiveEditFieldKey>>(new Set());
  const [mobileEditingLineId, setMobileEditingLineId] = useState<string | null>(null);
  const [mobileEditingFields, setMobileEditingFields] = useState<Set<ReceiveEditFieldKey>>(
    new Set(),
  );
  const [editBaseline, setEditBaseline] = useState<string | null>(null);
  const [editErrors, setEditErrors] = useState<{ receiveNow?: string }>({});
  const [discrepancyLineId, setDiscrepancyLineId] = useState<string | null>(null);
  const [highlightUnresolved, setHighlightUnresolved] = useState(false);

  const receiveEditableFields = useMemo<ReadonlyArray<ExitsTableEditableField>>(
    () => [{ key: "receiveNow", label: t("transfer.receiveNow") }],
    [t],
  );

  const statusTone = inventoryTransferStatusTone(transfer.status);
  const discrepancyLine = transfer.lines.find((line) => line.lineId === discrepancyLineId) ?? null;

  function qtyErrorFor(lineId: string, sentQty: number): string | null {
    const parsed = parseReceivedQuantity(receivedByLine[lineId] ?? "", sentQty);
    if (parsed === "exceeds") {
      return t("transfer.receivedExceedsSent").replace("{sent}", formatTransferQty(sentQty));
    }
    if (parsed === "invalid" || parsed === "empty") {
      return t("transfer.invalidReceivedQuantity");
    }
    return null;
  }

  function clearDiscrepancy(lineId: string) {
    setReasonByLine((prev) => {
      const next = { ...prev };
      delete next[lineId];
      return next;
    });
    setNoteByLine((prev) => {
      const next = { ...prev };
      delete next[lineId];
      return next;
    });
  }

  function cancelRowEdit() {
    setEditingLineId(null);
    setEditingFields(new Set());
    setEditBaseline(null);
    setEditErrors({});
  }

  function cancelMobileEdit() {
    setMobileEditingLineId(null);
    setMobileEditingFields(new Set());
    setEditBaseline(null);
    setEditErrors({});
  }

  function startFieldEdit(lineId: string, mobile = false) {
    const baseline = receivedByLine[lineId] ?? "";
    setEditBaseline(baseline);
    setEditErrors({});
    if (mobile) {
      setEditingLineId(null);
      setEditingFields(new Set());
      setMobileEditingLineId(lineId);
      setMobileEditingFields(new Set(["receiveNow"]));
    } else {
      setMobileEditingLineId(null);
      setMobileEditingFields(new Set());
      setEditingLineId(lineId);
      setEditingFields(new Set(["receiveNow"]));
    }
  }

  function finishEdit(lineId: string, sentQty: number, mobile = false) {
    const text = receivedByLine[lineId] ?? "";
    const error = qtyErrorFor(lineId, sentQty);
    if (error) {
      setEditErrors({ receiveNow: error });
      return;
    }
    const parsed = parseReceivedQuantity(text, sentQty);
    if (typeof parsed !== "number") {
      setEditErrors({ receiveNow: t("transfer.invalidReceivedQuantity") });
      return;
    }
    if (parsed >= sentQty) {
      clearDiscrepancy(lineId);
    }
    if (mobile) cancelMobileEdit();
    else cancelRowEdit();
    if (parsed < sentQty) {
      setDiscrepancyLineId(lineId);
    }
  }

  function resetEdit(lineId: string, mobile = false) {
    if (editBaseline == null) {
      if (mobile) cancelMobileEdit();
      else cancelRowEdit();
      return;
    }
    if ((receivedByLine[lineId] ?? "") === editBaseline) {
      if (mobile) cancelMobileEdit();
      else cancelRowEdit();
      return;
    }
    setReceivedByLine((prev) => ({ ...prev, [lineId]: editBaseline }));
    setEditErrors({});
  }

  useEffect(() => {
    if (!editingLineId && !mobileEditingLineId) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      const activeId = editingLineId ?? mobileEditingLineId;
      if (!activeId || editBaseline == null) {
        cancelRowEdit();
        cancelMobileEdit();
        return;
      }
      setReceivedByLine((prev) => ({ ...prev, [activeId]: editBaseline }));
      cancelRowEdit();
      cancelMobileEdit();
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [editingLineId, mobileEditingLineId, editBaseline, setReceivedByLine]);

  useEffect(() => {
    if (!editingLineId || !editingFields.has("receiveNow")) return;
    const timer = window.setTimeout(() => {
      document
        .querySelector<HTMLInputElement>(`[data-testid="transfer-receive-qty-${editingLineId}"]`)
        ?.focus();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [editingLineId, editingFields]);

  useEffect(() => {
    if (!mobileEditingLineId || !mobileEditingFields.has("receiveNow")) return;
    const timer = window.setTimeout(() => {
      document
        .querySelector<HTMLInputElement>(
          `[data-testid="transfer-receive-qty-mobile-${mobileEditingLineId}"]`,
        )
        ?.focus();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [mobileEditingLineId, mobileEditingFields]);

  function trySubmit() {
    setHighlightUnresolved(true);
    if (!canSubmitReceive) {
      const firstShort = transfer.lines.find((line) => {
        const parsed = parseReceivedQuantity(receivedByLine[line.lineId] ?? "", line.sentQty);
        return typeof parsed === "number" && parsed < line.sentQty && !reasonByLine[line.lineId]?.trim();
      });
      if (firstShort) {
        setDiscrepancyLineId(firstShort.lineId);
      }
      return;
    }
    onSubmit();
  }

  function renderDiscrepancyLink(lineId: string, sentQty: number, editing: boolean) {
    if (editing) return null;
    const parsed = parseReceivedQuantity(receivedByLine[lineId] ?? "", sentQty);
    if (typeof parsed !== "number" || parsed >= sentQty) return null;
    const reason = reasonByLine[lineId]?.trim() ?? "";
    const needs = !reason;
    const highlight = highlightUnresolved && needs;
    return (
      <button
        type="button"
        className={`mt-1 block max-w-full truncate border-0 bg-transparent p-0 text-start text-[length:var(--exits-text-xs)] underline-offset-2 hover:underline${
          highlight ? " text-[var(--exits-warning)]" : needs ? " text-[var(--exits-warning)]" : " text-muted"
        }`}
        onClick={() => setDiscrepancyLineId(lineId)}
        data-testid={`transfer-discrepancy-summary-${lineId}`}
      >
        {needs ? (
          <span>
            {t("transfer.discrepancy")}: {t("transfer.needsClassification")} ⚠
          </span>
        ) : (
          <span>
            {t("transfer.discrepancy")}: {t(inventoryTransferDiscrepancyLabelKey(reason))} ✓
          </span>
        )}
      </button>
    );
  }

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

      <Card
        className="po-document-summary po-document-summary--meta-cards grid gap-3 p-3"
        treatment="bordered"
        data-testid="transfer-receive-summary"
      >
        <h2 className="po-document-summary__title m-0 text-[length:var(--exits-text-md)] font-semibold text-primary">
          {transfer.transferNumber?.trim() || t("transfer.draftNumber")}
        </h2>
        <dl className="po-document-summary__meta m-0">
          <div className="po-document-summary__field min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">{t("transfer.colRoute")}</dt>
            <dd className="m-0 truncate font-medium">
              {sourceName} → {destName}
            </dd>
          </div>
          <div className="po-document-summary__field min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("purchasing.fieldStatus")}
            </dt>
            <dd className="m-0">
              <StatusChip
                tone={statusTone}
                appearance="soft"
                shape="soft"
                icon={statusIcon}
                className="exits-status-chip--control"
              >
                {statusLabel || t(inventoryTransferStatusLabelKey(transfer.status))}
              </StatusChip>
            </dd>
          </div>
          <div className="po-document-summary__field min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">{t("transfer.lines")}</dt>
            <dd className="m-0 font-medium tabular-nums">{transfer.lines.length}</dd>
          </div>
        </dl>
      </Card>

      {!online ? (
        <Notice tone="warning" testId="transfer-receive-offline">
          {t("transfer.offline")}
        </Notice>
      ) : null}
      <Notice tone="info" testId="transfer-receive-final-hint">
        {t("transfer.receiveFinalHint")}
      </Notice>
      {localErrorAlert}

      <ExitsTableContainer data-testid="transfer-receive-table">
        <ExitsTable data-testid="transfer-receive-desktop">
          <ExitsTableHeader>
            <ExitsTableRow>
              <ExitsTableHead cellAlign="text" colSize="flex">
                {t("transfer.product")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="text" colSize="sku">
                {t("purchasing.colSku")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.sent")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.received")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.difference")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="text">{t("purchasing.colUnit")}</ExitsTableHead>
              <ExitsTableHead cellAlign="actions" colSize="actions" />
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {transfer.lines.map((line) => {
              const text = receivedByLine[line.lineId] ?? "";
              const parsed = parseReceivedQuantity(text, line.sentQty);
              const qtyReady = typeof parsed === "number";
              const diff = qtyReady ? line.sentQty - parsed : null;
              const editing = editingLineId === line.lineId;
              const editReceiveNow = editing && editingFields.has("receiveNow");
              const rowValidationId = `transfer-receive-qty-error-${line.lineId}`;
              const needsReason =
                qtyReady && parsed < line.sentQty && !reasonByLine[line.lineId]?.trim();
              const highlight = highlightUnresolved && needsReason;
              const showRowValidation = editing && Boolean(editErrors.receiveNow);

              return (
                <Fragment key={line.lineId}>
                <ExitsTableRow
                  editing={editing}
                  error={highlight}
                  data-testid={`transfer-receive-row-${line.lineId}`}
                >
                  <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                    <div>{line.productName}</div>
                    {line.lotNumber || line.expirationDate ? (
                      <div className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                        {line.lotNumber ?? "—"} · {line.expirationDate ?? "—"}
                      </div>
                    ) : null}
                    {renderDiscrepancyLink(line.lineId, line.sentQty, editing)}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="text" colSize="sku" className="text-muted tabular-nums">
                    {line.sku?.trim() || "—"}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                    {formatTransferQty(line.sentQty)}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric">
                    {editReceiveNow ? (
                      <ExitsTableInlineEditor
                        invalid={Boolean(editErrors.receiveNow)}
                        errorId={editErrors.receiveNow ? rowValidationId : undefined}
                      >
                        <QuantityInput
                          label={t("transfer.receiveNow")}
                          value={text}
                          onChange={(e) => {
                            setReceivedByLine((prev) => ({
                              ...prev,
                              [line.lineId]: e.target.value,
                            }));
                            clearDiscrepancy(line.lineId);
                            setEditErrors((prev) => ({ ...prev, receiveNow: undefined }));
                          }}
                          aria-invalid={Boolean(editErrors.receiveNow)}
                          aria-describedby={editErrors.receiveNow ? rowValidationId : undefined}
                          data-testid={`transfer-receive-qty-${line.lineId}`}
                        />
                      </ExitsTableInlineEditor>
                    ) : (
                      <span className="tabular-nums">{text || "0"}</span>
                    )}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                    {diff == null ? "—" : formatTransferQty(diff)}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="text">{line.unitOfMeasure}</ExitsTableCell>
                  <ExitsTableCell cellAlign="actions" colSize="actions">
                    <ExitsTableActions data-testid={`transfer-receive-actions-${line.lineId}`}>
                      {editing ? (
                        <>
                          <ReceiveIconAction
                            label={t("transfer.saveReceiveQty")}
                            variant="success"
                            data-testid={`transfer-receive-edit-save-${line.lineId}`}
                            onClick={() => finishEdit(line.lineId, line.sentQty)}
                          >
                            <Check className="size-4" aria-hidden />
                          </ReceiveIconAction>
                          <ReceiveIconAction
                            label={t("transfer.resetReceiveQty")}
                            title={t("transfer.resetReceiveQty")}
                            className="exits-table__action-reset"
                            data-testid={`transfer-receive-edit-reset-${line.lineId}`}
                            onClick={() => resetEdit(line.lineId)}
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </ReceiveIconAction>
                        </>
                      ) : (
                      <ExitsTableEditMenu
                          fields={receiveEditableFields}
                          ariaLabel={t("transfer.editReceiveQty")}
                          data-testid={`transfer-receive-edit-menu-${line.lineId}`}
                          onSelectField={() => startFieldEdit(line.lineId)}
                          onEditAll={() => startFieldEdit(line.lineId)}
                        />
                      )}
                    </ExitsTableActions>
                  </ExitsTableCell>
                </ExitsTableRow>
                {showRowValidation ? (
                  <ExitsTableRow error>
                    <ExitsTableCell colSpan={7}>
                      <div
                        id={rowValidationId}
                        className="exits-table__row-validation"
                        role="alert"
                        data-testid={rowValidationId}
                      >
                        <span className="exits-table__row-validation-message">
                          {editErrors.receiveNow}
                        </span>
                      </div>
                    </ExitsTableCell>
                  </ExitsTableRow>
                ) : null}
                </Fragment>
              );
            })}
          </ExitsTableBody>
        </ExitsTable>

        <ExitsTableMobile data-testid="transfer-receive-mobile">
          {transfer.lines.map((line) => {
            const text = receivedByLine[line.lineId] ?? "";
            const parsed = parseReceivedQuantity(text, line.sentQty);
            const qtyReady = typeof parsed === "number";
            const diff = qtyReady ? line.sentQty - parsed : null;
            const editing = mobileEditingLineId === line.lineId;
            const editReceiveNow = editing && mobileEditingFields.has("receiveNow");

            return (
              <ExitsTableMobileRow
                key={line.lineId}
                editing={editing}
                data-testid={`transfer-receive-card-${line.lineId}`}
              >
                <div className="exits-table-mobile__title-row">
                  <span className="exits-table-mobile__title">{line.productName}</span>
                  <ExitsTableActions>
                    {editing ? (
                      <>
                        <ReceiveIconAction
                          label={t("transfer.saveReceiveQty")}
                          variant="success"
                          data-testid={`transfer-receive-edit-save-mobile-${line.lineId}`}
                          onClick={() => finishEdit(line.lineId, line.sentQty, true)}
                        >
                          <Check className="size-4" aria-hidden />
                        </ReceiveIconAction>
                        <ReceiveIconAction
                          label={t("transfer.resetReceiveQty")}
                          className="exits-table__action-reset"
                          data-testid={`transfer-receive-edit-reset-mobile-${line.lineId}`}
                          onClick={() => resetEdit(line.lineId, true)}
                        >
                          <RotateCcw className="size-4" aria-hidden />
                        </ReceiveIconAction>
                      </>
                    ) : (
                      <ExitsTableEditMenu
                        fields={receiveEditableFields}
                        ariaLabel={t("transfer.editReceiveQty")}
                        data-testid={`transfer-receive-edit-menu-mobile-${line.lineId}`}
                        onSelectField={() => startFieldEdit(line.lineId, true)}
                        onEditAll={() => startFieldEdit(line.lineId, true)}
                      />
                    )}
                  </ExitsTableActions>
                </div>
                <p className="exits-table-mobile__meta m-0">
                  {line.sku?.trim() || "—"} · {line.unitOfMeasure}
                </p>
                <p className="exits-table-mobile__math mt-1 mb-0">
                  {t("transfer.sent")}: {formatTransferQty(line.sentQty)}
                  {" · "}
                  {t("transfer.difference")}: {diff == null ? "—" : formatTransferQty(diff)}
                </p>
                {editReceiveNow ? (
                  <div className="mt-2">
                    <QuantityInput
                      label={t("transfer.receiveNow")}
                      value={text}
                      onChange={(e) => {
                        setReceivedByLine((prev) => ({
                          ...prev,
                          [line.lineId]: e.target.value,
                        }));
                        clearDiscrepancy(line.lineId);
                        setEditErrors({});
                      }}
                      data-testid={`transfer-receive-qty-mobile-${line.lineId}`}
                    />
                    {editErrors.receiveNow ? (
                      <p
                        className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-destructive"
                        data-testid={`transfer-receive-qty-error-mobile-${line.lineId}`}
                      >
                        {editErrors.receiveNow}
                      </p>
                    ) : null}
                  </div>
                ) : (
                  <p className="mt-1 mb-0 tabular-nums">
                    {t("transfer.received")}: {text || "0"}
                  </p>
                )}
                {renderDiscrepancyLink(line.lineId, line.sentQty, editing)}
              </ExitsTableMobileRow>
            );
          })}
        </ExitsTableMobile>
      </ExitsTableContainer>

      <div className="receive-stock-actions">
        <div className="receive-stock-actions__primary">
          <Button
            type="button"
            intent="primary"
            appearance="ghost"
            className="font-semibold"
            disabled={busy}
            onClick={onBack}
            data-testid="transfer-receive-back"
          >
            {t("transfer.backToTransfer")}
          </Button>
          <Button
            type="button"
            disabled={busy || !canSubmitReceive}
            onClick={trySubmit}
            data-testid="transfer-receive-submit"
          >
            {busy ? t("transfer.receiving") : t("transfer.receive")}
          </Button>
        </div>
      </div>

      {discrepancyLine ? (
        <TransferReceiveDiscrepancyDialog
          open
          productName={discrepancyLine.productName}
          unitOfMeasure={discrepancyLine.unitOfMeasure}
          sentQty={discrepancyLine.sentQty}
          receivedQty={
            typeof parseReceivedQuantity(
              receivedByLine[discrepancyLine.lineId] ?? "",
              discrepancyLine.sentQty,
            ) === "number"
              ? (parseReceivedQuantity(
                  receivedByLine[discrepancyLine.lineId] ?? "",
                  discrepancyLine.sentQty,
                ) as number)
              : 0
          }
          reason={reasonByLine[discrepancyLine.lineId] ?? ""}
          note={noteByLine[discrepancyLine.lineId] ?? ""}
          onReasonChange={(reason) =>
            setReasonByLine((prev) => ({ ...prev, [discrepancyLine.lineId]: reason }))
          }
          onNoteChange={(note) =>
            setNoteByLine((prev) => ({ ...prev, [discrepancyLine.lineId]: note }))
          }
          onCancel={() => setDiscrepancyLineId(null)}
          onConfirm={() => {
            if (!(reasonByLine[discrepancyLine.lineId] ?? "").trim()) {
              return;
            }
            setDiscrepancyLineId(null);
          }}
        />
      ) : null}

      {confirmDialog}
    </div>
  );
}

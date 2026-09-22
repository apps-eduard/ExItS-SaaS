import { useEffect, useMemo, useState, Fragment, type ReactNode } from "react";
import { ArrowLeft, Check, RotateCcw } from "lucide-react";
import type {
  InventoryTransferDto,
  ReceiveInventoryTransferRequest,
} from "@/api/pos/pos-inventory-transfer-client";
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
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusTone,
} from "@/features/inventory/inventory-transfer-labels";
import {
  buildTransferReceiveLineEdits,
  buildTransferRemainingDecisionRows,
  lineOutstandingQty,
  type TransferReceiveLineEdit,
} from "@/features/inventory/inventory-transfer-receive-helpers";
import { buildTransferReceivePayload } from "@/features/inventory/transfer-receive-plan";
import { ReceiveDiscrepancyDialog } from "@/features/purchasing/ReceiveDiscrepancyDialog";
import { ReceiveRemainingQuantityTable } from "@/features/purchasing/ReceiveRemainingQuantityTable";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import {
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
import {
  countUnresolvedRemaining,
  sumRemainingUnits,
  type RemainingDecisionAction,
} from "@/features/purchasing/receive-remaining-decision";
import {
  formatReceiveDiscrepancySummary,
  isReceiveDiscrepancyClassified,
  lineHasReceiveDiscrepancy,
  lineNeedsReceiveDiscrepancyClassification,
} from "@/features/purchasing/receive-discrepancy-display";
import {
  buildReceiveOtherReasonOptions,
  buildReceiveOtherReasonSummaryLabels,
} from "@/features/purchasing/receive-other-reason-options";
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
  busy: boolean;
  online: boolean;
  localErrorAlert: ReactNode;
  onBack: () => void;
  onSubmitReceive: (body: ReceiveInventoryTransferRequest) => void;
};

export function InventoryTransferReceiveMode({
  transfer,
  sourceName,
  destName,
  statusLabel,
  statusIcon,
  busy,
  online,
  localErrorAlert,
  onBack,
  onSubmitReceive,
}: InventoryTransferReceiveModeProps) {
  const { t } = useI18n();
  const [lines, setLines] = useState<TransferReceiveLineEdit[]>(() =>
    buildTransferReceiveLineEdits(transfer),
  );
  const [reviewing, setReviewing] = useState(false);
  const [flowError, setFlowError] = useState<string | null>(null);
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [editingFields, setEditingFields] = useState<Set<ReceiveEditFieldKey>>(new Set());
  const [mobileEditingLineId, setMobileEditingLineId] = useState<string | null>(null);
  const [mobileEditingFields, setMobileEditingFields] = useState<Set<ReceiveEditFieldKey>>(
    new Set(),
  );
  const [editBaselineGood, setEditBaselineGood] = useState<string | null>(null);
  const [editErrors, setEditErrors] = useState<{ receiveNow?: string }>({});
  const [discrepancyOpen, setDiscrepancyOpen] = useState(false);
  const [discrepancyTargetProductId, setDiscrepancyTargetProductId] = useState<string | null>(
    null,
  );
  const [highlightUnclassified, setHighlightUnclassified] = useState(false);
  const [highlightUnresolvedRemaining, setHighlightUnresolvedRemaining] = useState(false);

  useEffect(() => {
    setLines(buildTransferReceiveLineEdits(transfer));
    setReviewing(false);
    setFlowError(null);
    setDiscrepancyOpen(false);
    setDiscrepancyTargetProductId(null);
    setHighlightUnclassified(false);
    setHighlightUnresolvedRemaining(false);
    cancelRowEdit();
    cancelMobileEdit();
  }, [transfer.transferId, transfer.status, transfer.updatedAtUtc, transfer.totalReceivedQty]);

  const receiveEditableFields = useMemo<ReadonlyArray<ExitsTableEditableField>>(
    () => [{ key: "receiveNow", label: t("transfer.goodReceivedNow") }],
    [t],
  );

  const statusTone = inventoryTransferStatusTone(transfer.status);

  const otherReasonOptions = useMemo(() => buildReceiveOtherReasonOptions(t), [t]);
  const discrepancyLabels = useMemo(
    () => ({
      damaged: t("transfer.damaged"),
      notDelivered: t("transfer.notDelivered"),
      otherReasons: buildReceiveOtherReasonSummaryLabels(t),
      otherFallback: t("purchasing.otherDiscrepancy"),
    }),
    [t],
  );

  const unclassifiedProductIds = useMemo(
    () =>
      new Set(
        lines.filter((line) => lineNeedsReceiveDiscrepancyClassification(line)).map((l) => l.productId),
      ),
    [lines],
  );

  const remainingDecisionRows = useMemo(() => {
    if (!reviewing) {
      return [];
    }
    return buildTransferRemainingDecisionRows(lines, discrepancyLabels);
  }, [lines, reviewing, discrepancyLabels]);

  const remainingDecisionSummary = useMemo(() => {
    const products = remainingDecisionRows.length;
    if (products === 0) {
      return "";
    }
    const units = formatStockQtyLabel(sumRemainingUnits(remainingDecisionRows));
    if (products === 1) {
      return t("purchasing.remainingDecisionSummaryOne").replace("{units}", units);
    }
    return t("purchasing.remainingDecisionSummary")
      .replace("{products}", String(products))
      .replace("{units}", units);
  }, [remainingDecisionRows, t]);

  const discrepancyLines = useMemo(() => {
    return lines
      .map((line) => {
        const good = parseNonNegativeQty(line.goodText) ?? 0;
        const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
        return { line, good, discrepancy };
      })
      .filter((entry) => entry.discrepancy > 1e-9)
      .filter((entry) =>
        discrepancyTargetProductId ? entry.line.productId === discrepancyTargetProductId : true,
      )
      .map(({ line, good }) => ({
        productId: line.productId,
        name: line.name,
        uom: line.uom,
        outstandingQty: line.outstandingQty,
        goodQty: good,
        damagedText: line.damagedText,
        notDeliveredText: line.notDeliveredText,
        otherText: line.otherText,
        otherReasonCode: line.otherReasonCode,
        otherReasonText: line.otherReasonText,
        otherExpanded: line.otherExpanded,
        remarksText: line.remarksText,
      }));
  }, [lines, discrepancyTargetProductId]);

  function updateLine(productId: string, patch: Partial<TransferReceiveLineEdit>) {
    setLines((prev) =>
      prev.map((line) => (line.productId === productId ? { ...line, ...patch } : line)),
    );
  }

  function openDiscrepancyForProduct(productId: string) {
    setFlowError(null);
    setHighlightUnclassified(false);
    setDiscrepancyTargetProductId(productId);
    setDiscrepancyOpen(true);
  }

  function closeDiscrepancyDialog() {
    setDiscrepancyOpen(false);
    setDiscrepancyTargetProductId(null);
  }

  function qtyErrorFor(line: TransferReceiveLineEdit): string | null {
    const parsed = parseNonNegativeQty(line.goodText);
    if (parsed === null) {
      return t("transfer.invalidReceiveNowQuantity");
    }
    if (parsed > line.outstandingQty) {
      return t("transfer.receiveExceedsOutstanding").replace(
        "{outstanding}",
        formatTransferQty(line.outstandingQty),
      );
    }
    return null;
  }

  function cancelRowEdit() {
    setEditingLineId(null);
    setEditingFields(new Set());
    setEditBaselineGood(null);
    setEditErrors({});
  }

  function cancelMobileEdit() {
    setMobileEditingLineId(null);
    setMobileEditingFields(new Set());
    setEditBaselineGood(null);
    setEditErrors({});
  }

  function startFieldEdit(line: TransferReceiveLineEdit, mobile = false) {
    setEditBaselineGood(line.goodText);
    setEditErrors({});
    if (mobile) {
      setEditingLineId(null);
      setEditingFields(new Set());
      setMobileEditingLineId(line.lineId);
      setMobileEditingFields(new Set(["receiveNow"]));
    } else {
      setMobileEditingLineId(null);
      setMobileEditingFields(new Set());
      setEditingLineId(line.lineId);
      setEditingFields(new Set(["receiveNow"]));
    }
  }

  function finishEdit(line: TransferReceiveLineEdit, mobile = false) {
    const current = lines.find((entry) => entry.lineId === line.lineId) ?? line;
    const error = qtyErrorFor(current);
    if (error) {
      setEditErrors({ receiveNow: error });
      return;
    }
    const good = parseNonNegativeQty(current.goodText) ?? 0;
    const hasDiscrepancy = receiveDiscrepancyQty(current.outstandingQty, good) > 1e-9;
    if (!hasDiscrepancy) {
      updateLine(current.productId, {
        damagedText: "0",
        notDeliveredText: "0",
        otherText: "0",
        otherReasonCode: "",
        otherReasonText: "",
        otherExpanded: false,
        remarksText: "",
        remainingAction: null,
      });
    }
    if (mobile) cancelMobileEdit();
    else cancelRowEdit();
    if (hasDiscrepancy) {
      openDiscrepancyForProduct(current.productId);
    }
  }

  function resetEdit(line: TransferReceiveLineEdit, mobile = false) {
    if (editBaselineGood == null) {
      if (mobile) cancelMobileEdit();
      else cancelRowEdit();
      return;
    }
    if (line.goodText === editBaselineGood) {
      if (mobile) cancelMobileEdit();
      else cancelRowEdit();
      return;
    }
    updateLine(line.productId, { goodText: editBaselineGood });
    setEditErrors({});
  }

  function onDiscrepancyConfirmed() {
    if (!discrepancyTargetProductId) {
      return;
    }
    const target = lines.find((line) => line.productId === discrepancyTargetProductId);
    if (!target || !isReceiveDiscrepancyClassified(target)) {
      setFlowError(t("transfer.discrepancyClassificationRequired"));
      return;
    }
    if (!target.remarksText.trim()) {
      setFlowError(t("transfer.discrepancyNoteRequired"));
      return;
    }
    setFlowError(null);
    setHighlightUnclassified(false);
    closeDiscrepancyDialog();
  }

  function tryBuildPayload(): ReceiveInventoryTransferRequest | null {
    const result = buildTransferReceivePayload(lines);
    if (!result.ok) {
      if (result.error === "no_activity") {
        setFlowError(t("transfer.receiveRequiresPositiveQty"));
      } else if (
        result.error === "classification_incomplete" ||
        result.error === "classification_mismatch"
      ) {
        setFlowError(t("transfer.discrepancyClassificationRequired"));
      } else if (result.error === "over_receive") {
        setFlowError(
          t("transfer.receiveExceedsOutstanding").replace("{outstanding}", t("transfer.outstanding")),
        );
      } else {
        setFlowError(t("transfer.invalidReceiveNowQuantity"));
      }
      return null;
    }
    setFlowError(null);
    return { lines: result.lines };
  }

  function onReview() {
    const parsedOk = lines.every((line) => parseNonNegativeQty(line.goodText) !== null);
    if (!parsedOk) {
      setFlowError(t("transfer.invalidReceiveNowQuantity"));
      return;
    }
    const over = lines.some((line) => {
      const good = parseNonNegativeQty(line.goodText) ?? 0;
      return good > line.outstandingQty + 1e-9;
    });
    if (over) {
      setFlowError(
        t("transfer.receiveExceedsOutstanding").replace("{outstanding}", t("transfer.outstanding")),
      );
      return;
    }
    if (unclassifiedProductIds.size > 0) {
      setHighlightUnclassified(true);
      setFlowError(t("transfer.classifyBeforeReview"));
      return;
    }
    if (!tryBuildPayload()) {
      return;
    }
    setHighlightUnclassified(false);
    setHighlightUnresolvedRemaining(false);
    setReviewing(true);
  }

  function onConfirmReceive() {
    const remainingRows = buildTransferRemainingDecisionRows(lines, discrepancyLabels);
    if (countUnresolvedRemaining(remainingRows) > 0) {
      setHighlightUnresolvedRemaining(true);
      setFlowError(t("purchasing.remainingDecisionRequired"));
      return;
    }
    const body = tryBuildPayload();
    if (!body) {
      return;
    }
    onSubmitReceive(body);
  }

  useEffect(() => {
    if (!editingLineId && !mobileEditingLineId) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      const activeId = editingLineId ?? mobileEditingLineId;
      const line = lines.find((entry) => entry.lineId === activeId);
      if (!line || editBaselineGood == null) {
        cancelRowEdit();
        cancelMobileEdit();
        return;
      }
      updateLine(line.productId, { goodText: editBaselineGood });
      cancelRowEdit();
      cancelMobileEdit();
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [editingLineId, mobileEditingLineId, editBaselineGood, lines]);

  useEffect(() => {
    if (!editingLineId || !editingFields.has("receiveNow")) return;
    const timer = window.setTimeout(() => {
      document
        .querySelector<HTMLInputElement>(`[data-testid="transfer-receive-qty-${editingLineId}"]`)
        ?.focus();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [editingLineId, editingFields]);

  const canReview = lines.some((line) => {
    const good = parseNonNegativeQty(line.goodText);
    return good !== null && good > 0 && good <= line.outstandingQty + 1e-9;
  });

  return (
    <div
      className="inventory-transfer-receive-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="inventory-transfer-receive-page"
    >
      <PageHeader
        title={
          transfer.status === "PartiallyReceived"
            ? t("transfer.receiveRemainingTitle")
            : t("transfer.receiveTitle")
        }
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
      <Notice tone="info" testId="transfer-receive-wave-hint">
        {t("transfer.receiveWaveHint")}
      </Notice>
      {flowError ? (
        <Notice tone="danger" testId="transfer-receive-flow-error">
          {flowError}
        </Notice>
      ) : null}
      {localErrorAlert}

      {!reviewing ? (
        <ExitsTableContainer data-testid="transfer-receive-table">
          <ExitsTable data-testid="transfer-receive-desktop">
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text" colSize="flex">
                  {t("transfer.product")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.sent")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.previouslyReceived")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.outstanding")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center" colSize="numeric">
                  {t("transfer.goodReceivedNow")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.colUnit")}</ExitsTableHead>
                <ExitsTableHead cellAlign="actions" colSize="actions" />
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {lines.map((line) => {
                const transferLine = transfer.lines.find((l) => l.lineId === line.lineId);
                const outstanding = transferLine ? lineOutstandingQty(transferLine) : line.outstandingQty;
                const editing = editingLineId === line.lineId;
                const editReceiveNow = editing && editingFields.has("receiveNow");
                const rowValidationId = `transfer-receive-qty-error-${line.lineId}`;
                const showRowValidation = editing && Boolean(editErrors.receiveNow);
                const canEditLine = outstanding > 0;
                const needsClassification = unclassifiedProductIds.has(line.productId);
                const discrepancySummary = formatReceiveDiscrepancySummary(line, discrepancyLabels);

                return (
                  <Fragment key={line.lineId}>
                    <ExitsTableRow
                      editing={editing}
                      error={highlightUnclassified && needsClassification}
                      data-testid={`transfer-receive-row-${line.lineId}`}
                    >
                      <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                        <div>{line.name}</div>
                        {transferLine?.lotNumber || transferLine?.expirationDate ? (
                          <div className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                            {transferLine.lotNumber ?? "—"} · {transferLine.expirationDate ?? "—"}
                          </div>
                        ) : null}
                        {!editing && lineHasReceiveDiscrepancy(line) ? (
                          <button
                            type="button"
                            className="mt-1 block max-w-full truncate border-0 bg-transparent p-0 text-left text-[length:var(--exits-text-xs)] font-normal underline-offset-2 hover:underline"
                            data-testid={`transfer-receive-discrepancy-summary-${line.productId}`}
                            disabled={!canEditLine}
                            onClick={() => openDiscrepancyForProduct(line.productId)}
                          >
                            {needsClassification ? (
                              <span className="text-[var(--exits-warning)]">
                                {t("transfer.discrepancy")}: {t("transfer.needsClassification")} ⚠
                              </span>
                            ) : discrepancySummary ? (
                              <span className="text-muted">
                                {t("transfer.discrepancy")}: {discrepancySummary} ✓
                              </span>
                            ) : null}
                          </button>
                        ) : null}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                        {formatTransferQty(line.sentQty)}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                        {formatTransferQty(line.receivedQty)}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                        {formatTransferQty(outstanding)}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="center" colSize="numeric">
                        {editReceiveNow ? (
                          <ExitsTableInlineEditor
                            invalid={Boolean(editErrors.receiveNow)}
                            errorId={editErrors.receiveNow ? rowValidationId : undefined}
                          >
                            <QuantityInput
                              label={t("transfer.goodReceivedNow")}
                              value={line.goodText}
                              onChange={(e) => {
                                updateLine(line.productId, { goodText: e.target.value });
                                setEditErrors((prev) => ({ ...prev, receiveNow: undefined }));
                              }}
                              aria-invalid={Boolean(editErrors.receiveNow)}
                              aria-describedby={editErrors.receiveNow ? rowValidationId : undefined}
                              data-testid={`transfer-receive-qty-${line.lineId}`}
                            />
                          </ExitsTableInlineEditor>
                        ) : (
                          <span className="tabular-nums">{line.goodText || "0"}</span>
                        )}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text">{line.uom}</ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions data-testid={`transfer-receive-actions-${line.lineId}`}>
                          {canEditLine ? (
                            editing ? (
                              <>
                                <ReceiveIconAction
                                  label={t("transfer.saveReceiveQty")}
                                  variant="success"
                                  data-testid={`transfer-receive-edit-save-${line.lineId}`}
                                  onClick={() => finishEdit(line)}
                                >
                                  <Check className="size-4" aria-hidden />
                                </ReceiveIconAction>
                                <ReceiveIconAction
                                  label={t("transfer.resetReceiveQty")}
                                  title={t("transfer.resetReceiveQty")}
                                  className="exits-table__action-reset"
                                  data-testid={`transfer-receive-edit-reset-${line.lineId}`}
                                  onClick={() => resetEdit(line)}
                                >
                                  <RotateCcw className="size-4" aria-hidden />
                                </ReceiveIconAction>
                              </>
                            ) : (
                              <ExitsTableEditMenu
                                fields={receiveEditableFields}
                                ariaLabel={t("transfer.editReceiveQty")}
                                data-testid={`transfer-receive-edit-menu-${line.lineId}`}
                                onSelectField={() => startFieldEdit(line)}
                                onEditAll={() => startFieldEdit(line)}
                              />
                            )
                          ) : null}
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
            {lines.map((line) => {
              const transferLine = transfer.lines.find((l) => l.lineId === line.lineId);
              const outstanding = transferLine ? lineOutstandingQty(transferLine) : line.outstandingQty;
              const editing = mobileEditingLineId === line.lineId;
              const editReceiveNow = editing && mobileEditingFields.has("receiveNow");
              const canEditLine = outstanding > 0;

              return (
                <ExitsTableMobileRow
                  key={line.lineId}
                  editing={editing}
                  data-testid={`transfer-receive-card-${line.lineId}`}
                >
                  <div className="exits-table-mobile__title-row">
                    <span className="exits-table-mobile__title">{line.name}</span>
                    {canEditLine ? (
                      <ExitsTableActions>
                        {editing ? (
                          <>
                            <ReceiveIconAction
                              label={t("transfer.saveReceiveQty")}
                              variant="success"
                              data-testid={`transfer-receive-edit-save-mobile-${line.lineId}`}
                              onClick={() => finishEdit(line, true)}
                            >
                              <Check className="size-4" aria-hidden />
                            </ReceiveIconAction>
                            <ReceiveIconAction
                              label={t("transfer.resetReceiveQty")}
                              className="exits-table__action-reset"
                              data-testid={`transfer-receive-edit-reset-mobile-${line.lineId}`}
                              onClick={() => resetEdit(line, true)}
                            >
                              <RotateCcw className="size-4" aria-hidden />
                            </ReceiveIconAction>
                          </>
                        ) : (
                          <ExitsTableEditMenu
                            fields={receiveEditableFields}
                            ariaLabel={t("transfer.editReceiveQty")}
                            data-testid={`transfer-receive-edit-menu-mobile-${line.lineId}`}
                            onSelectField={() => startFieldEdit(line, true)}
                            onEditAll={() => startFieldEdit(line, true)}
                          />
                        )}
                      </ExitsTableActions>
                    ) : null}
                  </div>
                  <p className="exits-table-mobile__math mt-1 mb-0">
                    {t("transfer.sent")}: {formatTransferQty(line.sentQty)}
                    {" · "}
                    {t("transfer.previouslyReceived")}: {formatTransferQty(line.receivedQty)}
                    {" · "}
                    {t("transfer.outstanding")}: {formatTransferQty(outstanding)}
                  </p>
                  {editReceiveNow ? (
                    <div className="mt-2">
                      <QuantityInput
                        label={t("transfer.goodReceivedNow")}
                        value={line.goodText}
                        onChange={(e) => {
                          updateLine(line.productId, { goodText: e.target.value });
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
                      {t("transfer.goodReceivedNow")}: {line.goodText || "0"}
                    </p>
                  )}
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>
        </ExitsTableContainer>
      ) : (
        <div className="grid gap-3" data-testid="transfer-receive-review-summary">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("transfer.reviewBeforeConfirm")}
          </h2>
          <ExitsTableContainer data-testid="transfer-receive-review-lines-table">
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="text" colSize="flex">
                    {t("transfer.product")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="center" colSize="numeric">
                    {t("transfer.goodReceivedNow")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="text">{t("purchasing.colUnit")}</ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                {lines
                  .filter((line) => {
                    const good = parseNonNegativeQty(line.goodText) ?? 0;
                    const damaged = parseNonNegativeQty(line.damagedText) ?? 0;
                    const notDelivered = parseNonNegativeQty(line.notDeliveredText) ?? 0;
                    const other = parseNonNegativeQty(line.otherText) ?? 0;
                    return good + damaged + notDelivered + other > 1e-9;
                  })
                  .map((line) => {
                    const good = parseNonNegativeQty(line.goodText) ?? 0;
                    const damaged = parseNonNegativeQty(line.damagedText) ?? 0;
                    const notDelivered = parseNonNegativeQty(line.notDeliveredText) ?? 0;
                    const other = parseNonNegativeQty(line.otherText) ?? 0;
                    const discrepancy = receiveDiscrepancyQty(line.outstandingQty, good);
                    const discrepancySummary = formatReceiveDiscrepancySummary(
                      line,
                      discrepancyLabels,
                    );
                    return (
                      <ExitsTableRow
                        key={line.lineId}
                        data-testid={`transfer-receive-review-${line.lineId}`}
                      >
                        <ExitsTableCell cellAlign="text" className="font-medium">
                          <div>{line.name}</div>
                          {discrepancy > 1e-9 ? (
                            <div className="mt-1 text-[length:var(--exits-text-xs)] font-normal text-muted">
                              {discrepancySummary
                                ? `${t("transfer.discrepancy")}: ${discrepancySummary}`
                                : null}
                              {` · ${
                                line.remainingAction === "cancel_remaining"
                                  ? t("purchasing.cancelRemaining")
                                  : line.remainingAction === "replace_later"
                                    ? t("purchasing.replaceLater")
                                    : t("purchasing.remainingDecisionCol")
                              }`}
                              {line.remarksText.trim()
                                ? ` · ${line.remarksText.trim()}`
                                : ""}
                              {damaged > 0
                                ? ` · ${t("transfer.damaged")}: ${formatStockQtyLabel(damaged, line.uom)}`
                                : ""}
                              {notDelivered > 0
                                ? ` · ${t("transfer.notDelivered")}: ${formatStockQtyLabel(notDelivered, line.uom)}`
                                : ""}
                              {other > 0
                                ? ` · ${t("purchasing.otherDiscrepancy")}: ${formatStockQtyLabel(other, line.uom)}`
                                : ""}
                            </div>
                          ) : null}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="center" className="tabular-nums">
                          {line.goodText || "0"}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text">{line.uom}</ExitsTableCell>
                      </ExitsTableRow>
                    );
                  })}
              </ExitsTableBody>
            </ExitsTable>
          </ExitsTableContainer>
        </div>
      )}

      {reviewing && remainingDecisionRows.length > 0 ? (
        <ReceiveRemainingQuantityTable
          title={t("purchasing.remainingDecisionTitle")}
          summaryText={remainingDecisionSummary}
          applyToAllLabel={t("purchasing.remainingApplyToAll")}
          productColLabel={t("transfer.product")}
          remainingColLabel={t("purchasing.remainingCol")}
          issueColLabel={t("purchasing.remainingIssueCol")}
          decisionColLabel={t("purchasing.remainingDecisionCol")}
          replaceLaterLabel={t("purchasing.replaceLater")}
          cancelRemainingLabel={t("purchasing.cancelRemaining")}
          rows={remainingDecisionRows}
          highlightUnresolved={highlightUnresolvedRemaining}
          onDecisionChange={(productId, action: RemainingDecisionAction) => {
            setHighlightUnresolvedRemaining(false);
            setFlowError(null);
            updateLine(productId, { remainingAction: action });
          }}
          onApplyToAll={(action) => {
            setHighlightUnresolvedRemaining(false);
            setFlowError(null);
            const remainingIds = new Set(remainingDecisionRows.map((row) => row.productId));
            setLines((prev) =>
              prev.map((line) =>
                remainingIds.has(line.productId) ? { ...line, remainingAction: action } : line,
              ),
            );
          }}
          testId="transfer-receive-remaining-decisions"
        />
      ) : null}

      <div className="receive-stock-actions">
        <div className="receive-stock-actions__primary">
          <Button
            type="button"
            intent="primary"
            appearance="ghost"
            className="font-semibold"
            disabled={busy}
            onClick={() => {
              if (reviewing) {
                setReviewing(false);
                setFlowError(null);
                return;
              }
              onBack();
            }}
            data-testid="transfer-receive-back"
          >
            {reviewing ? (
              <>
                <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
                {t("transfer.backToEditReceive")}
              </>
            ) : (
              t("transfer.backToTransfer")
            )}
          </Button>
          {!reviewing ? (
            <Button
              type="button"
              disabled={busy || !canReview}
              onClick={onReview}
              data-testid="transfer-receive-review"
            >
              {t("transfer.reviewBeforeConfirm")}
            </Button>
          ) : (
            <Button
              type="button"
              disabled={busy}
              onClick={onConfirmReceive}
              data-testid="transfer-receive-confirm"
            >
              {busy ? t("transfer.receiving") : t("transfer.receive")}
            </Button>
          )}
        </div>
      </div>

      <ReceiveDiscrepancyDialog
        open={discrepancyOpen}
        lines={discrepancyLines}
        onChangeLine={(productId, patch) => {
          updateLine(productId, {
            ...(patch.damagedText !== undefined ? { damagedText: patch.damagedText } : {}),
            ...(patch.notDeliveredText !== undefined ? { notDeliveredText: patch.notDeliveredText } : {}),
            ...(patch.otherText !== undefined ? { otherText: patch.otherText } : {}),
            ...(patch.otherReasonCode !== undefined ? { otherReasonCode: patch.otherReasonCode } : {}),
            ...(patch.otherReasonText !== undefined ? { otherReasonText: patch.otherReasonText } : {}),
            ...(patch.otherExpanded !== undefined ? { otherExpanded: patch.otherExpanded } : {}),
            ...(patch.remarksText !== undefined ? { remarksText: patch.remarksText } : {}),
          });
        }}
        onCancel={closeDiscrepancyDialog}
        onConfirm={onDiscrepancyConfirmed}
        title={t("transfer.classifyDiscrepancyTitle")}
        classifyHint={t("transfer.classifyDiscrepancySplitHint")}
        classifyAsLabel={t("purchasing.classifyAs")}
        allDamagedLabel={t("transfer.damaged")}
        allNotDeliveredLabel={t("transfer.notDelivered")}
        allOtherLabel={t("transfer.allOther")}
        damagedLabel={t("transfer.damaged")}
        notDeliveredLabel={t("transfer.notDelivered")}
        otherLabel={t("purchasing.otherDiscrepancy")}
        otherReasonLabel={t("purchasing.otherReason")}
        otherReasons={otherReasonOptions}
        otherDescriptionLabel={t("purchasing.otherReasonDescription")}
        remarksLabel={t("transfer.remarks")}
        remarksRequiredLabel={t("purchasing.required")}
        remainingToClassifyLabel={t("purchasing.remainingToClassify")}
        decreaseQtyLabel={t("purchasing.decreaseQty")}
        increaseQtyLabel={t("purchasing.increaseQty")}
        cancelLabel={t("transfer.dialogCancel")}
        confirmLabel={t("transfer.classifyDiscrepancyConfirm")}
        notAcceptedTemplate={t("purchasing.notAcceptedQty")}
      />
    </div>
  );
}

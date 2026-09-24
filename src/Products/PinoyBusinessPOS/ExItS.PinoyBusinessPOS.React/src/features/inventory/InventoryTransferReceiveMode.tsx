import { useEffect, useMemo, useState, Fragment, type ReactNode } from "react";
import { ArrowLeft, Check, RotateCcw } from "lucide-react";
import type {
  InventoryTransferDto,
  ReceiveInventoryTransferRequest,
} from "@/api/pos/pos-inventory-transfer-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
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
  lineOutstandingQty,
  type TransferReceiveLineEdit,
} from "@/features/inventory/inventory-transfer-receive-helpers";
import { TransferReceiveFollowUpTable } from "@/features/inventory/TransferReceiveFollowUpTable";
import {
  buildTransferFollowUpRows,
  countUnresolvedTransferFollowUp,
  defaultTransferFollowUps,
  sumTransferFollowUpUnits,
  type TransferFollowUpIssueKind,
} from "@/features/inventory/transfer-receive-follow-up";
import { buildTransferReceivePayload } from "@/features/inventory/transfer-receive-plan";
import { TransferReceiveActualProductPicker } from "@/features/inventory/TransferReceiveActualProductPicker";
import {
  requiresActualProduct,
  resolveDefaultOtherCustody,
} from "@/features/inventory/transfer-exception-custody-policy";
import { ReceiveDiscrepancyDialog } from "@/features/purchasing/ReceiveDiscrepancyDialog";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import {
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
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
  workspace: PosWorkspaceScope;
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
  /** When true, parent page already owns PageHeader / status / timeline actions. */
  embedded?: boolean;
};

export function InventoryTransferReceiveMode({
  workspace,
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
  embedded = false,
}: InventoryTransferReceiveModeProps) {
  const { t } = useI18n();
  const linkedStockRequest = Boolean(transfer.stockRequestId);
  const followUpDefaults = useMemo(
    () => defaultTransferFollowUps(linkedStockRequest),
    [linkedStockRequest],
  );
  const [lines, setLines] = useState<TransferReceiveLineEdit[]>(() =>
    buildTransferReceiveLineEdits(transfer, followUpDefaults),
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
  const [exceptionConfirmOpen, setExceptionConfirmOpen] = useState(false);
  const [pendingReceiveBody, setPendingReceiveBody] = useState<ReceiveInventoryTransferRequest | null>(
    null,
  );

  useEffect(() => {
    setLines(buildTransferReceiveLineEdits(transfer, followUpDefaults));
    setReviewing(false);
    setFlowError(null);
    setDiscrepancyOpen(false);
    setDiscrepancyTargetProductId(null);
    setHighlightUnclassified(false);
    setHighlightUnresolvedRemaining(false);
    cancelRowEdit();
    cancelMobileEdit();
  }, [transfer.transferId, transfer.status, transfer.updatedAtUtc, transfer.totalReceivedQty, followUpDefaults]);

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

  const followUpRows = useMemo(() => {
    if (!reviewing) {
      return [];
    }
    return buildTransferFollowUpRows(lines, discrepancyLabels);
  }, [lines, reviewing, discrepancyLabels]);

  const followUpSummary = useMemo(() => {
    const products = followUpRows.length;
    if (products === 0) {
      return "";
    }
    const units = formatStockQtyLabel(sumTransferFollowUpUnits(followUpRows));
    if (products === 1) {
      return t("purchasing.remainingDecisionSummaryOne").replace("{units}", units);
    }
    return t("purchasing.remainingDecisionSummary")
      .replace("{products}", String(products))
      .replace("{units}", units);
  }, [followUpRows, t]);

  function followUpLabel(
    line: TransferReceiveLineEdit,
    kind: TransferFollowUpIssueKind,
  ): string {
    if (kind === "missing") {
      if (line.missingFollowUp === "wait_original") {
        return t("transfer.followUp.waitOriginal");
      }
      if (line.missingFollowUp === "request_replacement") {
        return t("transfer.followUp.requestReplacement");
      }
      if (line.missingFollowUp === "accept_shortage") {
        return t("transfer.followUp.acceptShortage");
      }
    } else if (kind === "damaged") {
      if (line.damagedFollowUp === "request_replacement") {
        return t("transfer.followUp.requestReplacement");
      }
      if (line.damagedFollowUp === "accept_shortage") {
        return t("transfer.followUp.acceptShortage");
      }
    } else {
      if (line.otherFollowUp === "request_replacement") {
        return t("transfer.followUp.requestReplacement");
      }
      if (line.otherFollowUp === "accept_shortage") {
        return t("transfer.followUp.acceptShortage");
      }
    }
    return t("purchasing.remainingDecisionCol");
  }

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
        actualReceivedProductId: line.actualReceivedProductId,
        actualReceivedProductName: line.actualReceivedProductName,
        remarksText: line.remarksText,
      }));
  }, [lines, discrepancyTargetProductId]);

  const exceptionConfirmLines = useMemo(
    () =>
      lines.filter((line) => {
        const other = parseNonNegativeQty(line.otherText ?? "0") ?? 0;
        return other > 1e-9 && requiresActualProduct(line.otherReasonCode.trim());
      }),
    [lines],
  );

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
        actualReceivedProductId: null,
        actualReceivedProductName: null,
        remarksText: "",
        missingFollowUp: null,
        damagedFollowUp: null,
        otherFollowUp: null,
        damagedCustodyDecision: "KeepAtDestination",
        otherCustodyDecision: null,
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
    const missing = parseNonNegativeQty(target.notDeliveredText) ?? 0;
    const damaged = parseNonNegativeQty(target.damagedText) ?? 0;
    const other = parseNonNegativeQty(target.otherText ?? "0") ?? 0;
    const otherCode = target.otherReasonCode.trim();
    updateLine(target.productId, {
      missingFollowUp:
        missing > 1e-9 ? target.missingFollowUp ?? followUpDefaults.missingFollowUp : null,
      damagedFollowUp:
        damaged > 1e-9 ? target.damagedFollowUp ?? followUpDefaults.damagedFollowUp : null,
      otherFollowUp: other > 1e-9 ? target.otherFollowUp ?? followUpDefaults.otherFollowUp : null,
      otherCustodyDecision:
        other > 1e-9 && otherCode ? resolveDefaultOtherCustody(otherCode) : null,
    });
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
      } else if (result.error === "other_actual_product_required") {
        setFlowError(t("transfer.exceptionActualProductRequired"));
      } else if (result.error === "other_custody_required") {
        setFlowError(t("transfer.exceptionCustodyRequired"));
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
    const remainingRows = buildTransferFollowUpRows(lines, discrepancyLabels);
    if (countUnresolvedTransferFollowUp(remainingRows) > 0) {
      setHighlightUnresolvedRemaining(true);
      setFlowError(t("purchasing.remainingDecisionRequired"));
      return;
    }
    const body = tryBuildPayload();
    if (!body) {
      return;
    }
    if (exceptionConfirmLines.length > 0) {
      setPendingReceiveBody(body);
      setExceptionConfirmOpen(true);
      return;
    }
    onSubmitReceive(body);
  }

  function submitPendingReceive() {
    if (!pendingReceiveBody) {
      return;
    }
    onSubmitReceive(pendingReceiveBody);
    setExceptionConfirmOpen(false);
    setPendingReceiveBody(null);
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
      className={
        embedded
          ? "inventory-transfer-receive-body flex min-w-0 flex-col gap-3"
          : "inventory-transfer-receive-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      }
      data-testid={embedded ? "inventory-transfer-receive-body" : "inventory-transfer-receive-page"}
    >
      {embedded ? null : (
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
          onBack={onBack}
        />
      )}

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
            <dt className="text-[length:var(--exits-text-xs)] text-muted">{t("transfer.colLines")}</dt>
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
        <ExitsTableContainer className="transfer-receive-table" data-testid="transfer-receive-table">
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
          <ExitsTableContainer
            className="transfer-receive-table transfer-receive-table--review"
            data-testid="transfer-receive-review-lines-table"
          >
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="text" colSize="flex">
                    {t("transfer.product")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="center" colSize="numeric">
                    {t("transfer.goodReceivedNow")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="text" colSize="numeric">
                    {t("purchasing.colUnit")}
                  </ExitsTableHead>
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
                              {line.remarksText.trim()
                                ? ` · ${line.remarksText.trim()}`
                                : ""}
                              {damaged > 0
                                ? ` · ${t("transfer.damaged")}: ${formatStockQtyLabel(damaged, line.uom)} (${followUpLabel(line, "damaged")})`
                                : ""}
                              {notDelivered > 0
                                ? ` · ${t("transfer.notDelivered")}: ${formatStockQtyLabel(notDelivered, line.uom)} (${followUpLabel(line, "missing")})`
                                : ""}
                              {other > 0
                                ? ` · ${t("purchasing.otherDiscrepancy")}: ${formatStockQtyLabel(other, line.uom)} (${followUpLabel(line, "other")})${
                                    line.actualReceivedProductName?.trim()
                                      ? ` · ${t("transfer.actualItem")}: ${line.actualReceivedProductName.trim()}`
                                      : ""
                                  }`
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

      {reviewing && followUpRows.length > 0 ? (
        <TransferReceiveFollowUpTable
          title={t("transfer.followUp.title")}
          summaryText={followUpSummary}
          productColLabel={t("transfer.product")}
          qtyColLabel={t("purchasing.remainingCol")}
          issueColLabel={t("purchasing.remainingIssueCol")}
          decisionColLabel={t("transfer.followUp.decisionCol")}
          waitOriginalLabel={t("transfer.followUp.waitOriginal")}
          requestReplacementLabel={t("transfer.followUp.requestReplacement")}
          acceptShortageLabel={t("transfer.followUp.acceptShortage")}
          allowCustodyDecision={
            !transfer.damageHandlingPolicy ||
            transfer.damageHandlingPolicy === "ReceiverMayDecide"
          }
          custodyDecisionByProductId={
            new Map(lines.map((line) => [line.productId, line.damagedCustodyDecision]))
          }
          otherCustodyDecisionColLabel={t("transfer.exceptionCustodyLabel")}
          otherCustodyDecisionByProductId={
            new Map(
              lines.map((line) => [
                line.productId,
                line.otherCustodyDecision ??
                  (line.otherReasonCode.trim()
                    ? resolveDefaultOtherCustody(line.otherReasonCode.trim())
                    : "KeepAtDestination"),
              ]),
            )
          }
          linkedStockRequest={linkedStockRequest}
          rows={followUpRows}
          highlightUnresolved={highlightUnresolvedRemaining}
          onDecisionChange={(rowKey, action) => {
            setHighlightUnresolvedRemaining(false);
            setFlowError(null);
            const row = followUpRows.find((entry) => entry.rowKey === rowKey);
            if (!row) {
              return;
            }
            if (row.issueKind === "missing") {
              updateLine(row.productId, { missingFollowUp: action as typeof followUpDefaults.missingFollowUp });
            } else if (row.issueKind === "damaged") {
              updateLine(row.productId, { damagedFollowUp: action as typeof followUpDefaults.damagedFollowUp });
            } else {
              updateLine(row.productId, { otherFollowUp: action as typeof followUpDefaults.otherFollowUp });
            }
          }}
          onCustodyDecisionChange={(productId, decision) => {
            updateLine(productId, { damagedCustodyDecision: decision });
          }}
          onOtherCustodyDecisionChange={(productId, decision) => {
            updateLine(productId, { otherCustodyDecision: decision });
          }}
          testId="transfer-receive-follow-up"
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
            ...(patch.actualReceivedProductId !== undefined
              ? { actualReceivedProductId: patch.actualReceivedProductId }
              : {}),
            ...(patch.actualReceivedProductName !== undefined
              ? { actualReceivedProductName: patch.actualReceivedProductName }
              : {}),
            ...(patch.remarksText !== undefined ? { remarksText: patch.remarksText } : {}),
          });
        }}
        actualProductLabel={t("transfer.actualItem")}
        actualProductRequiredHint={t("transfer.exceptionActualProductHint")}
        forceReturnHint={t("transfer.exceptionForceReturnHint")}
        renderActualProductPicker={(draftLine) => (
          <TransferReceiveActualProductPicker
            workspace={workspace}
            excludeProductId={draftLine.productId}
            onSelect={(product) => {
              if (
                product.productId.toLowerCase() === draftLine.productId.toLowerCase()
              ) {
                return;
              }
              updateLine(draftLine.productId, {
                actualReceivedProductId: product.productId,
                actualReceivedProductName: product.name,
              });
            }}
          />
        )}
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

      <ConfirmActionDialog
        open={exceptionConfirmOpen}
        variant="warning"
        title={t("transfer.exceptionConfirmTitle")}
        description={t("transfer.exceptionConfirmIntro")}
        confirmLabel={t("transfer.receive")}
        cancelLabel={t("transfer.dialogCancel")}
        onCancel={() => {
          setExceptionConfirmOpen(false);
          setPendingReceiveBody(null);
        }}
        onConfirm={submitPendingReceive}
        testId="transfer-exception-receive-confirm"
      >
        <div className="flex flex-col gap-2 text-[length:var(--exits-text-sm)]">
          <ul className="m-0 list-disc ps-5">
            {exceptionConfirmLines.map((line) => {
              const other = parseNonNegativeQty(line.otherText ?? "0") ?? 0;
              const custody = resolveDefaultOtherCustody(line.otherReasonCode.trim());
              return (
                <li key={line.productId} data-testid={`transfer-exception-confirm-${line.productId}`}>
                  {t("transfer.exceptionConfirmLine")
                    .replace("{expected}", line.name)
                    .replace("{actual}", line.actualReceivedProductName ?? "—")
                    .replace("{qty}", formatStockQtyLabel(other, line.uom))
                    .replace(
                      "{custody}",
                      custody === "ReturnToSource"
                        ? t("transfer.custody.returnToSource")
                        : t("transfer.custody.keepAtDestination"),
                    )
                    .replace("{followUp}", followUpLabel(line, "other"))}
                </li>
              );
            })}
          </ul>
          <p className="m-0 text-muted">{t("transfer.exceptionConfirmStockHint")}</p>
        </div>
      </ConfirmActionDialog>
    </div>
  );
}

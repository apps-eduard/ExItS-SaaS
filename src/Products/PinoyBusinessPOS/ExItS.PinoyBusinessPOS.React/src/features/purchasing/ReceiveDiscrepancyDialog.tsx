import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import {
  classificationRemaining,
  isClassificationComplete,
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import {
  forcesReturnToSource,
  isActualProductSameAsExpected,
  requiresActualProduct,
} from "@/features/inventory/transfer-exception-custody-policy";

export type DiscrepancyLineDraft = {
  productId: string;
  name: string;
  uom: string;
  outstandingQty: number;
  goodQty: number;
  damagedText: string;
  notDeliveredText: string;
  otherText: string;
  otherReasonCode: string;
  otherReasonText: string;
  otherExpanded?: boolean;
  actualReceivedProductId?: string | null;
  actualReceivedProductName?: string | null;
  remarksText: string;
};

export type ReceiveDiscrepancyOtherReasonOption = {
  value: string;
  label: string;
};

export type ClassifyQuickFill = "damaged" | "notDelivered" | "other" | "__unset__";

export type ReceiveDiscrepancyDialogProps = {
  open: boolean;
  lines: DiscrepancyLineDraft[];
  onChangeLine: (productId: string, patch: Partial<DiscrepancyLineDraft>) => void;
  onCancel: () => void;
  onConfirm: () => void;
  title: string;
  classifyHint: string;
  /** Segmented switch label (e.g. "Classify as"). */
  classifyAsLabel?: string;
  allDamagedLabel: string;
  allNotDeliveredLabel: string;
  allOtherLabel?: string;
  damagedLabel: string;
  notDeliveredLabel: string;
  otherLabel?: string;
  otherReasonLabel?: string;
  otherReasons?: ReceiveDiscrepancyOtherReasonOption[];
  otherDescriptionLabel?: string;
  remarksLabel: string;
  /** Accessible label for the required marker (e.g. "Required"). */
  remarksRequiredLabel: string;
  remainingToClassifyLabel: string;
  decreaseQtyLabel?: string;
  increaseQtyLabel?: string;
  cancelLabel: string;
  confirmLabel: string;
  notAcceptedTemplate: string;
  actualProductLabel?: string;
  actualProductRequiredHint?: string;
  forceReturnHint?: string;
  renderActualProductPicker?: (line: DiscrepancyLineDraft) => React.ReactNode;
};

function formatNotAccepted(template: string, qty: number, uom: string): string {
  return template
    .replace("{qty}", formatStockQtyLabel(qty, uom))
    .replace("{uom}", uom);
}

function otherReasonComplete(line: DiscrepancyLineDraft, other: number): boolean {
  if (other <= 1e-9) {
    return true;
  }
  const code = line.otherReasonCode.trim();
  if (!code) {
    return false;
  }
  if (code === "Other" && !line.otherReasonText.trim()) {
    return false;
  }
  if (requiresActualProduct(code) && !line.actualReceivedProductId?.trim()) {
    return false;
  }
  if (isActualProductSameAsExpected(code, line.productId, line.actualReceivedProductId)) {
    return false;
  }
  return true;
}

function qtyValue(text: string): number {
  return parseNonNegativeQty(text) ?? 0;
}

/** Max for one classification bucket so Damaged + Not delivered + Other stay ≤ discrepancy. */
function maxBucketQty(discrepancyQty: number, othersSum: number): number {
  return Math.max(0, discrepancyQty - Math.max(0, othersSum));
}

/**
 * Classification drawer (left) shown only for lines where Good received &lt; Outstanding.
 */
export function ReceiveDiscrepancyDialog({
  open,
  lines,
  onChangeLine,
  onCancel,
  onConfirm,
  title,
  classifyHint,
  classifyAsLabel = "Classify as",
  allDamagedLabel,
  allNotDeliveredLabel,
  allOtherLabel = "Other",
  damagedLabel,
  notDeliveredLabel,
  otherLabel = "Other",
  otherReasonLabel = "Reason",
  otherReasons = [],
  otherDescriptionLabel = "Description",
  remarksLabel,
  remarksRequiredLabel,
  remainingToClassifyLabel,
  decreaseQtyLabel = "Decrease quantity",
  increaseQtyLabel = "Increase quantity",
  cancelLabel,
  confirmLabel,
  notAcceptedTemplate,
  actualProductLabel = "Actual item received",
  actualProductRequiredHint = "Select the product that was actually received.",
  forceReturnHint = "This reason requires return to source.",
  renderActualProductPicker,
}: ReceiveDiscrepancyDialogProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const safeIndex = Math.min(activeIndex, Math.max(0, lines.length - 1));
  const line = lines[safeIndex] ?? null;

  const discrepancy = line
    ? receiveDiscrepancyQty(line.outstandingQty, line.goodQty)
    : 0;
  const damaged = line ? parseNonNegativeQty(line.damagedText) : 0;
  const notDelivered = line ? parseNonNegativeQty(line.notDeliveredText) : 0;
  const other = line ? parseNonNegativeQty(line.otherText) : 0;
  const remaining =
    line && damaged !== null && notDelivered !== null && other !== null
      ? classificationRemaining(discrepancy, damaged, notDelivered, other)
      : discrepancy;
  const allDamagedSelected =
    damaged !== null &&
    notDelivered !== null &&
    other !== null &&
    discrepancy > 1e-9 &&
    Math.abs(damaged - discrepancy) <= 1e-9 &&
    Math.abs(notDelivered) <= 1e-9 &&
    Math.abs(other) <= 1e-9;
  const allNotDeliveredSelected =
    damaged !== null &&
    notDelivered !== null &&
    other !== null &&
    discrepancy > 1e-9 &&
    Math.abs(notDelivered - discrepancy) <= 1e-9 &&
    Math.abs(damaged) <= 1e-9 &&
    Math.abs(other) <= 1e-9;
  const allOtherSelected =
    damaged !== null &&
    notDelivered !== null &&
    other !== null &&
    discrepancy > 1e-9 &&
    Math.abs(other - discrepancy) <= 1e-9 &&
    Math.abs(damaged) <= 1e-9 &&
    Math.abs(notDelivered) <= 1e-9;

  const quickFillValue: ClassifyQuickFill = allDamagedSelected
    ? "damaged"
    : allNotDeliveredSelected
      ? "notDelivered"
      : allOtherSelected
        ? "other"
        : "__unset__";

  const otherChosen =
    line != null &&
    (allOtherSelected ||
      Boolean(line.otherExpanded) ||
      (other !== null && other > 1e-9) ||
      Boolean(line.otherReasonCode.trim()));

  const allComplete = useMemo(
    () =>
      lines.every((entry) => {
        const d = parseNonNegativeQty(entry.damagedText);
        const n = parseNonNegativeQty(entry.notDeliveredText);
        const o = parseNonNegativeQty(entry.otherText);
        if (d === null || n === null || o === null) {
          return false;
        }
        if (!entry.remarksText.trim()) {
          return false;
        }
        if (!otherReasonComplete(entry, o)) {
          return false;
        }
        return isClassificationComplete(
          receiveDiscrepancyQty(entry.outstandingQty, entry.goodQty),
          d,
          n,
          o,
        );
      }),
    [lines],
  );

  function applyAllDamaged() {
    if (!line) {
      return;
    }
    onChangeLine(line.productId, {
      damagedText: String(discrepancy),
      notDeliveredText: "0",
      otherText: "0",
      otherReasonCode: "",
      otherReasonText: "",
      otherExpanded: false,
    });
  }

  function applyAllNotDelivered() {
    if (!line) {
      return;
    }
    onChangeLine(line.productId, {
      damagedText: "0",
      notDeliveredText: String(discrepancy),
      otherText: "0",
      otherReasonCode: "",
      otherReasonText: "",
      otherExpanded: false,
    });
  }

  function applyAllOther() {
    if (!line) {
      return;
    }
    onChangeLine(line.productId, {
      damagedText: "0",
      notDeliveredText: "0",
      otherText: String(discrepancy),
      otherExpanded: true,
    });
  }

  function onQuickFillChange(next: ClassifyQuickFill) {
    if (next === "damaged") {
      applyAllDamaged();
      return;
    }
    if (next === "notDelivered") {
      applyAllNotDelivered();
      return;
    }
    if (next === "other") {
      applyAllOther();
    }
  }

  if (!line) {
    return null;
  }

  return (
    <SideDrawer
      open={open}
      onClose={onCancel}
      title={title}
      description={classifyHint}
      side="left"
      testId="receive-discrepancy-dialog"
      panelClassName="exits-form-drawer__panel exits-form-drawer__panel--md"
      closeLabel={cancelLabel}
    >
      <div className="exits-form-drawer" data-testid="receive-discrepancy-form">
        <div className="exits-form-drawer__body">
          {lines.length > 1 ? (
            <div className="mb-2 flex flex-wrap gap-2" data-testid="receive-discrepancy-line-tabs">
              {lines.map((entry, index) => (
                <Button
                  key={entry.productId}
                  type="button"
                  size="sm"
                  variant={index === safeIndex ? "default" : "outline"}
                  data-testid={`receive-discrepancy-tab-${entry.productId}`}
                  onClick={() => setActiveIndex(index)}
                >
                  {entry.name}
                </Button>
              ))}
            </div>
          ) : null}

          <div
            className="flex flex-col gap-3"
            data-testid={`receive-discrepancy-line-${line.productId}`}
          >
            <div
              className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5 shadow-[var(--exits-shadow-sm)]"
              data-testid="receive-discrepancy-product-card"
            >
              <p className="m-0 font-semibold">{line.name}</p>
              <p
                className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="receive-discrepancy-not-accepted"
              >
                {formatNotAccepted(notAcceptedTemplate, discrepancy, line.uom)}
              </p>
              {otherChosen && line.actualReceivedProductName?.trim() ? (
                <p
                  className="m-0 mt-1.5 text-[length:var(--exits-text-sm)]"
                  data-testid={`receive-discrepancy-product-card-actual-${line.productId}`}
                >
                  <span className="text-muted">{actualProductLabel}: </span>
                  <span className="font-medium">{line.actualReceivedProductName}</span>
                </p>
              ) : null}
            </div>

            <div className="flex flex-col gap-1.5">
              <span className="exits-type-label font-bold">{classifyAsLabel}</span>
              <ExitsPillSelect<ClassifyQuickFill>
                appearance="tile"
                aria-label={classifyAsLabel}
                value={quickFillValue}
                onChange={onQuickFillChange}
                className="grid-cols-3"
                testId="receive-discrepancy-quick-fill"
                options={[
                  { value: "damaged", label: allDamagedLabel },
                  { value: "notDelivered", label: allNotDeliveredLabel },
                  { value: "other", label: allOtherLabel },
                ]}
              />
            </div>

            <div
              className="grid grid-cols-1 gap-3 sm:grid-cols-3"
              data-testid="receive-discrepancy-qty-row"
            >
              <label className="exits-type-label flex flex-col gap-1.5">
                <span>{damagedLabel}</span>
                <QuantityStepper
                  compact
                  variant="auto"
                  min={0}
                  max={
                    otherChosen
                      ? maxBucketQty(discrepancy, qtyValue(line.notDeliveredText) + qtyValue(line.otherText))
                      : discrepancy
                  }
                  value={qtyValue(line.damagedText)}
                  onChange={(next) =>
                    onChangeLine(line.productId, { damagedText: String(next) })
                  }
                  decreaseLabel={decreaseQtyLabel}
                  increaseLabel={increaseQtyLabel}
                  ariaLabel={damagedLabel}
                  valueTestId={`receive-discrepancy-damaged-${line.productId}`}
                  className="justify-start"
                />
              </label>
              <label className="exits-type-label flex flex-col gap-1.5">
                <span>{notDeliveredLabel}</span>
                <QuantityStepper
                  compact
                  variant="auto"
                  min={0}
                  max={
                    otherChosen
                      ? maxBucketQty(discrepancy, qtyValue(line.damagedText) + qtyValue(line.otherText))
                      : discrepancy
                  }
                  value={qtyValue(line.notDeliveredText)}
                  onChange={(next) =>
                    onChangeLine(line.productId, { notDeliveredText: String(next) })
                  }
                  decreaseLabel={decreaseQtyLabel}
                  increaseLabel={increaseQtyLabel}
                  ariaLabel={notDeliveredLabel}
                  valueTestId={`receive-discrepancy-not-delivered-${line.productId}`}
                  className="justify-start"
                />
              </label>
              <label className="exits-type-label flex flex-col gap-1.5">
                <span>{otherLabel}</span>
                <QuantityStepper
                  compact
                  variant="auto"
                  min={0}
                  max={
                    otherChosen
                      ? maxBucketQty(discrepancy, qtyValue(line.damagedText) + qtyValue(line.notDeliveredText))
                      : discrepancy
                  }
                  value={qtyValue(line.otherText)}
                  disabled={!otherChosen}
                  onChange={(next) =>
                    onChangeLine(line.productId, {
                      otherText: String(next),
                      otherExpanded: next > 0 || line.otherExpanded,
                    })
                  }
                  decreaseLabel={decreaseQtyLabel}
                  increaseLabel={increaseQtyLabel}
                  ariaLabel={otherLabel}
                  valueTestId={`receive-discrepancy-other-qty-${line.productId}`}
                  className="justify-start"
                />
              </label>
            </div>

            {otherChosen ? (
              <div
                className="flex flex-col gap-3"
                data-testid={`receive-discrepancy-other-panel-${line.productId}`}
              >
                <label className="exits-type-label flex flex-col gap-1.5">
                  <span>{otherReasonLabel}</span>
                  <select
                    className="exits-select"
                    value={line.otherReasonCode}
                onChange={(e) => {
                  const nextCode = e.target.value;
                  const keepActual = requiresActualProduct(nextCode);
                  const sameAsExpected =
                    keepActual &&
                    isActualProductSameAsExpected(
                      nextCode,
                      line.productId,
                      line.actualReceivedProductId,
                    );
                  onChangeLine(line.productId, {
                    otherReasonCode: nextCode,
                    otherReasonText: nextCode === "Other" ? line.otherReasonText : "",
                    actualReceivedProductId: keepActual && !sameAsExpected
                      ? line.actualReceivedProductId
                      : null,
                    actualReceivedProductName: keepActual && !sameAsExpected
                      ? line.actualReceivedProductName
                      : null,
                  });
                }}
                    data-testid={`receive-discrepancy-other-reason-${line.productId}`}
                  >
                    <option value="">—</option>
                    {otherReasons.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </label>
                {line.otherReasonCode === "Other" ? (
                  <label className="exits-type-label flex flex-col gap-1.5">
                    <span className="inline-flex items-center gap-1">
                      {otherDescriptionLabel}
                      <span
                        className="text-[length:var(--exits-text-xs)] font-bold text-[var(--exits-danger)]"
                        aria-hidden="true"
                      >
                        *
                      </span>
                    </span>
                    <input
                      className="exits-input"
                      type="text"
                      value={line.otherReasonText}
                      onChange={(e) =>
                        onChangeLine(line.productId, { otherReasonText: e.target.value })
                      }
                      data-testid={`receive-discrepancy-other-description-${line.productId}`}
                    />
                  </label>
                ) : null}
                {requiresActualProduct(line.otherReasonCode) && other !== null && other > 1e-9 ? (
                  <div
                    className="flex flex-col gap-1.5"
                    data-testid={`receive-discrepancy-actual-product-${line.productId}`}
                  >
                    <span className="exits-type-label inline-flex items-center gap-1">
                      {actualProductLabel}
                      <span
                        className="text-[length:var(--exits-text-xs)] font-bold text-[var(--exits-danger)]"
                        aria-hidden="true"
                      >
                        *
                      </span>
                    </span>
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {actualProductRequiredHint}
                    </p>
                    {line.actualReceivedProductName ? (
                      <p
                        className="m-0 text-[length:var(--exits-text-sm)] font-medium"
                        data-testid={`receive-discrepancy-actual-product-selected-${line.productId}`}
                      >
                        {line.actualReceivedProductName}
                      </p>
                    ) : null}
                    {renderActualProductPicker?.(line)}
                  </div>
                ) : null}
                {forcesReturnToSource(line.otherReasonCode) && other !== null && other > 1e-9 ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid={`receive-discrepancy-force-return-${line.productId}`}
                  >
                    {forceReturnHint}
                  </p>
                ) : null}
              </div>
            ) : null}

            <label className="exits-type-label flex flex-col gap-1.5">
              <span className="inline-flex items-center gap-1">
                {remarksLabel}
                <span
                  className="text-[length:var(--exits-text-xs)] font-bold text-[var(--exits-danger)]"
                  aria-hidden="true"
                >
                  *
                </span>
                <span className="sr-only">{remarksRequiredLabel}</span>
              </span>
              <textarea
                className="exits-input"
                rows={2}
                required
                aria-required="true"
                value={line.remarksText}
                onChange={(e) => onChangeLine(line.productId, { remarksText: e.target.value })}
                data-testid={`receive-discrepancy-remarks-${line.productId}`}
              />
            </label>

            <p
              className="m-0 text-[length:var(--exits-text-sm)] tabular-nums font-medium"
              data-testid="receive-discrepancy-remaining"
            >
              {remainingToClassifyLabel.replace(
                "{qty}",
                formatStockQtyLabel(remaining, line.uom),
              )}
            </p>
          </div>
        </div>

        <div className="exits-form-drawer__footer">
          <div className="exits-form-drawer__footer-actions">
            <Button
              type="button"
              intent="danger"
              appearance="solid"
              data-testid="receive-discrepancy-cancel"
              onClick={onCancel}
            >
              {cancelLabel}
            </Button>
            <Button
              type="button"
              disabled={!allComplete}
              data-testid="receive-discrepancy-confirm"
              onClick={onConfirm}
            >
              {confirmLabel}
            </Button>
          </div>
        </div>
      </div>
    </SideDrawer>
  );
}

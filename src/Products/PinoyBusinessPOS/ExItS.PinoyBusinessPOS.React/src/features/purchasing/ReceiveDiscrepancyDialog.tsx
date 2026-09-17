import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { QuantityInput } from "@/components/exits/MoneyQuantityInputs";
import {
  classificationRemaining,
  isClassificationComplete,
  parseNonNegativeQty,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import { cn } from "@/lib/cn";

export type DiscrepancyLineDraft = {
  productId: string;
  name: string;
  uom: string;
  outstandingQty: number;
  goodQty: number;
  damagedText: string;
  notDeliveredText: string;
  remarksText: string;
};

export type ReceiveDiscrepancyDialogProps = {
  open: boolean;
  lines: DiscrepancyLineDraft[];
  onChangeLine: (productId: string, patch: Partial<DiscrepancyLineDraft>) => void;
  onCancel: () => void;
  onConfirm: () => void;
  title: string;
  classifyHint: string;
  allDamagedLabel: string;
  allNotDeliveredLabel: string;
  damagedLabel: string;
  notDeliveredLabel: string;
  remarksLabel: string;
  /** Accessible label for the required marker (e.g. "Required"). */
  remarksRequiredLabel: string;
  remainingToClassifyLabel: string;
  cancelLabel: string;
  confirmLabel: string;
  notAcceptedTemplate: string;
};

function formatNotAccepted(template: string, qty: number, uom: string): string {
  return template
    .replace("{qty}", formatStockQtyLabel(qty, uom))
    .replace("{uom}", uom);
}

/**
 * Classification dialog shown only for lines where Good received &lt; Outstanding.
 */
export function ReceiveDiscrepancyDialog({
  open,
  lines,
  onChangeLine,
  onCancel,
  onConfirm,
  title,
  classifyHint,
  allDamagedLabel,
  allNotDeliveredLabel,
  damagedLabel,
  notDeliveredLabel,
  remarksLabel,
  remarksRequiredLabel,
  remainingToClassifyLabel,
  cancelLabel,
  confirmLabel,
  notAcceptedTemplate,
}: ReceiveDiscrepancyDialogProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const safeIndex = Math.min(activeIndex, Math.max(0, lines.length - 1));
  const line = lines[safeIndex] ?? null;

  const discrepancy = line
    ? receiveDiscrepancyQty(line.outstandingQty, line.goodQty)
    : 0;
  const damaged = line ? parseNonNegativeQty(line.damagedText) : 0;
  const notDelivered = line ? parseNonNegativeQty(line.notDeliveredText) : 0;
  const remaining =
    line && damaged !== null && notDelivered !== null
      ? classificationRemaining(discrepancy, damaged, notDelivered)
      : discrepancy;
  const allDamagedSelected =
    damaged !== null &&
    notDelivered !== null &&
    discrepancy > 1e-9 &&
    Math.abs(damaged - discrepancy) <= 1e-9 &&
    Math.abs(notDelivered) <= 1e-9;
  const allNotDeliveredSelected =
    damaged !== null &&
    notDelivered !== null &&
    discrepancy > 1e-9 &&
    Math.abs(notDelivered - discrepancy) <= 1e-9 &&
    Math.abs(damaged) <= 1e-9;

  const allComplete = useMemo(
    () =>
      lines.every((entry) => {
        const d = parseNonNegativeQty(entry.damagedText);
        const n = parseNonNegativeQty(entry.notDeliveredText);
        if (d === null || n === null) {
          return false;
        }
        if (!entry.remarksText.trim()) {
          return false;
        }
        return isClassificationComplete(
          receiveDiscrepancyQty(entry.outstandingQty, entry.goodQty),
          d,
          n,
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
    });
  }

  function applyAllNotDelivered() {
    if (!line) {
      return;
    }
    onChangeLine(line.productId, {
      damagedText: "0",
      notDeliveredText: String(discrepancy),
    });
  }

  if (!line) {
    return null;
  }

  return (
    <ExitsModal
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          onCancel();
        }
      }}
      title={title}
      description={classifyHint}
      size="md"
      testId="receive-discrepancy-dialog"
      footer={
        <>
          <Button
            type="button"
            variant="ghost"
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
        </>
      }
    >
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

      <div className="flex flex-col gap-3" data-testid={`receive-discrepancy-line-${line.productId}`}>
        <div className="flex flex-col gap-1">
          <p className="m-0 font-medium">{line.name}</p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="receive-discrepancy-not-accepted">
            {formatNotAccepted(notAcceptedTemplate, discrepancy, line.uom)}
          </p>
        </div>

        <div
          role="group"
          aria-label={`${allDamagedLabel} / ${allNotDeliveredLabel}`}
          className="grid grid-cols-2 gap-1 rounded-[var(--exits-control-radius)] border border-border bg-[var(--exits-surface-muted)] p-1"
          data-testid="receive-discrepancy-quick-fill"
        >
          <button
            type="button"
            data-testid={`receive-discrepancy-all-damaged-${line.productId}`}
            aria-pressed={allDamagedSelected}
            className={cn(
              "inline-flex min-h-9 items-center justify-center rounded-[var(--exits-control-radius)] px-2.5",
              "text-[length:var(--exits-text-sm)] font-medium transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              allDamagedSelected
                ? "bg-surface text-foreground shadow-sm"
                : "text-muted hover:bg-[color-mix(in_srgb,var(--exits-surface)_70%,transparent)] hover:text-foreground",
            )}
            onClick={applyAllDamaged}
          >
            {allDamagedLabel}
          </button>
          <button
            type="button"
            data-testid={`receive-discrepancy-all-not-delivered-${line.productId}`}
            aria-pressed={allNotDeliveredSelected}
            className={cn(
              "inline-flex min-h-9 items-center justify-center rounded-[var(--exits-control-radius)] px-2.5",
              "text-[length:var(--exits-text-sm)] font-medium transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              allNotDeliveredSelected
                ? "bg-surface text-foreground shadow-sm"
                : "text-muted hover:bg-[color-mix(in_srgb,var(--exits-surface)_70%,transparent)] hover:text-foreground",
            )}
            onClick={applyAllNotDelivered}
          >
            {allNotDeliveredLabel}
          </button>
        </div>

        <div
          className="grid grid-cols-1 gap-3 sm:grid-cols-2"
          data-testid="receive-discrepancy-qty-row"
        >
          <QuantityInput
            label={damagedLabel}
            value={line.damagedText}
            onChange={(e) => onChangeLine(line.productId, { damagedText: e.target.value })}
            data-testid={`receive-discrepancy-damaged-${line.productId}`}
          />
          <QuantityInput
            label={notDeliveredLabel}
            value={line.notDeliveredText}
            onChange={(e) => onChangeLine(line.productId, { notDeliveredText: e.target.value })}
            data-testid={`receive-discrepancy-not-delivered-${line.productId}`}
          />
        </div>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
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
    </ExitsModal>
  );
}

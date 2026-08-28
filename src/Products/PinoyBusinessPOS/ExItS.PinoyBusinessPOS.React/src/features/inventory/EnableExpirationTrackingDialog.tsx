import { useEffect, useMemo, useState } from "react";
import {
  enableExpirationTracking,
  type EnableExpirationTrackingResponse,
} from "@/api/pos/pos-inventory-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  canSubmitExpirationAllocation,
  createExpirationLotDraft,
  isExpiryDateInPast,
  remainingToAllocate,
  sumAllocatedQuantity,
  toExistingStockLotInputs,
  type ExpirationLotDraft,
  parseExpirationLotRows,
} from "@/features/inventory/enable-expiration-tracking-helpers";
import { formatLocalDateOnly } from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";

export type EnableExpirationTrackingDialogProps = {
  open: boolean;
  workspace: PosWorkspaceScope;
  productId: string;
  productName: string;
  onHandQuantity: number;
  unitOfMeasure: string;
  expirationWarningDays?: number | null;
  onClose: () => void;
  onSuccess: (result: EnableExpirationTrackingResponse) => void;
};

export function EnableExpirationTrackingDialog({
  open,
  workspace,
  productId,
  productName,
  onHandQuantity,
  unitOfMeasure,
  expirationWarningDays,
  onClose,
  onSuccess,
}: EnableExpirationTrackingDialogProps) {
  const { t } = useI18n();
  const [rows, setRows] = useState<ExpirationLotDraft[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const today = formatLocalDateOnly();

  useEffect(() => {
    if (!open) {
      return;
    }
    setError(null);
    setSubmitting(false);
    const defaultQty = onHandQuantity > 0 ? String(onHandQuantity) : "";
    setRows([createExpirationLotDraft(defaultQty)]);
  }, [open, onHandQuantity, productId]);

  const parsedRows = useMemo(() => parseExpirationLotRows(rows), [rows]);
  const allocated = sumAllocatedQuantity(
    parsedRows.filter((row) => Number.isFinite(row.quantity) && row.quantity > 0),
  );
  const remaining = remainingToAllocate(onHandQuantity, allocated);
  const canSubmit = canSubmitExpirationAllocation(onHandQuantity, rows) && !submitting;

  function updateRow(id: string, patch: Partial<ExpirationLotDraft>) {
    setRows((current) => current.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }

  function addRow() {
    setRows((current) => [...current, createExpirationLotDraft()]);
  }

  function removeRow(id: string) {
    setRows((current) => (current.length <= 1 ? current : current.filter((row) => row.id !== id)));
  }

  async function onSubmit() {
    if (!canSubmitExpirationAllocation(onHandQuantity, rows) || submitting) {
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const warningDays =
        expirationWarningDays != null &&
        !Number.isNaN(expirationWarningDays) &&
        expirationWarningDays > 0
          ? expirationWarningDays
          : null;
      const result = await enableExpirationTracking(workspace, productId, {
        existingStockLots: toExistingStockLotInputs(rows),
        expectedOnHandQuantity: onHandQuantity,
        expirationWarningDays: warningDays,
      });
      onSuccess(result);
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : err instanceof Error
            ? err.message
            : t("error.detail"),
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (!open) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onClose}
      data-testid="enable-expiration-tracking-backdrop"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="enable-expiration-tracking-title"
        data-testid="enable-expiration-tracking-dialog"
        className="flex w-full max-w-lg max-h-[90dvh] flex-col gap-3 overflow-y-auto rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <h2
          id="enable-expiration-tracking-title"
          className="m-0 text-[length:var(--exits-text-md)] font-semibold"
        >
          {t("inventory.enableExpirationTracking")}
        </h2>

        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("inventory.enableExpirationCopy")
            .replace("{qty}", String(onHandQuantity))
            .replace("{uom}", unitOfMeasure)}
        </p>

        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
          data-testid="enable-expiration-current-stock"
        >
          {t("inventory.enableExpirationCurrentStock")}: {onHandQuantity} {unitOfMeasure}
          {productName ? ` · ${productName}` : ""}
        </p>

        <div className="flex flex-col gap-3" data-testid="enable-expiration-lot-rows">
          {rows.map((row, index) => {
            const past = isExpiryDateInPast(row.expiryDate, today);
            return (
              <div
                key={row.id}
                className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
                data-testid={`enable-expiration-lot-row-${index}`}
              >
                <Input
                  label={`${t("inventory.enableExpirationQuantity")} *`}
                  name={`enableExpirationQty-${row.id}`}
                  inputMode="decimal"
                  value={row.quantity}
                  onChange={(e) => updateRow(row.id, { quantity: e.target.value })}
                  data-testid={`enable-expiration-qty-${index}`}
                />
                <Input
                  label={`${t("inventory.enableExpirationExpiry")} *`}
                  name={`enableExpirationExpiry-${row.id}`}
                  type="date"
                  value={row.expiryDate}
                  onChange={(e) => updateRow(row.id, { expiryDate: e.target.value })}
                  data-testid={`enable-expiration-expiry-${index}`}
                />
                {past ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid={`enable-expiration-past-warning-${index}`}
                  >
                    {t("inventory.enableExpirationPastWarning")}
                  </p>
                ) : null}
                <Input
                  label={t("inventory.enableExpirationLotOptional")}
                  name={`enableExpirationLot-${row.id}`}
                  value={row.lotNumber}
                  onChange={(e) => updateRow(row.id, { lotNumber: e.target.value })}
                  data-testid={`enable-expiration-lot-${index}`}
                />
                {rows.length > 1 ? (
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11 w-fit"
                    onClick={() => removeRow(row.id)}
                    data-testid={`enable-expiration-remove-${index}`}
                  >
                    {t("inventory.enableExpirationRemoveRow")}
                  </Button>
                ) : null}
              </div>
            );
          })}
        </div>

        <Button
          type="button"
          variant="ghost"
          className="min-h-11 w-fit"
          onClick={addRow}
          data-testid="enable-expiration-add-row"
        >
          {t("inventory.enableExpirationAddRow")}
        </Button>

        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
          data-testid="enable-expiration-allocated"
        >
          {t("inventory.enableExpirationAllocated")
            .replace("{allocated}", String(allocated))
            .replace("{onHand}", String(onHandQuantity))}
        </p>
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="enable-expiration-remaining"
        >
          {t("inventory.enableExpirationRemaining").replace("{remaining}", String(remaining))}
        </p>

        {error ? (
          <p
            className="m-0 rounded-[var(--exits-radius-md)] bg-muted px-3 py-2 text-[length:var(--exits-text-sm)]"
            data-testid="enable-expiration-error"
            role="alert"
          >
            {error}
          </p>
        ) : null}

        <div className="mt-1 flex flex-wrap justify-end gap-2">
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            onClick={onClose}
            disabled={submitting}
            data-testid="enable-expiration-cancel"
          >
            {t("inventory.enableExpirationCancel")}
          </Button>
          <Button
            type="button"
            className="min-h-11"
            disabled={!canSubmit}
            onClick={() => void onSubmit()}
            data-testid="enable-expiration-submit"
          >
            {submitting
              ? t("loading.label")
              : t("inventory.enableExpirationSubmit")}
          </Button>
        </div>
      </div>
    </div>
  );
}

import { useEffect, useMemo, useState } from "react";
import { Plus } from "lucide-react";
import {
  enableExpirationTracking,
  type EnableExpirationTrackingResponse,
} from "@/api/pos/pos-inventory-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  canSubmitExpirationAllocation,
  clampLotDraftQuantity,
  createExpirationLotDraft,
  isExpiryDateInPast,
  isOverAllocated,
  maxQuantityForRow,
  parseLotDraftQuantity,
  remainingToAllocate,
  sumAllocatedQuantity,
  toExistingStockLotInputs,
  type ExpirationLotDraft,
  parseExpirationLotRows,
} from "@/features/inventory/enable-expiration-tracking-helpers";
import { formatLocalDateOnly } from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";

export type AssignExpirationLotsFormProps = {
  workspace: PosWorkspaceScope;
  productId: string;
  productName: string;
  onHandQuantity: number;
  unitOfMeasure: string;
  expirationWarningDays?: number | null;
  /** When assigning lots for already-tracked stock (repair), use assign copy. */
  intent?: "enable" | "assign";
  onSuccess: (result: EnableExpirationTrackingResponse) => void;
};

export function AssignExpirationLotsForm({
  workspace,
  productId,
  productName,
  onHandQuantity,
  unitOfMeasure,
  expirationWarningDays,
  intent = "assign",
  onSuccess,
}: AssignExpirationLotsFormProps) {
  const { t } = useI18n();
  const [rows, setRows] = useState<ExpirationLotDraft[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const today = formatLocalDateOnly();

  useEffect(() => {
    setError(null);
    setSubmitting(false);
    const defaultQty = onHandQuantity > 0 ? String(onHandQuantity) : "";
    setRows([createExpirationLotDraft(defaultQty)]);
  }, [onHandQuantity, productId]);

  const parsedRows = useMemo(() => parseExpirationLotRows(rows), [rows]);
  const allocated = sumAllocatedQuantity(
    parsedRows.filter((row) => Number.isFinite(row.quantity) && row.quantity > 0),
  );
  const remaining = remainingToAllocate(onHandQuantity, allocated);
  const overAllocated = isOverAllocated(allocated, onHandQuantity);
  const allocationReady = canSubmitExpirationAllocation(onHandQuantity, rows);
  const canSubmit = allocationReady && !submitting;
  const canAddRow = remaining > 0 && !overAllocated && !submitting;
  const missingExpiry =
    !overAllocated &&
    remaining <= 0 &&
    rows.some((row) => (parseLotDraftQuantity(row.quantity) ?? 0) > 0 && !row.expiryDate.trim());

  function updateRow(id: string, patch: Partial<ExpirationLotDraft>) {
    setRows((current) => current.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }

  function updateQuantity(id: string, raw: string) {
    setRows((current) => {
      const clamped = clampLotDraftQuantity(onHandQuantity, current, id, raw);
      return current.map((row) => (row.id === id ? { ...row, quantity: clamped } : row));
    });
  }

  function addRow() {
    if (!canAddRow) {
      return;
    }
    const defaultQty = remaining > 0 ? String(remaining) : "";
    setRows((current) => [...current, createExpirationLotDraft(defaultQty)]);
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

  const submitLabel =
    intent === "enable"
      ? t("inventory.enableExpirationTracking")
      : t("inventory.assignExpirationDates");

  return (
    <div className="flex flex-col gap-3" data-testid="assign-expiration-lots-form">
      {intent === "enable" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("inventory.enableExpirationCopy")
            .replace("{qty}", String(onHandQuantity))
            .replace("{uom}", unitOfMeasure)}
        </p>
      ) : null}

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
          const rowMax = maxQuantityForRow(onHandQuantity, rows, row.id);
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
                onChange={(e) => updateQuantity(row.id, e.target.value)}
                data-testid={`enable-expiration-qty-${index}`}
              />
              {rowMax < onHandQuantity ? (
                <p
                  className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                  data-testid={`enable-expiration-qty-max-${index}`}
                >
                  {t("inventory.enableExpirationQuantityMax").replace("{max}", String(rowMax))}
                </p>
              ) : null}
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
                  variant="outline"
                  className="w-fit"
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

      <div className="flex flex-wrap items-center gap-2" data-testid="enable-expiration-secondary-actions">
        <Button
          type="button"
          variant="outline"
          className="w-fit"
          onClick={addRow}
          disabled={!canAddRow}
          data-testid="enable-expiration-add-row"
        >
          <Plus className="size-4 shrink-0" aria-hidden />
          {t("inventory.enableExpirationAddRow")}
        </Button>
      </div>
      {!canAddRow && remaining <= 0 && !overAllocated ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="enable-expiration-add-hint"
        >
          {t("inventory.enableExpirationAddDisabledHint")}
        </p>
      ) : null}

      <p
        className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
        data-testid="enable-expiration-allocated"
      >
        {t("inventory.enableExpirationAllocated")
          .replace("{allocated}", String(allocated))
          .replace("{onHand}", String(onHandQuantity))}
      </p>
      {overAllocated ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-destructive"
          data-testid="enable-expiration-over-allocated"
          role="alert"
        >
          {t("inventory.enableExpirationOverAllocated")
            .replace("{allocated}", String(allocated))
            .replace("{onHand}", String(onHandQuantity))}
        </p>
      ) : remaining > 0 ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="enable-expiration-remaining"
        >
          {t("inventory.enableExpirationRemaining").replace("{remaining}", String(remaining))}
        </p>
      ) : null}

      {!canSubmit && !submitting ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="enable-expiration-submit-hint"
        >
          {missingExpiry
            ? t("inventory.enableExpirationExpiryRequiredHint")
            : t("inventory.enableExpirationSubmitHint")
                .replace("{onHand}", String(onHandQuantity))
                .replace("{uom}", unitOfMeasure)}
        </p>
      ) : null}

      {error ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)] text-destructive"
          data-testid="enable-expiration-error"
          role="alert"
        >
          {error}
        </p>
      ) : null}

      <div className="flex flex-wrap justify-end gap-2" data-testid="enable-expiration-primary-actions">
        <Button
          type="button"
          disabled={!canSubmit}
          onClick={() => void onSubmit()}
          data-testid="enable-expiration-submit"
        >
          {submitting ? t("loading.label") : submitLabel}
        </Button>
      </div>
    </div>
  );
}

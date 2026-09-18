import { useEffect, useMemo, useState } from "react";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  classifyReturnBatchLine,
  type ReturnBatchDto,
  type ReturnBatchLineDto,
} from "@/api/pos/pos-return-batches-client";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  isValidClassificationTotal,
  parseReturnQuantityInput,
  roundReturnQuantity,
} from "@/features/returns/return-classification";

type Props = {
  open: boolean;
  workspace: PosWorkspaceScope;
  batch: ReturnBatchDto;
  line: ReturnBatchLineDto | null;
  onClose: () => void;
  onSaved: (updated: ReturnBatchDto) => void;
};

export function ReturnInspectionDialog({ open, workspace, batch, line, onClose, onSaved }: Props) {
  const { t } = useI18n();
  const [sellableQuantity, setSellableQuantity] = useState(0);
  const [damagedQuantity, setDamagedQuantity] = useState(0);
  const [inspectionNote, setInspectionNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!line || !open) {
      return;
    }
    setSellableQuantity(roundReturnQuantity(line.sellableQuantity ?? line.acceptedQuantity));
    setDamagedQuantity(roundReturnQuantity(line.damagedQuantity ?? 0));
    setInspectionNote(line.inspectionNote ?? "");
    setError(null);
  }, [line, open]);

  const returnedQuantity = useMemo(() => roundReturnQuantity(line?.acceptedQuantity ?? 0), [line]);
  const totalClassified = useMemo(
    () => roundReturnQuantity(sellableQuantity + damagedQuantity),
    [sellableQuantity, damagedQuantity],
  );

  if (!line) {
    return null;
  }

  const validTotal = isValidClassificationTotal(returnedQuantity, sellableQuantity, damagedQuantity);

  async function onSave() {
    if (!line) {
      return;
    }
    if (saving) {
      return;
    }
    if (!validTotal) {
      setError(t("returns.totalClassified"));
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const updated = await classifyReturnBatchLine(
        workspace,
        batch.returnBatchId,
        line.returnBatchLineId,
        {
          sellableQuantity,
          damagedQuantity,
          inspectionNote: inspectionNote.trim() || undefined,
          expectedUpdatedAtUtc: batch.updatedAtUtc,
        },
      );
      onSaved(updated);
      onClose();
    } catch (err) {
      setError((err as Error).message || t("error.title"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      panelId="return-inspection-dialog"
      testId="return-inspection-dialog"
      title={`${t("returns.returnInspection")} · ${line.productNameSnapshot}`}
      closeLabel={t("sell.cancel")}
      presentation="sheet-mobile-dialog-desktop"
    >
      <div className="flex min-w-0 flex-col gap-3 pb-1">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("returns.returnedQuantity")}: {returnedQuantity} {line.unitOfMeasure}
        </p>

        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("returns.sellableAgain")}
            <input
              type="number"
              min={0}
              step={0.001}
              value={sellableQuantity || ""}
              data-testid="return-inspection-sellable-input"
              className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              onChange={(event) => setSellableQuantity(parseReturnQuantityInput(event.target.value))}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("returns.damagedWriteOff")}
            <input
              type="number"
              min={0}
              step={0.001}
              value={damagedQuantity || ""}
              data-testid="return-inspection-damaged-input"
              className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              onChange={(event) => setDamagedQuantity(parseReturnQuantityInput(event.target.value))}
            />
          </label>
        </div>

        <div className="grid grid-cols-2 gap-2">
          <QuantityStepper
            value={sellableQuantity}
            increaseLabel={t("returns.increaseQty")}
            decreaseLabel={t("returns.decreaseQty")}
            onIncrement={() => setSellableQuantity((prev) => roundReturnQuantity(prev + 1))}
            onDecrement={() => setSellableQuantity((prev) => Math.max(0, roundReturnQuantity(prev - 1)))}
          />
          <QuantityStepper
            value={damagedQuantity}
            increaseLabel={t("returns.increaseQty")}
            decreaseLabel={t("returns.decreaseQty")}
            onIncrement={() => setDamagedQuantity((prev) => roundReturnQuantity(prev + 1))}
            onDecrement={() => setDamagedQuantity((prev) => Math.max(0, roundReturnQuantity(prev - 1)))}
          />
        </div>

        <p
          className="m-0 text-[length:var(--exits-text-sm)]"
          data-testid="return-inspection-total-classified"
        >
          {t("returns.totalClassified")}: {totalClassified} / {returnedQuantity}
        </p>

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]" htmlFor="return-inspection-note">
          {t("returns.notes")}
          <input
            id="return-inspection-note"
            type="text"
            value={inspectionNote}
            data-testid="return-inspection-note"
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            onChange={(event) => setInspectionNote(event.target.value)}
          />
        </label>

        {error ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            data-testid="return-inspection-error"
          >
            {error}
          </p>
        ) : null}

        <div className="mt-1 flex flex-wrap justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onClose} disabled={saving}>
            {t("sell.cancel")}
          </Button>
          <Button
            type="button"
            onClick={() => void onSave()}
            disabled={saving || !validTotal}
            data-testid="return-inspection-save"
          >
            {saving ? t("returns.submitting") : t("returns.saveClassification")}
          </Button>
        </div>
      </div>
    </BottomSheet>
  );
}

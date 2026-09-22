import { useEffect, useMemo, useState } from "react";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  classifyReturnBatchLine,
  getReturnBatch,
  type ReturnBatchDto,
  type ReturnBatchLineDto,
} from "@/api/pos/pos-return-batches-client";
import { isStaleReturnConflict } from "@/api/pos/pos-sale-returns-client";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  allocateComplementaryReturnQuantity,
  isValidClassificationTotal,
  roundReturnQuantity,
} from "@/features/returns/return-classification";
import { describeReturnError } from "@/features/returns/return-errors";

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
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState(batch.updatedAtUtc);

  const returnedQuantity = useMemo(() => roundReturnQuantity(line?.acceptedQuantity ?? 0), [line]);

  useEffect(() => {
    if (!line || !open) {
      return;
    }
    const fromSellable = allocateComplementaryReturnQuantity(
      line.acceptedQuantity,
      line.sellableQuantity ?? line.acceptedQuantity,
    );
    setSellableQuantity(fromSellable.primary);
    setDamagedQuantity(fromSellable.complementary);
    setInspectionNote(line.inspectionNote ?? "");
    setExpectedUpdatedAtUtc(batch.updatedAtUtc);
    setError(null);
  }, [line, open, batch.updatedAtUtc]);

  const totalClassified = useMemo(
    () => roundReturnQuantity(sellableQuantity + damagedQuantity),
    [sellableQuantity, damagedQuantity],
  );

  function setSellableLinked(next: number) {
    const allocated = allocateComplementaryReturnQuantity(returnedQuantity, next);
    setSellableQuantity(allocated.primary);
    setDamagedQuantity(allocated.complementary);
    setError(null);
  }

  function setDamagedLinked(next: number) {
    const allocated = allocateComplementaryReturnQuantity(returnedQuantity, next);
    setDamagedQuantity(allocated.primary);
    setSellableQuantity(allocated.complementary);
    setError(null);
  }

  if (!line) {
    return null;
  }

  const validTotal = isValidClassificationTotal(returnedQuantity, sellableQuantity, damagedQuantity);

  async function classifyOnce(updatedAtUtc: string) {
    if (!line) {
      throw new Error("Missing return line.");
    }
    return classifyReturnBatchLine(
      workspace,
      batch.returnBatchId,
      line.returnBatchLineId,
      {
        sellableQuantity,
        damagedQuantity,
        inspectionNote: inspectionNote.trim() || undefined,
        expectedUpdatedAtUtc: updatedAtUtc,
      },
    );
  }

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
      let updated: ReturnBatchDto;
      try {
        updated = await classifyOnce(expectedUpdatedAtUtc);
      } catch (err) {
        if (!isStaleReturnConflict(err)) {
          throw err;
        }
        // Refresh snapshot and retry once (covers true concurrent edits).
        const fresh = await getReturnBatch(workspace, batch.returnBatchId);
        onSaved(fresh);
        setExpectedUpdatedAtUtc(fresh.updatedAtUtc);
        updated = await classifyOnce(fresh.updatedAtUtc);
      }
      onSaved(updated);
      onClose();
    } catch (err) {
      setError(describeReturnError(err, t));
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

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
            {t("returns.sellableAgain")}
            <span className="flex w-fit flex-col items-center gap-1">
              <QuantityStepper
                variant="auto"
                value={sellableQuantity}
                onChange={setSellableLinked}
                min={0}
                max={returnedQuantity}
                step={1}
                precision={3}
                decreaseLabel={t("returns.decreaseQty")}
                increaseLabel={t("returns.increaseQty")}
                ariaLabel={t("returns.sellableAgain")}
                valueTestId="return-inspection-sellable-input"
              />
              <span className="text-[length:var(--exits-text-xs)] font-bold text-muted">
                {line.unitOfMeasure}
              </span>
            </span>
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
            {t("returns.damagedWriteOff")}
            <span className="flex w-fit flex-col items-center gap-1">
              <QuantityStepper
                variant="auto"
                value={damagedQuantity}
                onChange={setDamagedLinked}
                min={0}
                max={returnedQuantity}
                step={1}
                precision={3}
                decreaseLabel={t("returns.decreaseQty")}
                increaseLabel={t("returns.increaseQty")}
                ariaLabel={t("returns.damagedWriteOff")}
                valueTestId="return-inspection-damaged-input"
              />
              <span className="text-[length:var(--exits-text-xs)] font-bold text-muted">
                {line.unitOfMeasure}
              </span>
            </span>
          </label>
        </div>

        <p
          className="m-0 text-[length:var(--exits-text-sm)]"
          data-testid="return-inspection-total-classified"
        >
          {t("returns.classifiedProgress")
            .replace("{classified}", String(totalClassified))
            .replace("{returned}", String(returnedQuantity))}
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
          <Button
            type="button"
            intent="danger"
            appearance="solid"
            onClick={onClose}
            disabled={saving}
            data-testid="return-inspection-cancel"
          >
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

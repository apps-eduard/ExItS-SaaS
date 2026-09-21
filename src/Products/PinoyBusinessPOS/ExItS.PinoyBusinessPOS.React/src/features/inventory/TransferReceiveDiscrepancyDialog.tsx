import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { INVENTORY_TRANSFER_DISCREPANCY_REASONS } from "@/api/pos/pos-inventory-transfer-client";
import { formatTransferQty, inventoryTransferDiscrepancyLabelKey } from "@/features/inventory/inventory-transfer-labels";
import { useI18n } from "@/i18n/I18nProvider";

export type TransferReceiveDiscrepancyDialogProps = {
  open: boolean;
  productName: string;
  unitOfMeasure: string;
  sentQty: number;
  receivedQty: number;
  reason: string;
  note: string;
  onReasonChange: (reason: string) => void;
  onNoteChange: (note: string) => void;
  onCancel: () => void;
  onConfirm: () => void;
};

/**
 * Transfer receive discrepancy classification — reason + optional note.
 * Mirrors PO receive dialog shell; keeps transfer API fields (not damaged/not-delivered split).
 */
export function TransferReceiveDiscrepancyDialog({
  open,
  productName,
  unitOfMeasure,
  sentQty,
  receivedQty,
  reason,
  note,
  onReasonChange,
  onNoteChange,
  onCancel,
  onConfirm,
}: TransferReceiveDiscrepancyDialogProps) {
  const { t } = useI18n();
  const shortQty = Math.max(0, sentQty - receivedQty);
  const reasonMissing = !reason.trim();
  const canConfirm = !reasonMissing;

  return (
    <ExitsModal
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          onCancel();
        }
      }}
      title={t("transfer.classifyDiscrepancyTitle")}
      description={`${productName} · ${t("transfer.difference")}: ${formatTransferQty(shortQty)} ${unitOfMeasure}`}
      testId="transfer-discrepancy-dialog"
      footer={
        <div className="flex flex-wrap justify-end gap-2">
          <Button type="button" intent="neutral" appearance="outline" onClick={onCancel}>
            {t("transfer.dialogCancel")}
          </Button>
          <Button
            type="button"
            disabled={!canConfirm}
            onClick={onConfirm}
            data-testid="transfer-discrepancy-dialog-confirm"
          >
            {t("transfer.classifyDiscrepancyConfirm")}
          </Button>
        </div>
      }
    >
      <div className="flex flex-col gap-3">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("transfer.classifyDiscrepancyHint")
            .replace("{sent}", formatTransferQty(sentQty))
            .replace("{received}", formatTransferQty(receivedQty))}
        </p>
        <label className="flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)] font-medium">
            {t("transfer.discrepancyReason")}
            <span className="text-destructive" aria-hidden>
              {" "}
              *
            </span>
          </span>
          <select
            className={`exits-select${reasonMissing ? " border-destructive" : ""}`}
            value={reason}
            required
            aria-invalid={reasonMissing ? true : undefined}
            onChange={(e) => onReasonChange(e.target.value)}
            data-testid="transfer-discrepancy-dialog-reason"
          >
            <option value="">{t("transfer.selectDiscrepancy")}</option>
            {INVENTORY_TRANSFER_DISCREPANCY_REASONS.map((code) => (
              <option key={code} value={code}>
                {t(inventoryTransferDiscrepancyLabelKey(code))}
              </option>
            ))}
          </select>
          {reasonMissing ? (
            <span className="text-[length:var(--exits-text-xs)] text-destructive">
              {t("transfer.discrepancyReasonRequired")}
            </span>
          ) : null}
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)] font-medium">
            {t("transfer.discrepancyNote")}
          </span>
          <input
            className="exits-input"
            value={note}
            onChange={(e) => onNoteChange(e.target.value)}
            placeholder={t("transfer.discrepancyNote")}
            data-testid="transfer-discrepancy-dialog-note"
          />
        </label>
      </div>
    </ExitsModal>
  );
}

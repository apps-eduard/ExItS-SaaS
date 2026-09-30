import { useEffect, useState } from "react";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { useI18n } from "@/i18n/I18nProvider";

export type EditLotIdentityDialogProps = {
  open: boolean;
  lot: PosInventoryLotDto | null;
  productName: string;
  locationName: string;
  unitOfMeasure: string;
  busy: boolean;
  errorMessage?: string | null;
  onCancel: () => void;
  onSave: (values: {
    expirationDate: string;
    lotNumber: string | null;
    reason: string;
    expectedUpdatedAtUtc: string;
  }) => void;
};

export function EditLotIdentityDialog({
  open,
  lot,
  productName,
  locationName,
  unitOfMeasure,
  busy,
  errorMessage,
  onCancel,
  onSave,
}: EditLotIdentityDialogProps) {
  const { t } = useI18n();
  const [expirationDate, setExpirationDate] = useState("");
  const [lotNumber, setLotNumber] = useState("");
  const [reason, setReason] = useState("");

  useEffect(() => {
    if (!open || !lot) {
      return;
    }
    setExpirationDate(lot.expirationDate);
    setLotNumber(lot.lotNumber ?? "");
    setReason("");
  }, [open, lot]);

  const reasonTrimmed = reason.trim();
  const canSave = Boolean(expirationDate) && reasonTrimmed.length > 0 && !busy && lot;

  return (
    <ExitsModal
      open={open}
      busy={busy}
      onOpenChange={(next) => {
        if (!next && !busy) {
          onCancel();
        }
      }}
      title={t("inventory.editLotIdentityTitle")}
      description={t("inventory.editLotIdentityHelper")}
      testId="edit-lot-identity-dialog"
      footer={
        <div className="flex flex-wrap justify-end gap-2">
          <Button type="button" intent="neutral" appearance="outline" disabled={busy} onClick={onCancel}>
            {t("inventory.editLotIdentityCancel")}
          </Button>
          <Button
            type="button"
            disabled={!canSave}
            data-testid="edit-lot-identity-save"
            onClick={() => {
              if (!lot || !canSave) {
                return;
              }
              onSave({
                expirationDate,
                lotNumber: lotNumber.trim() ? lotNumber.trim() : null,
                reason: reasonTrimmed,
                expectedUpdatedAtUtc: lot.updatedAtUtc,
              });
            }}
          >
            {busy ? t("inventory.editLotIdentitySaving") : t("inventory.editLotIdentitySave")}
          </Button>
        </div>
      }
    >
      {lot ? (
        <div className="flex flex-col gap-3" data-testid="edit-lot-identity-form">
          <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)]">
            <div className="flex flex-wrap gap-x-2">
              <dt className="m-0 text-muted">{t("inventory.editLotIdentityProduct")}</dt>
              <dd className="m-0 font-medium">{productName}</dd>
            </div>
            <div className="flex flex-wrap gap-x-2">
              <dt className="m-0 text-muted">{t("inventory.editLotIdentityLocation")}</dt>
              <dd className="m-0 font-medium">{locationName}</dd>
            </div>
            <div className="flex flex-wrap gap-x-2">
              <dt className="m-0 text-muted">{t("inventory.editLotIdentityAvailable")}</dt>
              <dd className="m-0 font-medium">
                {lot.quantityOnHand} {unitOfMeasure}
              </dd>
            </div>
          </dl>

          <label className="flex flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)] font-medium">
              {t("inventory.editLotIdentityExpiry")} *
            </span>
            <input
              type="date"
              className="exits-input"
              value={expirationDate}
              disabled={busy}
              onChange={(e) => setExpirationDate(e.target.value)}
              data-testid="edit-lot-identity-expiry"
              required
            />
          </label>

          <label className="flex flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)] font-medium">
              {t("inventory.editLotIdentityBatch")}
            </span>
            <input
              type="text"
              className="exits-input"
              value={lotNumber}
              disabled={busy}
              maxLength={64}
              onChange={(e) => setLotNumber(e.target.value)}
              data-testid="edit-lot-identity-batch"
              placeholder={t("inventory.editLotIdentityBatchOptional")}
            />
          </label>

          <label className="flex flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)] font-medium">
              {t("inventory.editLotIdentityReason")} *
            </span>
            <textarea
              className="exits-input min-h-[4.5rem]"
              value={reason}
              disabled={busy}
              maxLength={512}
              onChange={(e) => setReason(e.target.value)}
              data-testid="edit-lot-identity-reason"
              required
            />
          </label>

          {errorMessage ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {errorMessage}
            </p>
          ) : null}
        </div>
      ) : null}
    </ExitsModal>
  );
}

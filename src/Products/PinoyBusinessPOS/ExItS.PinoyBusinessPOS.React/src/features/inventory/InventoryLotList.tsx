import { Lock, Pencil } from "lucide-react";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { formatLotBatchLabel } from "@/features/inventory/inventory-detail-helpers";
import { resolveLotExpiryLabel } from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

type InventoryLotListProps = {
  lots: PosInventoryLotDto[];
  unitOfMeasure: string;
  formatStatus: (lot: PosInventoryLotDto) => string;
  selectable?: boolean;
  selectedLotId?: string;
  onSelectLot?: (lotId: string) => void;
  namePrefix?: string;
  /** When set, shows edit/lock actions for lot identity correction. */
  onEditLotIdentity?: (lot: PosInventoryLotDto) => void;
};

function statusBadgeClass(lot: PosInventoryLotDto): string {
  const label = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
  switch (label.kind) {
    case "expired":
      return "inventory-lot-badge inventory-lot-badge--expired";
    case "expiresToday":
    case "expiresInDays":
      return "inventory-lot-badge inventory-lot-badge--near";
    default:
      return "inventory-lot-badge inventory-lot-badge--good";
  }
}

function lockReasonMessage(
  reason: string | null | undefined,
  t: (key: MessageKey) => string,
): string {
  switch (reason) {
    case "ActiveTransferDraft":
      return t("inventory.lotIdentityLockedActiveDraft");
    case "ReceivedFromTransfer":
      return t("inventory.lotIdentityLockedReceivedFromTransfer");
    case "Transferred":
      return t("inventory.lotIdentityLockedTransferred");
    case "ReferencedByDocument":
      return t("inventory.lotIdentityLockedReferenced");
    default:
      return t("inventory.lotIdentityLockedUsed");
  }
}

function LotIdentityAction({
  lot,
  onEditLotIdentity,
}: {
  lot: PosInventoryLotDto;
  onEditLotIdentity?: (lot: PosInventoryLotDto) => void;
}) {
  const { t } = useI18n();
  if (!onEditLotIdentity) {
    return null;
  }

  if (lot.canEditIdentity) {
    return (
      <Button
        type="button"
        size="icon"
        intent="neutral"
        appearance="ghost"
        aria-label={t("inventory.editLotIdentityAria")}
        title={t("inventory.editLotIdentityAria")}
        data-testid={`lot-edit-identity-${lot.lotId}`}
        onClick={() => onEditLotIdentity(lot)}
      >
        <Pencil className="size-4" aria-hidden />
      </Button>
    );
  }

  const message = lockReasonMessage(lot.identityLockReason, t);
  return (
    <span
      className="inline-flex text-muted"
      title={message}
      aria-label={message}
      data-testid={`lot-identity-locked-${lot.lotId}`}
    >
      <Lock className="size-4" aria-hidden />
    </span>
  );
}

export function InventoryLotList({
  lots,
  unitOfMeasure,
  formatStatus,
  selectable = false,
  selectedLotId,
  onSelectLot,
  namePrefix = "inventory-lot",
  onEditLotIdentity,
}: InventoryLotListProps) {
  const { t } = useI18n();
  const showActions = Boolean(onEditLotIdentity);

  if (lots.length === 0) {
    return null;
  }

  return (
    <>
      <div className="inventory-lot-table hidden min-[640px]:block" data-testid="inventory-lot-table">
        <table className="inventory-lot-table__grid">
          <thead>
            <tr>
              {selectable ? <th scope="col" className="sr-only">{t("inventory.selectLot")}</th> : null}
              <th scope="col">{t("inventory.lotColumnExpiry")}</th>
              <th scope="col">{t("inventory.lotColumnBatch")}</th>
              <th scope="col">{t("inventory.lotColumnAvailable")}</th>
              <th scope="col">{t("inventory.lotColumnStatus")}</th>
              {showActions ? <th scope="col">{t("inventory.lotColumnAction")}</th> : null}
            </tr>
          </thead>
          <tbody>
            {lots.map((lot) => (
              <tr key={lot.lotId} data-testid={`${namePrefix}-${lot.lotId}`}>
                {selectable ? (
                  <td>
                    <input
                      type="radio"
                      name="inventory-lot-picker"
                      value={lot.lotId}
                      checked={selectedLotId === lot.lotId}
                      onChange={() => onSelectLot?.(lot.lotId)}
                      aria-label={`${lot.expirationDate} ${formatLotBatchLabel(lot.lotNumber)}`}
                    />
                  </td>
                ) : null}
                <td>{lot.expirationDate}</td>
                <td>{formatLotBatchLabel(lot.lotNumber)}</td>
                <td>
                  {lot.quantityOnHand} {unitOfMeasure}
                </td>
                <td>
                  <span className={statusBadgeClass(lot)}>{formatStatus(lot)}</span>
                </td>
                {showActions ? (
                  <td>
                    <LotIdentityAction lot={lot} onEditLotIdentity={onEditLotIdentity} />
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul className="inventory-lot-cards min-[640px]:hidden mt-2 mb-0 flex list-none flex-col gap-2 p-0">
        {lots.map((lot) => (
          <li key={lot.lotId}>
            <Card
              className={`inventory-lot-card p-3 ${selectable && selectedLotId === lot.lotId ? "inventory-lot-card--selected" : ""}`}
              data-testid={`${namePrefix}-${lot.lotId}`}
            >
              {selectable ? (
                <label className="inventory-lot-card__pick flex cursor-pointer items-start gap-3">
                  <input
                    type="radio"
                    name="inventory-lot-picker-mobile"
                    value={lot.lotId}
                    checked={selectedLotId === lot.lotId}
                    onChange={() => onSelectLot?.(lot.lotId)}
                    className="mt-1"
                  />
                  <span className="flex min-w-0 flex-1 flex-col gap-1">
                    <LotCardBody
                      lot={lot}
                      unitOfMeasure={unitOfMeasure}
                      formatStatus={formatStatus}
                      onEditLotIdentity={onEditLotIdentity}
                    />
                  </span>
                </label>
              ) : (
                <LotCardBody
                  lot={lot}
                  unitOfMeasure={unitOfMeasure}
                  formatStatus={formatStatus}
                  onEditLotIdentity={onEditLotIdentity}
                />
              )}
            </Card>
          </li>
        ))}
      </ul>
    </>
  );
}

function LotCardBody({
  lot,
  unitOfMeasure,
  formatStatus,
  onEditLotIdentity,
}: {
  lot: PosInventoryLotDto;
  unitOfMeasure: string;
  formatStatus: (lot: PosInventoryLotDto) => string;
  onEditLotIdentity?: (lot: PosInventoryLotDto) => void;
}) {
  const { t } = useI18n();
  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-semibold">{lot.expirationDate}</span>
        <span className="flex items-center gap-2">
          <span className={statusBadgeClass(lot)}>{formatStatus(lot)}</span>
          <LotIdentityAction lot={lot} onEditLotIdentity={onEditLotIdentity} />
        </span>
      </div>
      <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)]">
        {lot.quantityOnHand} {unitOfMeasure}
      </p>
      <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("inventory.lotCardBatch")}: {formatLotBatchLabel(lot.lotNumber)}
      </p>
      {!lot.canEditIdentity && onEditLotIdentity ? (
        <p className="mt-1 mb-0 text-[length:var(--exits-text-xs)] text-muted">
          {lockReasonMessage(lot.identityLockReason, t)}
        </p>
      ) : null}
    </>
  );
}

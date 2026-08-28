import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { Card } from "@/components/ui/card";
import { formatLotBatchLabel } from "@/features/inventory/inventory-detail-helpers";
import { resolveLotExpiryLabel } from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";

type InventoryLotListProps = {
  lots: PosInventoryLotDto[];
  unitOfMeasure: string;
  formatStatus: (lot: PosInventoryLotDto) => string;
  selectable?: boolean;
  selectedLotId?: string;
  onSelectLot?: (lotId: string) => void;
  namePrefix?: string;
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

export function InventoryLotList({
  lots,
  unitOfMeasure,
  formatStatus,
  selectable = false,
  selectedLotId,
  onSelectLot,
  namePrefix = "inventory-lot",
}: InventoryLotListProps) {
  const { t } = useI18n();

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
                    <LotCardBody lot={lot} unitOfMeasure={unitOfMeasure} formatStatus={formatStatus} />
                  </span>
                </label>
              ) : (
                <LotCardBody lot={lot} unitOfMeasure={unitOfMeasure} formatStatus={formatStatus} />
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
}: {
  lot: PosInventoryLotDto;
  unitOfMeasure: string;
  formatStatus: (lot: PosInventoryLotDto) => string;
}) {
  const { t } = useI18n();
  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-semibold">{lot.expirationDate}</span>
        <span className={statusBadgeClass(lot)}>{formatStatus(lot)}</span>
      </div>
      <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)]">
        {lot.quantityOnHand} {unitOfMeasure}
      </p>
      <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("inventory.lotCardBatch")}: {formatLotBatchLabel(lot.lotNumber)}
      </p>
    </>
  );
}

import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { ActorAttribution, ActorName } from "@/features/actors/ActorAttribution";
import type { OrganizationActorDisplayName } from "@/api/platform/actor-directory-client";
import {
  inventoryMovementTypeLabelKey,
  resolveMovementStockValue,
} from "@/features/purchasing/purchase-cost-display";
import {
  extractTransferReferenceNumber,
  inventoryTransferDetailPath,
} from "@/features/inventory/inventory-movement-transfer-ref";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";

function formatMovementWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString();
}

function formatQtyEffect(quantityEffect: number, unitOfMeasure: string): string {
  const sign = quantityEffect > 0 ? "+" : "";
  return `${sign}${quantityEffect} ${unitOfMeasure}`;
}

function MovementTypeCell({
  movement,
  onOpenReservations,
}: {
  movement: PosStockMovementDto;
  onOpenReservations?: () => void;
}) {
  const { t } = useI18n();
  const typeLabel = t(inventoryMovementTypeLabelKey(movement.movementType));
  const transferNumber = extractTransferReferenceNumber(movement);
  const transferId = movement.sourceId?.trim() || null;
  const canOpenDrawer = Boolean(transferNumber && onOpenReservations);
  const transferPath =
    transferNumber && transferId ? inventoryTransferDetailPath(transferId) : null;

  return (
    <span
      className="inline-flex min-w-0 flex-wrap items-baseline gap-x-1.5 gap-y-0.5"
      data-testid={`inventory-movement-type-${movement.movementId}`}
    >
      <span className="text-muted">{typeLabel}</span>
      {transferNumber && canOpenDrawer ? (
        <button
          type="button"
          className="min-w-0 truncate font-semibold text-primary underline-offset-2 hover:underline"
          onClick={onOpenReservations}
          data-testid={`inventory-movement-transfer-ref-${movement.movementId}`}
        >
          {transferNumber}
        </button>
      ) : transferNumber && transferPath ? (
        <AppLinkWithReturn
          to={transferPath}
          className="min-w-0 truncate font-semibold text-primary no-underline hover:underline"
          data-testid={`inventory-movement-transfer-ref-${movement.movementId}`}
        >
          {transferNumber}
        </AppLinkWithReturn>
      ) : transferNumber ? (
        <span
          className="min-w-0 truncate font-semibold"
          data-testid={`inventory-movement-transfer-ref-${movement.movementId}`}
        >
          {transferNumber}
        </span>
      ) : null}
    </span>
  );
}

export type InventoryMovementsResponsiveListProps = {
  movements: PosStockMovementDto[];
  unitOfMeasure: string;
  resolveActor: (actorId: string) => OrganizationActorDisplayName | null | undefined;
  actorsLoading: boolean;
  /** Opens the product reservations drawer (same as reserved / in-transit badges). */
  onOpenReservations?: () => void;
};

/**
 * Movement history: cards on small screens, table from md up.
 */
export function InventoryMovementsResponsiveList({
  movements,
  unitOfMeasure,
  resolveActor,
  actorsLoading,
  onOpenReservations,
}: InventoryMovementsResponsiveListProps) {
  const { t } = useI18n();

  if (movements.length === 0) {
    return null;
  }

  return (
    <>
      <ul
        className="mt-2 mb-0 flex list-none flex-col gap-2 p-0 md:hidden"
        data-testid="inventory-movements-cards"
      >
        {movements.map((movement) => {
          const stockValue = resolveMovementStockValue(movement);
          return (
            <li key={movement.movementId}>
              <Card className="p-3" data-testid={`inventory-movement-card-${movement.movementId}`}>
                <p className="m-0 font-semibold">
                  {formatQtyEffect(movement.quantityEffect, unitOfMeasure)}
                </p>
                <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)]">
                  <MovementTypeCell
                    movement={movement}
                    onOpenReservations={onOpenReservations}
                  />
                </p>
                {movement.unitCost != null ? (
                  <dl
                    className="mt-2 mb-0 grid gap-1 text-[length:var(--exits-text-sm)]"
                    data-testid={`inventory-movement-cost-${movement.movementId}`}
                  >
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("inventory.unitPurchaseCost")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={movement.unitCost} />
                        <span className="text-muted"> / {unitOfMeasure}</span>
                      </dd>
                    </div>
                    {stockValue != null ? (
                      <div className="flex flex-wrap items-baseline justify-between gap-2">
                        <dt className="text-muted">{t("inventory.stockValue")}</dt>
                        <dd className="m-0">
                          <MoneyDisplay amount={stockValue} />
                        </dd>
                      </div>
                    ) : null}
                  </dl>
                ) : null}
                {movement.expirationDate ? (
                  <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("inventory.movementExpiry")}: {movement.expirationDate}
                    {movement.lotNumber
                      ? ` · ${t("inventory.movementLot")}: ${movement.lotNumber}`
                      : ""}
                  </p>
                ) : null}
                <div className="mt-2">
                  <ActorAttribution
                    labelKey="common.recordedBy"
                    actorId={movement.recordedBy}
                    occurredAtUtc={movement.recordedAtUtc}
                    resolved={resolveActor(movement.recordedBy)}
                    isLoading={actorsLoading}
                    testId={`inventory-movement-actor-${movement.movementId}`}
                  />
                </div>
              </Card>
            </li>
          );
        })}
      </ul>

      <div
        className="inventory-movements-table-shell mt-2 hidden min-w-0 overflow-x-auto md:block"
        data-testid="inventory-movements-table"
      >
        <table className="inventory-movements-table w-full min-w-[40rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="inventory-movements-table__head border-b border-border">
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("inventory.movementCol.when")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("inventory.movementCol.qty")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("inventory.movementCol.type")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("inventory.movementUnitCost")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("inventory.movementStockValue")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("inventory.movementCol.batch")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("common.recordedBy")}
              </th>
            </tr>
          </thead>
          <tbody>
            {movements.map((movement) => {
              const stockValue = resolveMovementStockValue(movement);
              const batchParts: string[] = [];
              if (movement.expirationDate) {
                batchParts.push(`${t("inventory.movementExpiry")}: ${movement.expirationDate}`);
              }
              if (movement.lotNumber) {
                batchParts.push(`${t("inventory.movementLot")}: ${movement.lotNumber}`);
              }
              return (
                <tr
                  key={movement.movementId}
                  className="inventory-movements-table__row border-b border-border"
                  data-testid={`inventory-movement-row-${movement.movementId}`}
                >
                  <td className="whitespace-nowrap px-3 py-2.5 text-muted tabular-nums">
                    {formatMovementWhen(movement.recordedAtUtc)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-2.5 font-semibold tabular-nums">
                    {formatQtyEffect(movement.quantityEffect, unitOfMeasure)}
                  </td>
                  <td className="px-3 py-2.5">
                    <MovementTypeCell
                      movement={movement}
                      onOpenReservations={onOpenReservations}
                    />
                  </td>
                  <td
                    className="whitespace-nowrap px-3 py-2.5 text-right tabular-nums"
                    data-testid={`inventory-movement-table-cost-${movement.movementId}`}
                  >
                    {movement.unitCost != null ? (
                      <>
                        <MoneyDisplay amount={movement.unitCost} className="font-normal" />
                        <span className="text-muted"> / {unitOfMeasure}</span>
                      </>
                    ) : (
                      <span className="text-muted">—</span>
                    )}
                  </td>
                  <td className="whitespace-nowrap px-3 py-2.5 text-right tabular-nums">
                    {stockValue != null ? (
                      <MoneyDisplay amount={stockValue} className="font-normal" />
                    ) : (
                      <span className="text-muted">—</span>
                    )}
                  </td>
                  <td className="max-w-[12rem] px-3 py-2.5 text-muted">
                    {batchParts.length > 0 ? batchParts.join(" · ") : "—"}
                  </td>
                  <td className="px-3 py-2.5">
                    <ActorName
                      actorId={movement.recordedBy}
                      resolved={resolveActor(movement.recordedBy)}
                      isLoading={actorsLoading}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

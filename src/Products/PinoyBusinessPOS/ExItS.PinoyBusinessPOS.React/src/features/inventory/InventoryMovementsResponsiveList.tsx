import type { KeyboardEvent } from "react";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { ActorAttribution, ActorName } from "@/features/actors/ActorAttribution";
import type { OrganizationActorDisplayName } from "@/api/platform/actor-directory-client";
import {
  inventoryMovementTypeLabelKey,
  resolveMovementStockValue,
} from "@/features/purchasing/purchase-cost-display";
import {
  describeMovementBucketEffects,
  formatSignedBucketQty,
  movementNeedsBucketBreakdown,
} from "@/features/inventory/inventory-movement-bucket-effects";
import { resolveDamageHoldDecisionDisplay } from "@/features/inventory/inventory-movement-damage-hold-display";
import { extractTransferReferenceNumber } from "@/features/inventory/inventory-movement-transfer-ref";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

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

function formatSellableBalanceQty(value: number): string {
  if (!Number.isFinite(value)) {
    return "—";
  }
  return Number.isInteger(value) ? String(value) : String(value);
}

function MovementSellableAfterTag({
  sellableAfter,
  unitOfMeasure,
}: {
  sellableAfter: number | null | undefined;
  unitOfMeasure: string;
}) {
  const { t } = useI18n();
  if (sellableAfter == null || !Number.isFinite(sellableAfter)) {
    return null;
  }
  return (
    <span
      className="mt-1 inline-flex max-w-full rounded border border-border bg-muted/30 px-1.5 py-0.5 text-[length:var(--exits-text-xs)] font-normal text-muted"
      data-testid="inventory-movement-sellable-after"
    >
      {t("inventory.sellableAfter")}: {formatSellableBalanceQty(sellableAfter)} {unitOfMeasure}
    </span>
  );
}

function MovementBucketBreakdown({
  movementType,
  quantityEffect,
}: {
  movementType: string;
  quantityEffect: number;
}) {
  const { t } = useI18n();
  if (!movementNeedsBucketBreakdown(movementType)) {
    return null;
  }
  const effects = describeMovementBucketEffects(movementType, quantityEffect);
  return (
    <p
      className="mt-1 mb-0 text-[length:var(--exits-text-xs)] text-muted"
      data-testid="inventory-movement-bucket-effects"
    >
      {t("inventory.bucketPhysical")}: {formatSignedBucketQty(effects.physicalDelta)}
      {" · "}
      {t("inventory.bucketSellable")}: {formatSignedBucketQty(effects.sellableDelta)}
      {" · "}
      {t("inventory.bucketDamaged")}: {formatSignedBucketQty(effects.damagedDelta)}
      {effects.inspectionHoldDelta !== 0
        ? ` · ${t("inventory.bucketInspectionHold")}: ${formatSignedBucketQty(effects.inspectionHoldDelta)}`
        : null}
    </p>
  );
}

function MovementTypeCell({ movement }: { movement: PosStockMovementDto }) {
  const { t } = useI18n();
  const typeLabel = t(inventoryMovementTypeLabelKey(movement.movementType));
  const transferNumber = extractTransferReferenceNumber(movement);
  const damageHoldDecision = resolveDamageHoldDecisionDisplay(movement);

  return (
    <span
      className="inline-flex min-w-0 flex-col gap-0.5"
      data-testid={`inventory-movement-type-${movement.movementId}`}
    >
      <span className="inline-flex min-w-0 flex-wrap items-baseline gap-x-1.5 gap-y-0.5">
        <span className="text-muted">{typeLabel}</span>
        {transferNumber ? (
          <span
            className="min-w-0 truncate font-semibold text-primary"
            data-testid={`inventory-movement-transfer-ref-${movement.movementId}`}
          >
            {transferNumber}
          </span>
        ) : null}
      </span>
      {damageHoldDecision ? (
        <span
          className="text-[length:var(--exits-text-xs)] text-muted"
          data-testid={`inventory-movement-damage-hold-decision-${movement.movementId}`}
        >
          {t(damageHoldDecision.followUpLabelKey)}
          {" · "}
          {t(damageHoldDecision.custodyLabelKey)}
        </span>
      ) : null}
    </span>
  );
}

function activateOnKeyboard(
  event: KeyboardEvent<HTMLElement>,
  onActivate: () => void,
) {
  if (event.key === "Enter" || event.key === " ") {
    event.preventDefault();
    onActivate();
  }
}

export type InventoryMovementsResponsiveListProps = {
  movements: PosStockMovementDto[];
  unitOfMeasure: string;
  resolveActor: (actorId: string) => OrganizationActorDisplayName | null | undefined;
  actorsLoading: boolean;
  /** Opens transaction details for the selected movement (not stock reservations). */
  onOpenMovement?: (movement: PosStockMovementDto) => void;
};

/**
 * Movement history: cards on small screens, table from md up.
 * Entire row / card opens transaction details when onOpenMovement is provided.
 */
export function InventoryMovementsResponsiveList({
  movements,
  unitOfMeasure,
  resolveActor,
  actorsLoading,
  onOpenMovement,
}: InventoryMovementsResponsiveListProps) {
  const { t } = useI18n();
  const interactive = Boolean(onOpenMovement);

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
          const open = () => onOpenMovement?.(movement);
          return (
            <li key={movement.movementId}>
              <Card
                as="div"
                className="p-3"
                interactive={interactive}
                data-testid={`inventory-movement-card-${movement.movementId}`}
                onClick={interactive ? open : undefined}
              >
                <p className="m-0 font-semibold">
                  {formatQtyEffect(movement.quantityEffect, unitOfMeasure)}
                </p>
                <MovementBucketBreakdown
                  movementType={movement.movementType}
                  quantityEffect={movement.quantityEffect}
                />
                <div className="mt-0.5">
                  <MovementSellableAfterTag
                    sellableAfter={movement.sellableAfter}
                    unitOfMeasure={unitOfMeasure}
                  />
                </div>
                <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)]">
                  <MovementTypeCell movement={movement} />
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
              const open = () => onOpenMovement?.(movement);
              return (
                <tr
                  key={movement.movementId}
                  className={cn(
                    "inventory-movements-table__row border-b border-border",
                    interactive &&
                      "inventory-movements-table__row--interactive cursor-pointer focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-primary",
                  )}
                  data-testid={`inventory-movement-row-${movement.movementId}`}
                  role={interactive ? "button" : undefined}
                  tabIndex={interactive ? 0 : undefined}
                  onClick={interactive ? open : undefined}
                  onKeyDown={
                    interactive
                      ? (event) => activateOnKeyboard(event, open)
                      : undefined
                  }
                >
                  <td className="whitespace-nowrap px-3 py-2.5 text-muted tabular-nums">
                    {formatMovementWhen(movement.recordedAtUtc)}
                  </td>
                  <td className="px-3 py-2.5 font-semibold tabular-nums">
                    <div className="whitespace-nowrap">
                      {formatQtyEffect(movement.quantityEffect, unitOfMeasure)}
                    </div>
                    <MovementBucketBreakdown
                      movementType={movement.movementType}
                      quantityEffect={movement.quantityEffect}
                    />
                    <div className="mt-0.5 font-normal">
                      <MovementSellableAfterTag
                        sellableAfter={movement.sellableAfter}
                        unitOfMeasure={unitOfMeasure}
                      />
                    </div>
                  </td>
                  <td className="px-3 py-2.5">
                    <MovementTypeCell movement={movement} />
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

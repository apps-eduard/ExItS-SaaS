import type { KeyboardEvent } from "react";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { ActorName } from "@/features/actors/ActorAttribution";
import type { OrganizationActorDisplayName } from "@/api/platform/actor-directory-client";
import {
  inventoryMovementTypeLabelKey,
  resolveMovementStockValue,
} from "@/features/purchasing/purchase-cost-display";
import { resolveDamageHoldDecisionDisplay } from "@/features/inventory/inventory-movement-damage-hold-display";
import {
  directPurchaseDetailPath,
} from "@/features/inventory/inventory-movement-direct-purchase-ref";
import { useDirectPurchaseMovementDisplayRef } from "@/features/inventory/useDirectPurchaseMovementDisplayRef";
import { extractTransferReferenceNumber } from "@/features/inventory/inventory-movement-transfer-ref";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Card } from "@/components/ui/card";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
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

function quantityEffectColorClass(quantityEffect: number): string {
  if (quantityEffect > 0) {
    return "text-[var(--exits-success)]";
  }
  if (quantityEffect < 0) {
    return "text-[var(--exits-danger)]";
  }
  return "";
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
      className="mt-1 inline-flex max-w-full rounded border border-[color-mix(in_srgb,var(--exits-info)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_12%,var(--exits-surface))] px-1.5 py-0.5 text-[length:var(--exits-text-xs)] font-normal text-[var(--exits-info)]"
      data-testid="inventory-movement-sellable-after"
    >
      {t("inventory.sellableAfter")}: {formatSellableBalanceQty(sellableAfter)} {unitOfMeasure}
    </span>
  );
}

function MovementTypeCell({ movement }: { movement: PosStockMovementDto }) {
  const { t } = useI18n();
  const typeLabel = t(inventoryMovementTypeLabelKey(movement.movementType));
  const transferNumber = extractTransferReferenceNumber(movement);
  const { receiptId: directPurchaseId, label: directPurchaseLabel } =
    useDirectPurchaseMovementDisplayRef(movement);
  const damageHoldDecision = resolveDamageHoldDecisionDisplay(movement);

  return (
    <span
      className="inline-flex min-w-0 flex-col gap-0.5"
      data-testid={`inventory-movement-type-${movement.movementId}`}
    >
      <span className="text-muted">{typeLabel}</span>
      {transferNumber ? (
        <span
          className="min-w-0 truncate font-semibold text-primary"
          data-testid={`inventory-movement-transfer-ref-${movement.movementId}`}
        >
          {transferNumber}
        </span>
      ) : null}
      {directPurchaseLabel && directPurchaseId ? (
        <AppLinkWithReturn
          to={directPurchaseDetailPath(directPurchaseId)}
          className="min-w-0 truncate font-semibold text-primary underline underline-offset-2"
          data-testid={`inventory-movement-direct-purchase-ref-${movement.movementId}`}
          onClick={(event) => event.stopPropagation()}
          onKeyDown={(event) => event.stopPropagation()}
        >
          {directPurchaseLabel}
        </AppLinkWithReturn>
      ) : null}
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
                <p
                  className={cn(
                    "m-0 font-semibold",
                    quantityEffectColorClass(movement.quantityEffect),
                  )}
                  data-testid={`inventory-movement-qty-${movement.movementId}`}
                >
                  {formatQtyEffect(movement.quantityEffect, unitOfMeasure)}
                </p>
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
                      <dt className="text-muted">{t("inventory.movementUnitCost")}</dt>
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
                <div
                  className="mt-2 flex min-w-0 flex-col gap-0.5"
                  data-testid={`inventory-movement-actor-${movement.movementId}`}
                >
                  <span className="text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                    {formatMovementWhen(movement.recordedAtUtc)}
                  </span>
                  <ActorName
                    actorId={movement.recordedBy}
                    resolved={resolveActor(movement.recordedBy)}
                    isLoading={actorsLoading}
                    className="text-[length:var(--exits-text-sm)]"
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
        <table className="inventory-movements-table w-full min-w-[36rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="inventory-movements-table__head border-b border-border">
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-bold text-muted">
                {t("inventory.movementCol.when")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-bold text-muted">
                {t("inventory.movementCol.qty")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-bold text-muted">
                {t("inventory.movementCol.type")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-bold text-muted">
                {t("inventory.movementUnitCost")}
              </th>
              <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-bold text-muted">
                {t("inventory.movementStockValue")}
              </th>
            </tr>
          </thead>
          <tbody>
            {movements.map((movement) => {
              const stockValue = resolveMovementStockValue(movement);
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
                  <td
                    className="px-3 py-2.5"
                    data-testid={`inventory-movement-when-${movement.movementId}`}
                  >
                    <div className="flex min-w-0 flex-col gap-0.5">
                      <span className="whitespace-nowrap text-muted tabular-nums">
                        {formatMovementWhen(movement.recordedAtUtc)}
                      </span>
                      <ActorName
                        actorId={movement.recordedBy}
                        resolved={resolveActor(movement.recordedBy)}
                        isLoading={actorsLoading}
                        className="min-w-0 truncate"
                      />
                    </div>
                  </td>
                  <td className="px-3 py-2.5 font-semibold tabular-nums">
                    <div
                      className={cn(
                        "whitespace-nowrap",
                        quantityEffectColorClass(movement.quantityEffect),
                      )}
                      data-testid={`inventory-movement-qty-${movement.movementId}`}
                    >
                      {formatQtyEffect(movement.quantityEffect, unitOfMeasure)}
                    </div>
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
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

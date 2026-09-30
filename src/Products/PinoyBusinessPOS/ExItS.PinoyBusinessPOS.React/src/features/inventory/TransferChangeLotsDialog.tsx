import { useEffect, useMemo, useState } from "react";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import {
  allocateTransferLotsFefo,
  isTransferLotExpired,
  selectTransferPositiveOnHandLots,
  sortLotsForTransferFefo,
  sumTransferLotAllocationQty,
  type TransferEligibleLot,
  type TransferLotAllocationSlice,
} from "@/features/inventory/inventory-transfer-fefo-allocate";
import { cn } from "@/lib/cn";

type Translate = (key: string) => string;

export type TransferChangeLotsDialogProps = {
  open: boolean;
  productName: string;
  unitOfMeasure: string;
  /** Current line quantity — used as the target for Use FEFO reset. */
  transferQuantity: number;
  /** Hard cap for total allocation (branch / transferable available). */
  maxTransferableQuantity: number;
  lots: readonly PosInventoryLotDto[];
  initialAllocations: readonly TransferLotAllocationSlice[];
  onOpenChange: (open: boolean) => void;
  onApply: (allocations: TransferLotAllocationSlice[], mode: "auto" | "manual") => void;
  t: Translate;
};

function formatExpiry(isoDate: string): string {
  const date = new Date(`${isoDate}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return isoDate;
  }
  return date.toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

function toDraftMap(
  lotIds: readonly string[],
  initialAllocations: readonly TransferLotAllocationSlice[],
): Record<string, number> {
  const map: Record<string, number> = {};
  for (const lotId of lotIds) {
    map[lotId] = 0;
  }
  for (const row of initialAllocations) {
    map[row.lotId] = row.quantity;
  }
  return map;
}

function toAllocatableLots(lots: readonly PosInventoryLotDto[]): TransferEligibleLot[] {
  return selectTransferPositiveOnHandLots(lots).map((lot) => ({
    lotId: lot.lotId,
    lotNumber: lot.lotNumber ?? null,
    expirationDate: lot.expirationDate,
    quantityOnHand: lot.quantityOnHand,
  }));
}

/** Per-lot cap is lot on-hand only; line quantity follows the sum on Apply. */
export function maxAssignableTransferLotQty(lotQuantityOnHand: number): number {
  return Math.max(0, lotQuantityOnHand);
}

type DisplayLotRow = TransferEligibleLot & {
  expired: boolean;
};

/**
 * Manual lot override dialog for one logical transfer product.
 * Lot quantities are free within on-hand; Apply sets the line quantity to their sum.
 */
export function TransferChangeLotsDialog({
  open,
  productName,
  unitOfMeasure,
  transferQuantity,
  maxTransferableQuantity,
  lots,
  initialAllocations,
  onOpenChange,
  onApply,
  t,
}: TransferChangeLotsDialogProps) {
  const allocatable = useMemo(
    () => sortLotsForTransferFefo(toAllocatableLots(lots)),
    [lots],
  );

  const displayRows = useMemo((): DisplayLotRow[] => {
    const byId = new Map(lots.map((lot) => [lot.lotId, lot]));
    return allocatable.map((lot) => ({
      ...lot,
      expired: isTransferLotExpired(byId.get(lot.lotId) ?? {}),
    }));
  }, [allocatable, lots]);

  const [draftQtyByLot, setDraftQtyByLot] = useState<Record<string, number>>({});

  useEffect(() => {
    if (!open) {
      return;
    }
    setDraftQtyByLot(
      toDraftMap(
        allocatable.map((lot) => lot.lotId),
        initialAllocations,
      ),
    );
  }, [open, transferQuantity, allocatable, initialAllocations]);

  const allocated = sumTransferLotAllocationQty(
    Object.entries(draftQtyByLot).map(([lotId, quantity]) => ({ lotId, quantity })),
  );

  const lotOverLimit = allocatable.some(
    (lot) => (draftQtyByLot[lot.lotId] ?? 0) > lot.quantityOnHand + 1e-12,
  );
  const totalOverMax = allocated > maxTransferableQuantity + 1e-12;
  const hasNegative = Object.values(draftQtyByLot).some((qty) => qty < 0);
  const canApply =
    !hasNegative && !lotOverLimit && !totalOverMax && allocated > 0;

  function setLotQty(lotId: string, quantity: number) {
    setDraftQtyByLot((prev) => ({
      ...prev,
      [lotId]: quantity,
    }));
  }

  function useMaxForLot(lotId: string) {
    const lot = allocatable.find((row) => row.lotId === lotId);
    if (!lot) {
      return;
    }
    setLotQty(lotId, maxAssignableTransferLotQty(lot.quantityOnHand));
  }

  function buildSlices(): TransferLotAllocationSlice[] | null {
    const slices: TransferLotAllocationSlice[] = [];
    for (const lot of allocatable) {
      const quantity = draftQtyByLot[lot.lotId] ?? 0;
      if (!(quantity > 0)) {
        continue;
      }
      if (quantity > lot.quantityOnHand) {
        return null;
      }
      slices.push({
        lotId: lot.lotId,
        lotNumber: lot.lotNumber,
        expirationDate: lot.expirationDate,
        quantity,
        lotAvailableQuantity: lot.quantityOnHand,
      });
    }
    if (!(sumTransferLotAllocationQty(slices) > 0)) {
      return null;
    }
    return slices;
  }

  function applyFefo() {
    const target = transferQuantity > 0 ? transferQuantity : allocated;
    const result = allocateTransferLotsFefo(allocatable, target > 0 ? target : 0);
    if (!result.ok) {
      return;
    }
    setDraftQtyByLot(
      toDraftMap(
        allocatable.map((lot) => lot.lotId),
        result.allocations,
      ),
    );
  }

  function isCurrentDraftAuto(): boolean {
    const target = transferQuantity > 0 ? transferQuantity : allocated;
    const auto = allocateTransferLotsFefo(allocatable, target > 0 ? target : 0);
    if (!auto.ok) {
      return false;
    }
    if (auto.allocations.length !== Object.values(draftQtyByLot).filter((q) => q > 0).length) {
      return false;
    }
    return auto.allocations.every(
      (row) => (draftQtyByLot[row.lotId] ?? 0) === row.quantity,
    );
  }

  return (
    <ExitsModal
      open={open}
      onOpenChange={onOpenChange}
      title={t("transfer.selectStockLots")}
      closeLabel={t("transfer.cancelCreate")}
      testId="transfer-change-lots-dialog"
      size="lg"
      fullHeightOnCompact
      closeOnOutsideClick={false}
      className="lg:max-w-3xl"
    >
      <div className="flex flex-col gap-3" data-testid="transfer-change-lots">
        <div>
          <p className="m-0 text-[length:var(--exits-text-md)] font-semibold">{productName}</p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("transfer.lotTotalBecomesQuantity")
              .replace("{qty}", String(allocated))
              .replace("{uom}", unitOfMeasure)}
          </p>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[28rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
            <thead>
              <tr className="border-b border-border">
                <th className="px-2 py-2 font-bold text-muted">{t("transfer.expiry")}</th>
                <th className="px-2 py-2 font-bold text-muted">{t("transfer.batchLot")}</th>
                <th className="px-2 py-2 text-right font-bold text-muted">
                  {t("transfer.colAvailable")}
                </th>
                <th className="px-2 py-2 text-right font-bold text-muted">
                  {t("transfer.transferQtyCol")}
                </th>
              </tr>
            </thead>
            <tbody>
              {displayRows.map((lot) => {
                const qty = draftQtyByLot[lot.lotId] ?? 0;
                const maxAllowed = maxAssignableTransferLotQty(lot.quantityOnHand);
                const overLot = qty > maxAllowed + 1e-12;
                const availableLabel = `${lot.quantityOnHand} ${unitOfMeasure}`;
                const stepperMax = overLot ? qty : maxAllowed;
                return (
                  <tr
                    key={lot.lotId}
                    className="border-b border-border"
                    data-testid={`transfer-change-lot-row-${lot.lotId}`}
                    data-expired={lot.expired ? "true" : "false"}
                  >
                    <td className="px-2 py-2">
                      <div className="flex flex-col gap-0.5">
                        <span>{formatExpiry(lot.expirationDate)}</span>
                        {lot.expired ? (
                          <span
                            className="text-[length:var(--exits-text-xs)] text-destructive"
                            data-testid={`transfer-change-lot-expired-label-${lot.lotId}`}
                          >
                            {t("transfer.expired")}
                          </span>
                        ) : null}
                      </div>
                    </td>
                    <td className="px-2 py-2">{lot.lotNumber ?? "—"}</td>
                    <td className="px-2 py-2 text-right tabular-nums">
                      <button
                        type="button"
                        className={cn(
                          "underline-offset-2 hover:underline",
                          overLot ? "text-destructive" : "text-foreground",
                        )}
                        onClick={() => useMaxForLot(lot.lotId)}
                        data-testid={`transfer-change-lot-use-max-${lot.lotId}`}
                        aria-label={`${t("transfer.useMax")} ${availableLabel}`}
                      >
                        {availableLabel}
                      </button>
                    </td>
                    <td className="px-2 py-2">
                      <div className="flex flex-col items-end gap-1">
                        <QuantityStepper
                          compact
                          variant="auto"
                          value={qty}
                          min={0}
                          max={stepperMax}
                          precision={4}
                          step={1}
                          unitOfMeasure={unitOfMeasure}
                          sellingMode="PerItem"
                          invalid={overLot}
                          incrementDisabled={!overLot && qty >= maxAllowed - 1e-12}
                          decreaseLabel={t("transfer.decreaseQuantity")}
                          increaseLabel={t("transfer.increaseQuantity")}
                          ariaLabel={`${t("transfer.transferQtyCol")} ${lot.lotNumber ?? lot.lotId}`}
                          valueTestId={`transfer-change-lot-qty-${lot.lotId}`}
                          className={cn(overLot && "border-destructive")}
                          onChange={(next) => setLotQty(lot.lotId, Math.max(0, next))}
                        />
                        {overLot ? (
                          <p
                            className="m-0 max-w-[14rem] text-right text-[length:var(--exits-text-xs)] text-destructive"
                            data-testid={`transfer-change-lot-over-msg-${lot.lotId}`}
                          >
                            {t("transfer.lotExceedsAvailable")
                              .replace("{qty}", String(lot.quantityOnHand))
                              .replace("{uom}", unitOfMeasure)}{" "}
                            <button
                              type="button"
                              className="font-semibold underline underline-offset-2"
                              onClick={() => useMaxForLot(lot.lotId)}
                              data-testid={`transfer-change-lot-use-max-link-${lot.lotId}`}
                            >
                              {t("transfer.useMax")}
                            </button>
                          </p>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div className="text-[length:var(--exits-text-sm)]" data-testid="transfer-change-lots-totals">
          <p className="m-0 font-semibold" data-testid="transfer-change-lots-allocated">
            {t("transfer.allocated")}: {allocated} {unitOfMeasure}
          </p>
          {totalOverMax ? (
            <p className="m-0 text-destructive" data-testid="transfer-change-lots-over-available">
              {t("transfer.lotTotalExceedsAvailable")
                .replace("{qty}", String(maxTransferableQuantity))
                .replace("{uom}", unitOfMeasure)}
            </p>
          ) : (
            <p className="m-0 text-muted">
              {t("transfer.lotTotalHint")}
            </p>
          )}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2">
          <Button
            type="button"
            appearance="outline"
            onClick={applyFefo}
            data-testid="transfer-change-lots-use-fefo"
          >
            {t("transfer.useFefo")}
          </Button>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              appearance="ghost"
              onClick={() => onOpenChange(false)}
              data-testid="transfer-change-lots-cancel"
            >
              {t("transfer.cancelCreate")}
            </Button>
            <Button
              type="button"
              disabled={!canApply}
              onClick={() => {
                const slices = buildSlices();
                if (!slices) {
                  return;
                }
                onApply(slices, isCurrentDraftAuto() ? "auto" : "manual");
                onOpenChange(false);
              }}
              data-testid="transfer-change-lots-apply"
            >
              {t("transfer.applyLots")}
            </Button>
          </div>
        </div>
      </div>
    </ExitsModal>
  );
}

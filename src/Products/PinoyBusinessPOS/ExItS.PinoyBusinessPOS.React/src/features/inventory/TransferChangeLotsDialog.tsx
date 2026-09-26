import { useEffect, useMemo, useState } from "react";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import {
  allocateTransferLotsFefo,
  selectTransferEligibleLots,
  sortLotsForTransferFefo,
  sumTransferLotAllocationQty,
  type TransferLotAllocationSlice,
} from "@/features/inventory/inventory-transfer-fefo-allocate";
import { cn } from "@/lib/cn";

type Translate = (key: string) => string;

export type TransferChangeLotsDialogProps = {
  open: boolean;
  productName: string;
  unitOfMeasure: string;
  transferQuantity: number;
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
  eligibleLotIds: readonly string[],
  initialAllocations: readonly TransferLotAllocationSlice[],
): Record<string, number> {
  const map: Record<string, number> = {};
  for (const lotId of eligibleLotIds) {
    map[lotId] = 0;
  }
  for (const row of initialAllocations) {
    map[row.lotId] = row.quantity;
  }
  return map;
}

/**
 * Manual lot override dialog for one logical transfer product.
 */
export function TransferChangeLotsDialog({
  open,
  productName,
  unitOfMeasure,
  transferQuantity,
  lots,
  initialAllocations,
  onOpenChange,
  onApply,
  t,
}: TransferChangeLotsDialogProps) {
  const eligible = useMemo(
    () => sortLotsForTransferFefo(selectTransferEligibleLots(lots)),
    [lots],
  );

  const [draftQtyByLot, setDraftQtyByLot] = useState<Record<string, number>>({});

  useEffect(() => {
    if (!open) {
      return;
    }
    setDraftQtyByLot(
      toDraftMap(
        eligible.map((lot) => lot.lotId),
        initialAllocations,
      ),
    );
  }, [open, transferQuantity, eligible, initialAllocations]);

  const allocated = sumTransferLotAllocationQty(
    Object.entries(draftQtyByLot).map(([lotId, quantity]) => ({ lotId, quantity })),
  );
  const remaining = transferQuantity - allocated;
  const overBy = allocated > transferQuantity ? allocated - transferQuantity : 0;

  const lotOverLimit = eligible.some(
    (lot) => (draftQtyByLot[lot.lotId] ?? 0) > lot.quantityOnHand,
  );
  const hasNegative = Object.values(draftQtyByLot).some((qty) => qty < 0);
  const canApply =
    !hasNegative &&
    !lotOverLimit &&
    remaining === 0 &&
    allocated === transferQuantity &&
    transferQuantity > 0;

  function buildSlices(): TransferLotAllocationSlice[] | null {
    const slices: TransferLotAllocationSlice[] = [];
    for (const lot of eligible) {
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
    if (sumTransferLotAllocationQty(slices) !== transferQuantity) {
      return null;
    }
    return slices;
  }

  function applyFefo() {
    const result = allocateTransferLotsFefo(eligible, transferQuantity);
    if (!result.ok) {
      return;
    }
    setDraftQtyByLot(
      toDraftMap(
        eligible.map((lot) => lot.lotId),
        result.allocations,
      ),
    );
  }

  function isCurrentDraftAuto(): boolean {
    const auto = allocateTransferLotsFefo(eligible, transferQuantity);
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
      className="lg:max-w-3xl"
    >
      <div className="flex flex-col gap-3" data-testid="transfer-change-lots">
        <div>
          <p className="m-0 text-[length:var(--exits-text-md)] font-semibold">{productName}</p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("transfer.transferQuantity")}: {transferQuantity} {unitOfMeasure}
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
              {eligible.map((lot) => {
                const qty = draftQtyByLot[lot.lotId] ?? 0;
                const overLot = qty > lot.quantityOnHand;
                return (
                  <tr
                    key={lot.lotId}
                    className="border-b border-border"
                    data-testid={`transfer-change-lot-row-${lot.lotId}`}
                  >
                    <td className="px-2 py-2">{formatExpiry(lot.expirationDate)}</td>
                    <td className="px-2 py-2">{lot.lotNumber ?? "—"}</td>
                    <td className="px-2 py-2 text-right tabular-nums">
                      {lot.quantityOnHand} {unitOfMeasure}
                    </td>
                    <td className="px-2 py-2 text-right">
                      <input
                        type="number"
                        min={0}
                        step="any"
                        className={cn(
                          "exits-input w-24 text-right tabular-nums",
                          overLot && "border-destructive",
                        )}
                        value={Number.isFinite(qty) ? qty : 0}
                        onChange={(event) => {
                          const next = Number(event.target.value);
                          setDraftQtyByLot((prev) => ({
                            ...prev,
                            [lot.lotId]: Number.isFinite(next) ? next : 0,
                          }));
                        }}
                        data-testid={`transfer-change-lot-qty-${lot.lotId}`}
                      />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div className="text-[length:var(--exits-text-sm)]" data-testid="transfer-change-lots-totals">
          <p className="m-0">
            {t("transfer.allocated")}: {allocated} {unitOfMeasure}
          </p>
          {overBy > 0 ? (
            <p className="m-0 text-destructive">
              {t("transfer.overAllocatedBy")
                .replace("{qty}", String(overBy))
                .replace("{uom}", unitOfMeasure)}
            </p>
          ) : (
            <p className="m-0 text-muted">
              {t("transfer.remaining")}: {remaining} {unitOfMeasure}
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

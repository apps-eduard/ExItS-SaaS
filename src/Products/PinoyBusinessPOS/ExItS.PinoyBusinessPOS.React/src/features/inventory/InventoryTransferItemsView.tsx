import { Trash2 } from "lucide-react";
import type { TransferLotAllocationSlice } from "@/features/inventory/inventory-transfer-fefo-allocate";
import { Button } from "@/components/ui/button";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { cn } from "@/lib/cn";
import { roundMoneyAmount } from "@/lib/money-input";

export type InventoryTransferSelectedLine = {
  key: string;
  name: string;
  sku: string | null;
  quantity: number;
  unitOfMeasure: string;
  availableQuantity: number;
  maxQuantity: number;
  unitCost: number | null;
  tracksExpiration: boolean;
  allocationMode: "auto" | "manual";
  allocations: readonly TransferLotAllocationSlice[];
  hasIssue: boolean;
  onQtyChange: (next: number) => void;
  onRemove: () => void;
  onChangeLots: (() => void) | null;
};

type Translate = (key: string) => string;

type InventoryTransferItemsViewProps = {
  lines: readonly InventoryTransferSelectedLine[];
  formatAvailable: (qty: number, uom: string) => string;
  t: Translate;
};

function formatExpiryShort(isoDate: string): string {
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

function ExpiryLotCell({
  line,
  t,
  compact = false,
  includeTestIds = true,
}: {
  line: InventoryTransferSelectedLine;
  t: Translate;
  compact?: boolean;
  /** When false, omit test ids (mobile duplicate under product). */
  includeTestIds?: boolean;
}) {
  if (!line.tracksExpiration) {
    return (
      <span
        className="text-muted"
        data-testid={includeTestIds ? `transfer-line-expiry-lot-${line.key}` : undefined}
      >
        —
      </span>
    );
  }

  return (
    <div
      className={cn("flex flex-col gap-1", compact && "mt-2")}
      data-testid={includeTestIds ? `transfer-line-allocation-${line.key}` : undefined}
    >
      {line.allocations.length > 0 ? (
        line.allocations.map((slice) => (
          <div
            key={slice.lotId}
            className="text-[length:var(--exits-text-xs)] text-muted"
            data-testid={
              includeTestIds
                ? `transfer-line-alloc-slice-${line.key}-${slice.lotId}`
                : undefined
            }
          >
            {slice.quantity} {line.unitOfMeasure} · {formatExpiryShort(slice.expirationDate)}
            {slice.lotNumber ? ` · ${slice.lotNumber}` : ""}
          </div>
        ))
      ) : (
        <span className="text-[length:var(--exits-text-xs)] text-muted">—</span>
      )}
      <div className="mt-0.5 flex flex-wrap items-center gap-2">
        <span
          className="text-[length:var(--exits-text-xs)] text-muted"
          data-testid={includeTestIds ? `transfer-line-expiry-mode-${line.key}` : undefined}
        >
          {line.allocationMode === "auto"
            ? t("transfer.fefoAutomaticallySelected")
            : t("transfer.manualLotAllocation")}
        </span>
        {line.onChangeLots ? (
          <Button
            type="button"
            appearance="outline"
            onClick={() => line.onChangeLots?.()}
            data-testid={includeTestIds ? `transfer-change-lots-${line.key}` : undefined}
          >
            {t("transfer.changeLots")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}

/**
 * Transfer create selected lines — Product · Available · Expiry/Lot · Unit cost · Total · Qty · Action.
 */
export function InventoryTransferItemsView({
  lines,
  formatAvailable,
  t,
}: InventoryTransferItemsViewProps) {
  const tableBody = (
    <ExitsTableContainer className="po-order-items-table transfer-order-items-table">
      <ExitsTable>
        <ExitsTableHeader>
          <ExitsTableRow>
            <ExitsTableHead
              cellAlign="text"
              colSize="flex"
              className="po-order-items-table__product-col"
            >
              {t("purchasing.colProduct")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="po-order-items-table__available-col">
              {t("transfer.colAvailable")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="text" className="transfer-order-items-table__expiry-col">
              {t("transfer.colExpiryLot")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="numeric" className="po-order-items-table__price-col">
              {t("purchasing.unitCost")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="numeric" className="po-order-items-table__total-col">
              {t("purchasing.totalCost")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="po-order-items-table__qty-col">
              {t("purchasing.qty")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="po-order-items-table__action-col">
              {t("purchasing.colAction")}
            </ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {lines.map((line) => {
            const maxQty = line.maxQuantity;
            const outOfStock = maxQty <= 0;
            const canDecrease = line.quantity > 1;
            const canIncrease = line.quantity < maxQty;
            const availableLabel = outOfStock
              ? t("transfer.outOfStock")
              : formatAvailable(maxQty, line.unitOfMeasure);
            const unitCost = line.unitCost != null && line.unitCost > 0 ? line.unitCost : null;
            const lineTotal =
              unitCost != null && line.quantity > 0
                ? roundMoneyAmount(line.quantity * unitCost)
                : null;

            return (
              <ExitsTableRow key={line.key} data-testid={`transfer-line-${line.key}`}>
                <ExitsTableCell
                  cellAlign="text"
                  colSize="flex"
                  className="po-order-items-table__product-col"
                >
                  <div className="font-medium leading-snug po-order-items-table__product-name">
                    {line.name}
                  </div>
                  {line.sku ? (
                    <div className="text-[length:var(--exits-text-xs)] text-muted">{line.sku}</div>
                  ) : null}
                  {line.tracksExpiration ? (
                    <div
                      className="mt-1 text-[length:var(--exits-text-xs)] text-muted"
                      data-testid={`transfer-line-tracks-expiry-${line.key}`}
                    >
                      {t("transfer.tracksExpiry")}
                    </div>
                  ) : null}
                  <div className="po-order-items-table__product-available-mobile">
                    <span
                      className={cn(
                        "po-order-items__available-readout",
                        (outOfStock || line.hasIssue) &&
                          "po-order-items__available-readout--zero",
                      )}
                    >
                      <span
                        className={cn(
                          "po-order-items__available-qty tabular-nums",
                          (outOfStock || line.hasIssue) && "text-destructive",
                        )}
                      >
                        {outOfStock ? 0 : maxQty}
                      </span>
                      {line.unitOfMeasure ? (
                        <span className="po-order-items__available-unit">
                          {line.unitOfMeasure}
                        </span>
                      ) : null}
                    </span>
                  </div>
                  <div className="transfer-order-items-table__expiry-mobile">
                    <ExpiryLotCell line={line} t={t} compact includeTestIds={false} />
                  </div>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__available-col">
                  <span
                    className={cn(
                      "po-order-items__available-readout",
                      (outOfStock || line.hasIssue) &&
                        "po-order-items__available-readout--zero",
                    )}
                    data-testid={`transfer-line-available-${line.key}`}
                  >
                    {outOfStock ? (
                      <span className="po-order-items__available-qty tabular-nums text-destructive">
                        {availableLabel}
                      </span>
                    ) : (
                      <>
                        <span
                          className={cn(
                            "po-order-items__available-qty tabular-nums",
                            line.hasIssue && "text-destructive",
                          )}
                        >
                          {maxQty}
                        </span>
                        {line.unitOfMeasure ? (
                          <span className="po-order-items__available-unit">
                            {line.unitOfMeasure}
                          </span>
                        ) : null}
                      </>
                    )}
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="text" className="transfer-order-items-table__expiry-col">
                  <ExpiryLotCell line={line} t={t} />
                </ExitsTableCell>
                <ExitsTableCell cellAlign="numeric" className="po-order-items-table__price-col">
                  <span
                    className="tabular-nums"
                    data-testid={`transfer-line-unit-cost-${line.key}`}
                  >
                    {unitCost != null ? <MoneyDisplay amount={unitCost} /> : "—"}
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="numeric" className="po-order-items-table__total-col">
                  <span
                    className="tabular-nums font-semibold"
                    data-testid={`transfer-line-total-cost-${line.key}`}
                  >
                    {lineTotal != null ? <MoneyDisplay amount={lineTotal} /> : "—"}
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__qty-col">
                  <div className="po-order-items__qty-stack">
                    <QuantityStepper
                      compact
                      variant="auto"
                      editOnClick
                      value={line.quantity}
                      min={0}
                      precision={4}
                      step={1}
                      unitOfMeasure={line.unitOfMeasure}
                      sellingMode="PerItem"
                      invalid={Boolean(line.hasIssue) || !(line.quantity > 0)}
                      decreaseLabel={t("transfer.decreaseQuantity")}
                      increaseLabel={t("transfer.increaseQuantity")}
                      incrementDisabled={!canIncrease}
                      decrementDisabled={!canDecrease}
                      ariaLabel={t("transfer.quantity")}
                      valueTestId={`transfer-line-qty-${line.key}`}
                      className="po-order-items__qty-stepper"
                      onChange={(next) => line.onQtyChange(next)}
                    />
                    {line.unitOfMeasure ? (
                      <span className="po-order-items__qty-unit">{line.unitOfMeasure}</span>
                    ) : null}
                  </div>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__action-col">
                  <ExitsTableActions className="po-order-items__row-actions justify-center">
                    <Button
                      type="button"
                      intent="danger"
                      appearance="outline"
                      size="icon"
                      aria-label={t("transfer.remove")}
                      onClick={() => line.onRemove()}
                      data-testid={`transfer-remove-${line.key}`}
                    >
                      <Trash2 className="size-4" aria-hidden />
                    </Button>
                  </ExitsTableActions>
                </ExitsTableCell>
              </ExitsTableRow>
            );
          })}
        </ExitsTableBody>
      </ExitsTable>
    </ExitsTableContainer>
  );

  return (
    <ExitsResponsiveDataView
      layout="table"
      testId="transfer-order-items"
      className={cn("po-order-items-responsive")}
      table={tableBody}
      list={null}
    />
  );
}

import { Trash2 } from "lucide-react";
import { isByWeightSellingMode } from "@/cart/sell-cart-helpers";
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

export type RequestStockSelectedLine = {
  key: string;
  name: string;
  sku: string | null;
  quantity: number;
  unitOfMeasure: string;
  sellingMode: string;
  maxQuantity: number;
  unitCost: number | null;
  hasIssue: boolean;
  onQtyChange: (next: number) => void;
  onRemove: () => void;
};

type Translate = (key: string) => string;

type RequestStockItemsViewProps = {
  lines: readonly RequestStockSelectedLine[];
  formatAvailable: (qty: number, uom: string) => string;
  t: Translate;
};

/**
 * Request stock selected lines — mirrors transfer create table without lot/FEFO columns.
 */
export function RequestStockItemsView({
  lines,
  formatAvailable,
  t,
}: RequestStockItemsViewProps) {
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
              {t("retailWarehouse.request.colWarehouseAvailable")}
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
            const byWeight = isByWeightSellingMode(line.sellingMode);
            const canDecrease = line.quantity > 1e-9;
            const canIncrease = line.quantity < maxQty;
            const availableLabel = outOfStock
              ? t("retailWarehouse.request.warehouseOutOfStock")
              : formatAvailable(maxQty, line.unitOfMeasure);
            const unitCost = line.unitCost != null && line.unitCost > 0 ? line.unitCost : null;
            const lineTotal =
              unitCost != null && line.quantity > 0
                ? roundMoneyAmount(line.quantity * unitCost)
                : null;

            return (
              <ExitsTableRow key={line.key} data-testid={`request-line-${line.key}`}>
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
                  {byWeight ? (
                    <div className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                      {t("sell.tileByWeight")}
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
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__available-col">
                  <span
                    className={cn(
                      "po-order-items__available-readout",
                      (outOfStock || line.hasIssue) &&
                        "po-order-items__available-readout--zero",
                    )}
                    data-testid={`request-line-available-${line.key}`}
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
                <ExitsTableCell cellAlign="numeric" className="po-order-items-table__price-col">
                  <span className="tabular-nums" data-testid={`request-line-cost-${line.key}`}>
                    {unitCost != null ? <MoneyDisplay amount={unitCost} /> : "—"}
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="numeric" className="po-order-items-table__total-col">
                  <span
                    className="tabular-nums font-semibold"
                    data-testid={`request-line-total-${line.key}`}
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
                      decreaseLabel={t("retailWarehouse.request.decrease")}
                      increaseLabel={t("retailWarehouse.request.increase")}
                      incrementDisabled={!canIncrease}
                      decrementDisabled={!canDecrease}
                      ariaLabel={t("purchasing.qty")}
                      valueTestId={`request-line-qty-${line.key}`}
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
                      aria-label={t("retailWarehouse.request.remove")}
                      onClick={() => line.onRemove()}
                      data-testid={`request-line-remove-${line.key}`}
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
      testId="request-order-items"
      className={cn("po-order-items-responsive")}
      table={tableBody}
      list={null}
    />
  );
}

import { Trash2 } from "lucide-react";
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
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { cn } from "@/lib/cn";

export type InventoryTransferSelectedLine = {
  key: string;
  name: string;
  quantity: number;
  unitOfMeasure: string;
  availableQuantity: number;
  lotAvailableQuantity: number | null;
  lotNumber: string | null;
  expirationDate: string | null;
  hasIssue: boolean;
  onQtyChange: (next: number) => void;
  onRemove: () => void;
};

type Translate = (key: string) => string;

type InventoryTransferItemsViewProps = {
  lines: readonly InventoryTransferSelectedLine[];
  formatAvailable: (qty: number, uom: string) => string;
  t: Translate;
};

/**
 * Transfer create selected lines — same ExitsTable chrome as purchase create
 * (Product · Available · Qty · Action). Always table presentation.
 */
export function InventoryTransferItemsView({
  lines,
  formatAvailable,
  t,
}: InventoryTransferItemsViewProps) {
  const tableBody = (
    <ExitsTableContainer className="po-order-items-table">
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
            const maxQty =
              line.lotAvailableQuantity != null
                ? Math.min(line.availableQuantity, line.lotAvailableQuantity)
                : line.availableQuantity;
            const outOfStock = maxQty <= 0;
            const canDecrease = line.quantity > 1;
            const canIncrease = line.quantity < maxQty;
            const availableLabel = outOfStock
              ? t("transfer.outOfStock")
              : formatAvailable(maxQty, line.unitOfMeasure);
            const lotMeta =
              line.lotNumber || line.expirationDate
                ? `${t("transfer.lot")}: ${line.lotNumber ?? "—"} · ${t("transfer.expiry")}: ${line.expirationDate ?? "—"}`
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
                  {lotMeta ? (
                    <div className="text-[length:var(--exits-text-xs)] text-muted">{lotMeta}</div>
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

import { useState } from "react";
import { ShoppingCart, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  formatQuantityDisplay,
  isByWeightSellingMode,
} from "@/cart/sell-cart-helpers";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { estimateLineCost } from "@/features/warehouse/retail-warehouse-request-math";
import { remainingWarehouseAvailable } from "@/features/warehouse/retail-warehouse-request-availability";
import { requestStockDisplayUom } from "@/features/warehouse/RequestStockProductCard";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";

export type RequestStockBasketLine = {
  productId: string;
  name: string;
  sku?: string | null;
  unitOfMeasure: string;
  sellingMode: string;
  quantity: number;
  branchOnHandQuantity: number;
  warehouseAvailableQuantity: number;
  warehouseUnitCost: number | null;
};

type RequestStockCartPanelProps = {
  lines: RequestStockBasketLine[];
  productCount: number;
  estimatedCostTotal: number | null;
  requestNotes: string;
  onRequestNotesChange: (value: string) => void;
  onIncrement: (productId: string) => void;
  onDecrement: (productId: string) => void;
  onRemove: (productId: string) => void;
  onEditWeight: (line: RequestStockBasketLine) => void;
  onSubmit: () => void;
  submitPending?: boolean;
  submitError?: boolean;
  submitBlocked?: boolean;
  lineWarnings?: ReadonlyMap<string, string>;
  warehouseName?: string;
  showClose?: boolean;
  onClose?: () => void;
  panelId?: string;
};

function cartLineInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

/**
 * SellCartPanel layout twin for Request stock — same shell/lines/footer chrome,
 * different total label and primary action.
 */
export function RequestStockCartPanel({
  lines,
  productCount,
  estimatedCostTotal,
  requestNotes,
  onRequestNotesChange,
  onIncrement,
  onDecrement,
  onRemove,
  onEditWeight,
  onSubmit,
  submitPending = false,
  submitError = false,
  submitBlocked = false,
  lineWarnings,
  warehouseName: _warehouseName,
  showClose = false,
  onClose,
  panelId = "request-cart",
}: RequestStockCartPanelProps) {
  const { t } = useI18n();
  const [notesOpen, setNotesOpen] = useState(() => requestNotes.trim().length > 0);
  const submitEnabled = lines.length > 0 && !submitPending && !submitBlocked;
  const costAmount = estimatedCostTotal ?? 0;

  return (
    <div
      className="sell-cart-panel flex min-h-0 flex-1 flex-col overflow-hidden"
      data-testid="retail-warehouse-basket"
    >
      <div
        className="sell-cart-panel__header flex shrink-0 items-start justify-between gap-2"
        data-testid="retail-warehouse-basket-header"
      >
        <div className="sell-cart-panel__title min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("retailWarehouse.request.cartLabel")}
          </h2>
          {lines.length > 0 ? (
            <p
              className="sell-cart-panel__meta m-0 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="retail-warehouse-products-count"
            >
              {t("retailWarehouse.request.productsCount").replace(
                "{count}",
                String(productCount),
              )}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 items-center gap-0.5">
          {showClose && onClose ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-8 px-2 text-[length:var(--exits-text-xs)]"
              aria-label={t("sell.cartSheetClose")}
              data-testid="retail-warehouse-cart-sheet-close"
              onClick={onClose}
            >
              {t("sell.cartSheetClose")}
            </Button>
          ) : null}
        </div>
      </div>

      {lines.length === 0 ? (
        <div className="sell-cart-empty flex flex-1 flex-col items-center justify-center gap-1 px-2 py-6 text-center">
          <ShoppingCart className="size-5 text-muted" strokeWidth={1.75} aria-hidden />
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("retailWarehouse.request.cartEmptyDetail")}
          </p>
        </div>
      ) : (
        <ul
          className="sell-cart-lines m-0 min-h-0 flex-1 list-none overflow-y-auto p-0"
          data-testid="retail-warehouse-basket-lines"
        >
          {lines.map((line) => {
            const byWeight = isByWeightSellingMode(line.sellingMode);
            const uom = requestStockDisplayUom(line.sellingMode, line.unitOfMeasure);
            const qtyLabel = formatQuantityDisplay(line.quantity);
            const lineCost = estimateLineCost(line.quantity, line.warehouseUnitCost);
            const unitCostLabel =
              line.warehouseUnitCost != null
                ? `${formatPeso(line.warehouseUnitCost)}/${uom}`
                : null;
            const atMax = line.quantity >= line.warehouseAvailableQuantity - 1e-9;
            const warning = lineWarnings?.get(line.productId) ?? null;
            const remaining = remainingWarehouseAvailable(
              line.warehouseAvailableQuantity,
              line.quantity,
            );

            return (
              <li
                key={line.productId}
                className="sell-cart-line sell-cart-line--enter"
                data-testid={`retail-warehouse-basket-line-${line.productId}`}
                data-stock-invalid={warning ? "true" : undefined}
              >
                <div className="sell-cart-line__media" aria-hidden>
                  <span className="sell-cart-line__initial">{cartLineInitial(line.name)}</span>
                </div>
                <div className="sell-cart-line__body">
                  <div className="sell-cart-line__top">
                    <p className="sell-cart-line__name">{line.name}</p>
                    <div className="sell-cart-line__price-actions">
                      {lineCost != null ? (
                        <MoneyDisplay
                          amount={lineCost}
                          className="sell-cart-line__amount"
                          testId={`retail-warehouse-line-cost-${line.productId}`}
                        />
                      ) : (
                        <span className="sell-cart-line__amount text-muted">—</span>
                      )}
                      <Button
                        type="button"
                        variant="ghost"
                        className="sell-cart-line__remove"
                        aria-label={t("retailWarehouse.request.remove")}
                        data-testid={`retail-warehouse-basket-remove-${line.productId}`}
                        onClick={() => onRemove(line.productId)}
                      >
                        <Trash2 className="size-4" aria-hidden strokeWidth={2} />
                      </Button>
                    </div>
                  </div>

                  <div className="sell-cart-line__bottom">
                    <span className="sell-cart-line__meta">
                      {qtyLabel} {uom}
                      {unitCostLabel ? (
                        <span className="sell-cart-line__unit-price">
                          {" "}
                          · {unitCostLabel}
                        </span>
                      ) : null}
                    </span>

                    {byWeight ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="sell-cart-line__edit"
                        data-testid={`retail-warehouse-edit-weight-${line.productId}`}
                        onClick={() => onEditWeight(line)}
                      >
                        {qtyLabel} {uom}
                      </Button>
                    ) : (
                      <div className="sell-cart-line__qty">
                        <QuantityStepper
                          compact
                          value={qtyLabel}
                          valueTestId={`retail-warehouse-qty-${line.productId}`}
                          decreaseLabel={t("retailWarehouse.request.decrease")}
                          increaseLabel={t("retailWarehouse.request.increase")}
                          onDecrement={() => onDecrement(line.productId)}
                          onIncrement={() => onIncrement(line.productId)}
                          decrementDisabled={line.quantity <= 1}
                          incrementDisabled={atMax}
                        />
                      </div>
                    )}
                  </div>
                  {warning ? (
                    <p
                      role="alert"
                      className="sell-cart-line__stock"
                      data-testid={`retail-warehouse-line-warning-${line.productId}`}
                    >
                      {warning}
                    </p>
                  ) : line.warehouseAvailableQuantity > 0 ? (
                    <p
                      className="sell-cart-line__stock m-0 text-[length:var(--exits-text-xs)] text-muted"
                      data-testid={`retail-warehouse-line-remaining-${line.productId}`}
                      data-warehouse-actual={line.warehouseAvailableQuantity}
                      data-warehouse-remaining={remaining}
                      title={t("retailWarehouse.request.remainingTooltip")}
                    >
                      {t("retailWarehouse.request.remainingCompact")
                        .replace("{qty}", formatQuantityDisplay(remaining))
                        .replace("{uom}", uom)}
                    </p>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div
        className="sell-cart-footer mt-auto flex shrink-0 flex-col gap-2"
        data-testid="retail-warehouse-basket-footer"
      >
        {!notesOpen ? (
          <button
            type="button"
            className="self-start px-0 text-[length:var(--exits-text-xs)] font-medium text-muted underline-offset-2 hover:underline"
            data-testid="retail-warehouse-add-note"
            onClick={() => setNotesOpen(true)}
          >
            {t("retailWarehouse.request.addNote")}
          </button>
        ) : (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-xs)]">
            <span className="text-muted">{t("stockRequest.notes")}</span>
            <textarea
              className="exits-input min-h-[2.25rem] resize-y text-[length:var(--exits-text-sm)]"
              rows={2}
              value={requestNotes}
              onChange={(e) => onRequestNotesChange(e.target.value)}
              data-testid="retail-warehouse-request-notes"
              id={`${panelId}-notes`}
            />
          </label>
        )}

        <div
          className="sell-cart-footer__total-row flex items-baseline justify-between gap-3"
          data-testid="retail-warehouse-footer-total"
        >
          <span className="sell-cart-footer__total-label text-[length:var(--exits-text-sm)] font-semibold">
            {t("retailWarehouse.request.estimatedWarehouseCost")}
          </span>
          <MoneyDisplay
            amount={costAmount}
            className="sell-cart-footer__total-amount text-[length:var(--exits-text-md)] font-bold"
            testId="retail-warehouse-estimated-cost"
          />
        </div>

        <Button
          type="button"
          className={cn(
            "sell-cart-pay w-full",
            submitEnabled ? "sell-cart-pay--ready" : "sell-cart-pay--disabled",
          )}
          disabled={!submitEnabled}
          onClick={onSubmit}
          data-testid="retail-warehouse-submit"
        >
          {t("stockRequest.submit")}
        </Button>
        {submitError ? (
          <p className="text-danger m-0 text-center text-[length:var(--exits-text-sm)]">
            {t("stockRequest.submitError")}
          </p>
        ) : null}
      </div>
    </div>
  );
}

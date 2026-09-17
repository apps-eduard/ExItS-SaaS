import { useEffect, useMemo, useState } from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  requiresWholeEnteredQuantity,
  resolveSellUnitPrice,
  resolveSellCardStock,
  roundMoney,
  roundQuantity,
} from "@/cart/sell-cart-helpers";
import { sellStockCaption } from "@/features/sell/sell-stock-caption";
import { useI18n } from "@/i18n/I18nProvider";

type SellUnitEntryDialogProps = {
  open: boolean;
  product: PosCatalogProductDto | null;
  options: PosCatalogProductUnitDto[];
  initialUnitId?: string | null;
  initialQuantity?: number;
  stockHint?: {
    isTracked?: boolean;
    onHandQuantity?: number | null;
    sellableQuantity?: number | null;
    tracksExpiration?: boolean;
    stockStatus?: string | null;
    isLowStock?: boolean | null;
  } | null;
  stockError?: string | null;
  onConfirm: (unit: PosCatalogProductUnitDto, quantity: number) => void;
  onCancel: () => void;
};

export function SellUnitEntryDialog({
  open,
  product,
  options,
  initialUnitId,
  initialQuantity = 1,
  stockHint,
  stockError = null,
  onConfirm,
  onCancel,
}: SellUnitEntryDialogProps) {
  const { t } = useI18n();
  const [selectedUnitId, setSelectedUnitId] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    if (open && !opened && product && options.length > 0) {
      const initial = options.find((unit) => unit.unitId === initialUnitId) ?? options[0]!;
      setSelectedUnitId(initial.unitId);
      setQuantity(initialQuantity > 0 ? roundQuantity(initialQuantity) : 1);
      setOpened(true);
    } else if (!open) {
      setOpened(false);
    }
  }, [initialQuantity, initialUnitId, open, opened, options, product]);

  const selectedUnit = useMemo(
    () => options.find((unit) => unit.unitId === selectedUnitId) ?? null,
    [options, selectedUnitId],
  );

  const unitPrice = product && selectedUnit ? resolveSellUnitPrice(product, selectedUnit) : 0;
  const subtotal = roundMoney(unitPrice * quantity);
  const whole = selectedUnit ? requiresWholeEnteredQuantity(selectedUnit) : true;
  const step = whole ? 1 : 0.001;
  const stock = product
    ? resolveSellCardStock({
        isTracked: stockHint?.isTracked ?? product.isTracked,
        onHandQuantity: stockHint?.onHandQuantity ?? product.onHandQuantity,
        unitOfMeasure: product.unitOfMeasure,
        tracksExpiration: stockHint?.tracksExpiration ?? product.tracksExpiration,
        sellableQuantity: stockHint?.sellableQuantity,
        stockStatus: stockHint?.stockStatus ?? product.stockStatus,
        isLowStock: stockHint?.isLowStock,
      })
    : null;

  if (!open || !product || !selectedUnit) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onCancel}
      data-testid="sell-unit-entry-backdrop"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="sell-unit-entry-title"
        data-testid="sell-unit-entry"
        className="flex w-full max-w-md max-h-[85dvh] flex-col gap-3 overflow-y-auto rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <h2
          id="sell-unit-entry-title"
          className="m-0 text-[length:var(--exits-text-md)] font-semibold"
        >
          {t("sell.sellAsTitle")}
        </h2>
        <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
          {product.name}
        </p>
        {stock ? (
          <p
            data-testid="sell-stock-hint"
            className={`m-0 text-[length:var(--exits-text-xs)] text-muted sell-product-card__stock--${stock.tone}`}
          >
            {sellStockCaption(t, stock)}
          </p>
        ) : null}

        <div role="group" aria-label={t("sell.sellAsTitle")} className="flex flex-col gap-2">
          {options.map((unit) => {
            const price = resolveSellUnitPrice(product, unit);
            const selected = unit.unitId === selectedUnit.unitId;
            return (
              <button
                key={unit.unitId}
                type="button"
                data-testid={`sell-unit-option-${unit.unitId}`}
                aria-pressed={selected}
                className={`flex flex-col items-start gap-1 rounded-[var(--exits-radius-md)] border p-3 text-left ${
                  selected ? "border-primary bg-[var(--exits-surface-muted)]" : "border-border"
                }`}
                onClick={() => {
                  setSelectedUnitId(unit.unitId);
                  if (requiresWholeEnteredQuantity(unit) && quantity !== Math.trunc(quantity)) {
                    setQuantity(Math.max(1, Math.trunc(quantity)));
                  }
                }}
              >
                <span className="text-[length:var(--exits-text-sm)] font-semibold">
                  {unit.displayName}
                </span>
                <span className="text-[length:var(--exits-text-xs)] text-muted">
                  <MoneyDisplay amount={price} className="font-normal" /> / {unit.displayName}
                </span>
                {unit.multiplierToBase !== 1 ? (
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {t("sell.unitEqualsBase")
                      .replace("{unit}", unit.displayName)
                      .replace("{multiplier}", formatQuantityDisplay(unit.multiplierToBase))
                      .replace("{base}", product.unitOfMeasure)}
                  </span>
                ) : null}
              </button>
            );
          })}
        </div>

        <div className="flex items-center justify-between gap-2">
          <span className="text-[length:var(--exits-text-sm)]">{t("sell.quantityLabel")}</span>
          <QuantityStepper
            value={formatQuantityDisplay(quantity)}
            valueTestId="sell-unit-qty"
            decreaseLabel={t("sell.cartDecrease")}
            increaseLabel={t("sell.cartIncrease")}
            onDecrement={() => setQuantity((current) => roundQuantity(Math.max(0, current - step)))}
            onIncrement={() => setQuantity((current) => roundQuantity(current + step))}
          />
        </div>

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("sell.quantityDirect")}
          <input
            data-testid="sell-unit-qty-input"
            type="number"
            inputMode="decimal"
            min={whole ? 1 : step}
            step={step}
            value={quantity}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
            onChange={(event) => {
              const next = Number(event.target.value);
              if (!Number.isFinite(next)) {
                return;
              }
              setQuantity(whole ? Math.max(0, Math.trunc(next)) : Math.max(0, roundQuantity(next)));
            }}
          />
        </label>

        <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="sell-unit-subtotal">
          {t("sell.cartSubtotalLabel")}: <MoneyDisplay amount={subtotal} />
        </p>

        {stockError ? (
          <p
            role="alert"
            data-testid="sell-stock-error"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {stockError}
          </p>
        ) : null}

        <div className="flex flex-wrap justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onCancel}>
            {t("sell.cancel")}
          </Button>
          <Button
            type="button"
            data-testid="sell-unit-add"
            disabled={!(quantity > 0)}
            onClick={() => onConfirm(selectedUnit, quantity)}
          >
            {t("sell.addToCart")}
          </Button>
        </div>
      </div>
    </div>
  );
}

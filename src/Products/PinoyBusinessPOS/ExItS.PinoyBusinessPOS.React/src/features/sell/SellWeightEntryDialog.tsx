import { useEffect, useMemo, useState } from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  normalizeWeightToKilograms,
  resolveSellUnitPrice,
  resolveSellCardStock,
  roundMoney,
  type WeightInputUnit,
} from "@/cart/sell-cart-helpers";
import { sellStockCaption } from "@/features/sell/sell-stock-caption";
import { formatSellLinePreview } from "@/features/sell/format-sell-line-preview";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";

type SellWeightEntryDialogProps = {
  open: boolean;
  product: PosCatalogProductDto | null;
  unit?: PosCatalogProductUnitDto | null;
  initialKilograms?: number | null;
  stockHint?: {
    isTracked?: boolean;
    onHandQuantity?: number | null;
    sellableQuantity?: number | null;
    tracksExpiration?: boolean;
    stockStatus?: string | null;
    isLowStock?: boolean | null;
  } | null;
  stockError?: string | null;
  /** When set, weight above this (kg) cannot be confirmed. */
  maxKilograms?: number | null;
  maxAvailableLabel?: string | null;
  /** Override "Add to cart" when reused outside Sell (e.g. Request stock). */
  confirmAddLabel?: string;
  onConfirm: (kilograms: number) => void;
  onRemove?: () => void;
  onCancel: () => void;
};

export function SellWeightEntryDialog({
  open,
  product,
  unit = null,
  initialKilograms = null,
  stockHint,
  stockError = null,
  maxKilograms = null,
  maxAvailableLabel = null,
  confirmAddLabel,
  onConfirm,
  onRemove,
  onCancel,
}: SellWeightEntryDialogProps) {
  const { t } = useI18n();
  const [rawValue, setRawValue] = useState("");
  const [unitCode, setUnitCode] = useState<WeightInputUnit>("kg");
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    if (open && !opened) {
      const kg = initialKilograms != null && initialKilograms > 0 ? initialKilograms : null;
      setUnitCode("kg");
      setRawValue(kg != null ? formatQuantityDisplay(kg) : "");
      setOpened(true);
    } else if (!open) {
      setOpened(false);
    }
  }, [initialKilograms, open, opened]);

  const unitPrice = product ? resolveSellUnitPrice(product, unit) : 0;
  const editing = initialKilograms != null && initialKilograms > 0;

  const parsed = useMemo(() => {
    const raw = Number(rawValue);
    if (!Number.isFinite(raw)) {
      return null;
    }
    return normalizeWeightToKilograms(raw, unitCode);
  }, [rawValue, unitCode]);

  const kilograms = parsed && "kilograms" in parsed ? parsed.kilograms : null;
  const errorCode = parsed && "error" in parsed ? parsed.error : null;
  const preview = kilograms != null ? roundMoney(unitPrice * kilograms) : null;
  const maxKg =
    maxKilograms != null && Number.isFinite(maxKilograms) && maxKilograms >= 0
      ? maxKilograms
      : null;
  const overMax =
    kilograms != null && maxKg != null && kilograms > maxKg + 1e-9;

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

  if (!open || !product) {
    return null;
  }

  const errorMessage =
    errorCode === "zero"
      ? t("sell.weightErrorZero")
      : errorCode === "precision"
        ? t("sell.weightErrorPrecision")
        : errorCode === "invalid" || errorCode === "unit"
          ? t("sell.weightErrorInvalid")
          : overMax && maxKg != null
            ? t("retailWarehouse.request.onlyAvailable")
                .replace("{qty}", formatQuantityDisplay(maxKg))
                .replace("{uom}", "kg")
            : null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onCancel}
      data-testid="sell-weight-entry-backdrop"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="sell-weight-entry-title"
        data-testid="sell-weight-entry"
        className="flex w-full max-w-md flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <h2
          id="sell-weight-entry-title"
          className="m-0 text-[length:var(--exits-text-md)] font-semibold"
        >
          {editing ? t("sell.weightEditTitle") : t("sell.weightAddTitle")}
        </h2>
        <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
          {product.name}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          <MoneyDisplay amount={unitPrice} className="font-normal" /> {t("sell.pricePerKg")}
        </p>
        {stock ? (
          <p
            data-testid="sell-stock-hint"
            className={`m-0 text-[length:var(--exits-text-xs)] text-muted sell-product-card__stock--${stock.tone}`}
          >
            {sellStockCaption(t, stock)}
          </p>
        ) : null}
        {maxAvailableLabel ? (
          <p
            data-testid="sell-weight-max-available"
            className="m-0 text-[length:var(--exits-text-xs)] text-muted"
          >
            {maxAvailableLabel}
          </p>
        ) : null}

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("sell.weightQuantity")}
          <input
            data-testid="sell-weight-input"
            type="number"
            inputMode="decimal"
            autoFocus
            min={unitCode === "g" ? 1 : 0.001}
            step={unitCode === "g" ? 1 : 0.001}
            value={rawValue}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
            onChange={(event) => setRawValue(event.target.value)}
          />
        </label>

        <fieldset className="m-0 border-0 p-0">
          <legend className="mb-1 text-[length:var(--exits-text-sm)]">
            {t("sell.weightUnit")}
          </legend>
          <div className="flex gap-2" role="radiogroup" aria-label={t("sell.weightUnit")}>
            {(["kg", "g"] as const).map((code) => (
              <button
                key={code}
                type="button"
                role="radio"
                aria-checked={unitCode === code}
                data-testid={`sell-weight-unit-${code}`}
                className={` flex-1 rounded-[var(--exits-radius-md)] border px-3 ${
                  unitCode === code
                    ? "border-primary bg-[var(--exits-surface-muted)]"
                    : "border-border"
                }`}
                onClick={() => setUnitCode(code)}
              >
                {code === "kg" ? t("sell.weightUnitKg") : t("sell.weightUnitG")}
              </button>
            ))}
          </div>
        </fieldset>

        {errorMessage && rawValue.trim() !== "" ? (
          <p
            role="alert"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {errorMessage}
          </p>
        ) : null}

        {stockError ? (
          <p
            role="alert"
            data-testid="sell-stock-error"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {stockError}
          </p>
        ) : null}

        {preview != null && kilograms != null ? (
          <p
            data-testid="sell-weight-preview"
            className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-sm)] font-semibold tabular-nums"
            aria-live="polite"
          >
            {formatSellLinePreview(t("sell.linePreview"), {
              qty: formatQuantityDisplay(
                unitCode === "g" ? Math.round(kilograms * 1000) : kilograms,
              ),
              unit: unitCode,
              price: `${formatPeso(unitPrice)} ${t("sell.pricePerKg")}`,
              amount: formatPeso(preview),
            })}
          </p>
        ) : null}

        <div className="flex flex-wrap justify-end gap-2">
          {editing && onRemove ? (
            <Button
              type="button"
              variant="ghost"
              data-testid="sell-weight-remove"
              onClick={onRemove}
            >
              {t("sell.cartRemove")}
            </Button>
          ) : null}
          <Button type="button" variant="ghost" onClick={onCancel}>
            {t("sell.cancel")}
          </Button>
          <Button
            type="button"
            data-testid="sell-weight-confirm"
            disabled={kilograms == null || overMax}
            onClick={() => {
              if (kilograms != null && !overMax) {
                onConfirm(kilograms);
              }
            }}
          >
            {editing ? t("sell.weightUpdate") : (confirmAddLabel ?? t("sell.weightAdd"))}
          </Button>
        </div>
      </div>
    </div>
  );
}

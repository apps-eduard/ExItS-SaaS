import { useEffect, useMemo, useState } from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  formatStockUnavailableMessage,
  normalizeWeightToKilograms,
  resolveSellCardStock,
  resolveSellUnitPrice,
  resolveStockHint,
  roundMoney,
  type WeightInputUnit,
} from "@/cart/sell-cart-helpers";
import { resolveBranchStockGuardQuantity } from "@/features/catalog/catalog-stock-display";
import { sellStockCaption } from "@/features/sell/sell-stock-caption";
import { useI18n } from "@/i18n/I18nProvider";

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

function formatKgThreeDp(kilograms: number): string {
  return kilograms.toFixed(3);
}

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
      // Start blank for new adds; prefill only when editing an existing cart weight.
      setRawValue(kg != null ? formatQuantityDisplay(kg) : "");
      setOpened(true);
    } else if (!open) {
      setOpened(false);
    }
  }, [initialKilograms, open, opened]);

  const unitPrice = product ? resolveSellUnitPrice(product, unit) : 0;
  const editing = initialKilograms != null && initialKilograms > 0;

  const parsed = useMemo(() => {
    if (rawValue.trim() === "") {
      return null;
    }
    const raw = Number(rawValue);
    if (!Number.isFinite(raw)) {
      return null;
    }
    return normalizeWeightToKilograms(raw, unitCode);
  }, [rawValue, unitCode]);

  const kilograms = parsed && "kilograms" in parsed ? parsed.kilograms : null;
  const errorCode = parsed && "error" in parsed ? parsed.error : null;
  const preview = kilograms != null ? roundMoney(unitPrice * kilograms) : null;

  const tracked = stockHint?.isTracked ?? product?.isTracked;
  const tracksExpiration = stockHint?.tracksExpiration ?? product?.tracksExpiration;
  const sellableQuantity = stockHint?.sellableQuantity ?? product?.sellableQuantity;

  const branchQty = product
    ? resolveBranchStockGuardQuantity({
        isTracked: tracked,
        onHandQuantity: stockHint?.onHandQuantity ?? product.onHandQuantity,
        branchAvailableQuantity: product.branchAvailableQuantity,
        branchOnHandQuantity: product.branchOnHandQuantity,
        organizationOnHandQuantity: product.organizationOnHandQuantity,
        sellableQuantity,
        tracksExpiration,
      })
    : null;

  const stockQty = branchQty ?? stockHint?.onHandQuantity ?? product?.onHandQuantity ?? null;

  const stock = product
    ? resolveSellCardStock({
        isTracked: tracked,
        onHandQuantity: stockQty,
        unitOfMeasure: "kg",
        tracksExpiration,
        sellableQuantity,
        stockStatus: stockHint?.stockStatus ?? product.stockStatus,
        isLowStock: stockHint?.isLowStock,
      })
    : null;

  const stockCapKg = useMemo(() => {
    if (maxKilograms != null && Number.isFinite(maxKilograms) && maxKilograms >= 0) {
      return maxKilograms;
    }
    const hint = resolveStockHint({
      isTracked: tracked,
      onHandQuantity: stockQty,
      unitOfMeasure: "kg",
      tracksExpiration,
      sellableQuantity,
    });
    return hint?.quantity ?? null;
  }, [maxKilograms, sellableQuantity, stockQty, tracked, tracksExpiration]);

  const overStock =
    kilograms != null && stockCapKg != null && kilograms > stockCapKg + 1e-9;

  if (!open || !product) {
    return null;
  }

  const hasEntry = rawValue.trim() !== "";
  const parseErrorMessage =
    errorCode === "zero"
      ? t("sell.weightErrorZero")
      : errorCode === "precision"
        ? t("sell.weightErrorPrecision")
        : errorCode === "invalid" || errorCode === "unit"
          ? t("sell.weightErrorInvalid")
          : null;

  const stockErrorMessage =
    overStock && stockCapKg != null
      ? formatStockUnavailableMessage({
          ok: false,
          available: stockCapKg,
          unitOfMeasure: "kg",
          includeUnit: true,
        })
      : null;

  const errorMessage = parseErrorMessage ?? stockErrorMessage;
  const canConfirm = kilograms != null && !overStock && errorCode == null;

  const enteredQtyLabel =
    unitCode === "g" && kilograms != null
      ? String(Math.round(kilograms * 1000))
      : kilograms != null
        ? formatQuantityDisplay(kilograms)
        : rawValue;

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
        className="sell-weight-entry flex w-full max-w-md flex-col gap-2.5 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
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

        <div className="flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)]">{t("sell.weightQuantity")}</span>
          <div className="sell-weight-entry__control">
            <input
              id="sell-weight-input"
              data-testid="sell-weight-input"
              type="number"
              inputMode="decimal"
              autoFocus
              min={unitCode === "g" ? 1 : 0.001}
              step={unitCode === "g" ? 1 : 0.001}
              value={rawValue}
              aria-label={t("sell.weightQuantity")}
              className="sell-weight-entry__input exits-input exits-input--no-spin tabular-nums"
              onChange={(event) => setRawValue(event.target.value)}
            />
            <div
              className="sell-weight-entry__units"
              role="radiogroup"
              aria-label={t("sell.weightUnit")}
            >
              {(["kg", "g"] as const).map((code) => (
                <button
                  key={code}
                  type="button"
                  role="radio"
                  aria-checked={unitCode === code}
                  data-testid={`sell-weight-unit-${code}`}
                  className={
                    unitCode === code
                      ? "sell-weight-entry__unit sell-weight-entry__unit--active"
                      : "sell-weight-entry__unit"
                  }
                  onClick={() => setUnitCode(code)}
                >
                  {code}
                </button>
              ))}
            </div>
          </div>
        </div>

        {unitCode === "g" && kilograms != null ? (
          <p
            data-testid="sell-weight-conversion"
            className="m-0 text-[length:var(--exits-text-xs)] text-muted tabular-nums"
            aria-live="polite"
          >
            {t("sell.weightGramsEqualsKg")
              .replace("{grams}", String(Math.round(kilograms * 1000)))
              .replace("{kg}", formatKgThreeDp(kilograms))}
          </p>
        ) : null}

        {hasEntry && errorMessage ? (
          <p
            role="alert"
            data-testid="sell-weight-inline-error"
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

        {preview != null && kilograms != null && !overStock && errorCode == null ? (
          <div
            data-testid="sell-weight-preview"
            className="sell-weight-entry__summary"
            aria-live="polite"
          >
            <div className="sell-weight-entry__summary-row">
              <span>{t("sell.weightSummaryPriceLabel")}</span>
              <span className="tabular-nums">
                <MoneyDisplay amount={unitPrice} className="font-normal" /> {t("sell.pricePerKg")}
              </span>
            </div>
            <div className="sell-weight-entry__summary-row">
              <span>{t("sell.weightSummaryWeightLabel")}</span>
              <span className="tabular-nums">
                {enteredQtyLabel} {unitCode}
              </span>
            </div>
            <div className="sell-weight-entry__summary-row sell-weight-entry__summary-row--total">
              <span>{t("sell.weightSummaryTotalLabel")}</span>
              <MoneyDisplay amount={preview} />
            </div>
          </div>
        ) : null}

        <div className="mt-1 flex flex-wrap justify-end gap-2">
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
            disabled={!canConfirm}
            onClick={() => {
              if (canConfirm && kilograms != null) {
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

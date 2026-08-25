import { useEffect, useMemo, useState } from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  normalizeCustomQuantity,
  resolveSellUnitPrice,
  resolveSellCardStock,
  roundMoney,
} from "@/cart/sell-cart-helpers";
import { sellStockCaption } from "@/features/sell/sell-stock-caption";
import { useI18n } from "@/i18n/I18nProvider";

type SellCustomQuantityDialogProps = {
  open: boolean;
  product: PosCatalogProductDto | null;
  unit: PosCatalogProductUnitDto | null;
  initialQuantity?: number | null;
  stockHint?: {
    isTracked?: boolean;
    onHandQuantity?: number | null;
    sellableQuantity?: number | null;
    tracksExpiration?: boolean;
    stockStatus?: string | null;
    isLowStock?: boolean | null;
  } | null;
  stockError?: string | null;
  onConfirm: (quantity: number) => void;
  onRemove?: () => void;
  onCancel: () => void;
};

export function SellCustomQuantityDialog({
  open,
  product,
  unit,
  initialQuantity = null,
  stockHint,
  stockError = null,
  onConfirm,
  onRemove,
  onCancel,
}: SellCustomQuantityDialogProps) {
  const { t } = useI18n();
  const [rawValue, setRawValue] = useState("");
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    if (open && !opened) {
      const qty = initialQuantity != null && initialQuantity > 0 ? initialQuantity : null;
      setRawValue(qty != null ? formatQuantityDisplay(qty) : "");
      setOpened(true);
    } else if (!open) {
      setOpened(false);
    }
  }, [initialQuantity, open, opened]);

  const unitPrice = product ? resolveSellUnitPrice(product, unit) : 0;
  const unitLabel =
    unit?.shortLabel?.trim() ||
    unit?.displayName ||
    product?.unitOfMeasure ||
    t("sell.quantityLabel");
  const editing = initialQuantity != null && initialQuantity > 0;

  const parsed = useMemo(() => {
    const raw = Number(rawValue);
    if (!Number.isFinite(raw)) {
      return null;
    }
    return normalizeCustomQuantity(raw);
  }, [rawValue]);

  const quantity = parsed && "quantity" in parsed ? parsed.quantity : null;
  const errorCode = parsed && "error" in parsed ? parsed.error : null;
  const preview = quantity != null ? roundMoney(unitPrice * quantity) : null;

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

  if (!open || !product || !unit) {
    return null;
  }

  const errorMessage =
    errorCode === "zero"
      ? t("sell.customQtyErrorZero")
      : errorCode === "precision"
        ? t("sell.customQtyErrorPrecision")
        : errorCode === "invalid"
          ? t("sell.customQtyErrorInvalid")
          : null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onCancel}
      data-testid="sell-custom-qty-backdrop"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="sell-custom-qty-title"
        data-testid="sell-custom-qty-entry"
        className="flex w-full max-w-md flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <h2
          id="sell-custom-qty-title"
          className="m-0 text-[length:var(--exits-text-md)] font-semibold"
        >
          {editing ? t("sell.customQtyEditTitle") : t("sell.customQtyAddTitle")}
        </h2>
        <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
          {product.name}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          <MoneyDisplay amount={unitPrice} className="font-normal" />{" "}
          {t("sell.pricePerUnit").replace("{unit}", unitLabel)}
        </p>
        {stock ? (
          <p
            data-testid="sell-stock-hint"
            className={`m-0 text-[length:var(--exits-text-xs)] text-muted sell-product-card__stock--${stock.tone}`}
          >
            {sellStockCaption(t, stock)}
          </p>
        ) : null}

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("sell.quantityLabel")} ({unitLabel})
          <input
            data-testid="sell-custom-qty-input"
            type="number"
            inputMode="decimal"
            autoFocus
            min={0.001}
            step={0.001}
            value={rawValue}
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
            onChange={(event) => setRawValue(event.target.value)}
          />
        </label>

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

        {preview != null && quantity != null ? (
          <p
            data-testid="sell-custom-qty-preview"
            className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
            aria-live="polite"
          >
            {t("sell.linePreview")
              .replace("{qty}", formatQuantityDisplay(quantity))
              .replace("{unit}", unitLabel)
              .replace("{price}", unitPrice.toFixed(2))
              .replace("{amount}", preview.toFixed(2))}
          </p>
        ) : null}

        <div className="flex flex-wrap justify-end gap-2">
          {editing && onRemove ? (
            <Button
              type="button"
              variant="ghost"
              data-testid="sell-custom-qty-remove"
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
            data-testid="sell-custom-qty-confirm"
            disabled={quantity == null}
            onClick={() => {
              if (quantity != null) {
                onConfirm(quantity);
              }
            }}
          >
            {editing ? t("sell.customQtyUpdate") : t("sell.customQtyAdd")}
          </Button>
        </div>
      </div>
    </div>
  );
}

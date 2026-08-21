import { useEffect, useMemo, useState } from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  normalizeWeightToKilograms,
  resolveSellUnitPrice,
  resolveStockHint,
  roundMoney,
  type WeightInputUnit,
} from "@/cart/sell-cart-helpers";
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
  } | null;
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

  const hint = product
    ? resolveStockHint({
        isTracked: stockHint?.isTracked ?? product.isTracked,
        onHandQuantity: stockHint?.onHandQuantity ?? product.onHandQuantity,
        unitOfMeasure: product.unitOfMeasure,
        tracksExpiration: stockHint?.tracksExpiration ?? product.tracksExpiration,
        sellableQuantity: stockHint?.sellableQuantity,
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
        {hint ? (
          <p
            data-testid="sell-stock-hint"
            className="m-0 text-[length:var(--exits-text-xs)] text-muted"
          >
            {hint.label === "sellable"
              ? t("sell.stockSellable")
                  .replace("{qty}", formatQuantityDisplay(hint.quantity))
                  .replace("{unit}", hint.unitOfMeasure)
              : t("sell.stockOnHand")
                  .replace("{qty}", formatQuantityDisplay(hint.quantity))
                  .replace("{unit}", hint.unitOfMeasure)}
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
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
                className={`min-h-11 flex-1 rounded-[var(--exits-radius-md)] border px-3 ${
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

        {preview != null && kilograms != null ? (
          <p
            data-testid="sell-weight-preview"
            className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
            aria-live="polite"
          >
            {t("sell.linePreview")
              .replace("{qty}", formatQuantityDisplay(kilograms))
              .replace("{unit}", "kg")
              .replace("{price}", unitPrice.toFixed(2))
              .replace("{amount}", preview.toFixed(2))}
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
            disabled={kilograms == null}
            onClick={() => {
              if (kilograms != null) {
                onConfirm(kilograms);
              }
            }}
          >
            {editing ? t("sell.weightUpdate") : t("sell.weightAdd")}
          </Button>
        </div>
      </div>
    </div>
  );
}

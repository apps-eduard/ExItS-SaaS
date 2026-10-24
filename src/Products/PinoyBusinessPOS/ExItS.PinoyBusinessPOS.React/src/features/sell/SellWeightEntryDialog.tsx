import { useEffect, useMemo, useState } from "react";
import { Scale } from "lucide-react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  formatStockUnavailableMessage,
  normalizeWeightToKilograms,
  resolveSellCardStock,
  resolveSellUnitPrice,
  resolveStockHint,
  roundMoney,
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

function SavePlusIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4V12" />
      <path d="M16 13H8a1 1 0 0 0-1 1v7" />
      <path d="M19 22v-6" />
      <path d="M22 19h-6" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function SavePenIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M13.33 13H8a1 1 0 0 0-1 1v7" />
      <path d="M14.363 17.634a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506l4.013-4.009a1 1 0 1 0-3.004-3.004z" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v.3" />
    </svg>
  );
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
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    if (open && !opened) {
      const kg = initialKilograms != null && initialKilograms > 0 ? initialKilograms : null;
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
    return normalizeWeightToKilograms(raw, "kg");
  }, [rawValue]);

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

  if (!product) {
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
    kilograms != null ? formatQuantityDisplay(kilograms) : rawValue;

  const confirmWeight = () => {
    if (canConfirm && kilograms != null) {
      onConfirm(kilograms);
    }
  };

  return (
    <ExitsModal
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          onCancel();
        }
      }}
      title={editing ? t("sell.weightEditTitle") : t("sell.weightAddTitle")}
      testId="sell-weight-entry"
      id="sell-weight-entry"
      className="sell-weight-entry-dialog sell-weight-entry-dialog--soft-glass sell-weight-entry"
      closeLabel={t("sell.cancel")}
      size="md"
      ribbon
      ribbonIcon={Scale}
      onSubmit={() => {
        confirmWeight();
      }}
      footer={
        <>
          {editing && onRemove ? (
            <Button
              type="button"
              variant="ghost"
              intent="danger"
              className="sell-weight-entry-dialog__btn me-auto"
              data-testid="sell-weight-remove"
              onClick={onRemove}
            >
              {t("sell.cartRemove")}
            </Button>
          ) : null}
          <Button
            type="button"
            intent="danger"
            appearance="glossy"
            emphasis="strong"
            className="sell-weight-entry-dialog__btn min-w-[8rem]"
            data-testid="sell-weight-cancel"
            onClick={onCancel}
          >
            {t("sell.cancel")}
          </Button>
          <Button
            type="submit"
            intent="primary"
            appearance="glossy"
            className="sell-weight-entry-dialog__btn min-w-[8rem]"
            data-testid="sell-weight-confirm"
            disabled={!canConfirm}
          >
            {editing ? (
              <SavePenIcon className="size-4 shrink-0" />
            ) : (
              <SavePlusIcon className="size-4 shrink-0" />
            )}
            {editing ? t("sell.weightUpdate") : (confirmAddLabel ?? t("sell.weightAdd"))}
          </Button>
        </>
      }
    >
      <div
        className="sell-weight-entry-dialog__body flex min-w-0 flex-col gap-2.5"
        data-shell="soft-glass"
      >
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
              data-exits-modal-autofocus="true"
              type="number"
              inputMode="decimal"
              min={0.001}
              step={0.001}
              value={rawValue}
              aria-label={t("sell.weightQuantity")}
              className="sell-weight-entry__input exits-input exits-input--no-spin tabular-nums"
              onChange={(event) => setRawValue(event.target.value)}
            />
            <span className="sell-weight-entry__unit-fixed" aria-hidden>
              kg
            </span>
          </div>
        </div>

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
                {enteredQtyLabel} kg
              </span>
            </div>
            <div className="sell-weight-entry__summary-row sell-weight-entry__summary-row--total">
              <span>{t("sell.weightSummaryTotalLabel")}</span>
              <MoneyDisplay amount={preview} />
            </div>
          </div>
        ) : null}
      </div>
    </ExitsModal>
  );
}

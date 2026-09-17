import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { exceedsManagerSalePriceLimit } from "@/features/checkout/map-cart-price-overrides";
import { useI18n } from "@/i18n/I18nProvider";

export type SellPriceOverrideDialogProps = {
  open: boolean;
  productName: string;
  /** Catalog / resolved baseline unit price (Current price). */
  currentUnitPrice: number;
  initialRequestedUnitPrice?: number | null;
  initialReason?: string | null;
  /** When false, show friendly denial if requested exceeds 100% deviation. */
  allowUnlimited: boolean;
  onApply: (requestedUnitPrice: number, reason: string) => void;
  onUseRegularPrice: () => void;
  onCancel: () => void;
};

function parseUnitPrice(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return null;
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value)) {
    return null;
  }
  return value;
}

export function SellPriceOverrideDialog({
  open,
  productName,
  currentUnitPrice,
  initialRequestedUnitPrice = null,
  initialReason = null,
  allowUnlimited,
  onApply,
  onUseRegularPrice,
  onCancel,
}: SellPriceOverrideDialogProps) {
  const { t } = useI18n();
  const [rawPrice, setRawPrice] = useState("");
  const [reason, setReason] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [opened, setOpened] = useState(false);

  const hasPendingOverride =
    initialRequestedUnitPrice != null &&
    Number.isFinite(initialRequestedUnitPrice) &&
    Math.abs(initialRequestedUnitPrice - currentUnitPrice) > 1e-9;

  useEffect(() => {
    if (open && !opened) {
      const seed =
        initialRequestedUnitPrice != null && Number.isFinite(initialRequestedUnitPrice)
          ? initialRequestedUnitPrice
          : currentUnitPrice;
      setRawPrice(seed.toFixed(2));
      setReason(initialReason?.trim() ?? "");
      setFormError(null);
      setOpened(true);
    } else if (!open) {
      setOpened(false);
    }
  }, [currentUnitPrice, initialReason, initialRequestedUnitPrice, open, opened]);

  if (!open) {
    return null;
  }

  function submit() {
    const parsed = parseUnitPrice(rawPrice);
    if (parsed === null) {
      setFormError(t("sell.priceOverrideInvalid"));
      return;
    }
    if (!(parsed > 0)) {
      setFormError(t("sell.priceOverrideZero"));
      return;
    }
    const trimmedReason = reason.trim();
    if (!trimmedReason) {
      setFormError(t("sell.priceOverrideReasonRequired"));
      return;
    }
    if (!allowUnlimited && exceedsManagerSalePriceLimit(currentUnitPrice, parsed)) {
      setFormError(t("sell.priceOverrideAboveLimit"));
      return;
    }
    setFormError(null);
    onApply(parsed, trimmedReason);
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onCancel}
      data-testid="sell-price-override-backdrop"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="sell-price-override-title"
        data-testid="sell-price-override-dialog"
        className="flex w-full max-w-md flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <h2
          id="sell-price-override-title"
          className="m-0 text-[length:var(--exits-text-md)] font-semibold"
        >
          {t("sell.priceOverrideTitle")}
        </h2>
        <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
          {productName}
        </p>
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="sell-price-override-current"
        >
          {t("sell.priceOverrideCurrent")}: <MoneyDisplay amount={currentUnitPrice} />
        </p>

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("sell.priceOverrideNew")}
          <input
            data-testid="sell-price-override-new"
            type="number"
            inputMode="decimal"
            autoFocus
            min={0.01}
            step={0.01}
            value={rawPrice}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
            onChange={(event) => {
              setRawPrice(event.target.value);
              setFormError(null);
            }}
          />
        </label>

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("sell.priceOverrideReason")}
          <input
            data-testid="sell-price-override-reason"
            type="text"
            value={reason}
            maxLength={512}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            onChange={(event) => {
              setReason(event.target.value);
              setFormError(null);
            }}
          />
        </label>

        {formError ? (
          <p
            role="alert"
            data-testid="sell-price-override-form-error"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {formError}
          </p>
        ) : null}

        <div className="flex flex-wrap justify-end gap-2">
          {hasPendingOverride ? (
            <Button
              type="button"
              variant="ghost"
              data-testid="sell-price-override-use-regular"
              onClick={onUseRegularPrice}
            >
              {t("sell.priceOverrideUseRegular")}
            </Button>
          ) : null}
          <Button
            type="button"
            variant="ghost"
            data-testid="sell-price-override-cancel"
            onClick={onCancel}
          >
            {t("sell.cancel")}
          </Button>
          <Button type="button" data-testid="sell-price-override-apply" onClick={submit}>
            {t("sell.priceOverrideApply")}
          </Button>
        </div>
      </div>
    </div>
  );
}

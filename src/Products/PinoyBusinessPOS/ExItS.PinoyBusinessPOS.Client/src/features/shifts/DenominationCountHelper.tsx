import { useEffect, useMemo, useState } from "react";
import { Coins, Minus, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatDenominationValue,
  type CashDenominationCountItem,
} from "@/api/pos/pos-operational-setup-client";
import type { CashCountDenominationLineDto } from "@/api/pos/pos-shifts-client";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";

export type DenominationHelperItem = CashDenominationCountItem;

type Props = {
  denominations: DenominationHelperItem[];
  currencyCode: string;
  total: string;
  onTotalChange: (value: string) => void;
  onLinesChange?: (lines: CashCountDenominationLineDto[]) => void;
  disabled?: boolean;
  testIdPrefix?: string;
};

function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export function DenominationCountHelper({
  denominations,
  currencyCode,
  total,
  onTotalChange,
  onLinesChange,
  disabled = false,
  testIdPrefix = "denom",
}: Props) {
  const { t } = useI18n();
  const [quantities, setQuantities] = useState<Record<string, number>>({});

  const sorted = useMemo(
    () => [...denominations].sort((a, b) => a.sortOrder - b.sortOrder || b.value - a.value),
    [denominations],
  );

  const denominationSignature = useMemo(
    () => sorted.map((denom) => `${denom.sortOrder}:${denom.value}`).join("|"),
    [sorted],
  );

  useEffect(() => {
    setQuantities({});
    onTotalChange("");
    onLinesChange?.([]);
  }, [denominationSignature, onLinesChange, onTotalChange]);

  function emit(next: Record<string, number>) {
    setQuantities(next);
    let sum = 0;
    const lines: CashCountDenominationLineDto[] = [];
    for (const denom of sorted) {
      const key = String(denom.value);
      const quantity = next[key] ?? 0;
      if (quantity > 0) {
        const lineTotal = roundMoney(denom.value * quantity);
        sum = roundMoney(sum + lineTotal);
        lines.push({
          denominationValue: denom.value,
          quantity,
          lineTotal,
        });
      }
    }
    onTotalChange(sum > 0 || Object.keys(next).length > 0 ? sum.toFixed(2) : "");
    onLinesChange?.(lines);
  }

  function setQuantity(value: number, quantity: number) {
    const nextQty = Math.max(0, Math.floor(quantity));
    const key = String(value);
    const next = { ...quantities, [key]: nextQty };
    if (nextQty === 0) {
      delete next[key];
    }
    emit(next);
  }

  function reset() {
    emit({});
  }

  const totalAmount = Number(total);
  const totalLabel =
    total.trim().length > 0 && Number.isFinite(totalAmount)
      ? formatPeso(totalAmount)
      : formatPeso(0);

  if (sorted.length === 0) {
    return (
      <div data-testid={`${testIdPrefix}-empty`} className="denom-helper denom-helper--empty">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
          {t("shift.denomEmpty")}
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("shift.denomEmptyDetail")}
        </p>
      </div>
    );
  }

  return (
    <div data-testid={`${testIdPrefix}-helper`} className="denom-helper flex flex-col gap-3">
      <div className="flex items-start gap-2">
        <Coins className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden />
        <div>
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("shift.denomHelper")}
          </h3>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("shift.denomHelperHint")}
          </p>
        </div>
      </div>

      <ul className="denom-helper__grid m-0 grid list-none gap-2 p-0">
        {sorted.map((denom) => {
          const key = String(denom.value);
          const quantity = quantities[key] ?? 0;
          const active = quantity > 0;
          const label = denom.label?.trim() || formatDenominationValue(denom.value);
          const lineTotal = roundMoney(denom.value * quantity);

          return (
            <li key={key} className="min-w-0" data-testid={`${testIdPrefix}-row-${key}`}>
              <div
                className={cn(
                  "denom-helper__tile",
                  active && "denom-helper__tile--active",
                  disabled && "denom-helper__tile--disabled",
                )}
              >
                <button
                  type="button"
                  className="denom-helper__tap"
                  disabled={disabled}
                  aria-label={t("shift.denomIncreaseAria").replace("{denom}", label)}
                  data-testid={`${testIdPrefix}-inc-${key}`}
                  onClick={() => setQuantity(denom.value, quantity + 1)}
                >
                  <span className="denom-helper__value tabular-nums">{label}</span>
                  <span
                    className={cn(
                      "denom-helper__qty tabular-nums",
                      active && "denom-helper__qty--active",
                    )}
                    data-testid={`${testIdPrefix}-qty-${key}`}
                  >
                    ×{quantity}
                  </span>
                  {active ? (
                    <span className="denom-helper__line-total tabular-nums">
                      {formatPeso(lineTotal)}
                    </span>
                  ) : (
                    <span className="denom-helper__tap-hint">{t("shift.denomTapToAdd")}</span>
                  )}
                </button>

                <button
                  type="button"
                  className="denom-helper__dec"
                  disabled={disabled || quantity <= 0}
                  aria-label={t("shift.denomDecreaseAria").replace("{denom}", label)}
                  data-testid={`${testIdPrefix}-dec-${key}`}
                  onClick={() => setQuantity(denom.value, quantity - 1)}
                >
                  <Minus className="size-4" aria-hidden />
                </button>
              </div>
            </li>
          );
        })}
      </ul>

      <div className="denom-helper__footer">
        <p className="m-0 inline-flex min-w-0 flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Coins className="size-4 shrink-0 text-primary" aria-hidden />
          <span>{t("shift.cashOnHand")}:</span>
          <span className="tabular-nums font-semibold" data-testid={`${testIdPrefix}-total`}>
            {totalLabel}
          </span>
          <span className="text-muted">({currencyCode})</span>
        </p>
        <Button
          type="button"
          variant="outline"
          className="min-h-9 shrink-0"
          disabled={disabled || Object.keys(quantities).length === 0}
          data-testid={`${testIdPrefix}-reset`}
          onClick={reset}
        >
          <RotateCcw className="size-4 shrink-0" aria-hidden />
          {t("shift.resetCount")}
        </Button>
      </div>
    </div>
  );
}

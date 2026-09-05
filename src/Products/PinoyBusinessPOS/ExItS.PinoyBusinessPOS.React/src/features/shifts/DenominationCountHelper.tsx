import { useEffect, useMemo, useRef, useState } from "react";
import { Minus, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatDenominationValue,
  type CashDenominationCountItem,
} from "@/api/pos/pos-operational-setup-client";
import type { CashCountDenominationLineDto } from "@/api/pos/pos-shifts-client";
import { formatDenominationCurrency, formatPeso } from "@/lib/format-money";
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
  /** Hide the helper title/hint when the parent already shows them. */
  hideHeader?: boolean;
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
  hideHeader = false,
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

  const onTotalChangeRef = useRef(onTotalChange);
  const onLinesChangeRef = useRef(onLinesChange);
  onTotalChangeRef.current = onTotalChange;
  onLinesChangeRef.current = onLinesChange;

  useEffect(() => {
    setQuantities({});
    onTotalChangeRef.current("");
    onLinesChangeRef.current?.([]);
  }, [denominationSignature]);

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
    <div data-testid={`${testIdPrefix}-helper`} className="denom-helper flex flex-col gap-2">
      {hideHeader ? null : (
        <div className="min-w-0">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("shift.denomHelper")}
          </h3>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
            {t("shift.denomHelperHint")}
          </p>
        </div>
      )}

      <ul className="denom-helper__grid m-0 grid list-none gap-1.5 p-0">
        {sorted.map((denom) => {
          const key = String(denom.value);
          const quantity = quantities[key] ?? 0;
          const active = quantity > 0;
          const label = formatDenominationCurrency(denom.value);
          const ariaLabel = denom.label?.trim() || formatDenominationValue(denom.value);

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
                  aria-label={t("shift.denomIncreaseAria").replace("{denom}", ariaLabel)}
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
                </button>

                <button
                  type="button"
                  className="denom-helper__dec"
                  disabled={disabled || quantity <= 0}
                  aria-label={t("shift.denomDecreaseAria").replace("{denom}", ariaLabel)}
                  data-testid={`${testIdPrefix}-dec-${key}`}
                  onClick={() => setQuantity(denom.value, quantity - 1)}
                >
                  <Minus className="size-3.5" aria-hidden />
                </button>
              </div>
            </li>
          );
        })}
      </ul>

      <div className="denom-helper__footer">
        <p className="m-0 inline-flex min-w-0 flex-1 flex-wrap items-baseline justify-between gap-x-3 gap-y-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("shift.cashOnHand")}</span>
          <span className="tabular-nums font-semibold" data-testid={`${testIdPrefix}-total`}>
            {totalLabel}
            <span className="sr-only"> ({currencyCode})</span>
          </span>
        </p>
        <Button
          type="button"
          variant="ghost"
          className="h-auto min-h-0 shrink-0 px-2 py-1 text-[length:var(--exits-text-xs)]"
          disabled={disabled || Object.keys(quantities).length === 0}
          data-testid={`${testIdPrefix}-reset`}
          onClick={reset}
        >
          <RotateCcw className="size-3.5 shrink-0" aria-hidden />
          {t("shift.resetCount")}
        </Button>
      </div>
    </div>
  );
}

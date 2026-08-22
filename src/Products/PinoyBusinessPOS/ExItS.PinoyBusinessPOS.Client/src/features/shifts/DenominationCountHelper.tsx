import { useEffect, useMemo, useState } from "react";
import { Coins, Minus, Plus, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatDenominationValue,
  type CashDenominationCountItem,
} from "@/api/pos/pos-operational-setup-client";
import type { CashCountDenominationLineDto } from "@/api/pos/pos-shifts-client";

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
    () =>
      [...denominations].sort(
        (a, b) => a.sortOrder - b.sortOrder || b.value - a.value,
      ),
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

  if (sorted.length === 0) {
    return (
      <div
        data-testid={`${testIdPrefix}-empty`}
        className="rounded-[var(--exits-radius-md)] border border-dashed border-border bg-surface-muted/40 px-3 py-3"
      >
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
    <div data-testid={`${testIdPrefix}-helper`} className="flex flex-col gap-3">
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
      <ul className="m-0 grid list-none grid-cols-2 gap-2 p-0">
        {sorted.map((denom) => {
          const key = String(denom.value);
          const quantity = quantities[key] ?? 0;
          const active = quantity > 0;
          return (
            <li
              key={key}
              className={`flex min-w-0 flex-col gap-1 rounded-[var(--exits-radius-md)] border px-2 py-1.5 transition-colors ${
                active
                  ? "border-primary/40 bg-primary/5"
                  : "border-border bg-surface"
              }`}
              data-testid={`${testIdPrefix}-row-${key}`}
            >
              <span className="truncate tabular-nums text-[length:var(--exits-text-sm)] font-medium">
                {denom.label?.trim() || formatDenominationValue(denom.value)}
              </span>
              <div className="flex items-center justify-end gap-0.5">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-9 shrink-0"
                  disabled={disabled || quantity <= 0}
                  aria-label={`Decrease ${formatDenominationValue(denom.value)}`}
                  data-testid={`${testIdPrefix}-dec-${key}`}
                  onClick={() => setQuantity(denom.value, quantity - 1)}
                >
                  <Minus className="size-4" aria-hidden />
                </Button>
                <span
                  className="min-w-8 text-center tabular-nums text-[length:var(--exits-text-sm)] font-semibold"
                  data-testid={`${testIdPrefix}-qty-${key}`}
                >
                  {quantity}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-9 shrink-0"
                  disabled={disabled}
                  aria-label={`Increase ${formatDenominationValue(denom.value)}`}
                  data-testid={`${testIdPrefix}-inc-${key}`}
                  onClick={() => setQuantity(denom.value, quantity + 1)}
                >
                  <Plus className="size-4" aria-hidden />
                </Button>
              </div>
            </li>
          );
        })}
      </ul>
      <div className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2">
        <p className="m-0 inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Coins className="size-4 shrink-0 text-primary" aria-hidden />
          {t("shift.cashOnHand")}:{" "}
          <span className="tabular-nums font-semibold">
            {total || "0.00"} {currencyCode}
          </span>
        </p>
        <Button
          type="button"
          variant="ghost"
          className="min-h-9 shrink-0"
          disabled={disabled}
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

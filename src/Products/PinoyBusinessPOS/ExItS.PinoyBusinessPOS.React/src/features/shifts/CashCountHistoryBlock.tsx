import { useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { formatDenominationValue } from "@/api/pos/pos-operational-setup-client";
import type { CashCountDenominationLineDto } from "@/api/pos/pos-shifts-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { formatDenominationCurrency, formatPeso } from "@/lib/format-money";
import { useI18n } from "@/i18n/I18nProvider";

type Props = {
  label: string;
  counted: boolean;
  amount: number | null;
  lines: CashCountDenominationLineDto[] | null | undefined;
  testId: string;
  breakdownLabel: string;
};

export function CashCountHistoryBlock({
  label,
  counted,
  amount,
  lines,
  testId,
  breakdownLabel,
}: Props) {
  const { t } = useI18n();
  const [expanded, setExpanded] = useState(false);
  const hasLines = counted && Boolean(lines && lines.length > 0);

  if (!counted) {
    return (
      <div className="flex items-center justify-between gap-2 py-0.5" data-testid={testId}>
        <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
        <span
          className="text-[length:var(--exits-text-sm)] font-medium"
          data-testid={`${testId}-not-counted`}
        >
          {t("shift.notCounted")}
        </span>
      </div>
    );
  }

  const displayAmount = amount ?? 0;

  if (!hasLines) {
    return (
      <div className="flex items-center justify-between gap-2 py-0.5" data-testid={testId}>
        <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
        <span className="tabular-nums text-[length:var(--exits-text-sm)] font-medium">
          <MoneyDisplay amount={displayAmount} testId={`${testId}-amount`} />
        </span>
      </div>
    );
  }

  return (
    <div data-testid={testId} className="flex flex-col gap-1">
      <button
        type="button"
        className="flex w-full items-center justify-between gap-2 py-0.5 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        aria-expanded={expanded}
        data-testid={`${testId}-toggle`}
        onClick={() => setExpanded((value) => !value)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            setExpanded((value) => !value);
          }
        }}
      >
        <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
        <span className="flex shrink-0 items-center gap-1.5 tabular-nums text-[length:var(--exits-text-sm)] font-medium">
          <MoneyDisplay amount={displayAmount} testId={`${testId}-amount`} />
          {expanded ? (
            <ChevronDown className="size-4 text-muted" aria-hidden />
          ) : (
            <ChevronRight className="size-4 text-muted" aria-hidden />
          )}
        </span>
      </button>
      {expanded ? (
        <ul
          className="m-0 list-none p-0"
          aria-label={breakdownLabel}
          data-testid={`${testId}-breakdown`}
        >
          {lines!.map((line) => {
            const lineTotal = line.lineTotal ?? line.denominationValue * line.quantity;
            return (
              <li
                key={`${line.denominationValue}-${line.quantity}`}
                className="cash-count-history__line"
                data-testid={`${testId}-line-${formatDenominationValue(line.denominationValue)}`}
              >
                <span className="text-muted">
                  {formatDenominationCurrency(line.denominationValue)} ×{line.quantity}
                </span>
                <span className="font-medium">{formatPeso(lineTotal)}</span>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}

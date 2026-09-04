import { useState } from "react";
import { ChevronDown, ChevronRight, ListCollapse } from "lucide-react";
import { formatDenominationValue } from "@/api/pos/pos-operational-setup-client";
import type { CashCountDenominationLineDto } from "@/api/pos/pos-shifts-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
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
      <div className="flex items-center justify-between gap-2" data-testid={testId}>
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
      <div className="flex items-center justify-between gap-2" data-testid={testId}>
        <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
        <span className="tabular-nums text-[length:var(--exits-text-sm)] font-medium">
          <MoneyDisplay amount={displayAmount} testId={`${testId}-amount`} />
        </span>
      </div>
    );
  }

  return (
    <div data-testid={testId} className="flex flex-col gap-2">
      <button
        type="button"
        className="flex w-full items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-left shadow-sm transition-colors hover:bg-surface-muted/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
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
        <span className="flex min-w-0 items-start gap-2">
          <ListCollapse className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden />
          <span className="flex min-w-0 flex-col">
            <span className="text-[length:var(--exits-text-sm)] font-medium">{label}</span>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {t("shift.tapDenominationHint")}
            </span>
          </span>
        </span>
        <span className="flex shrink-0 items-center gap-2 tabular-nums text-[length:var(--exits-text-sm)] font-semibold">
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
          className="m-0 flex list-none flex-col gap-1 p-0 sm:gap-1"
          aria-label={breakdownLabel}
          data-testid={`${testId}-breakdown`}
        >
          {lines!.map((line) => {
            const lineTotal = line.lineTotal ?? line.denominationValue * line.quantity;
            return (
              <li
                key={`${line.denominationValue}-${line.quantity}`}
                className="grid grid-cols-1 gap-1 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)] tabular-nums sm:grid-cols-3 sm:gap-2"
                data-testid={`${testId}-line-${formatDenominationValue(line.denominationValue)}`}
              >
                <span>{formatDenominationValue(line.denominationValue)}</span>
                <span className="sm:text-center">× {line.quantity}</span>
                <span className="font-medium sm:text-right">
                  <MoneyDisplay amount={lineTotal} />
                </span>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}

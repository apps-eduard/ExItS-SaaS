import type { ReactNode } from "react";
import {
  ArrowDownCircle,
  ArrowUpCircle,
  CheckCircle2,
  MonitorSmartphone,
  Receipt,
  Scale,
  Store,
  Wallet,
} from "lucide-react";
import type { PosCashierShiftDto, PosCashierShiftSummaryDto } from "@/api/pos/pos-shifts-client";
import { Card } from "@/components/ui/card";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { cashCountModeMessageKey } from "@/features/shifts/cash-count-mode-label";
import { CashCountHistoryBlock } from "@/features/shifts/CashCountHistoryBlock";
import {
  classifyCashVariance,
  resolveClosingCashCounted,
  resolveHistoricalClosingMode,
  resolveHistoricalOpeningMode,
} from "@/features/shifts/shift-cash-history";
import { useI18n } from "@/i18n/I18nProvider";

function SummaryRow({
  label,
  testId,
  icon: Icon,
  children,
}: {
  label: string;
  testId: string;
  icon?: typeof Wallet;
  children: ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-3" data-testid={testId}>
      <span className="inline-flex min-w-0 items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
        {Icon ? <Icon className="size-4 shrink-0 text-primary/80" aria-hidden /> : null}
        {label}
      </span>
      <span className="min-w-0 text-right text-[length:var(--exits-text-sm)] font-medium">
        {children}
      </span>
    </div>
  );
}

type Props = {
  shift: PosCashierShiftDto;
  summary: PosCashierShiftSummaryDto | undefined;
  closed: boolean;
};

export function ShiftCashHistoryPanel({ shift, summary, closed }: Props) {
  const { t } = useI18n();
  const openingMode = resolveHistoricalOpeningMode(shift);
  const closingModeHistory = resolveHistoricalClosingMode(shift);
  const closingCounted = resolveClosingCashCounted(shift);
  const openingLines = shift.openingDenominationLines ?? [];
  const closingLines = shift.closingDenominationLines ?? [];
  const varianceAmount = summary?.cashVarianceAmount ?? shift.cashVarianceAmount ?? null;
  const varianceKind = varianceAmount == null ? null : classifyCashVariance(varianceAmount);

  return (
    <Card className="flex flex-col gap-4" data-testid="shift-cash-history-panel">
      <div className="flex items-start gap-2.5" data-testid="shift-register-label">
        <MonitorSmartphone className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
        <div className="min-w-0 flex-1">
          <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
            {t("shift.registerSection")}
          </p>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] font-semibold">
            {shift.registerCode
              ? `${shift.registerCode} — ${shift.registerName ?? ""}`
              : t("shift.noRegisterOnShift")}
          </p>
        </div>
      </div>

      <div className="flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface-muted/20 p-3">
        <SummaryRow
          testId="shift-opening-policy"
          label={t("shift.openingCashCountPolicy")}
          icon={Wallet}
        >
          <span data-testid="shift-opening-policy-value">
            {t(cashCountModeMessageKey(openingMode))}
          </span>
        </SummaryRow>

        <CashCountHistoryBlock
          testId="shift-opening-history"
          label={t("shift.openingCashLabel")}
          counted={shift.openingCashCounted}
          amount={shift.openingCashAmount}
          lines={openingLines}
          breakdownLabel={t("shift.viewOpeningDenominationBreakdown")}
        />
      </div>

      {closed ? (
        <>
          <div className="flex items-center gap-2 pt-1" data-testid="shift-cash-summary-heading">
            <Receipt className="size-5 shrink-0 text-primary" aria-hidden />
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("shift.cashSummaryHeading")}
            </h2>
          </div>

          {summary ? (
            <div className="flex flex-col gap-2.5 rounded-[var(--exits-radius-md)] border border-border bg-surface-muted/20 p-3">
              <SummaryRow testId="shift-cash-sales" label={t("shift.cashSales")} icon={ArrowUpCircle}>
                <MoneyDisplay amount={summary.cashSalesTotal} />
              </SummaryRow>
              <SummaryRow testId="shift-cash-refunds" label={t("shift.cashRefunds")} icon={ArrowDownCircle}>
                <MoneyDisplay amount={summary.cashRefundsTotal} />
              </SummaryRow>
              <SummaryRow testId="shift-cash-in" label={t("shift.cashIn")} icon={ArrowUpCircle}>
                <MoneyDisplay amount={summary.totalCashIn} />
              </SummaryRow>
              <SummaryRow testId="shift-cash-out" label={t("shift.cashOut")} icon={ArrowDownCircle}>
                <MoneyDisplay amount={summary.totalCashOut} />
              </SummaryRow>
              <SummaryRow
                testId="shift-expected-cash"
                label={t("shift.expectedClosingCash")}
                icon={Wallet}
              >
                <MoneyDisplay amount={summary.expectedCashAmount} />
              </SummaryRow>
            </div>
          ) : null}

          <div className="flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface-muted/20 p-3">
            <SummaryRow
              testId="shift-closing-policy"
              label={t("shift.closingCashCountPolicy")}
              icon={Wallet}
            >
              <span data-testid="shift-closing-policy-value">
                {t(cashCountModeMessageKey(closingModeHistory))}
              </span>
            </SummaryRow>

            <CashCountHistoryBlock
              testId="shift-closing-history"
              label={t("shift.closingCashLabel")}
              counted={closingCounted}
              amount={shift.closingCashAmount ?? null}
              lines={closingLines}
              breakdownLabel={t("shift.viewDenominationBreakdown")}
            />

            {varianceAmount != null && varianceKind != null ? (
              <SummaryRow testId="shift-cash-variance" label={t("shift.difference")} icon={Scale}>
                {varianceKind === "balanced" ? (
                  <span
                    className="inline-flex items-center gap-1 text-[var(--exits-success)]"
                    data-testid="shift-variance-balanced"
                  >
                    <CheckCircle2 className="size-4 shrink-0" aria-hidden />
                    {t("shift.varianceBalanced")}
                  </span>
                ) : (
                  <span className="inline-flex flex-wrap items-center justify-end gap-1">
                    <span data-testid={`shift-variance-${varianceKind}`}>
                      {varianceKind === "over"
                        ? t("shift.varianceOverBy")
                        : t("shift.varianceShortBy")}
                    </span>
                    <MoneyDisplay amount={Math.abs(varianceAmount)} />
                  </span>
                )}
              </SummaryRow>
            ) : null}
          </div>

          {summary && (summary.gCashSalesTotal !== 0 || summary.utangSalesTotal !== 0) ? (
            <div
              className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border bg-surface-muted/10 p-3"
              data-testid="shift-noncash-info"
            >
              <SummaryRow testId="shift-gcash-sales" label={t("shift.gCashSales")} icon={Store}>
                <MoneyDisplay amount={summary.gCashSalesTotal} />
              </SummaryRow>
              <SummaryRow testId="shift-utang-sales" label={t("shift.utangSales")} icon={Store}>
                <MoneyDisplay amount={summary.utangSalesTotal} />
              </SummaryRow>
              <p className="mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("shift.nonCashInfoHint")}
              </p>
            </div>
          ) : null}

          {shift.closingNotes ? (
            <p
              className="mb-0 rounded-[var(--exits-radius-md)] border border-border bg-surface-muted/20 px-3 py-2 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="shift-closing-notes-readonly"
            >
              {t("shift.closingNotesLabel")}: {shift.closingNotes}
            </p>
          ) : null}
        </>
      ) : summary ? (
        <SummaryRow
          testId="shift-expected-cash"
          label={t("shift.expectedCashLabel")}
          icon={Wallet}
        >
          <MoneyDisplay amount={summary.expectedCashAmount} />
        </SummaryRow>
      ) : null}
    </Card>
  );
}

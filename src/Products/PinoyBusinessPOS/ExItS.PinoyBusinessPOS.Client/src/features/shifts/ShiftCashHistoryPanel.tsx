import type { ReactNode } from "react";
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
  children,
}: {
  label: string;
  testId: string;
  children: ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-3" data-testid={testId}>
      <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
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
    <Card className="flex flex-col gap-3" data-testid="shift-cash-history-panel">
      <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="shift-register-label">
        <span className="font-semibold">{t("shift.registerSection")}: </span>
        {shift.registerCode
          ? `${shift.registerCode} — ${shift.registerName ?? ""}`
          : t("shift.noRegisterOnShift")}
      </p>

      <SummaryRow testId="shift-opening-policy" label={t("shift.openingCashCountPolicy")}>
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

      {closed ? (
        <>
          <h2
            className="m-0 pt-1 text-[length:var(--exits-text-md)] font-semibold"
            data-testid="shift-cash-summary-heading"
          >
            {t("shift.cashSummaryHeading")}
          </h2>

          {summary ? (
            <>
              <SummaryRow testId="shift-cash-sales" label={t("shift.cashSales")}>
                <MoneyDisplay amount={summary.cashSalesTotal} />
              </SummaryRow>
              <SummaryRow testId="shift-cash-refunds" label={t("shift.cashRefunds")}>
                <MoneyDisplay amount={summary.cashRefundsTotal} />
              </SummaryRow>
              <SummaryRow testId="shift-cash-in" label={t("shift.cashIn")}>
                <MoneyDisplay amount={summary.totalCashIn} />
              </SummaryRow>
              <SummaryRow testId="shift-cash-out" label={t("shift.cashOut")}>
                <MoneyDisplay amount={summary.totalCashOut} />
              </SummaryRow>
              <SummaryRow testId="shift-expected-cash" label={t("shift.expectedClosingCash")}>
                <MoneyDisplay amount={summary.expectedCashAmount} />
              </SummaryRow>
            </>
          ) : null}

          <SummaryRow testId="shift-closing-policy" label={t("shift.closingCashCountPolicy")}>
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
            <SummaryRow testId="shift-cash-variance" label={t("shift.difference")}>
              {varianceKind === "balanced" ? (
                <span data-testid="shift-variance-balanced">{t("shift.varianceBalanced")}</span>
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

          {summary && (summary.gCashSalesTotal !== 0 || summary.utangSalesTotal !== 0) ? (
            <div
              className="flex flex-col gap-2 border-t border-border pt-3"
              data-testid="shift-noncash-info"
            >
              <SummaryRow testId="shift-gcash-sales" label={t("shift.gCashSales")}>
                <MoneyDisplay amount={summary.gCashSalesTotal} />
              </SummaryRow>
              <SummaryRow testId="shift-utang-sales" label={t("shift.utangSales")}>
                <MoneyDisplay amount={summary.utangSalesTotal} />
              </SummaryRow>
              <p className="mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("shift.nonCashInfoHint")}
              </p>
            </div>
          ) : null}

          {shift.closingNotes ? (
            <p
              className="mb-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="shift-closing-notes-readonly"
            >
              {t("shift.closingNotesLabel")}: {shift.closingNotes}
            </p>
          ) : null}
        </>
      ) : summary ? (
        <SummaryRow testId="shift-expected-cash" label={t("shift.expectedCashLabel")}>
          <MoneyDisplay amount={summary.expectedCashAmount} />
        </SummaryRow>
      ) : null}
    </Card>
  );
}

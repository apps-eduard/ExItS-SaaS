import { Banknote, CircleDollarSign, Receipt, Users, Wallet } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  STATEMENT_ENTRY_FILTERS,
  defaultStatementPeriod,
  formatStatementWhen,
  matchesStatementEntryFilter,
  statementLineDescription,
  type CustomerStatementTotalsView,
  type StatementEntryFilter,
} from "@/features/customers/customer-statement-view";
import { useI18n } from "@/i18n/I18nProvider";

export type CustomerStatementViewProps = {
  testId: string;
  displayName: string;
  backTo: string;
  backTestId: string;
  subjectLoading: boolean;
  subjectError: boolean;
  subjectErrorDetail?: string;
  statementLoading: boolean;
  statementError: boolean;
  statementErrorDetail?: string;
  statement: CustomerStatementTotalsView | null | undefined;
  onPeriodChange?: (period: { periodStart: string; periodEnd: string }) => void;
  periodStart: string;
  periodEnd: string;
  onPeriodStartChange: (value: string) => void;
  onPeriodEndChange: (value: string) => void;
};

/**
 * Canonical customer statement renderer. Personal and Business pass identity/context only.
 */
export function CustomerStatementView({
  testId,
  displayName,
  backTo,
  backTestId,
  subjectLoading,
  subjectError,
  subjectErrorDetail,
  statementLoading,
  statementError,
  statementErrorDetail,
  statement,
  periodStart,
  periodEnd,
  onPeriodStartChange,
  onPeriodEndChange,
}: CustomerStatementViewProps) {
  const { t } = useI18n();
  const [entryFilter, setEntryFilter] = useState<StatementEntryFilter>("all");

  const filteredLines = useMemo(() => {
    const lines = statement?.lines ?? [];
    return lines.filter((line) => matchesStatementEntryFilter(line.entryType, entryFilter));
  }, [entryFilter, statement?.lines]);

  if (subjectLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (subjectError) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={subjectErrorDetail ?? t("customers.notFound")}
      />
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid={testId}>
      <PageHeader
        title={t("customers.statementTitle")}
        description={t("customers.statementLede").replace("{name}", displayName)}
        backTo={backTo}
        backLabel={t("customers.backDetail")}
        backTestId={backTestId}
      />

      <Card
        className="customer-statement-toolbar flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end"
        data-testid="statement-filters"
      >
        <div className="flex min-w-0 flex-wrap gap-3">
          <label
            className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="statement-start"
          >
            {t("customers.periodStart")}
            <input
              id="statement-start"
              data-testid="statement-period-start"
              type="date"
              className="exits-input"
              value={periodStart}
              onChange={(event) => onPeriodStartChange(event.target.value)}
            />
          </label>
          <label
            className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="statement-end"
          >
            {t("customers.periodEnd")}
            <input
              id="statement-end"
              data-testid="statement-period-end"
              type="date"
              className="exits-input"
              value={periodEnd}
              onChange={(event) => onPeriodEndChange(event.target.value)}
            />
          </label>
        </div>
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("customers.statementEntryFilter")}
          testId="statement-entry-filters"
          className="shrink-0 sm:ml-auto"
          items={STATEMENT_ENTRY_FILTERS.map((filter) => ({
            key: filter.key,
            label: t(filter.labelKey),
            state: entryFilter === filter.key ? "active" : "idle",
            testId: `statement-entry-filter-${filter.key}`,
            onSelect: () => setEntryFilter(filter.key),
          }))}
        />
      </Card>

      {statementLoading ? <LoadingState label={t("loading.label")} /> : null}
      {statementError ? (
        <ErrorState
          title={t("error.title")}
          detail={statementErrorDetail ?? t("error.detail")}
        />
      ) : null}

      {statement ? (
        <>
          <Card className="flex flex-col gap-3 p-4" data-testid="statement-summary">
            <dl className="branch-mgmt-overview__grid m-0">
              <div className="branch-mgmt-overview__item">
                <dt>
                  <Receipt
                    className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--outstanding"
                    aria-hidden
                  />
                  {t("customers.amountOwed")}
                </dt>
                <dd className="tabular-nums">
                  <MoneyDisplay amount={statement.outstandingBalance} />
                </dd>
              </div>
              <div className="branch-mgmt-overview__item">
                <dt>
                  <Wallet
                    className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--available"
                    aria-hidden
                  />
                  {t("customers.remainingBalance")}
                </dt>
                <dd className="tabular-nums">
                  <MoneyDisplay amount={statement.closingBalance} />
                </dd>
              </div>
              <div className="branch-mgmt-overview__item">
                <dt>
                  <CircleDollarSign
                    className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--limit"
                    aria-hidden
                  />
                  {t("customers.periodCharges")}
                </dt>
                <dd className="tabular-nums">
                  <MoneyDisplay amount={statement.periodCreditTotal} />
                </dd>
              </div>
              <div className="branch-mgmt-overview__item">
                <dt>
                  <Banknote
                    className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--term"
                    aria-hidden
                  />
                  {t("customers.periodPayments")}
                </dt>
                <dd className="tabular-nums">
                  <MoneyDisplay amount={statement.periodRepaymentTotal} />
                </dd>
              </div>
            </dl>
          </Card>

          {statement.lines.length === 0 || filteredLines.length === 0 ? (
            <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
              title={t("customers.statementEmpty")}
              detail={t("customers.statementEmptyDetail")}
            />
          ) : (
            <Card className="overflow-hidden p-0" data-testid="statement-lines">
              <div className="min-w-0 overflow-x-auto">
                <table className="customer-ledger-table w-full min-w-[40rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                  <thead>
                    <tr className="border-b border-border">
                      <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                        {t("transactions.col.dateTime")}
                      </th>
                      <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                        {t("inventory.movementCol.type")}
                      </th>
                      <th className="min-w-[14rem] px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                        {t("expense.description")}
                      </th>
                      <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                        {t("expense.amount")}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredLines.map((line) => {
                      const description = statementLineDescription(line);
                      const isCredit = line.entryType.toLowerCase() === "credit";
                      return (
                        <tr
                          key={line.entryId}
                          className="border-b border-border last:border-b-0"
                          data-testid={`statement-line-${line.entryId}`}
                        >
                          <td className="whitespace-nowrap px-3 py-2.5 align-middle text-muted tabular-nums">
                            {formatStatementWhen(line.recordedAtUtc)}
                          </td>
                          <td className="whitespace-nowrap px-3 py-2.5 align-middle">
                            <StatusChip tone={isCredit ? "warning" : "success"}>
                              {line.entryType}
                            </StatusChip>
                          </td>
                          <td className="max-w-[24rem] px-3 py-2.5 align-middle text-muted">
                            {line.sourceSaleId ? (
                              <Link
                                to={`/sell/sales/${line.sourceSaleId}/summary`}
                                className="line-clamp-2 font-medium text-[var(--exits-primary)] underline-offset-2 hover:underline"
                                data-testid={`statement-sale-link-${line.entryId}`}
                              >
                                {description}
                              </Link>
                            ) : (
                              <span className="line-clamp-2">{description}</span>
                            )}
                          </td>
                          <td className="whitespace-nowrap px-3 py-2.5 align-middle text-right font-semibold tabular-nums">
                            <MoneyDisplay amount={line.amount} />
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </Card>
          )}
        </>
      ) : null}
    </div>
  );
}

export function useStatementPeriodState() {
  const defaults = useMemo(() => defaultStatementPeriod(), []);
  const [periodStart, setPeriodStart] = useState(defaults.periodStart);
  const [periodEnd, setPeriodEnd] = useState(defaults.periodEnd);
  return { periodStart, setPeriodStart, periodEnd, setPeriodEnd };
}

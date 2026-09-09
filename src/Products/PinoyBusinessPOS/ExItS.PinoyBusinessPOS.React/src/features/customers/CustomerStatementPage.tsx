import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getCustomer, getCustomerStatement } from "@/api/pos/pos-customers-client";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

type StatementEntryFilter = "all" | "credit" | "repayment";

const ENTRY_FILTERS: ReadonlyArray<{
  key: StatementEntryFilter;
  labelKey: MessageKey;
}> = [
  { key: "all", labelKey: "customers.statementFilterAll" },
  { key: "credit", labelKey: "customers.statementFilterCredit" },
  { key: "repayment", labelKey: "customers.statementFilterRepayment" },
];

function defaultPeriod() {
  const end = new Date();
  const start = new Date(end);
  start.setDate(start.getDate() - 30);
  const toIsoDate = (value: Date) => value.toISOString().slice(0, 10);
  return { periodStart: toIsoDate(start), periodEnd: toIsoDate(end) };
}

function statementDescription(line: {
  remarks?: string | null;
  status: string;
}): string {
  return line.remarks?.trim() || line.status || "—";
}

function formatStatementWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function matchesEntryFilter(entryType: string, filter: StatementEntryFilter): boolean {
  if (filter === "all") {
    return true;
  }
  return entryType.trim().toLowerCase() === filter;
}

export function CustomerStatementPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  const { boundWorkspace } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const defaults = useMemo(() => defaultPeriod(), []);
  const [periodStart, setPeriodStart] = useState(defaults.periodStart);
  const [periodEnd, setPeriodEnd] = useState(defaults.periodEnd);
  const [entryFilter, setEntryFilter] = useState<StatementEntryFilter>("all");

  const customerQuery = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  const statementQuery = useQuery({
    queryKey: [
      "customers",
      "statement",
      workspace?.organizationId,
      customerId,
      periodStart,
      periodEnd,
    ],
    enabled:
      Boolean(workspace) && Boolean(customerId) && Boolean(periodStart) && Boolean(periodEnd),
    queryFn: ({ signal }) =>
      getCustomerStatement(
        workspace!,
        customerId!,
        {
          periodStart,
          periodEnd,
          organizationDisplayName: boundWorkspace?.organizationDisplayName,
        },
        signal,
      ),
  });

  const filteredLines = useMemo(() => {
    const lines = statementQuery.data?.lines ?? [];
    return lines.filter((line) => matchesEntryFilter(line.entryType, entryFilter));
  }, [entryFilter, statementQuery.data?.lines]);

  if (!workspace || !customerId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (customerQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (customerQuery.isError || !customerQuery.data) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={(customerQuery.error as Error | undefined)?.message ?? t("customers.notFound")}
      />
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customer-statement-page">
      <PageHeader
        title={t("customers.statementTitle")}
        description={t("customers.statementLede").replace("{name}", customerQuery.data.displayName)}
        backTo={`/customers/${customerId}`}
        backLabel={t("customers.backDetail")}
        backTestId="page-header-back-customers"
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
              onChange={(event) => setPeriodStart(event.target.value)}
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
              onChange={(event) => setPeriodEnd(event.target.value)}
            />
          </label>
        </div>
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("customers.statementEntryFilter")}
          testId="statement-entry-filters"
          className="shrink-0 sm:ml-auto"
          items={ENTRY_FILTERS.map((filter) => ({
            key: filter.key,
            label: t(filter.labelKey),
            state: entryFilter === filter.key ? "active" : "idle",
            testId: `statement-entry-filter-${filter.key}`,
            onSelect: () => setEntryFilter(filter.key),
          }))}
        />
      </Card>

      {statementQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {statementQuery.isError ? (
        <ErrorState title={t("error.title")} detail={(statementQuery.error as Error).message} />
      ) : null}

      {statementQuery.data ? (
        <>
          <Card data-testid="statement-summary">
            <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
              <div>
                <dt className="text-muted">{t("customers.amountOwed")}</dt>
                <dd className="m-0 font-semibold">
                  <MoneyDisplay amount={statementQuery.data.outstandingBalance} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("customers.remainingBalance")}</dt>
                <dd className="m-0 font-semibold">
                  <MoneyDisplay amount={statementQuery.data.closingBalance} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("customers.periodCharges")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={statementQuery.data.periodCreditTotal} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("customers.periodPayments")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={statementQuery.data.periodRepaymentTotal} />
                </dd>
              </div>
            </dl>
          </Card>

          {statementQuery.data.lines.length === 0 ? (
            <EmptyState
              title={t("customers.statementEmpty")}
              detail={t("customers.statementEmptyDetail")}
            />
          ) : filteredLines.length === 0 ? (
            <EmptyState
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
                      const description = statementDescription(line);
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

import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getCustomer, getCustomerStatement } from "@/api/pos/pos-customers-client";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function defaultPeriod() {
  const end = new Date();
  const start = new Date(end);
  start.setDate(start.getDate() - 30);
  const toIsoDate = (value: Date) => value.toISOString().slice(0, 10);
  return { periodStart: toIsoDate(start), periodEnd: toIsoDate(end) };
}

export function CustomerStatementPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  const { boundWorkspace } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const defaults = useMemo(() => defaultPeriod(), []);
  const [periodStart, setPeriodStart] = useState(defaults.periodStart);
  const [periodEnd, setPeriodEnd] = useState(defaults.periodEnd);

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

      <Card className="flex flex-wrap gap-3">
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="statement-start"
        >
          {t("customers.periodStart")}
          <input
            id="statement-start"
            data-testid="statement-period-start"
            type="date"
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
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
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={periodEnd}
            onChange={(event) => setPeriodEnd(event.target.value)}
          />
        </label>
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
          ) : (
            <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="statement-lines">
              {statementQuery.data.lines.map((line) => (
                <li key={line.entryId}>
                  <Card className="p-3">
                    <div className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]">
                      <div className="min-w-0">
                        <p className="m-0 font-semibold">{line.entryType}</p>
                        <p className="mb-0 mt-1 truncate text-muted">
                          {line.remarks?.trim() || line.status}
                        </p>
                      </div>
                      <MoneyDisplay amount={line.amount} />
                    </div>
                  </Card>
                </li>
              ))}
            </ul>
          )}
        </>
      ) : null}
    </div>
  );
}

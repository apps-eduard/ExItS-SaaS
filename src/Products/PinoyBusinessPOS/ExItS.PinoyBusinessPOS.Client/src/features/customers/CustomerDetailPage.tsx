import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canEditCustomer, canRecordRepayment, canViewStatement } from "@/access/pos-capabilities";
import {
  deactivateCustomer,
  getCustomer,
  getCustomerCreditSummary,
  hasExItsPersonalLink,
  listCustomerCreditEntries,
  listCustomerRepayments,
  reactivateCustomer,
} from "@/api/pos/pos-customers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function CustomerDetailPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowEdit = canEditCustomer(sessionGrant);
  const allowRepay = canRecordRepayment(sessionGrant);
  const allowStatement = canViewStatement(sessionGrant);

  const customerQuery = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  const summaryQuery = useQuery({
    queryKey: ["customers", "credit-summary", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) => getCustomerCreditSummary(workspace!, customerId!, signal),
  });

  const creditsQuery = useQuery({
    queryKey: ["customers", "credits", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) =>
      listCustomerCreditEntries(workspace!, customerId!, { pageSize: 20 }, signal),
  });

  const repaymentsQuery = useQuery({
    queryKey: ["customers", "repayments", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) =>
      listCustomerRepayments(workspace!, customerId!, { pageSize: 20 }, signal),
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

  const customer = customerQuery.data;
  const amountOwed = summaryQuery.data?.outstandingAmount ?? 0;
  const linked = hasExItsPersonalLink(customer);

  async function toggleStatus() {
    if (!allowEdit || acting || !workspace || !customerId) {
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      if (customer.status.toLowerCase() === "active") {
        await deactivateCustomer(workspace, customerId);
      } else {
        await reactivateCustomer(workspace, customerId);
      }
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.detail"));
    } finally {
      setActing(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customer-detail-page">
      <PageHeader title={customer.displayName} description={t("customers.detailLede")} />
      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone={customer.status.toLowerCase() === "active" ? "success" : "warning"}>
          {customer.status}
        </StatusChip>
        {linked ? (
          <span data-testid="customer-link-status">
            <StatusChip tone="info">{t("customers.linkedPersonal")}</StatusChip>
          </span>
        ) : (
          <span
            data-testid="customer-link-status-none"
            className="text-[length:var(--exits-text-xs)] text-muted"
          >
            {t("customers.notLinked")}
          </span>
        )}
      </div>

      {actionError ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
        </Card>
      ) : null}

      <Card data-testid="customer-amount-owed">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.amountOwed")}
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-lg)] font-semibold">
          <MoneyDisplay amount={amountOwed} testId="customer-amount-owed-value" />
        </p>
      </Card>

      <Card>
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div>
            <dt className="text-muted">{t("customers.mobile")}</dt>
            <dd className="m-0">{customer.mobileNumber?.trim() || "—"}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("customers.address")}</dt>
            <dd className="m-0">{customer.address?.trim() || "—"}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("customers.notes")}</dt>
            <dd className="m-0 whitespace-pre-wrap">{customer.notes?.trim() || "—"}</dd>
          </div>
        </dl>
      </Card>

      <div className="flex flex-wrap gap-2">
        {allowEdit ? (
          <Button asChild className="min-h-11" data-testid="customer-edit">
            <Link to={`/customers/${customerId}/edit`}>{t("customers.edit")}</Link>
          </Button>
        ) : null}
        {allowRepay ? (
          <Button asChild variant="ghost" className="min-h-11" data-testid="customer-repay">
            <Link to={`/customers/${customerId}/repay`}>{t("customers.recordPayment")}</Link>
          </Button>
        ) : null}
        {allowStatement ? (
          <Button asChild variant="ghost" className="min-h-11" data-testid="customer-statement">
            <Link to={`/customers/${customerId}/statement`}>{t("customers.viewStatement")}</Link>
          </Button>
        ) : null}
        {allowEdit ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            data-testid="customer-toggle-status"
            disabled={acting}
            onClick={() => void toggleStatus()}
          >
            {customer.status.toLowerCase() === "active"
              ? t("customers.deactivate")
              : t("customers.reactivate")}
          </Button>
        ) : null}
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/customers">{t("customers.back")}</Link>
        </Button>
      </div>

      <section data-testid="customer-credits-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.creditsTitle")}
        </h2>
        {creditsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {creditsQuery.isSuccess && creditsQuery.data.items.length === 0 ? (
          <EmptyState
            title={t("customers.creditsEmpty")}
            detail={t("customers.creditsEmptyDetail")}
          />
        ) : null}
        <ul className="mt-2 flex list-none flex-col gap-2 p-0">
          {creditsQuery.data?.items.map((entry) => (
            <li key={entry.creditEntryId}>
              <Card className="p-3" data-testid={`customer-credit-${entry.creditEntryId}`}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="m-0 font-semibold">
                      <MoneyDisplay amount={entry.amount} />
                    </p>
                    <p className="mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                      {entry.remarks || entry.status}
                    </p>
                  </div>
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {entry.status}
                  </span>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </section>

      <section data-testid="customer-payments-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.paymentsTitle")}
        </h2>
        {repaymentsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {repaymentsQuery.isSuccess && repaymentsQuery.data.items.length === 0 ? (
          <EmptyState
            title={t("customers.paymentsEmpty")}
            detail={t("customers.paymentsEmptyDetail")}
          />
        ) : null}
        <ul className="mt-2 flex list-none flex-col gap-2 p-0">
          {repaymentsQuery.data?.items.map((payment) => (
            <li key={payment.repaymentId}>
              <Card className="p-3" data-testid={`customer-payment-${payment.repaymentId}`}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="m-0 font-semibold">
                      <MoneyDisplay amount={payment.amount} />
                    </p>
                    <p className="mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                      {payment.remarks?.trim() || payment.status}
                    </p>
                  </div>
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {payment.status}
                  </span>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}

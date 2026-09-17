import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { getCustomer, getCustomerStatement } from "@/api/pos/pos-customers-client";
import { LoadingState } from "@/components/exits/LoadingState";
import {
  CustomerStatementView,
  useStatementPeriodState,
} from "@/features/customers/CustomerStatementView";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

/** Personal customer statement — thin adapter over shared CustomerStatementView. */
export function CustomerStatementPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  const { boundWorkspace } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const { periodStart, setPeriodStart, periodEnd, setPeriodEnd } = useStatementPeriodState();

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

  return (
    <CustomerStatementView
      testId="customer-statement-page"
      displayName={customerQuery.data?.displayName ?? ""}
      backTo={`/customers/${customerId}`}
      backTestId="page-header-back-customers"
      subjectLoading={customerQuery.isLoading}
      subjectError={customerQuery.isError || !customerQuery.data}
      subjectErrorDetail={
        (customerQuery.error as Error | undefined)?.message ?? t("customers.notFound")
      }
      statementLoading={statementQuery.isLoading}
      statementError={statementQuery.isError}
      statementErrorDetail={(statementQuery.error as Error | undefined)?.message}
      statement={statementQuery.data ?? null}
      periodStart={periodStart}
      periodEnd={periodEnd}
      onPeriodStartChange={setPeriodStart}
      onPeriodEndChange={setPeriodEnd}
    />
  );
}

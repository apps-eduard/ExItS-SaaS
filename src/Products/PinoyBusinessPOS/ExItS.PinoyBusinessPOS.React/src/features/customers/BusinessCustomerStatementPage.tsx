import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import {
  getBusinessCustomer,
  getBusinessCustomerStatement,
} from "@/api/pos/pos-connected-suppliers-client";
import { LoadingState } from "@/components/exits/LoadingState";
import {
  CustomerStatementView,
  useStatementPeriodState,
} from "@/features/customers/CustomerStatementView";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

/** Business customer statement — thin adapter over shared CustomerStatementView. */
export function BusinessCustomerStatementPage() {
  const { t } = useI18n();
  const { connectionId } = useParams<{ connectionId: string }>();
  const { boundWorkspace } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const { periodStart, setPeriodStart, periodEnd, setPeriodEnd } = useStatementPeriodState();
  const backTo = connectionId ? `/customers/business/${connectionId}` : "/customers?kind=businesses";

  const customerQuery = useQuery({
    queryKey: ["business-customers", "detail", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId),
    queryFn: ({ signal }) => getBusinessCustomer(workspace!, connectionId!, signal),
  });

  const statementQuery = useQuery({
    queryKey: [
      "business-customers",
      "statement",
      workspace?.organizationId,
      connectionId,
      periodStart,
      periodEnd,
    ],
    enabled:
      Boolean(workspace) && Boolean(connectionId) && Boolean(periodStart) && Boolean(periodEnd),
    queryFn: ({ signal }) =>
      getBusinessCustomerStatement(
        workspace!,
        connectionId!,
        {
          periodStart,
          periodEnd,
          organizationDisplayName: boundWorkspace?.organizationDisplayName,
        },
        signal,
      ),
  });

  if (!workspace || !connectionId) {
    return <LoadingState label={t("session.loading")} />;
  }

  const displayName = customerQuery.data?.organizationDisplayName ?? "";

  return (
    <CustomerStatementView
      testId="business-customer-statement-page"
      displayName={displayName}
      backTo={backTo}
      backTestId="page-header-back-business-customer"
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

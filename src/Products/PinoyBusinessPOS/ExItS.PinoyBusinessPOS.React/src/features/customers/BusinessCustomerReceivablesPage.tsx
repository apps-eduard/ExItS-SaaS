import { useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  getBusinessCustomer,
  getBusinessCustomerUtangSummary,
  listBusinessCustomerReceivables,
} from "@/api/pos/pos-connected-suppliers-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { B2bObligationsView } from "@/features/b2b-obligations/B2bObligationsView";
import {
  mapReceivableToObligation,
  type B2bObligationListFilter,
} from "@/features/b2b-obligations/b2b-obligations-model";
import { RecordPaymentModal } from "@/features/customers/RecordPaymentModal";
import { useI18n } from "@/i18n/I18nProvider";
import { canRecordRepayment } from "@/access/pos-capabilities";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function parseFilter(raw: string | null): B2bObligationListFilter {
  switch ((raw ?? "").trim().toLowerCase()) {
    case "overdue":
      return "overdue";
    case "paid":
      return "paid";
    case "all":
      return "all";
    default:
      return "open";
  }
}

export function BusinessCustomerReceivablesPage() {
  const { t } = useI18n();
  const { connectionId } = useParams<{ connectionId: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const allowRepay = canRecordRepayment(sessionGrant);
  const [paymentCreditEntryId, setPaymentCreditEntryId] = useState<string | null>(null);
  const [paymentOpen, setPaymentOpen] = useState(false);
  const filter = parseFilter(searchParams.get("filter"));

  const backTo = connectionId
    ? `/customers/business/${connectionId}`
    : "/customers?kind=businesses";

  const customerQuery = useQuery({
    queryKey: ["business-customers", "detail", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId),
    queryFn: ({ signal }) => getBusinessCustomer(workspace!, connectionId!, signal),
  });

  const summaryQuery = useQuery({
    queryKey: ["business-customers", "utang-summary", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId) && online,
    queryFn: ({ signal }) => getBusinessCustomerUtangSummary(workspace!, connectionId!, signal),
  });

  const receivablesQuery = useQuery({
    queryKey: ["business-customers", "receivables", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId) && online,
    queryFn: ({ signal }) => listBusinessCustomerReceivables(workspace!, connectionId!, signal),
  });

  const obligations = useMemo(
    () => (receivablesQuery.data ?? []).map(mapReceivableToObligation),
    [receivablesQuery.data],
  );

  useEffect(() => {
    if (!searchParams.get("filter")) {
      const next = new URLSearchParams(searchParams);
      next.set("filter", "open");
      setSearchParams(next, { replace: true });
    }
  }, [searchParams, setSearchParams]);

  if (!workspace || !connectionId) {
    return <LoadingState label={t("session.loading")} />;
  }

  const displayName = customerQuery.data?.organizationDisplayName ?? "";
  const outstanding = summaryQuery.data?.outstandingAmount ?? 0;

  return (
    <div className="flex flex-col gap-4" data-testid="business-customer-receivables-page">
      <PageHeader
        title={t("customers.receivables.pageTitleAll")}
        subtitle={displayName || undefined}
        backTo={backTo}
        backLabel={t("customers.backDetail")}
        backTestId="page-header-back-business-customer"
      />

      {!online ? (
        <ErrorState
          title={t("customers.receivables.offline")}
          detail={t("customers.receivables.offlineDetail")}
        />
      ) : null}

      {online && (customerQuery.isLoading || receivablesQuery.isLoading) ? (
        <LoadingState label={t("customers.receivables.loading")} />
      ) : null}

      {online && (customerQuery.isError || receivablesQuery.isError) ? (
        <ErrorState
          title={t("customers.receivables.loadFailed")}
          detail={
            (receivablesQuery.error as Error | undefined)?.message ??
            (customerQuery.error as Error | undefined)?.message ??
            t("error.detail")
          }
        />
      ) : null}

      {online && receivablesQuery.isSuccess ? (
        <B2bObligationsView
          perspective="receivable"
          items={obligations}
          filter={filter}
          onFilterChange={(next) => {
            const params = new URLSearchParams(searchParams);
            params.set("filter", next);
            setSearchParams(params, { replace: true });
          }}
          canRecordPayment={allowRepay}
          onRecordPayment={(id) => {
            setPaymentCreditEntryId(id);
            setPaymentOpen(true);
          }}
          testIdPrefix="business-receivables"
        />
      ) : null}

      {allowRepay ? (
        <RecordPaymentModal
          open={paymentOpen}
          onOpenChange={(open) => {
            setPaymentOpen(open);
            if (!open) {
              setPaymentCreditEntryId(null);
            }
          }}
          customerKind="business"
          connectionId={connectionId}
          displayName={displayName}
          outstandingBalance={outstanding}
          preselectedCreditEntryId={paymentCreditEntryId}
          onSuccess={() => {
            void receivablesQuery.refetch();
            void summaryQuery.refetch();
          }}
        />
      ) : null}

      <p className="m-0">
        <Link
          className="text-[length:var(--exits-text-sm)]"
          to={backTo}
          data-testid="business-receivables-back-detail"
        >
          {t("customers.receivables.backToCustomer")}
        </Link>
      </p>
    </div>
  );
}

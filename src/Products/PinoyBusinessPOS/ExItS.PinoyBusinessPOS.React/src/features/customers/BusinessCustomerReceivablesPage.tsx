import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Wallet } from "lucide-react";
import {
  getBusinessCustomer,
  getBusinessCustomerUtangSummary,
  listBusinessCustomerReceivables,
} from "@/api/pos/pos-connected-suppliers-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { RecordPaymentModal } from "@/features/customers/RecordPaymentModal";
import { orderOpenReceivables } from "@/features/customers/business-payment-allocation";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { canRecordRepayment } from "@/access/pos-capabilities";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function receivableSourceLabelKey(sourceType: string | null | undefined): MessageKey {
  const normalized = (sourceType ?? "").trim().toLowerCase();
  if (normalized === "po") {
    return "customers.receivables.source.po";
  }
  if (normalized === "directpurchase" || normalized === "direct") {
    return "customers.receivables.source.direct";
  }
  if (normalized === "sale") {
    return "customers.receivables.source.sale";
  }
  return "customers.receivables.source.other";
}

function formatDueDate(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }
  const trimmed = value.trim();
  if (/^\d{4}-\d{2}-\d{2}/.test(trimmed)) {
    return trimmed.slice(0, 10);
  }
  return trimmed;
}

export function BusinessCustomerReceivablesPage() {
  const { t } = useI18n();
  const { connectionId } = useParams<{ connectionId: string }>();
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const allowRepay = canRecordRepayment(sessionGrant);
  const [paymentCreditEntryId, setPaymentCreditEntryId] = useState<string | null>(null);
  const [paymentOpen, setPaymentOpen] = useState(false);

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

  const openItems = useMemo(() => {
    const items = (receivablesQuery.data ?? []).filter((item) => item.outstandingBalance > 0);
    return orderOpenReceivables(
      items.map((item) => ({
        ...item,
        dueDate: item.dueDate,
        createdAtUtc: item.createdAtUtc,
      })),
    );
  }, [receivablesQuery.data]);

  if (!workspace || !connectionId) {
    return <LoadingState label={t("session.loading")} />;
  }

  const displayName = customerQuery.data?.organizationDisplayName ?? "";
  const outstanding = summaryQuery.data?.outstandingAmount ?? 0;

  return (
    <div className="flex flex-col gap-4" data-testid="business-customer-receivables-page">
      <PageHeader
        title={t("customers.receivables.pageTitle")}
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

      {online && receivablesQuery.isSuccess && openItems.length === 0 ? (
        <EmptyState
          title={t("customers.receivables.empty")}
          detail={t("customers.receivables.emptyDetail")}
        />
      ) : null}

      {online && openItems.length > 0 ? (
        <Card className="overflow-hidden p-0">
          <div className="min-w-0 overflow-x-auto">
            <table className="w-full min-w-[36rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead>
                <tr className="border-b border-border bg-surface">
                  <th className="px-3 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("customers.receivables.columnSource")}
                  </th>
                  <th className="px-3 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("customers.receivables.columnReference")}
                  </th>
                  <th className="px-3 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("customers.receivables.dueDate")}
                  </th>
                  <th className="px-3 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("customers.receivables.outstanding")}
                  </th>
                  <th className="px-3 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("customers.recordPayment")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {openItems.map((item) => (
                  <tr
                    key={item.creditEntryId}
                    className="border-b border-border last:border-b-0"
                    data-testid={`business-receivable-row-${item.creditEntryId}`}
                  >
                    <td className="px-3 py-2 align-middle">
                      {t(receivableSourceLabelKey(item.sourceType))}
                    </td>
                    <td className="px-3 py-2 align-middle">
                      {item.sourceReference?.trim() || "—"}
                    </td>
                    <td className="px-3 py-2 align-middle tabular-nums">
                      {formatDueDate(item.dueDate)}
                    </td>
                    <td className="px-3 py-2 align-middle tabular-nums">
                      <MoneyDisplay amount={item.outstandingBalance} />
                    </td>
                    <td className="px-3 py-2 align-middle">
                      {allowRepay ? (
                        <Button
                          type="button"
                          variant="success"
                          data-testid={`business-receivable-repay-${item.creditEntryId}`}
                          onClick={() => {
                            setPaymentCreditEntryId(item.creditEntryId);
                            setPaymentOpen(true);
                          }}
                        >
                          <Wallet className="size-4 shrink-0" aria-hidden />
                          {t("customers.recordPayment")}
                        </Button>
                      ) : (
                        "—"
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
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

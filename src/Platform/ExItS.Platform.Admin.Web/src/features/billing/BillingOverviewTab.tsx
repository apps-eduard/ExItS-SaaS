import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getBillingOperationsSummary } from "@/api/billing/billing-operations-client";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import { formatNumber } from "@/lib/i18n/format";

type SummaryCard = {
  id: string;
  labelKey: "billing.summary.pending" | "billing.summary.rejected" | "billing.summary.voided" | "billing.summary.confirmed" | "billing.summary.pastDue" | "billing.summary.grace";
  count: number;
  href: string;
  tone: "warning" | "danger" | "success" | "neutral";
};

export function BillingOverviewTab() {
  const { t, language } = usePreferences();
  const query = useQuery({
    queryKey: ["billing-operations", "summary"],
    queryFn: ({ signal }) => getBillingOperationsSummary(env.platformApiBaseUrl, signal),
  });

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load billing summary" })
    : null;

  const cards: SummaryCard[] = query.data
    ? [
        {
          id: "pending",
          labelKey: "billing.summary.pending",
          count: query.data.pendingPaymentCount,
          href: "/admin/payments/issues?issueType=pending-payment",
          tone: "warning",
        },
        {
          id: "rejected",
          labelKey: "billing.summary.rejected",
          count: query.data.rejectedPaymentCount,
          href: "/admin/payments/issues?issueType=rejected-payment",
          tone: "danger",
        },
        {
          id: "voided",
          labelKey: "billing.summary.voided",
          count: query.data.voidedPaymentCount,
          href: "/admin/payments/issues?issueType=voided-payment",
          tone: "danger",
        },
        {
          id: "confirmed",
          labelKey: "billing.summary.confirmed",
          count: query.data.confirmedPaymentCount,
          href: "/admin/payments/list?status=Confirmed",
          tone: "success",
        },
        {
          id: "past-due",
          labelKey: "billing.summary.pastDue",
          count: query.data.pastDueSubscriptionCount,
          href: "/admin/payments/issues?issueType=past-due-subscription",
          tone: "danger",
        },
        {
          id: "grace",
          labelKey: "billing.summary.grace",
          count: query.data.gracePeriodSubscriptionCount,
          href: "/admin/payments/issues?issueType=grace-period-subscription",
          tone: "warning",
        },
      ]
    : [];

  if (query.isPending) {
    return (
      <div role="status" aria-busy="true" aria-label={t("billing.summary.loading")}>
        <DashboardWidgetSkeleton rows={4} />
      </div>
    );
  }

  if (query.isError && diagnostic) {
    return (
      <ErrorState diagnostic={diagnostic} headingLevel="h2" onRetry={() => void query.refetch()} />
    );
  }

  return (
    <div className="grid gap-3">
      <p className="text-[length:var(--exits-text-sm)] text-muted">{t("billing.summary.hint")}</p>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {cards.map((card) => (
          <Link
            key={card.id}
            to={card.href}
            className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 hover:bg-surface-muted/40 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
          >
            <p className="text-[length:var(--exits-text-xs)] font-medium text-muted">{t(card.labelKey)}</p>
            <p className="mt-1 text-[length:var(--exits-text-2xl)] font-semibold tabular-nums text-foreground">
              {formatNumber(card.count, language)}
            </p>
          </Link>
        ))}
      </div>
    </div>
  );
}

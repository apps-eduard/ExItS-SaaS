import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardStatCard } from "@/components/exits/dashboard/DashboardStatCard";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { formatStatusLine } from "@/components/exits/dashboard/StatusBreakdown";
import { usePaymentStatusCountQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";

export function PaymentsSummaryWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const confirmed = usePaymentStatusCountQuery(enabled, "Confirmed");
  const pending = usePaymentStatusCountQuery(enabled, "PendingConfirmation");
  const loading = confirmed.isPending || pending.isPending;
  const failed = confirmed.isError || pending.isError;

  return (
    <DashboardSection variant="metric" title={t("dashboard.payments.title")}>
      {loading ? <DashboardWidgetSkeleton rows={2} /> : null}
      {failed && !loading ? (
        <DashboardWidgetError
          onRetry={() => {
            void confirmed.refetch();
            void pending.refetch();
          }}
        />
      ) : null}
      {!loading && !failed && confirmed.data && pending.data ? (
        <DashboardStatCard
          value={formatNumber(confirmed.data.totalCount, language)}
          detail={formatStatusLine([
            {
              key: "Confirmed",
              label: t("dashboard.payments.confirmed"),
              value: formatNumber(confirmed.data.totalCount, language),
            },
            {
              key: "Pending",
              label: t("dashboard.payments.pending"),
              value: formatNumber(pending.data.totalCount, language),
            },
          ])}
        />
      ) : null}
    </DashboardSection>
  );
}

import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardStatCard } from "@/components/exits/dashboard/DashboardStatCard";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { formatStatusLine } from "@/components/exits/dashboard/StatusBreakdown";
import { useOrganizationSummaryQueries } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";

export function OrganizationsSummaryWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const { total, active, closed, suspended } = useOrganizationSummaryQueries(enabled);
  const loading = total.isPending || active.isPending || closed.isPending || suspended.isPending;
  const failed = total.isError || active.isError || closed.isError || suspended.isError;

  return (
    <DashboardSection variant="metric" title={t("dashboard.organizations.title")}>
      {loading ? <DashboardWidgetSkeleton rows={2} /> : null}
      {failed && !loading ? (
        <DashboardWidgetError
          onRetry={() => {
            void total.refetch();
            void active.refetch();
            void closed.refetch();
            void suspended.refetch();
          }}
        />
      ) : null}
      {!loading && !failed && total.data && active.data && closed.data && suspended.data ? (
        <DashboardStatCard
          value={formatNumber(total.data.totalCount, language)}
          detail={formatStatusLine([
            {
              key: "Active",
              label: t("dashboard.status.Active"),
              value: formatNumber(active.data.totalCount, language),
            },
            {
              key: "Suspended",
              label: t("dashboard.status.Suspended"),
              value: formatNumber(suspended.data.totalCount, language),
            },
            {
              key: "Closed",
              label: t("dashboard.status.Closed"),
              value: formatNumber(closed.data.totalCount, language),
            },
          ])}
        />
      ) : null}
    </DashboardSection>
  );
}

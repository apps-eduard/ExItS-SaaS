import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardStatCard } from "@/components/exits/dashboard/DashboardStatCard";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { StatusBreakdown } from "@/components/exits/dashboard/StatusBreakdown";
import { useSubscriptionSummaryQueries } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";

export function SubscriptionsSummaryWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const { total, trialing, active, pastDue, gracePeriod } = useSubscriptionSummaryQueries(enabled);
  const loading =
    total.isPending ||
    trialing.isPending ||
    active.isPending ||
    pastDue.isPending ||
    gracePeriod.isPending;
  const failed =
    total.isError || trialing.isError || active.isError || pastDue.isError || gracePeriod.isError;

  return (
    <DashboardSection
      title={t("dashboard.subscriptions.title")}
      description={t("dashboard.subscriptions.hint")}
    >
      {loading ? <DashboardWidgetSkeleton rows={2} /> : null}
      {failed && !loading ? (
        <DashboardWidgetError
          onRetry={() => {
            void total.refetch();
            void trialing.refetch();
            void active.refetch();
            void pastDue.refetch();
            void gracePeriod.refetch();
          }}
        />
      ) : null}
      {!loading &&
      !failed &&
      total.data &&
      trialing.data &&
      active.data &&
      pastDue.data &&
      gracePeriod.data ? (
        <div className="grid gap-3">
          <DashboardStatCard
            label={t("dashboard.subscriptions.total")}
            value={formatNumber(total.data.totalCount, language)}
          />
          <StatusBreakdown
            items={[
              {
                key: "Trialing",
                label: t("dashboard.status.Trialing"),
                value: formatNumber(trialing.data.totalCount, language),
                tone: "info",
              },
              {
                key: "Active",
                label: t("dashboard.status.Active"),
                value: formatNumber(active.data.totalCount, language),
                tone: "success",
              },
              {
                key: "PastDue",
                label: t("dashboard.status.PastDue"),
                value: formatNumber(pastDue.data.totalCount, language),
                tone: "danger",
              },
              {
                key: "GracePeriod",
                label: t("dashboard.status.GracePeriod"),
                value: formatNumber(gracePeriod.data.totalCount, language),
                tone: "warning",
              },
            ]}
          />
        </div>
      ) : null}
    </DashboardSection>
  );
}

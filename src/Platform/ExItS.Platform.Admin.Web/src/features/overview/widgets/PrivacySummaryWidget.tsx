import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardStatCard } from "@/components/exits/dashboard/DashboardStatCard";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { formatStatusLine } from "@/components/exits/dashboard/StatusBreakdown";
import { usePrivacyOverviewQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";

export function PrivacySummaryWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const query = usePrivacyOverviewQuery(enabled);

  return (
    <DashboardSection variant="metric" title={t("dashboard.privacy.title")}>
      {query.isPending ? <DashboardWidgetSkeleton rows={2} /> : null}
      {query.isError ? <DashboardWidgetError onRetry={() => void query.refetch()} /> : null}
      {query.data ? (
        <DashboardStatCard
          value={query.data.overallReadiness || t("dashboard.privacy.readinessUnknown")}
          detail={formatStatusLine([
            {
              key: "Ready",
              label: t("dashboard.privacy.ready"),
              value: formatNumber(query.data.readyCount, language),
            },
            {
              key: "Action",
              label: t("dashboard.privacy.actionNeeded"),
              value: formatNumber(query.data.actionNeededCount, language),
            },
          ])}
        />
      ) : null}
    </DashboardSection>
  );
}

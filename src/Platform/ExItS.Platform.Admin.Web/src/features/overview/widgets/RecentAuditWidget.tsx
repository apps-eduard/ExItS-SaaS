import { RecentActivityList } from "@/components/exits/dashboard/RecentActivityList";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { useRecentAuditQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatDate } from "@/lib/i18n/format";
import type { MessageKey } from "@/lib/i18n/messages";

function outcomeTone(outcome: string): "success" | "warning" | "danger" | "neutral" {
  if (outcome === "Succeeded") {
    return "success";
  }
  if (outcome === "Denied") {
    return "warning";
  }
  if (outcome === "Failed") {
    return "danger";
  }
  return "neutral";
}

function outcomeLabel(t: (key: MessageKey) => string, outcome: string): string {
  if (outcome === "Succeeded") {
    return t("dashboard.audit.outcome.Succeeded");
  }
  if (outcome === "Denied") {
    return t("dashboard.audit.outcome.Denied");
  }
  if (outcome === "Failed") {
    return t("dashboard.audit.outcome.Failed");
  }
  return outcome;
}

export function RecentAuditWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const query = useRecentAuditQuery(enabled);

  return (
    <DashboardSection title={t("dashboard.audit.title")} description={t("dashboard.audit.hint")}>
      {query.isPending ? <DashboardWidgetSkeleton rows={4} /> : null}
      {query.isError ? <DashboardWidgetError onRetry={() => void query.refetch()} /> : null}
      {query.data ? (
        <RecentActivityList
          emptyLabel={t("dashboard.audit.empty")}
          items={query.data.items.map((record) => ({
            id: record.id,
            title: record.actionCode,
            meta: `${record.actorIdentifier} · ${formatDate(new Date(record.occurredAtUtc), language)}`,
            outcomeLabel: outcomeLabel(t, record.outcome),
            tone: outcomeTone(record.outcome),
          }))}
        />
      ) : null}
    </DashboardSection>
  );
}

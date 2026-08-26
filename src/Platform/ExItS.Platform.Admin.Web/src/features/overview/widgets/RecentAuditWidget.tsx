import { Link } from "react-router-dom";
import { RecentActivityList } from "@/components/exits/dashboard/RecentActivityList";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { useRecentAuditQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatDate } from "@/lib/i18n/format";
import type { MessageKey } from "@/lib/i18n/messages";
import {
  presentAuditAction,
  presentAuditActor,
  presentAuditType,
} from "@/lib/audit/audit-presentation";

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
          caption={t("dashboard.audit.title")}
          emptyLabel={t("dashboard.audit.empty")}
          columns={{
            action: t("dashboard.table.action"),
            actor: t("dashboard.table.actor"),
            context: t("dashboard.table.context"),
            time: t("dashboard.table.time"),
            outcome: t("dashboard.table.outcome"),
          }}
          items={query.data.items.map((record) => {
            const action = presentAuditAction(record.actionCode, t);
            const actor = presentAuditActor(record.actorIdentifier, t);
            const context = presentAuditType(record.targetType, t);
            return {
              id: record.id,
              title: action.label,
              rawTitle: action.raw,
              actor: actor.label,
              actorDetail: actor.detail,
              rawActor: actor.raw,
              context: context.label,
              rawContext: context.raw,
              time: formatDate(new Date(record.occurredAtUtc), language),
              outcomeLabel: outcomeLabel(t, record.outcome),
              tone: outcomeTone(record.outcome),
            };
          })}
        />
      ) : null}
      <p className="mt-3">
        <Link
          to="/admin/audit"
          className="text-[length:var(--exits-text-xs)] font-medium text-foreground underline-offset-2 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
        >
          {t("dashboard.audit.viewLog")}
        </Link>
      </p>
    </DashboardSection>
  );
}

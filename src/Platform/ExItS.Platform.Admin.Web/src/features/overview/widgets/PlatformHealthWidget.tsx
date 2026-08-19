import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { usePlatformHealthQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import type { HealthReportedStatus } from "@/api/ops/health-client";
import type { MessageKey } from "@/lib/i18n/messages";

function healthTone(status: HealthReportedStatus): "success" | "warning" | "danger" | "neutral" {
  if (status === "Healthy") {
    return "success";
  }
  if (status === "Degraded") {
    return "warning";
  }
  if (status === "Unhealthy") {
    return "danger";
  }
  return "neutral";
}

function healthLabel(
  t: (key: MessageKey) => string,
  status: HealthReportedStatus,
  rawBody: string,
): string {
  if (status === "Healthy") {
    return t("dashboard.health.Healthy");
  }
  if (status === "Degraded") {
    return t("dashboard.health.Degraded");
  }
  if (status === "Unhealthy") {
    return t("dashboard.health.Unhealthy");
  }
  return rawBody.length > 0 ? rawBody : t("dashboard.health.unknown");
}

export function PlatformHealthWidget({ enabled }: { enabled: boolean }) {
  const { t } = usePreferences();
  const query = usePlatformHealthQuery(enabled);

  return (
    <DashboardSection
      variant="quiet"
      title={t("dashboard.health.title")}
      description={t("dashboard.health.hint")}
    >
      {query.isPending ? <DashboardWidgetSkeleton rows={2} /> : null}
      {query.isError ? <DashboardWidgetError onRetry={() => void query.refetch()} /> : null}
      {query.data ? (
        <ul className="grid gap-1.5">
          <li className="flex items-center justify-between gap-3">
            <span className="min-w-0 break-words text-[length:var(--exits-text-sm)] text-muted">
              {t("dashboard.health.liveness")}
            </span>
            <StatusIndicator
              tone={healthTone(query.data.liveness.reportedStatus)}
              label={healthLabel(
                t,
                query.data.liveness.reportedStatus,
                query.data.liveness.rawBody,
              )}
            />
          </li>
          <li className="flex items-center justify-between gap-3">
            <span className="min-w-0 break-words text-[length:var(--exits-text-sm)] text-muted">
              {t("dashboard.health.readiness")}
            </span>
            <StatusIndicator
              tone={healthTone(query.data.readiness.reportedStatus)}
              label={healthLabel(
                t,
                query.data.readiness.reportedStatus,
                query.data.readiness.rawBody,
              )}
            />
          </li>
        </ul>
      ) : null}
    </DashboardSection>
  );
}

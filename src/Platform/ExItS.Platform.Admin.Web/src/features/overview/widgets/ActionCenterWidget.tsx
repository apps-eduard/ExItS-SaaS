import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  actionCenterItemHref,
  getActionCenter,
  type ActionCenterItem,
} from "@/api/admin/action-center-client";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import type { DashboardAuthorization } from "@/features/overview/use-dashboard-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

function severityTone(item: ActionCenterItem): "success" | "warning" | "danger" | "neutral" {
  if (item.severity === "danger") return "danger";
  if (item.severity === "warning") return "warning";
  return "neutral";
}

function severityLabel(item: ActionCenterItem, t: (key: MessageKey) => string): string {
  if (item.severity === "danger") return t("dashboard.actionCenter.severity.danger");
  if (item.severity === "warning") return t("dashboard.actionCenter.severity.warning");
  return t("dashboard.actionCenter.severity.neutral");
}

export function ActionCenterWidget({ access }: { access: DashboardAuthorization }) {
  const { t } = usePreferences();
  const enabled = access.status === "loaded" && access.canViewPortfolio;

  const query = useQuery({
    queryKey: ["action-center"],
    enabled,
    queryFn: ({ signal }) => getActionCenter(env.platformApiBaseUrl, signal),
  });

  if (!enabled) {
    return null;
  }

  return (
    <DashboardSection
      title={t("dashboard.actionCenter.title")}
      description={t("dashboard.actionCenter.hint")}
    >
      {query.isPending ? <DashboardWidgetSkeleton rows={4} /> : null}
      {query.isError ? (
        <DashboardWidgetError onRetry={() => void query.refetch()} />
      ) : null}
      {query.isSuccess && query.data.items.length > 0 ? (
        <ul className="grid gap-2">
          {query.data.items.map((item) => (
            <li key={item.id}>
              <Link
                to={actionCenterItemHref(item)}
                className="flex items-start gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface-muted/30 px-3 py-2.5 hover:bg-surface-muted/60 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
              >
                <div className="min-w-0 flex-1">
                  <p className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
                    {item.title}
                  </p>
                  <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">{item.reason}</p>
                  {item.organizationDisplayName ? (
                    <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {item.organizationDisplayName}
                      {item.productCode ? ` · ${item.productCode}` : ""}
                    </p>
                  ) : null}
                </div>
                <StatusIndicator tone={severityTone(item)} label={severityLabel(item, t)} />
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("dashboard.actionCenter.empty")}
        </p>
      ) : null}
    </DashboardSection>
  );
}

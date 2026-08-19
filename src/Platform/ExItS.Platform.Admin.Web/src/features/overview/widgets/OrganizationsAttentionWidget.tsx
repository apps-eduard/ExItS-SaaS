import { Badge } from "@/components/ui/badge";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { useSuspendedOrganizationsQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";

export function OrganizationsAttentionWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const query = useSuspendedOrganizationsQuery(enabled);

  return (
    <DashboardSection
      title={t("dashboard.attention.title")}
      description={t("dashboard.attention.hint")}
    >
      {query.isPending ? <DashboardWidgetSkeleton /> : null}
      {query.isError ? <DashboardWidgetError onRetry={() => void query.refetch()} /> : null}
      {query.data ? (
        <div className="grid gap-3">
          <p className="font-[family-name:var(--exits-font-tabular)] text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
            {formatNumber(query.data.totalCount, language)}
          </p>
          {query.data.totalCount === 0 ? (
            <p className="text-[length:var(--exits-text-sm)] text-muted break-words">
              {t("dashboard.attention.empty")}
            </p>
          ) : (
            <ul className="divide-y divide-border">
              {query.data.items.map((organization) => (
                <li key={organization.id} className="flex items-center justify-between gap-3 py-2">
                  <span className="min-w-0 truncate text-[length:var(--exits-text-sm)]">
                    {organization.displayName}
                  </span>
                  <Badge tone="danger">{t("dashboard.status.Suspended")}</Badge>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </DashboardSection>
  );
}

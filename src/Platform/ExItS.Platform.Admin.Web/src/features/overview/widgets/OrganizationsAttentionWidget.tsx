import { Badge } from "@/components/ui/badge";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { useSuspendedOrganizationsQuery } from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";

export function OrganizationsAttentionWidget({ enabled }: { enabled: boolean }) {
  const { t } = usePreferences();
  const query = useSuspendedOrganizationsQuery(enabled);

  return (
    <DashboardSection
      title={t("dashboard.attention.title")}
      description={t("dashboard.attention.hint")}
    >
      {query.isPending ? <DashboardWidgetSkeleton /> : null}
      {query.isError ? <DashboardWidgetError onRetry={() => void query.refetch()} /> : null}
      {query.data ? (
        <AdminTable
          caption={t("dashboard.attention.title")}
          empty={t("dashboard.attention.empty")}
          columns={[
            {
              id: "name",
              header: t("dashboard.table.name"),
              cell: (organization) => <span className="truncate">{organization.displayName}</span>,
            },
            {
              id: "reason",
              header: t("dashboard.table.reason"),
              cell: () => <Badge tone="danger">{t("dashboard.status.Suspended")}</Badge>,
            },
            {
              id: "context",
              header: t("dashboard.table.context"),
              cell: (organization) => <span className="text-muted">{organization.slug}</span>,
            },
          ]}
          rows={query.data.items}
        />
      ) : null}
    </DashboardSection>
  );
}

import { Badge } from "@/components/ui/badge";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardStatCard } from "@/components/exits/dashboard/DashboardStatCard";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { formatStatusLine } from "@/components/exits/dashboard/StatusBreakdown";
import {
  usePendingVerificationAccountsQuery,
  useUnassignedAccountsQuery,
} from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";
import type { MessageKey } from "@/lib/i18n/messages";

function accountStatusLabel(t: (key: MessageKey) => string, status: string): string {
  if (status === "Active") {
    return t("dashboard.status.Active");
  }
  if (status === "Suspended") {
    return t("dashboard.status.Suspended");
  }
  if (status === "PendingVerification") {
    return t("dashboard.status.PendingVerification");
  }
  return status;
}

export function AccountsReviewWidget({
  enabled,
  variant = "list",
}: {
  enabled: boolean;
  variant?: "metric" | "list";
}) {
  const { t, language } = usePreferences();
  const unassigned = useUnassignedAccountsQuery(enabled);
  const pending = usePendingVerificationAccountsQuery(enabled);
  const loading = unassigned.isPending || pending.isPending;
  const failed = unassigned.isError || pending.isError;
  const ready = !loading && !failed && unassigned.data && pending.data;
  const attentionCount = ready ? unassigned.data.totalCount + pending.data.totalCount : 0;

  if (variant === "metric") {
    return (
      <DashboardSection variant="metric" title={t("dashboard.accounts.metricTitle")}>
        {loading ? <DashboardWidgetSkeleton rows={2} /> : null}
        {failed && !loading ? (
          <DashboardWidgetError
            onRetry={() => {
              void unassigned.refetch();
              void pending.refetch();
            }}
          />
        ) : null}
        {ready ? (
          <DashboardStatCard
            value={formatNumber(attentionCount, language)}
            detail={formatStatusLine([
              {
                key: "Unassigned",
                label: t("dashboard.accounts.unassigned"),
                value: formatNumber(unassigned.data.totalCount, language),
              },
              {
                key: "Pending",
                label: t("dashboard.accounts.pendingVerification"),
                value: formatNumber(pending.data.totalCount, language),
              },
            ])}
          />
        ) : null}
      </DashboardSection>
    );
  }

  return (
    <DashboardSection
      title={t("dashboard.accounts.title")}
      description={t("dashboard.accounts.hint")}
    >
      {loading ? <DashboardWidgetSkeleton /> : null}
      {failed && !loading ? (
        <DashboardWidgetError
          onRetry={() => {
            void unassigned.refetch();
            void pending.refetch();
          }}
        />
      ) : null}
      {ready ? (
        <AdminTable
          caption={t("dashboard.accounts.title")}
          empty={t("dashboard.accounts.empty")}
          columns={[
            {
              id: "name",
              header: t("dashboard.table.name"),
              cell: (user) => <span className="truncate">{user.displayName}</span>,
            },
            {
              id: "reason",
              header: t("dashboard.table.reason"),
              cell: () => t("dashboard.accounts.unassigned"),
            },
            {
              id: "status",
              header: t("dashboard.table.status"),
              align: "right",
              cell: (user) => <Badge tone="warning">{accountStatusLabel(t, user.status)}</Badge>,
            },
          ]}
          rows={unassigned.data.items}
        />
      ) : null}
    </DashboardSection>
  );
}

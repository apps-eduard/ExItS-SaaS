import { Badge } from "@/components/ui/badge";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardStatCard } from "@/components/exits/dashboard/DashboardStatCard";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
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

export function AccountsReviewWidget({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const unassigned = useUnassignedAccountsQuery(enabled);
  const pending = usePendingVerificationAccountsQuery(enabled);
  const loading = unassigned.isPending || pending.isPending;
  const failed = unassigned.isError || pending.isError;

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
      {!loading && !failed && unassigned.data && pending.data ? (
        <div className="grid gap-3">
          <div className="grid grid-cols-2 gap-2">
            <DashboardStatCard
              label={t("dashboard.accounts.unassigned")}
              value={formatNumber(unassigned.data.totalCount, language)}
            />
            <DashboardStatCard
              label={t("dashboard.accounts.pendingVerification")}
              value={formatNumber(pending.data.totalCount, language)}
            />
          </div>
          {unassigned.data.totalCount === 0 ? (
            <p className="text-[length:var(--exits-text-sm)] text-muted break-words">
              {t("dashboard.accounts.empty")}
            </p>
          ) : (
            <ul className="divide-y divide-border">
              {unassigned.data.items.map((user) => (
                <li key={user.id} className="flex items-center justify-between gap-3 py-2">
                  <span className="min-w-0 truncate text-[length:var(--exits-text-sm)]">
                    {user.displayName}
                  </span>
                  <Badge tone="warning">{accountStatusLabel(t, user.status)}</Badge>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </DashboardSection>
  );
}

import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { AccountsReviewWidget } from "@/features/overview/widgets/AccountsReviewWidget";
import { OrganizationsAttentionWidget } from "@/features/overview/widgets/OrganizationsAttentionWidget";
import { OrganizationsSummaryWidget } from "@/features/overview/widgets/OrganizationsSummaryWidget";
import { PlatformHealthWidget } from "@/features/overview/widgets/PlatformHealthWidget";
import { RecentAuditWidget } from "@/features/overview/widgets/RecentAuditWidget";
import { SubscriptionsSummaryWidget } from "@/features/overview/widgets/SubscriptionsSummaryWidget";
import { useDashboardAuthorization } from "@/features/overview/use-dashboard-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function OverviewPage() {
  const { t } = usePreferences();
  const access = useDashboardAuthorization();
  const hasAnyWidget =
    access.canViewOrganizations ||
    access.canViewSubscriptions ||
    access.canReviewAccounts ||
    access.canViewAudit ||
    access.canViewHealth;

  return (
    <section className="grid gap-4">
      <PageHeader title={t("nav.overview")} description={t("overview.description")} />

      {access.status === "loading" ? (
        <div
          className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3"
          role="status"
          aria-busy="true"
          aria-label={t("dashboard.loading")}
        >
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-[var(--exits-density-card-padding)]">
            <DashboardWidgetSkeleton rows={4} />
          </div>
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-[var(--exits-density-card-padding)]">
            <DashboardWidgetSkeleton rows={4} />
          </div>
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-[var(--exits-density-card-padding)]">
            <DashboardWidgetSkeleton rows={4} />
          </div>
        </div>
      ) : null}

      {access.status === "loaded" && !hasAnyWidget ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted break-words">
          {t("dashboard.empty")}
        </p>
      ) : null}

      {access.status === "loaded" && hasAnyWidget ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {access.canViewOrganizations ? (
            <OrganizationsSummaryWidget enabled={access.canViewOrganizations} />
          ) : null}
          {access.canViewSubscriptions ? (
            <SubscriptionsSummaryWidget enabled={access.canViewSubscriptions} />
          ) : null}
          {access.canReviewAccounts ? (
            <AccountsReviewWidget enabled={access.canReviewAccounts} />
          ) : null}
          {access.canViewHealth ? <PlatformHealthWidget enabled={access.canViewHealth} /> : null}
          {access.canViewOrganizations ? (
            <OrganizationsAttentionWidget enabled={access.canViewOrganizations} />
          ) : null}
          {access.canViewAudit ? (
            <div className="md:col-span-2 xl:col-span-3">
              <RecentAuditWidget enabled={access.canViewAudit} />
            </div>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}

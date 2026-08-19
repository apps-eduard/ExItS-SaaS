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
  const hasMetrics =
    access.canViewOrganizations || access.canViewSubscriptions || access.canReviewAccounts;

  return (
    <section className="grid gap-5">
      <PageHeader title={t("nav.overview")} description={t("overview.description")} />

      {access.status === "loading" ? (
        <div
          className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3"
          role="status"
          aria-busy="true"
          aria-label={t("dashboard.loading")}
        >
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <DashboardWidgetSkeleton rows={2} />
          </div>
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <DashboardWidgetSkeleton rows={2} />
          </div>
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <DashboardWidgetSkeleton rows={2} />
          </div>
        </div>
      ) : null}

      {access.status === "loaded" && !hasAnyWidget ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted break-words">
          {t("dashboard.empty")}
        </p>
      ) : null}

      {access.status === "loaded" && hasAnyWidget ? (
        <>
          {hasMetrics ? (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
              {access.canViewOrganizations ? (
                <OrganizationsSummaryWidget enabled={access.canViewOrganizations} />
              ) : null}
              {access.canViewSubscriptions ? (
                <SubscriptionsSummaryWidget enabled={access.canViewSubscriptions} />
              ) : null}
              {access.canReviewAccounts ? (
                <AccountsReviewWidget enabled={access.canReviewAccounts} variant="metric" />
              ) : null}
            </div>
          ) : null}

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_15rem] lg:items-start">
            <div className="grid gap-5">
              {access.canViewOrganizations ? (
                <OrganizationsAttentionWidget enabled={access.canViewOrganizations} />
              ) : null}
              {access.canReviewAccounts ? (
                <AccountsReviewWidget enabled={access.canReviewAccounts} variant="list" />
              ) : null}
            </div>
            {access.canViewHealth ? <PlatformHealthWidget enabled={access.canViewHealth} /> : null}
          </div>

          {access.canViewAudit ? <RecentAuditWidget enabled={access.canViewAudit} /> : null}
        </>
      ) : null}
    </section>
  );
}

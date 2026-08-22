import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { AccountsReviewWidget } from "@/features/overview/widgets/AccountsReviewWidget";
import { NeedsAttentionCenter } from "@/features/overview/widgets/NeedsAttentionCenter";
import { OrganizationsSummaryWidget } from "@/features/overview/widgets/OrganizationsSummaryWidget";
import { PaymentsSummaryWidget } from "@/features/overview/widgets/PaymentsSummaryWidget";
import { PlatformHealthWidget } from "@/features/overview/widgets/PlatformHealthWidget";
import { PrivacySummaryWidget } from "@/features/overview/widgets/PrivacySummaryWidget";
import { QuickAccessWidget } from "@/features/overview/widgets/QuickAccessWidget";
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
    access.canViewPayments ||
    access.canViewPrivacy ||
    access.canViewAudit ||
    access.canViewHealth ||
    access.canViewCatalog ||
    access.canViewMemberships ||
    access.canViewPersonalFeatures ||
    access.canViewGlobalCatalog ||
    access.canViewPlans;
  const hasMetrics =
    access.canViewOrganizations ||
    access.canViewSubscriptions ||
    access.canReviewAccounts ||
    access.canViewPayments ||
    access.canViewPrivacy;
  const hasAttention =
    access.canViewOrganizations ||
    access.canReviewAccounts ||
    access.canViewSubscriptions ||
    access.canViewPayments ||
    access.canViewPrivacy ||
    access.canViewHealth;
  const hasRail = access.canViewHealth || hasAnyWidget;

  return (
    <section className="grid gap-5">
      <PageHeader title={t("nav.overview")} description={t("overview.description")} />

      {access.status === "loading" ? (
        <div
          className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-5"
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
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-5">
              {access.canViewOrganizations ? (
                <OrganizationsSummaryWidget enabled={access.canViewOrganizations} />
              ) : null}
              {access.canViewSubscriptions ? (
                <SubscriptionsSummaryWidget enabled={access.canViewSubscriptions} />
              ) : null}
              {access.canReviewAccounts ? (
                <AccountsReviewWidget enabled={access.canReviewAccounts} variant="metric" />
              ) : null}
              {access.canViewPayments ? (
                <PaymentsSummaryWidget enabled={access.canViewPayments} />
              ) : null}
              {access.canViewPrivacy ? (
                <PrivacySummaryWidget enabled={access.canViewPrivacy} />
              ) : null}
            </div>
          ) : null}

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(14rem,18rem)] lg:items-start">
            <div className="grid min-w-0 gap-5">
              {hasAttention ? <NeedsAttentionCenter access={access} /> : null}
              {access.canViewAudit ? <RecentAuditWidget enabled={access.canViewAudit} /> : null}
            </div>
            {hasRail ? (
              <aside className="grid min-w-0 gap-5">
                {access.canViewHealth ? (
                  <PlatformHealthWidget enabled={access.canViewHealth} />
                ) : null}
                <QuickAccessWidget access={access} />
              </aside>
            ) : null}
          </div>
        </>
      ) : null}
    </section>
  );
}

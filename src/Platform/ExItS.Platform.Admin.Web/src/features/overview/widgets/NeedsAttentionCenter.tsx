import { AlertTriangle, Building2, CreditCard, ShieldAlert, Users } from "lucide-react";
import { Link } from "react-router-dom";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetError } from "@/components/exits/dashboard/DashboardWidgetError";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import type { DashboardAuthorization } from "@/features/overview/use-dashboard-authorization";
import {
  usePaymentStatusCountQuery,
  usePlatformHealthQuery,
  usePrivacyOverviewQuery,
  useSubscriptionAttentionQuery,
  useSuspendedOrganizationsQuery,
  useUnassignedAccountsQuery,
} from "@/features/overview/use-dashboard-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { formatNumber } from "@/lib/i18n/format";

type AttentionItem = {
  id: string;
  title: string;
  detail: string;
  href: string;
  tone: "warning" | "danger" | "neutral";
  icon: "org" | "user" | "sub" | "payment" | "privacy" | "health";
};

function AttentionIcon({ kind }: { kind: AttentionItem["icon"] }) {
  const className = "mt-0.5 size-4 shrink-0 text-muted";
  switch (kind) {
    case "org":
      return <Building2 className={className} aria-hidden="true" />;
    case "user":
      return <Users className={className} aria-hidden="true" />;
    case "sub":
      return <AlertTriangle className={className} aria-hidden="true" />;
    case "payment":
      return <CreditCard className={className} aria-hidden="true" />;
    case "privacy":
      return <ShieldAlert className={className} aria-hidden="true" />;
    case "health":
      return <AlertTriangle className={className} aria-hidden="true" />;
    default:
      return null;
  }
}

export function NeedsAttentionCenter({ access }: { access: DashboardAuthorization }) {
  const { t, language } = usePreferences();
  const suspendedOrgs = useSuspendedOrganizationsQuery(access.canViewOrganizations);
  const unassigned = useUnassignedAccountsQuery(access.canReviewAccounts);
  const pastDue = useSubscriptionAttentionQuery(access.canViewSubscriptions, "PastDue");
  const grace = useSubscriptionAttentionQuery(access.canViewSubscriptions, "GracePeriod");
  const pendingPayments = usePaymentStatusCountQuery(
    access.canViewPayments,
    "PendingConfirmation",
  );
  const privacy = usePrivacyOverviewQuery(access.canViewPrivacy);
  const health = usePlatformHealthQuery(access.canViewHealth);

  const anyEnabled =
    access.canViewOrganizations ||
    access.canReviewAccounts ||
    access.canViewSubscriptions ||
    access.canViewPayments ||
    access.canViewPrivacy ||
    access.canViewHealth;

  if (!anyEnabled) {
    return null;
  }

  const pendingFlags = [
    access.canViewOrganizations && suspendedOrgs.isPending,
    access.canReviewAccounts && unassigned.isPending,
    access.canViewSubscriptions && pastDue.isPending,
    access.canViewSubscriptions && grace.isPending,
    access.canViewPayments && pendingPayments.isPending,
    access.canViewPrivacy && privacy.isPending,
    access.canViewHealth && health.isPending,
  ].filter(Boolean);
  const allPending = pendingFlags.length > 0 && pendingFlags.every(Boolean);

  const failedSources = [
    access.canViewOrganizations && suspendedOrgs.isError,
    access.canReviewAccounts && unassigned.isError,
    access.canViewSubscriptions && pastDue.isError,
    access.canViewSubscriptions && grace.isError,
    access.canViewPayments && pendingPayments.isError,
    access.canViewPrivacy && privacy.isError,
    access.canViewHealth && health.isError,
  ].some(Boolean);

  const items: AttentionItem[] = [];

  if (suspendedOrgs.data && suspendedOrgs.data.totalCount > 0) {
    items.push({
      id: "suspended-orgs",
      title: t("dashboard.needsAttention.suspendedOrgs"),
      detail: t("dashboard.needsAttention.countDetail").replace(
        "{count}",
        formatNumber(suspendedOrgs.data.totalCount, language),
      ),
      href: "/admin/organizations?status=Suspended",
      tone: "danger",
      icon: "org",
    });
  }

  if (unassigned.data && unassigned.data.totalCount > 0) {
    items.push({
      id: "unassigned-users",
      title: t("dashboard.needsAttention.unassignedAccounts"),
      detail: t("dashboard.needsAttention.countDetail").replace(
        "{count}",
        formatNumber(unassigned.data.totalCount, language),
      ),
      href: "/admin/users?directory=Unassigned",
      tone: "warning",
      icon: "user",
    });
  }

  if (pastDue.data && pastDue.data.totalCount > 0) {
    items.push({
      id: "past-due-subs",
      title: t("dashboard.needsAttention.pastDueSubs"),
      detail: t("dashboard.needsAttention.countDetail").replace(
        "{count}",
        formatNumber(pastDue.data.totalCount, language),
      ),
      href: "/admin/subscriptions?status=PastDue",
      tone: "danger",
      icon: "sub",
    });
  }

  if (grace.data && grace.data.totalCount > 0) {
    items.push({
      id: "grace-subs",
      title: t("dashboard.needsAttention.graceSubs"),
      detail: t("dashboard.needsAttention.countDetail").replace(
        "{count}",
        formatNumber(grace.data.totalCount, language),
      ),
      href: "/admin/subscriptions?status=GracePeriod",
      tone: "warning",
      icon: "sub",
    });
  }

  if (pendingPayments.data && pendingPayments.data.totalCount > 0) {
    items.push({
      id: "pending-payments",
      title: t("dashboard.needsAttention.pendingPayments"),
      detail: t("dashboard.needsAttention.countDetail").replace(
        "{count}",
        formatNumber(pendingPayments.data.totalCount, language),
      ),
      href: "/admin/payments?status=PendingConfirmation",
      tone: "warning",
      icon: "payment",
    });
  }

  if (privacy.data && privacy.data.actionNeededCount > 0) {
    items.push({
      id: "privacy-gaps",
      title: t("dashboard.needsAttention.privacyGaps"),
      detail: t("dashboard.needsAttention.countDetail").replace(
        "{count}",
        formatNumber(privacy.data.actionNeededCount, language),
      ),
      href: "/admin/privacy-compliance",
      tone: "warning",
      icon: "privacy",
    });
  }

  if (health.data) {
    const unhealthy =
      health.data.liveness.reportedStatus === "Unhealthy" ||
      health.data.readiness.reportedStatus === "Unhealthy" ||
      health.data.liveness.reportedStatus === "Degraded" ||
      health.data.readiness.reportedStatus === "Degraded";
    if (unhealthy) {
      items.push({
        id: "health",
        title: t("dashboard.needsAttention.health"),
        detail: t("dashboard.needsAttention.healthDetail"),
        href: "/admin/system-health",
        tone: "danger",
        icon: "health",
      });
    }
  }

  return (
    <DashboardSection
      title={t("dashboard.needsAttention.title")}
      description={t("dashboard.needsAttention.hint")}
    >
      {allPending ? <DashboardWidgetSkeleton rows={4} /> : null}
      {!allPending && items.length > 0 ? (
        <ul className="grid gap-2">
          {items.map((item) => (
            <li key={item.id}>
              <Link
                to={item.href}
                className="flex items-start gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface-muted/30 px-3 py-2.5 hover:bg-surface-muted/60 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
              >
                <AttentionIcon kind={item.icon} />
                <div className="min-w-0 flex-1">
                  <p className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
                    {item.title}
                  </p>
                  <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">{item.detail}</p>
                </div>
                <StatusIndicator
                  tone={item.tone}
                  label={
                    item.tone === "danger"
                      ? t("dashboard.needsAttention.urgent")
                      : t("dashboard.needsAttention.review")
                  }
                />
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
      {!allPending && items.length === 0 && !failedSources ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("dashboard.needsAttention.empty")}
        </p>
      ) : null}
      {failedSources && !allPending ? (
        <div className={items.length > 0 ? "mt-3" : undefined}>
          <DashboardWidgetError
            onRetry={() => {
              void suspendedOrgs.refetch();
              void unassigned.refetch();
              void pastDue.refetch();
              void grace.refetch();
              void pendingPayments.refetch();
              void privacy.refetch();
              void health.refetch();
            }}
          />
        </div>
      ) : null}
    </DashboardSection>
  );
}

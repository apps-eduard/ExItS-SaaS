import { Link } from "react-router-dom";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import type { DashboardAuthorization } from "@/features/overview/use-dashboard-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

type QuickLink = {
  id: string;
  href: string;
  labelKey: MessageKey;
  allowed: boolean;
};

export function QuickAccessWidget({ access }: { access: DashboardAuthorization }) {
  const { t } = usePreferences();

  const links: QuickLink[] = [
    {
      id: "organizations",
      href: "/admin/organizations",
      labelKey: "dashboard.quickAccess.organizations",
      allowed: access.canViewOrganizations,
    },
    {
      id: "memberships",
      href: "/admin/organization-users",
      labelKey: "dashboard.quickAccess.memberships",
      allowed: access.canViewMemberships,
    },
    {
      id: "users",
      href: "/admin/users",
      labelKey: "dashboard.quickAccess.users",
      allowed: access.canReviewAccounts,
    },
    {
      id: "products",
      href: "/admin/products",
      labelKey: "dashboard.quickAccess.products",
      allowed: access.canViewCatalog,
    },
    {
      id: "global-catalog",
      href: "/admin/global-catalog/business-types",
      labelKey: "dashboard.quickAccess.globalCatalog",
      allowed: access.canViewGlobalCatalog,
    },
    {
      id: "plans",
      href: "/admin/plans",
      labelKey: "dashboard.quickAccess.plans",
      allowed: access.canViewPlans,
    },
    {
      id: "subscriptions",
      href: "/admin/subscriptions",
      labelKey: "dashboard.quickAccess.subscriptions",
      allowed: access.canViewSubscriptions,
    },
    {
      id: "payments",
      href: "/admin/payments",
      labelKey: "dashboard.quickAccess.payments",
      allowed: access.canViewPayments,
    },
    {
      id: "personal-features",
      href: "/admin/personal-features",
      labelKey: "dashboard.quickAccess.personalFeatures",
      allowed: access.canViewPersonalFeatures,
    },
    {
      id: "privacy",
      href: "/admin/privacy-compliance",
      labelKey: "dashboard.quickAccess.privacy",
      allowed: access.canViewPrivacy,
    },
    {
      id: "audit",
      href: "/admin/audit",
      labelKey: "dashboard.quickAccess.audit",
      allowed: access.canViewAudit,
    },
    {
      id: "health",
      href: "/admin/system-health",
      labelKey: "dashboard.quickAccess.health",
      allowed: access.canViewHealth,
    },
  ];

  const visible = links.filter((link) => link.allowed);
  if (visible.length === 0) {
    return null;
  }

  return (
    <DashboardSection
      title={t("dashboard.quickAccess.title")}
      description={t("dashboard.quickAccess.hint")}
    >
      <ul className="flex flex-wrap gap-2" role="list">
        {visible.map((link) => (
          <li key={link.id}>
            <Link
              to={link.href}
              className="inline-flex min-h-[var(--exits-touch-target-min)] items-center rounded-[var(--exits-density-radius)] border border-border bg-surface px-2.5 py-1 text-[length:var(--exits-text-xs)] font-medium text-foreground hover:bg-surface-muted/60 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              {t(link.labelKey)}
            </Link>
          </li>
        ))}
      </ul>
    </DashboardSection>
  );
}

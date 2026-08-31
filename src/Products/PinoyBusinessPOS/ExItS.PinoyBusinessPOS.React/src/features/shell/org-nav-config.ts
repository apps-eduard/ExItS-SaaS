import type { LucideIcon } from "lucide-react";
import {
  BarChart3,
  Boxes,
  ClipboardList,
  LayoutDashboard,
  ListChecks,
  MonitorSmartphone,
  PackagePlus,
  Receipt,
  RefreshCw,
  Settings,
  ShieldCheck,
  Truck,
  Users,
  Wallet,
} from "lucide-react";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  canCreateSale,
  canManageCatalog,
  canViewCustomerOrders,
  canViewInventory,
  canViewCustomers,
  canViewShifts,
  canViewReturns,
  canViewPurchasing,
  canViewSuppliers,
  canAccessReportsHub,
  canViewDashboard,
  canViewExpenses,
  canUseAdminExperience,
  hasOrganizationManagementAuthority,
  canInviteOrganizationStaff,
} from "@/access/pos-capabilities";
import type { WorkingExperience } from "@/workspace/working-experience";
import { workingExperienceRoute } from "@/workspace/working-experience";

export type OrgNavTabId = "home" | "sell" | "catalog" | "orders" | "more";

export type OrgNavTab = {
  id: OrgNavTabId;
  to: string;
  end: boolean;
  labelKey: "org.nav.home" | "org.nav.sell" | "org.nav.catalog" | "org.nav.orders" | "org.nav.more";
  testId: string;
  /** Stronger treatment for the primary POS action. */
  primary?: boolean;
};

/**
 * Primary org destinations for compact bottom navigation (max 5).
 * Sell is always placed in the center when present (primary POS action).
 * Catalog prefers catalog manage; falls back to inventory view.
 * Orders prefers customer orders; falls back to customers when orders unavailable.
 */
export function buildOrgBottomNavTabs(input: {
  grant: PosSessionGrantFacts | null | undefined;
  experience: WorkingExperience;
}): OrgNavTab[] {
  const homeTo =
    input.experience === "start_selling"
      ? "/role/cashier"
      : workingExperienceRoute(input.experience) === "/sell"
        ? "/role/cashier"
        : workingExperienceRoute(input.experience);

  const home: OrgNavTab = {
    id: "home",
    to: homeTo,
    end: homeTo === "/org" || homeTo.startsWith("/role/"),
    labelKey: "org.nav.home",
    testId: "org-nav-home",
  };

  const left: OrgNavTab[] = [home];
  const right: OrgNavTab[] = [];

  if (canManageCatalog(input.grant)) {
    left.push({
      id: "catalog",
      to: "/catalog",
      end: false,
      labelKey: "org.nav.catalog",
      testId: "org-nav-catalog",
    });
  } else if (canViewInventory(input.grant)) {
    left.push({
      id: "catalog",
      to: "/inventory",
      end: false,
      labelKey: "org.nav.catalog",
      testId: "org-nav-catalog",
    });
  }

  if (canViewCustomerOrders(input.grant)) {
    right.push({
      id: "orders",
      to: "/orders",
      end: false,
      labelKey: "org.nav.orders",
      testId: "org-nav-orders",
    });
  } else if (canViewCustomers(input.grant)) {
    right.push({
      id: "orders",
      to: "/customers",
      end: false,
      labelKey: "org.nav.orders",
      testId: "org-nav-orders",
    });
  }

  right.push({
    id: "more",
    to: "/more",
    end: false,
    labelKey: "org.nav.more",
    testId: "org-nav-more",
  });

  const sell: OrgNavTab | null = canCreateSale(input.grant)
    ? {
        id: "sell",
        to: "/sell",
        end: false,
        labelKey: "org.nav.sell",
        testId: "org-nav-sell",
        primary: true,
      }
    : null;

  // Keep Sell centered: Home · Catalog · Sell · Orders · More
  if (sell) {
    return [...left, sell, ...right].slice(0, 5);
  }

  return [...left, ...right].slice(0, 5);
}

export type OrgMoreLink = {
  to: string;
  labelKey:
    | "org.more.inventory"
    | "org.more.customers"
    | "org.more.shifts"
    | "org.more.returns"
    | "org.more.purchasing"
    | "org.more.suppliers"
    | "org.more.expenses"
    | "org.more.dashboard"
    | "org.more.reports"
    | "org.more.organization"
    | "org.more.devices"
    | "org.more.staff"
    | "org.more.roles"
    | "org.more.preferences"
    | "org.more.finishSetup";
  testId: string;
  icon: LucideIcon;
};

export type OrgMoreSectionId = "operations" | "insights" | "organization" | "settings";

export type OrgMoreSection = {
  id: OrgMoreSectionId;
  titleKey:
    | "org.more.group.operations"
    | "org.more.group.insights"
    | "org.more.group.organization"
    | "org.more.group.settings";
  testId: string;
  links: OrgMoreLink[];
};

/** Secondary destinations for the More hub — permission-filtered, flat list. */
export function buildOrgMoreLinks(grant: PosSessionGrantFacts | null | undefined): OrgMoreLink[] {
  return buildOrgMoreSections(grant).flatMap((section) => section.links);
}

/** Grouped More hub sections for scannable UX (Manager-home style panels). */
export function buildOrgMoreSections(
  grant: PosSessionGrantFacts | null | undefined,
  options?: { showFinishSetup?: boolean },
): OrgMoreSection[] {
  const operations: OrgMoreLink[] = [];
  const insights: OrgMoreLink[] = [];
  const organization: OrgMoreLink[] = [];
  const settings: OrgMoreLink[] = [];

  if (canViewInventory(grant)) {
    operations.push({
      to: "/inventory",
      labelKey: "org.more.inventory",
      testId: "org-more-inventory",
      icon: Boxes,
    });
  }
  if (canViewCustomers(grant)) {
    operations.push({
      to: "/customers",
      labelKey: "org.more.customers",
      testId: "org-more-customers",
      icon: Users,
    });
  }
  if (canViewShifts(grant)) {
    operations.push({
      to: "/shifts",
      labelKey: "org.more.shifts",
      testId: "org-more-shifts",
      icon: RefreshCw,
    });
  }
  if (canViewReturns(grant)) {
    operations.push({
      to: "/returns",
      labelKey: "org.more.returns",
      testId: "org-more-returns",
      icon: Receipt,
    });
  }
  if (canViewPurchasing(grant)) {
    operations.push({
      to: "/purchasing",
      labelKey: "org.more.purchasing",
      testId: "org-more-purchasing",
      icon: PackagePlus,
    });
  }
  if (canViewSuppliers(grant)) {
    operations.push({
      to: "/suppliers",
      labelKey: "org.more.suppliers",
      testId: "org-more-suppliers",
      icon: Truck,
    });
  }
  if (canViewExpenses(grant)) {
    operations.push({
      to: "/expenses",
      labelKey: "org.more.expenses",
      testId: "org-more-expenses",
      icon: Wallet,
    });
  }

  if (canViewDashboard(grant)) {
    insights.push({
      to: "/dashboard",
      labelKey: "org.more.dashboard",
      testId: "org-more-dashboard",
      icon: LayoutDashboard,
    });
  }
  if (canAccessReportsHub(grant)) {
    insights.push({
      to: "/reports",
      labelKey: "org.more.reports",
      testId: "org-more-reports",
      icon: BarChart3,
    });
  }

  if (canUseAdminExperience(grant) || hasOrganizationManagementAuthority(grant)) {
    organization.push({
      to: "/org",
      labelKey: "org.more.organization",
      testId: "org-more-org",
      icon: ClipboardList,
    });
  }
  if (options?.showFinishSetup && hasOrganizationManagementAuthority(grant)) {
    organization.unshift({
      to: "/onboarding",
      labelKey: "org.more.finishSetup",
      testId: "org-more-finish-setup",
      icon: ListChecks,
    });
  }
  if (hasOrganizationManagementAuthority(grant)) {
    organization.push({
      to: "/org/devices",
      labelKey: "org.more.devices",
      testId: "org-more-devices",
      icon: MonitorSmartphone,
    });
  }
  if (canInviteOrganizationStaff(grant)) {
    organization.push({
      to: "/org/staff",
      labelKey: "org.more.staff",
      testId: "org-more-staff",
      icon: Users,
    });
    organization.push({
      to: "/org/roles",
      labelKey: "org.more.roles",
      testId: "org-more-roles",
      icon: ShieldCheck,
    });
  }

  settings.push({
    to: "/settings/preferences",
    labelKey: "org.more.preferences",
    testId: "org-more-preferences",
    icon: Settings,
  });

  return [
    {
      id: "operations" as const,
      titleKey: "org.more.group.operations" as const,
      testId: "org-more-group-operations",
      links: operations,
    },
    {
      id: "insights" as const,
      titleKey: "org.more.group.insights" as const,
      testId: "org-more-group-insights",
      links: insights,
    },
    {
      id: "organization" as const,
      titleKey: "org.more.group.organization" as const,
      testId: "org-more-group-organization",
      links: organization,
    },
    {
      id: "settings" as const,
      titleKey: "org.more.group.settings" as const,
      testId: "org-more-group-settings",
      links: settings,
    },
  ].filter((section) => section.links.length > 0);
}

/** Match nested routes to a primary tab (e.g. /sell/checkout → sell). */
export function matchOrgNavTab(
  pathname: string,
  tabs: ReadonlyArray<OrgNavTab>,
): OrgNavTabId | null {
  if (pathname === "/more" || pathname.startsWith("/more/")) {
    return tabs.some((t) => t.id === "more") ? "more" : null;
  }
  // Prefer longest prefix match among non-home tabs.
  let best: OrgNavTab | null = null;
  for (const tab of tabs) {
    if (tab.id === "home") {
      continue;
    }
    if (pathname === tab.to || pathname.startsWith(`${tab.to}/`)) {
      if (!best || tab.to.length > best.to.length) {
        best = tab;
      }
    }
  }
  if (best) {
    return best.id;
  }
  for (const tab of tabs) {
    if (tab.id !== "home") {
      continue;
    }
    if (tab.end ? pathname === tab.to : pathname === tab.to || pathname.startsWith(`${tab.to}/`)) {
      return "home";
    }
  }
  return null;
}

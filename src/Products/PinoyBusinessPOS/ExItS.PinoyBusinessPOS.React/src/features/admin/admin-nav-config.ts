import type { LucideIcon } from "lucide-react";
import {
  BarChart3,
  Building2,
  KeyRound,
  LayoutDashboard,
  Map,
  MapPin,
  MonitorSmartphone,
  QrCode,
  Settings,
  ShieldCheck,
  Users,
  Wallet,
} from "lucide-react";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  canAccessReportsHub,
  canInviteOrganizationStaff,
  canManageStoreAreas,
  canUseAdminExperience,
  canUseWarehouseBranches,
  canViewDashboard,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import type { MessageKey } from "@/i18n/messages";

export type AdminNavGroupId =
  | "overview"
  | "organization"
  | "business"
  | "review"
  | "security"
  | "settings";

export type AdminNavItemId =
  | "overview"
  | "areas"
  | "branches"
  | "staff"
  | "roles"
  | "devices"
  | "cash"
  | "businessQr"
  | "dashboard"
  | "reports"
  | "ownership"
  | "preferences";

export type AdminNavItem = {
  id: AdminNavItemId;
  to: string;
  labelKey: MessageKey;
  icon: LucideIcon;
  testId: string;
  /** Match nested routes under this prefix. */
  matchPrefixes: string[];
  /** Exact path match preferred for overview (/org). */
  end?: boolean;
  /** Entitlement-locked (visible but not entitled). */
  locked?: boolean;
  lockedReasonKey?: MessageKey;
};

export type AdminNavGroup = {
  id: AdminNavGroupId;
  titleKey: MessageKey;
  items: AdminNavItem[];
};

export type AdminMobileTabId = "home" | "manage" | "review" | "more";

export type AdminMobileTab = {
  id: AdminMobileTabId;
  to: string;
  end: boolean;
  labelKey: MessageKey;
  testId: string;
};

/**
 * Permission-aware Manage Business navigation.
 * Does not invent destinations — only existing implemented routes.
 */
export function buildAdminNavGroups(
  grant: PosSessionGrantFacts | null | undefined,
): AdminNavGroup[] {
  if (!canUseAdminExperience(grant) && !hasOrganizationManagementAuthority(grant)) {
    return [];
  }

  const canInvite = canInviteOrganizationStaff(grant);
  const canAdmin = hasOrganizationManagementAuthority(grant);
  const areasEntitled = canManageStoreAreas(grant);
  const groups: AdminNavGroup[] = [];

  groups.push({
    id: "overview",
    titleKey: "admin.nav.group.overview",
    items: [
      {
        id: "overview",
        to: "/org",
        labelKey: "admin.nav.overview",
        icon: Building2,
        testId: "admin-nav-overview",
        matchPrefixes: ["/org"],
        end: true,
      },
    ],
  });

  const organizationItems: AdminNavItem[] = [];
  if (canInvite) {
    if (areasEntitled) {
      organizationItems.push({
        id: "areas",
        to: "/org/areas",
        labelKey: "admin.nav.areas",
        icon: Map,
        testId: "admin-nav-areas",
        matchPrefixes: ["/org/areas"],
      });
    } else {
      organizationItems.push({
        id: "areas",
        to: "/org/areas",
        labelKey: "admin.nav.areas",
        icon: Map,
        testId: "admin-nav-areas",
        matchPrefixes: ["/org/areas"],
        locked: true,
        lockedReasonKey: "admin.nav.lockedPro",
      });
    }
    organizationItems.push({
      id: "branches",
      to: "/org/branches",
      labelKey: "admin.nav.branchesWarehouses",
      icon: MapPin,
      testId: "admin-nav-branches",
      matchPrefixes: ["/org/branches"],
    });
    organizationItems.push({
      id: "staff",
      to: "/org/staff",
      labelKey: "admin.nav.staff",
      icon: Users,
      testId: "admin-nav-staff",
      matchPrefixes: ["/org/staff"],
    });
    organizationItems.push({
      id: "roles",
      to: "/org/roles",
      labelKey: "admin.nav.roles",
      icon: ShieldCheck,
      testId: "admin-nav-roles",
      matchPrefixes: ["/org/roles"],
    });
  }
  if (canAdmin) {
    organizationItems.push({
      id: "devices",
      to: "/org/devices",
      labelKey: "admin.nav.devices",
      icon: MonitorSmartphone,
      testId: "admin-nav-devices",
      matchPrefixes: ["/org/devices"],
    });
  }
  if (organizationItems.length > 0) {
    groups.push({
      id: "organization",
      titleKey: "admin.nav.group.organization",
      items: organizationItems,
    });
  }

  const businessItems: AdminNavItem[] = [];
  if (canAdmin) {
    businessItems.push(
      {
        id: "cash",
        to: "/org/cash-handling",
        labelKey: "admin.nav.cashHandling",
        icon: Wallet,
        testId: "admin-nav-cash",
        matchPrefixes: ["/org/cash-handling"],
      },
      {
        id: "businessQr",
        to: "/org/business-qr",
        labelKey: "admin.nav.businessQr",
        icon: QrCode,
        testId: "admin-nav-business-qr",
        matchPrefixes: ["/org/business-qr"],
      },
    );
  }
  if (businessItems.length > 0) {
    groups.push({
      id: "business",
      titleKey: "admin.nav.group.business",
      items: businessItems,
    });
  }

  const reviewItems: AdminNavItem[] = [];
  if (canViewDashboard(grant)) {
    reviewItems.push({
      id: "dashboard",
      to: "/dashboard",
      labelKey: "admin.nav.dashboard",
      icon: LayoutDashboard,
      testId: "admin-nav-dashboard",
      matchPrefixes: ["/dashboard"],
    });
  }
  if (canAccessReportsHub(grant)) {
    reviewItems.push({
      id: "reports",
      to: "/reports",
      labelKey: "admin.nav.reports",
      icon: BarChart3,
      testId: "admin-nav-reports",
      matchPrefixes: ["/reports"],
    });
  }
  if (reviewItems.length > 0) {
    groups.push({
      id: "review",
      titleKey: "admin.nav.group.review",
      items: reviewItems,
    });
  }

  if (canInvite) {
    groups.push({
      id: "security",
      titleKey: "admin.nav.group.security",
      items: [
        {
          id: "ownership",
          to: "/org/ownership-transfer",
          labelKey: "admin.nav.ownership",
          icon: KeyRound,
          testId: "admin-nav-ownership",
          matchPrefixes: ["/org/ownership-transfer"],
        },
      ],
    });
  }

  groups.push({
    id: "settings",
    titleKey: "admin.nav.group.settings",
    items: [
      {
        id: "preferences",
        to: "/settings/preferences",
        labelKey: "admin.nav.preferences",
        icon: Settings,
        testId: "admin-nav-preferences",
        matchPrefixes: ["/settings/preferences"],
      },
    ],
  });

  return groups;
}

export function flattenAdminNavItems(groups: AdminNavGroup[]): AdminNavItem[] {
  return groups.flatMap((g) => g.items);
}

/** Resolve which sidebar item is active for the current path. */
export function matchAdminNavItem(
  pathname: string,
  items: AdminNavItem[],
): AdminNavItemId | null {
  const path = pathname.split("?")[0] ?? pathname;

  // Prefer longest matching prefix so /org/branches wins over /org.
  let best: AdminNavItem | null = null;
  let bestLen = -1;
  for (const item of items) {
    if (item.end) {
      if (path === item.to || path === `${item.to}/`) {
        return item.id;
      }
      continue;
    }
    for (const prefix of item.matchPrefixes) {
      if (path === prefix || path.startsWith(`${prefix}/`)) {
        if (prefix.length > bestLen) {
          best = item;
          bestLen = prefix.length;
        }
      }
    }
  }
  return best?.id ?? null;
}

/**
 * Mobile Manage Business bottom tabs (max 4).
 * Does not expose Sell/Catalog/Orders as primary admin destinations.
 */
export function buildAdminMobileTabs(
  grant: PosSessionGrantFacts | null | undefined,
): AdminMobileTab[] {
  if (!canUseAdminExperience(grant) && !hasOrganizationManagementAuthority(grant)) {
    return [];
  }

  const tabs: AdminMobileTab[] = [
    {
      id: "home",
      to: "/org",
      end: true,
      labelKey: "admin.mobile.home",
      testId: "admin-mobile-home",
    },
    {
      id: "manage",
      to: "/org/manage",
      end: false,
      labelKey: "admin.mobile.manage",
      testId: "admin-mobile-manage",
    },
  ];

  if (canViewDashboard(grant) || canAccessReportsHub(grant)) {
    tabs.push({
      id: "review",
      to: canViewDashboard(grant) ? "/dashboard" : "/reports",
      end: false,
      labelKey: "admin.mobile.review",
      testId: "admin-mobile-review",
    });
  }

  tabs.push({
    id: "more",
    to: "/org/more",
    end: false,
    labelKey: "admin.mobile.more",
    testId: "admin-mobile-more",
  });

  return tabs.slice(0, 4);
}

export function matchAdminMobileTab(
  pathname: string,
  tabs: AdminMobileTab[],
): AdminMobileTabId | null {
  const path = pathname.split("?")[0] ?? pathname;
  if (path === "/org" || path === "/org/") {
    return tabs.some((t) => t.id === "home") ? "home" : null;
  }
  if (path.startsWith("/org/manage")) {
    return "manage";
  }
  if (path.startsWith("/org/more")) {
    return "more";
  }
  if (path.startsWith("/dashboard") || path.startsWith("/reports")) {
    return tabs.some((t) => t.id === "review") ? "review" : null;
  }
  // Nested org admin destinations belong under Manage.
  if (
    path.startsWith("/org/areas") ||
    path.startsWith("/org/branches") ||
    path.startsWith("/org/staff") ||
    path.startsWith("/org/roles") ||
    path.startsWith("/org/devices")
  ) {
    return "manage";
  }
  if (
    path.startsWith("/org/cash-handling") ||
    path.startsWith("/org/business-qr") ||
    path.startsWith("/org/ownership-transfer") ||
    path.startsWith("/settings/preferences")
  ) {
    return "more";
  }
  return null;
}

/** Whether warehouse branch type is commercially entitled (config UI hint). */
export function adminWarehouseConfigAvailable(
  grant: PosSessionGrantFacts | null | undefined,
): boolean {
  return canUseWarehouseBranches(grant);
}

export function shouldUseAdminManagementShell(input: {
  experience: string | null | undefined;
  pathname: string;
}): boolean {
  if (input.experience !== "manage_business") {
    return false;
  }
  const path = input.pathname.split("?")[0] ?? input.pathname;
  if (path.startsWith("/sell")) return false;
  if (path.startsWith("/personal")) return false;
  if (path.startsWith("/onboarding")) return false;
  if (path.startsWith("/workspace")) return false;
  if (path.startsWith("/warehouse")) return false;
  if (path.startsWith("/role/")) return false;
  return true;
}

import type { LucideIcon } from "lucide-react";
import {
  BadgeDollarSign,
  Building2,
  ClipboardCheck,
  ContactRound,
  CreditCard,
  FileText,
  KeyRound,
  LayoutDashboard,
  LineChart,
  Map,
  MapPinned,
  MonitorSmartphone,
  Network,
  PackageCheck,
  PieChart,
  QrCode,
  Settings,
  ShieldCheck,
  Store,
  UserCog,
  Users,
  Wallet,
} from "lucide-react";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  canAccessReportsHub,
  canInviteOrganizationStaff,
  canManageBranchFulfillment,
  canManageStoreAreas,
  canUseAdminExperience,
  canUseWarehouseBranches,
  canViewDashboard,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import type { MessageKey } from "@/i18n/messages";
import type { AccessibleOrganizationWorkspace } from "@/workspace/types";

export type AdminNavGroupId =
  | "overview"
  | "organization"
  | "business"
  | "review"
  | "security"
  | "settings";

export type AdminNavItemId =
  | "overview"
  | "profile"
  | "documents"
  | "areas"
  | "branches"
  | "staff"
  | "roles"
  | "devices"
  | "subscription"
  | "cash"
  | "paymentMethods"
  | "businessQr"
  | "dashboard"
  | "reports"
  | "configureFulfillment"
  | "connectedCommerce"
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
  /** Distinctive group header icon (accordion affordance). */
  icon: LucideIcon;
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
  options?: { branchId?: string | null },
): AdminNavGroup[] {
  if (!canUseAdminExperience(grant) && !hasOrganizationManagementAuthority(grant)) {
    return [];
  }

  const canInvite = canInviteOrganizationStaff(grant);
  const canAdmin = hasOrganizationManagementAuthority(grant);
  const canFulfillment = canManageBranchFulfillment(grant);
  const areasEntitled = canManageStoreAreas(grant);
  const branchId = options?.branchId?.trim() || null;
  const groups: AdminNavGroup[] = [];

  groups.push({
    id: "overview",
    titleKey: "admin.nav.group.overview",
    icon: LayoutDashboard,
    items: [
      {
        id: "overview",
        to: "/org",
        labelKey: "admin.nav.overview",
        icon: LayoutDashboard,
        testId: "admin-nav-overview",
        matchPrefixes: ["/org"],
        end: true,
      },
    ],
  });

  const organizationItems: AdminNavItem[] = [];
  // Organization profile is visible to all Manage Business users (staff read-only).
  organizationItems.push({
    id: "profile",
    to: "/org/profile",
    labelKey: "admin.nav.profile",
    icon: ContactRound,
    testId: "admin-nav-profile",
    matchPrefixes: ["/org/profile"],
  });
  if (canAdmin) {
    organizationItems.push({
      id: "documents",
      to: "/org/documents-printing",
      labelKey: "admin.nav.documentsPrinting",
      icon: FileText,
      testId: "admin-nav-documents-printing",
      matchPrefixes: ["/org/documents-printing"],
    });
  }
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
      icon: MapPinned,
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
      icon: UserCog,
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
  // Commercial subscription self-service is Owner-only — never exposed to org staff.
  if (canInvite) {
    organizationItems.push({
      id: "subscription",
      to: "/org/subscription",
      labelKey: "admin.nav.subscription",
      icon: BadgeDollarSign,
      testId: "admin-nav-subscription",
      matchPrefixes: ["/org/subscription"],
    });
  }
  if (organizationItems.length > 0) {
    groups.push({
      id: "organization",
      titleKey: "admin.nav.group.organization",
      icon: Building2,
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
        id: "paymentMethods",
        to: "/org/payment-methods",
        labelKey: "admin.nav.paymentMethods",
        icon: CreditCard,
        testId: "admin-nav-payment-methods",
        matchPrefixes: ["/org/payment-methods"],
      },
      {
        id: "connectedCommerce",
        to: "/org/connected-commerce",
        labelKey: "admin.nav.connectedCommerce",
        icon: Network,
        testId: "admin-nav-connected-commerce",
        matchPrefixes: ["/org/connected-commerce"],
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
      icon: Store,
      items: businessItems,
    });
  }

  const reviewItems: AdminNavItem[] = [];
  if (canViewDashboard(grant)) {
    reviewItems.push({
      id: "dashboard",
      to: "/dashboard",
      labelKey: "admin.nav.dashboard",
      icon: PieChart,
      testId: "admin-nav-dashboard",
      matchPrefixes: ["/dashboard"],
    });
  }
  if (canAccessReportsHub(grant)) {
    reviewItems.push({
      id: "reports",
      to: "/reports",
      labelKey: "admin.nav.reports",
      icon: LineChart,
      testId: "admin-nav-reports",
      matchPrefixes: ["/reports"],
    });
  }
  if (canFulfillment) {
    reviewItems.push({
      id: "configureFulfillment",
      // Branch fulfillment Overview (PO fulfillment + readiness) — not the branches list.
      to: branchId ? branchFulfillmentEditPath(branchId, "overview") : "/org/branches",
      labelKey: "branches.detail.configureFulfillment",
      icon: PackageCheck,
      testId: "admin-nav-configure-fulfillment",
      matchPrefixes: branchId ? [`/org/branches/${branchId}/fulfillment`] : [],
    });
  }
  if (reviewItems.length > 0) {
    groups.push({
      id: "review",
      titleKey: "admin.nav.group.review",
      icon: ClipboardCheck,
      items: reviewItems,
    });
  }

  if (canInvite) {
    groups.push({
      id: "security",
      titleKey: "admin.nav.group.security",
      icon: ShieldCheck,
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
    icon: Settings,
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

/**
 * Prefer the bound branch; otherwise primary active retail branch for the org.
 * Manage Business binds organization-only (branchId null), so Review → Configure
 * Fulfillment must still land on a concrete fulfillment Overview page.
 */
export function resolveConfigureFulfillmentBranchId(input: {
  boundBranchId?: string | null;
  organizationId?: string | null;
  workspaces: AccessibleOrganizationWorkspace[];
}): string | null {
  const bound = input.boundBranchId?.trim();
  if (bound) return bound;

  const organizationId = input.organizationId?.trim();
  if (!organizationId) return null;

  const org = input.workspaces.find((w) => w.organizationId === organizationId);
  if (!org || org.branches.length === 0) return null;

  const active = org.branches.filter((b) => b.isActive);
  const pool = active.length > 0 ? active : org.branches;
  const retail = pool.filter((b) => !isWarehouseBranch(b.branchType));
  const candidates = retail.length > 0 ? retail : pool;
  const primary = candidates.find((b) => b.isPrimary);
  return (primary ?? candidates[0])?.branchId ?? null;
}

/** Resolve which sidebar item is active for the current path. */
export function matchAdminNavItem(
  pathname: string,
  items: AdminNavItem[],
): AdminNavItemId | null {
  const path = pathname.split("?")[0] ?? pathname;

  // Branch fulfillment edit pages belong to Configure Fulfillment (Review),
  // not Branches & Warehouses — even when the workspace branch id differs.
  if (/^\/org\/branches\/[^/]+\/fulfillment\/?$/.test(path)) {
    const fulfillment = items.find((item) => item.id === "configureFulfillment");
    if (fulfillment) return "configureFulfillment";
  }

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
    path.startsWith("/org/devices") ||
    path.startsWith("/org/subscription")
  ) {
    return "manage";
  }
  if (
    path.startsWith("/org/cash-handling") ||
    path.startsWith("/org/payment-methods") ||
    path.startsWith("/org/connected-commerce") ||
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
  const path = input.pathname.split("?")[0] ?? input.pathname;
  if (path.startsWith("/sell")) return false;
  if (path.startsWith("/personal")) return false;
  if (path.startsWith("/onboarding")) return false;
  if (path.startsWith("/workspace")) return false;
  if (path.startsWith("/warehouse")) return false;
  if (path.startsWith("/role/")) return false;
  // Org management IA always uses Admin shell — including Operations deep-links
  // (Supplier Readiness → fulfillment / payment methods / branches). Keep ops
  // chrome only for org notifications.
  if (path === "/org/notifications" || path.startsWith("/org/notifications/")) {
    return false;
  }
  if (path === "/org" || path.startsWith("/org/")) {
    return true;
  }
  if (input.experience !== "manage_business") {
    return false;
  }
  return true;
}

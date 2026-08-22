import type { LucideIcon } from "lucide-react";
import {
  BarChart3,
  Boxes,
  ClipboardList,
  LayoutDashboard,
  MonitorSmartphone,
  PackagePlus,
  Receipt,
  RefreshCw,
  Settings,
  Truck,
  UserPlus,
  Users,
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
    | "org.more.dashboard"
    | "org.more.reports"
    | "org.more.organization"
    | "org.more.devices"
    | "org.more.staff"
    | "org.more.preferences";
  testId: string;
  icon: LucideIcon;
};

/** Secondary destinations for the More hub — permission-filtered. */
export function buildOrgMoreLinks(grant: PosSessionGrantFacts | null | undefined): OrgMoreLink[] {
  const links: OrgMoreLink[] = [];

  if (canManageCatalog(grant) && canViewInventory(grant)) {
    links.push({
      to: "/inventory",
      labelKey: "org.more.inventory",
      testId: "org-more-inventory",
      icon: Boxes,
    });
  }
  if (canViewCustomers(grant)) {
    links.push({
      to: "/customers",
      labelKey: "org.more.customers",
      testId: "org-more-customers",
      icon: Users,
    });
  }
  if (canViewShifts(grant)) {
    links.push({
      to: "/shifts",
      labelKey: "org.more.shifts",
      testId: "org-more-shifts",
      icon: RefreshCw,
    });
  }
  if (canViewReturns(grant)) {
    links.push({
      to: "/returns",
      labelKey: "org.more.returns",
      testId: "org-more-returns",
      icon: Receipt,
    });
  }
  if (canViewPurchasing(grant)) {
    links.push({
      to: "/purchasing",
      labelKey: "org.more.purchasing",
      testId: "org-more-purchasing",
      icon: PackagePlus,
    });
  }
  if (canViewSuppliers(grant)) {
    links.push({
      to: "/suppliers",
      labelKey: "org.more.suppliers",
      testId: "org-more-suppliers",
      icon: Truck,
    });
  }
  if (canViewDashboard(grant)) {
    links.push({
      to: "/dashboard",
      labelKey: "org.more.dashboard",
      testId: "org-more-dashboard",
      icon: LayoutDashboard,
    });
  }
  if (canAccessReportsHub(grant)) {
    links.push({
      to: "/reports",
      labelKey: "org.more.reports",
      testId: "org-more-reports",
      icon: BarChart3,
    });
  }
  if (canUseAdminExperience(grant) || hasOrganizationManagementAuthority(grant)) {
    links.push({
      to: "/org",
      labelKey: "org.more.organization",
      testId: "org-more-org",
      icon: ClipboardList,
    });
  }
  if (hasOrganizationManagementAuthority(grant)) {
    links.push({
      to: "/org/devices",
      labelKey: "org.more.devices",
      testId: "org-more-devices",
      icon: MonitorSmartphone,
    });
  }
  if (canInviteOrganizationStaff(grant)) {
    links.push({
      to: "/org/staff/invite",
      labelKey: "org.more.staff",
      testId: "org-more-staff",
      icon: UserPlus,
    });
  }
  links.push({
    to: "/settings/preferences",
    labelKey: "org.more.preferences",
    testId: "org-more-preferences",
    icon: Settings,
  });

  return links;
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

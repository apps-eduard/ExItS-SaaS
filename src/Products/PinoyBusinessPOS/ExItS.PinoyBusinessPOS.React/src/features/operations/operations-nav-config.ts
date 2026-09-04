import type { LucideIcon } from "lucide-react";
import {
  ArrowLeftRight,
  BarChart3,
  Boxes,
  ClipboardList,
  LayoutDashboard,
  MonitorSmartphone,
  Package,
  PackagePlus,
  Receipt,
  RefreshCw,
  Settings,
  ShoppingCart,
  Truck,
  Users,
  Wallet,
} from "lucide-react";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  canAccessReportsHub,
  canCreateSale,
  canManageCatalog,
  canManageInventory,
  canManageRegisters,
  canUseOperationsExperience,
  canViewCustomerOrders,
  canViewCustomers,
  canViewDashboard,
  canViewExpenses,
  canViewInventory,
  canViewPurchasing,
  canViewRegisters,
  canViewReturns,
  canViewShifts,
  canViewSuppliers,
} from "@/access/pos-capabilities";
import {
  isWarehouseBranch,
  type OrganizationBranchType,
} from "@/features/branches/branch-type";
import type { MessageKey } from "@/i18n/messages";
import type { WorkingExperience } from "@/workspace/working-experience";
import { workingExperienceRoute } from "@/workspace/working-experience";

export type OperationsNavTabId =
  | "home"
  | "sell"
  | "inventory"
  | "orders"
  | "transfers"
  | "purchasing"
  | "more";

export type OperationsNavTab = {
  id: OperationsNavTabId;
  to: string;
  end: boolean;
  labelKey: MessageKey;
  testId: string;
  primary?: boolean;
};

export type OperationsSidebarItemId = string;

export type OperationsSidebarItem = {
  id: OperationsSidebarItemId;
  to: string;
  labelKey: MessageKey;
  icon: LucideIcon;
  testId: string;
  matchPrefixes: string[];
  end?: boolean;
};

export type OperationsSidebarGroupId =
  | "operations"
  | "daily"
  | "stock"
  | "customers"
  | "control"
  | "insights"
  | "utility";

export type OperationsSidebarGroup = {
  id: OperationsSidebarGroupId;
  titleKey: MessageKey;
  items: OperationsSidebarItem[];
};

/**
 * Operations shell when the principal has operations authority and is not in Manage Business.
 * Pure organization administrators never qualify (canUseOperationsExperience = false).
 */
export function shouldUseOperationsShell(input: {
  experience: WorkingExperience | null | undefined;
  pathname: string;
  grant: PosSessionGrantFacts | null | undefined;
}): boolean {
  if (!canUseOperationsExperience(input.grant)) {
    return false;
  }
  if (input.experience === "manage_business") {
    return false;
  }
  const path = input.pathname;
  if (
    path.startsWith("/personal") ||
    path.startsWith("/onboarding") ||
    path.startsWith("/workspace") ||
    path.startsWith("/org")
  ) {
    return false;
  }
  return true;
}

function retailHomeTo(experience: WorkingExperience): string {
  if (experience === "start_selling") {
    return "/role/cashier";
  }
  const route = workingExperienceRoute(experience);
  if (route === "/sell") {
    return "/role/cashier";
  }
  return route === "/org" ? "/role/manager" : route;
}

/**
 * Retail mobile/tablet bottom nav (max 5):
 * Home · Sell · Inventory · Orders · More
 */
export function buildOperationsBottomNavTabs(input: {
  grant: PosSessionGrantFacts | null | undefined;
  experience: WorkingExperience;
  branchType?: OrganizationBranchType | string | null;
}): OperationsNavTab[] {
  if (isWarehouseBranch(input.branchType)) {
    return buildWarehouseOperationsBottomNavTabs(input.grant);
  }

  const homeTo = retailHomeTo(input.experience);
  const tabs: OperationsNavTab[] = [
    {
      id: "home",
      to: homeTo,
      end: homeTo.startsWith("/role/"),
      labelKey: "org.nav.home",
      testId: "ops-nav-home",
    },
  ];

  if (canCreateSale(input.grant, input.branchType)) {
    tabs.push({
      id: "sell",
      to: "/sell",
      end: false,
      labelKey: "org.nav.sell",
      testId: "ops-nav-sell",
      primary: true,
    });
  }

  if (canViewInventory(input.grant)) {
    tabs.push({
      id: "inventory",
      to: "/inventory",
      end: false,
      labelKey: "org.nav.inventory",
      testId: "ops-nav-inventory",
    });
  }

  if (canViewCustomerOrders(input.grant)) {
    tabs.push({
      id: "orders",
      to: "/orders",
      end: false,
      labelKey: "org.nav.orders",
      testId: "ops-nav-orders",
    });
  }

  tabs.push({
    id: "more",
    to: "/more",
    end: false,
    labelKey: "org.nav.more",
    testId: "ops-nav-more",
  });

  return tabs.slice(0, 5);
}

/** Warehouse: Home · Inventory · Transfers · Purchasing · More — never Sell. */
function buildWarehouseOperationsBottomNavTabs(
  grant: PosSessionGrantFacts | null | undefined,
): OperationsNavTab[] {
  const tabs: OperationsNavTab[] = [
    {
      id: "home",
      to: "/warehouse",
      end: true,
      labelKey: "org.nav.home",
      testId: "ops-nav-home",
    },
  ];

  if (canViewInventory(grant)) {
    tabs.push({
      id: "inventory",
      to: "/inventory",
      end: false,
      labelKey: "org.nav.inventory",
      testId: "ops-nav-inventory",
    });
    tabs.push({
      id: "transfers",
      to: "/inventory/transfers",
      end: false,
      labelKey: "org.nav.transfers",
      testId: "ops-nav-transfers",
    });
  }

  if (canViewPurchasing(grant)) {
    tabs.push({
      id: "purchasing",
      to: "/purchasing",
      end: false,
      labelKey: "org.nav.purchasing",
      testId: "ops-nav-purchasing",
    });
  }

  tabs.push({
    id: "more",
    to: "/more",
    end: false,
    labelKey: "org.nav.more",
    testId: "ops-nav-more",
  });

  return tabs.slice(0, 5);
}

function pushItem(
  items: OperationsSidebarItem[],
  item: OperationsSidebarItem,
  allowed: boolean,
): void {
  if (allowed) {
    items.push(item);
  }
}

/** Desktop (>=1024) sidebar groups — Retail vs Warehouse, capability-filtered, no Admin links. */
export function buildOperationsSidebarGroups(input: {
  grant: PosSessionGrantFacts | null | undefined;
  branchType?: OrganizationBranchType | string | null;
  experience?: WorkingExperience;
}): OperationsSidebarGroup[] {
  const warehouse = isWarehouseBranch(input.branchType);
  const grant = input.grant;
  const homeTo = warehouse
    ? "/warehouse"
    : retailHomeTo(input.experience ?? "operations");

  const groups: OperationsSidebarGroup[] = [];

  groups.push({
    id: "operations",
    titleKey: "operations.nav.group.operations",
    items: [
      {
        id: "home",
        to: homeTo,
        labelKey: "org.nav.home",
        icon: LayoutDashboard,
        testId: "ops-sidebar-home",
        matchPrefixes: warehouse ? ["/warehouse"] : ["/role/manager", "/role/owner", "/role/cashier"],
        end: true,
      },
    ],
  });

  if (!warehouse) {
    const daily: OperationsSidebarItem[] = [];
    pushItem(
      daily,
      {
        id: "sell",
        to: "/sell",
        labelKey: "org.nav.sell",
        icon: ShoppingCart,
        testId: "ops-sidebar-sell",
        matchPrefixes: ["/sell"],
      },
      canCreateSale(grant, input.branchType),
    );
    pushItem(
      daily,
      {
        id: "orders",
        to: "/orders",
        labelKey: "org.nav.orders",
        icon: ClipboardList,
        testId: "ops-sidebar-orders",
        matchPrefixes: ["/orders"],
      },
      canViewCustomerOrders(grant),
    );
    if (daily.length > 0) {
      groups.push({
        id: "daily",
        titleKey: "operations.nav.group.daily",
        items: daily,
      });
    }
  }

  const stock: OperationsSidebarItem[] = [];
  if (!warehouse) {
    pushItem(
      stock,
      {
        id: "catalog",
        to: "/catalog",
        labelKey: "org.nav.catalog",
        icon: Package,
        testId: "ops-sidebar-catalog",
        matchPrefixes: ["/catalog"],
      },
      canManageCatalog(grant),
    );
  }
  pushItem(
    stock,
    {
      id: "inventory",
      to: "/inventory",
      labelKey: "org.nav.inventory",
      icon: Boxes,
      testId: "ops-sidebar-inventory",
      matchPrefixes: ["/inventory"],
    },
    canViewInventory(grant),
  );
  if (warehouse) {
    pushItem(
      stock,
      {
        id: "receive",
        to: "/purchasing/receive-stock",
        labelKey: "org.more.receiveStock",
        icon: PackagePlus,
        testId: "ops-sidebar-receive",
        matchPrefixes: ["/purchasing/receive-stock"],
      },
      canManageInventory(grant),
    );
    pushItem(
      stock,
      {
        id: "transfers",
        to: "/inventory/transfers",
        labelKey: "org.nav.transfers",
        icon: ArrowLeftRight,
        testId: "ops-sidebar-transfers",
        matchPrefixes: ["/inventory/transfers"],
      },
      canViewInventory(grant),
    );
  }
  pushItem(
    stock,
    {
      id: "purchasing",
      to: "/purchasing",
      labelKey: "org.nav.purchasing",
      icon: PackagePlus,
      testId: "ops-sidebar-purchasing",
      matchPrefixes: ["/purchasing"],
    },
    canViewPurchasing(grant),
  );
  if (!warehouse) {
    pushItem(
      stock,
      {
        id: "transfers",
        to: "/inventory/transfers",
        labelKey: "org.nav.transfers",
        icon: ArrowLeftRight,
        testId: "ops-sidebar-transfers",
        matchPrefixes: ["/inventory/transfers"],
      },
      canViewInventory(grant),
    );
  }
  pushItem(
    stock,
    {
      id: "suppliers",
      to: "/suppliers",
      labelKey: "org.more.suppliers",
      icon: Truck,
      testId: "ops-sidebar-suppliers",
      matchPrefixes: ["/suppliers"],
    },
    canViewSuppliers(grant),
  );
  if (warehouse) {
    pushItem(
      stock,
      {
        id: "expiring",
        to: "/inventory/expiration",
        labelKey: "org.more.expiringLots",
        icon: ClipboardList,
        testId: "ops-sidebar-expiring",
        matchPrefixes: ["/inventory/expiration"],
      },
      canViewInventory(grant),
    );
    pushItem(
      stock,
      {
        id: "movements",
        to: "/inventory/stock-use",
        labelKey: "org.more.stockMovements",
        icon: RefreshCw,
        testId: "ops-sidebar-movements",
        matchPrefixes: ["/inventory/stock-use"],
      },
      canViewInventory(grant),
    );
  }
  if (stock.length > 0) {
    groups.push({
      id: "stock",
      titleKey: "operations.nav.group.stock",
      items: stock,
    });
  }

  if (!warehouse) {
    const customers: OperationsSidebarItem[] = [];
    pushItem(
      customers,
      {
        id: "customers",
        to: "/customers",
        labelKey: "org.more.customers",
        icon: Users,
        testId: "ops-sidebar-customers",
        matchPrefixes: ["/customers"],
      },
      canViewCustomers(grant),
    );
    if (customers.length > 0) {
      groups.push({
        id: "customers",
        titleKey: "operations.nav.group.customers",
        items: customers,
      });
    }

    const control: OperationsSidebarItem[] = [];
    pushItem(
      control,
      {
        id: "expenses",
        to: "/expenses",
        labelKey: "org.more.expenses",
        icon: Wallet,
        testId: "ops-sidebar-expenses",
        matchPrefixes: ["/expenses"],
      },
      canViewExpenses(grant),
    );
    pushItem(
      control,
      {
        id: "returns",
        to: "/returns",
        labelKey: "org.more.returns",
        icon: Receipt,
        testId: "ops-sidebar-returns",
        matchPrefixes: ["/returns"],
      },
      canViewReturns(grant),
    );
    pushItem(
      control,
      {
        id: "shifts",
        to: "/shifts",
        labelKey: "org.more.shifts",
        icon: RefreshCw,
        testId: "ops-sidebar-shifts",
        matchPrefixes: ["/shifts"],
      },
      canViewShifts(grant),
    );
    pushItem(
      control,
      {
        id: "registers",
        to: "/registers",
        labelKey: "register.listTitle",
        icon: MonitorSmartphone,
        testId: "ops-sidebar-registers",
        matchPrefixes: ["/registers"],
      },
      canViewRegisters(grant) || canManageRegisters(grant),
    );
    if (control.length > 0) {
      groups.push({
        id: "control",
        titleKey: "operations.nav.group.control",
        items: control,
      });
    }
  } else {
    const control: OperationsSidebarItem[] = [];
    pushItem(
      control,
      {
        id: "adjustments",
        to: "/inventory/stock-use",
        labelKey: "org.more.stockMovements",
        icon: RefreshCw,
        testId: "ops-sidebar-adjustments",
        matchPrefixes: ["/inventory/stock-use"],
      },
      canViewInventory(grant),
    );
    if (control.length > 0) {
      groups.push({
        id: "control",
        titleKey: "operations.nav.group.control",
        items: control,
      });
    }
  }

  const insights: OperationsSidebarItem[] = [];
  pushItem(
    insights,
    {
      id: "dashboard",
      to: "/dashboard",
      labelKey: "org.more.dashboard",
      icon: LayoutDashboard,
      testId: "ops-sidebar-dashboard",
      matchPrefixes: ["/dashboard"],
    },
    canViewDashboard(grant),
  );
  pushItem(
    insights,
    {
      id: "reports",
      to: "/reports",
      labelKey: "org.more.reports",
      icon: BarChart3,
      testId: "ops-sidebar-reports",
      matchPrefixes: ["/reports"],
    },
    canAccessReportsHub(grant),
  );
  if (insights.length > 0) {
    groups.push({
      id: "insights",
      titleKey: "operations.nav.group.insights",
      items: insights,
    });
  }

  groups.push({
    id: "utility",
    titleKey: "operations.nav.group.utility",
    items: [
      {
        id: "preferences",
        to: "/settings/preferences",
        labelKey: "org.more.preferences",
        icon: Settings,
        testId: "ops-sidebar-preferences",
        matchPrefixes: ["/settings/preferences"],
      },
    ],
  });

  return groups.filter((g) => g.items.length > 0);
}

export function flattenOperationsSidebarItems(
  groups: ReadonlyArray<OperationsSidebarGroup>,
): OperationsSidebarItem[] {
  return groups.flatMap((g) => g.items);
}

export function matchOperationsSidebarItem(
  pathname: string,
  items: ReadonlyArray<OperationsSidebarItem>,
): OperationsSidebarItemId | null {
  let best: OperationsSidebarItem | null = null;
  for (const item of items) {
    if (item.end) {
      if (pathname === item.to) {
        return item.id;
      }
      continue;
    }
    for (const prefix of item.matchPrefixes) {
      if (pathname === prefix || pathname.startsWith(`${prefix}/`)) {
        if (!best || prefix.length > (best.matchPrefixes[0]?.length ?? 0)) {
          best = item;
        }
      }
    }
  }
  return best?.id ?? null;
}

export function matchOperationsNavTab(
  pathname: string,
  tabs: ReadonlyArray<OperationsNavTab>,
): OperationsNavTabId | null {
  if (pathname === "/more" || pathname.startsWith("/more/")) {
    return tabs.some((t) => t.id === "more") ? "more" : null;
  }
  let best: OperationsNavTab | null = null;
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

/** Admin-only path prefixes that must never appear in Operations navigation. */
export const OPERATIONS_FORBIDDEN_ADMIN_PREFIXES = [
  "/org/areas",
  "/org/branches",
  "/org/staff",
  "/org/roles",
  "/org/devices",
  "/org/cash-handling",
  "/org/business-qr",
  "/org/manage",
  "/org/ownership",
  "/onboarding",
] as const;

export function isAdminOnlyOperationsPath(pathname: string): boolean {
  if (pathname === "/org" || pathname.startsWith("/org/")) {
    // /org itself is Admin overview — forbidden in ops shell links
    return true;
  }
  return OPERATIONS_FORBIDDEN_ADMIN_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}

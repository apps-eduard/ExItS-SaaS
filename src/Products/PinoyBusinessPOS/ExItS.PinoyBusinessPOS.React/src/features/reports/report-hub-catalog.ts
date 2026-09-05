import type { LucideIcon } from "lucide-react";
import {
  canViewAdvancedReports,
  canViewReports,
} from "@/access/pos-capabilities";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  canAccessClassicReport,
  canAccessOperationalReport,
  type ClassicReportKind,
  type OperationalReportKind,
} from "@/features/reports/report-access";
import {
  iconForClassicReport,
  iconForOperationalReport,
} from "@/features/reports/report-hub-icons";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import type { MessageKey } from "@/i18n/messages";

/** Visible hub categories (Overview is curated shortcuts, not a full dump). */
export type ReportHubCategoryId =
  | "overview"
  | "sales"
  | "inventory"
  | "purchasing"
  | "expenses"
  | "utang"
  | "shifts";

export type ReportHubEntryId =
  | `classic:${ClassicReportKind}`
  | `operational:${OperationalReportKind}`
  | "dashboard";

export type ReportHubEntry = {
  id: ReportHubEntryId;
  category: Exclude<ReportHubCategoryId, "overview"> | "overview";
  path: string;
  titleKey: MessageKey;
  descriptionKey: MessageKey;
  icon: LucideIcon;
  testId: string;
  /** Categories where this entry appears (Overview shortcuts reuse sales/inventory items). */
  categories: ReportHubCategoryId[];
  /** Hide when workspace is a Warehouse location (retail-only operations). */
  retailOnly: boolean;
  searchTerms: string[];
};

const CATEGORY_ORDER: ReportHubCategoryId[] = [
  "overview",
  "sales",
  "inventory",
  "purchasing",
  "expenses",
  "utang",
  "shifts",
];

export const REPORT_HUB_CATEGORY_LABEL_KEYS: Record<ReportHubCategoryId, MessageKey> = {
  overview: "reports.hub.category.overview",
  sales: "reports.groupSales",
  inventory: "reports.groupInventory",
  purchasing: "reports.groupPurchasing",
  expenses: "reports.groupExpenses",
  utang: "reports.groupUtang",
  shifts: "reports.groupShifts",
};

function classicEntry(
  kind: ClassicReportKind,
  category: Exclude<ReportHubCategoryId, "overview">,
  path: string,
  titleKey: MessageKey,
  descriptionKey: MessageKey,
  options: { retailOnly?: boolean; overview?: boolean; searchTerms: string[] },
): ReportHubEntry {
  const categories: ReportHubCategoryId[] = [category];
  if (options.overview) {
    categories.push("overview");
  }
  return {
    id: `classic:${kind}`,
    category,
    path,
    titleKey,
    descriptionKey,
    icon: iconForClassicReport(kind),
    testId: `report-link-${kind}`,
    categories,
    retailOnly: options.retailOnly ?? false,
    searchTerms: options.searchTerms,
  };
}

function operationalEntry(
  kind: OperationalReportKind,
  category: Exclude<ReportHubCategoryId, "overview">,
  titleKey: MessageKey,
  descriptionKey: MessageKey,
  options: { retailOnly?: boolean; overview?: boolean; searchTerms: string[] },
): ReportHubEntry {
  const categories: ReportHubCategoryId[] = [category];
  if (options.overview) {
    categories.push("overview");
  }
  return {
    id: `operational:${kind}`,
    category,
    path: `/reports/operational/${kind}`,
    titleKey,
    descriptionKey,
    icon: iconForOperationalReport(kind),
    testId: `report-link-${kind}`,
    categories,
    retailOnly: options.retailOnly ?? false,
    searchTerms: options.searchTerms,
  };
}

/**
 * Full hub catalog (permission + warehouse filtering applied by buildReportHubCatalog).
 * Classic domain overviews + operational reports; operational `overview` omitted to avoid
 * duplicating classic Sales overview.
 */
export const REPORT_HUB_CATALOG: readonly ReportHubEntry[] = [
  classicEntry("sales", "sales", "/reports/sales", "reports.hub.salesOverviewTitle", "reports.hub.salesOverviewDetail", {
    retailOnly: true,
    searchTerms: ["sales", "overview", "revenue", "payments"],
  }),
  operationalEntry("sales-summary", "sales", "reports.salesSummary", "reports.hub.desc.salesSummary", {
    retailOnly: true,
    overview: true,
    searchTerms: ["sales", "summary", "totals", "transactions", "performance"],
  }),
  operationalEntry("sales-by-payment", "sales", "reports.salesByPayment", "reports.hub.desc.salesByPayment", {
    retailOnly: true,
    searchTerms: ["sales", "payment", "tender", "cash", "gcash"],
  }),
  operationalEntry("sales-by-product", "sales", "reports.salesByProduct", "reports.hub.desc.salesByProduct", {
    retailOnly: true,
    searchTerms: ["sales", "product", "sku", "items"],
  }),
  operationalEntry("returns", "sales", "reports.returns", "reports.hub.desc.returns", {
    retailOnly: true,
    searchTerms: ["returns", "refund", "sales"],
  }),
  operationalEntry("profitability", "sales", "reports.profitability", "reports.hub.desc.profitability", {
    retailOnly: true,
    searchTerms: ["profit", "profitability", "margin", "cogs"],
  }),
  operationalEntry(
    "product-profitability",
    "sales",
    "reports.productProfitability",
    "reports.hub.desc.productProfitability",
    {
      retailOnly: true,
      searchTerms: ["profit", "product", "profitability", "margin", "cogs"],
    },
  ),

  classicEntry(
    "inventory",
    "inventory",
    "/reports/inventory",
    "reports.hub.inventoryOverviewTitle",
    "reports.hub.inventoryOverviewDetail",
    {
      searchTerms: ["inventory", "stock", "overview", "levels"],
    },
  ),
  operationalEntry(
    "inventory-status",
    "inventory",
    "reports.inventoryStatus",
    "reports.hub.desc.inventoryStatus",
    {
      overview: true,
      searchTerms: ["inventory", "stock", "status", "on hand", "levels"],
    },
  ),
  operationalEntry(
    "inventory-movements",
    "inventory",
    "reports.inventoryMovements",
    "reports.hub.desc.inventoryMovements",
    {
      searchTerms: ["inventory", "stock", "movements", "transfers", "receive"],
    },
  ),
  operationalEntry(
    "stock-count-variance",
    "inventory",
    "reports.stockCountVariance",
    "reports.hub.desc.stockCountVariance",
    {
      searchTerms: ["stock", "count", "variance", "inventory", "audit"],
    },
  ),

  operationalEntry(
    "purchasing-summary",
    "purchasing",
    "reports.purchasingSummary",
    "reports.hub.desc.purchasingSummary",
    {
      overview: true,
      searchTerms: ["purchasing", "purchase", "summary", "supplier", "orders"],
    },
  ),
  operationalEntry(
    "purchase-outstanding",
    "purchasing",
    "reports.purchaseOutstanding",
    "reports.hub.desc.purchaseOutstanding",
    {
      searchTerms: ["purchase", "outstanding", "open", "orders", "receiving"],
    },
  ),
  operationalEntry(
    "supplier-purchasing",
    "purchasing",
    "reports.supplierPurchasing",
    "reports.hub.desc.supplierPurchasing",
    {
      searchTerms: ["supplier", "purchasing", "vendor"],
    },
  ),
  operationalEntry(
    "supplier-payables",
    "purchasing",
    "reports.supplierPayables",
    "reports.hub.desc.supplierPayables",
    {
      searchTerms: ["supplier", "payables", "credit", "owed", "balance"],
    },
  ),

  classicEntry(
    "expenses",
    "expenses",
    "/reports/expenses",
    "reports.hub.expensesOverviewTitle",
    "reports.hub.expensesOverviewDetail",
    {
      searchTerms: ["expenses", "spending", "overview", "cost"],
    },
  ),
  operationalEntry(
    "expenses-summary",
    "expenses",
    "reports.expenseSummary",
    "reports.hub.desc.expenseSummary",
    {
      overview: true,
      searchTerms: ["expense", "expenses", "summary", "spending"],
    },
  ),

  classicEntry("utang", "utang", "/reports/utang", "reports.hub.utangOverviewTitle", "reports.hub.utangOverviewDetail", {
    retailOnly: true,
    searchTerms: ["utang", "credit", "customer", "balances", "receivable"],
  }),
  operationalEntry("utang-by-product", "utang", "reports.utangByProduct", "reports.hub.desc.utangByProduct", {
    retailOnly: true,
    searchTerms: ["utang", "product", "credit", "customer"],
  }),

  operationalEntry("shifts", "shifts", "reports.shiftSummary", "reports.hub.desc.shiftSummary", {
    retailOnly: true,
    searchTerms: ["shift", "shifts", "cashier", "register", "summary"],
  }),
  operationalEntry("cash-variance", "shifts", "reports.cashVariance", "reports.hub.desc.cashVariance", {
    retailOnly: true,
    searchTerms: ["cash", "variance", "shift", "drawer", "till"],
  }),
];

function entryAccessible(
  grant: PosSessionGrantFacts | null | undefined,
  entry: ReportHubEntry,
): boolean {
  if (entry.id.startsWith("classic:")) {
    const kind = entry.id.slice("classic:".length) as ClassicReportKind;
    return canAccessClassicReport(grant, kind);
  }
  if (entry.id.startsWith("operational:")) {
    const kind = entry.id.slice("operational:".length) as OperationalReportKind;
    return canAccessOperationalReport(grant, kind);
  }
  return false;
}

export type ReportHubCatalogResult = {
  entries: ReportHubEntry[];
  categories: ReportHubCategoryId[];
  showAdvancedUpgrade: boolean;
};

export function buildReportHubCatalog(
  grant: PosSessionGrantFacts | null | undefined,
  options?: { branchType?: string | null },
): ReportHubCatalogResult {
  const warehouse = isWarehouseBranch(options?.branchType);
  const entries = REPORT_HUB_CATALOG.filter((entry) => {
    if (warehouse && entry.retailOnly) {
      return false;
    }
    return entryAccessible(grant, entry);
  });

  const categories = CATEGORY_ORDER.filter((category) =>
    entries.some((entry) => entry.categories.includes(category)),
  );

  const showAdvancedUpgrade =
    !canViewAdvancedReports(grant) &&
    (canViewReports(grant) ||
      entries.some((entry) => entry.id.startsWith("classic:")));

  return { entries, categories, showAdvancedUpgrade };
}

export function filterReportHubEntries(
  entries: ReportHubEntry[],
  category: ReportHubCategoryId,
  searchQuery: string,
  resolveText: (entry: ReportHubEntry) => { title: string; description: string },
): ReportHubEntry[] {
  const q = searchQuery.trim().toLowerCase();
  const inCategory = (entry: ReportHubEntry) => entry.categories.includes(category);

  if (!q) {
    return entries.filter(inCategory);
  }

  return entries.filter((entry) => {
    const { title, description } = resolveText(entry);
    const haystack = [
      title,
      description,
      ...entry.searchTerms,
      entry.titleKey,
      entry.descriptionKey,
    ]
      .join(" ")
      .toLowerCase();
    return haystack.includes(q);
  });
}

export function reportHubCategoryOrder(): readonly ReportHubCategoryId[] {
  return CATEGORY_ORDER;
}

import {
  canViewAdvancedReports,
  canViewExpenses,
  canViewInventory,
  canViewPurchasing,
  canViewReports,
  canViewShifts,
  hasOrganizationManagementAuthority,
  isPosCashierRole,
  isPosOperationsManager,
  isPosOwnerRole,
  resolveEffectivePosRoleCode,
  type PosSessionGrantFacts,
} from "@/access/pos-capabilities";

export {
  canAccessReportsHub,
  canViewDashboard,
  canViewExpenses,
  canViewReports,
} from "@/access/pos-capabilities";

/** Mirrors PosOperationalReportKind route segments used by MAUI/React. */
export type OperationalReportKind =
  | "overview"
  | "sales-summary"
  | "sales-by-payment"
  | "sales-by-product"
  | "returns"
  | "profitability"
  | "product-profitability"
  | "shifts"
  | "cash-variance"
  | "inventory-status"
  | "inventory-movements"
  | "stock-count-variance"
  | "purchasing-summary"
  | "purchase-outstanding"
  | "supplier-purchasing"
  | "supplier-payables"
  | "expenses-summary"
  | "utang-by-product";

export type ClassicReportKind = "sales" | "utang" | "inventory" | "expenses";

/**
 * Per-kind operational report access — mirrors PosRoleMatrix.AllowsReport + org management.
 */
export function canAccessOperationalReport(
  grant: PosSessionGrantFacts | null | undefined,
  kind: OperationalReportKind,
): boolean {
  if (kind === "profitability" || kind === "product-profitability") {
    return canViewReports(grant);
  }

  if (!canViewAdvancedReports(grant)) {
    return false;
  }

  if (hasOrganizationManagementAuthority(grant)) {
    return true;
  }

  if (!grant?.productAccessAllowed) {
    return false;
  }

  if (isPosOwnerRole(grant) || isPosOperationsManager(grant)) {
    return true;
  }

  const role = resolveEffectivePosRoleCode(grant)?.toLowerCase();

  if (role === "reportinguser") {
    return true;
  }

  if (isPosCashierRole(grant)) {
    return kind === "shifts" || kind === "cash-variance";
  }

  if (role === "inventorystaff") {
    return (
      kind === "inventory-status" ||
      kind === "inventory-movements" ||
      kind === "stock-count-variance" ||
      kind === "purchasing-summary" ||
      kind === "purchase-outstanding" ||
      kind === "supplier-payables"
    );
  }

  return false;
}

export function canAccessClassicReport(
  grant: PosSessionGrantFacts | null | undefined,
  kind: ClassicReportKind,
): boolean {
  if (kind === "inventory") {
    return canViewReports(grant) || canViewInventory(grant);
  }
  if (kind === "expenses") {
    return canViewExpenses(grant);
  }
  return canViewReports(grant);
}

/** Whether a kind needs date filters (purchase-outstanding / inventory-status / supplier-payables are as-of). */
export function operationalReportNeedsDates(kind: OperationalReportKind): boolean {
  return (
    kind !== "inventory-status" &&
    kind !== "purchase-outstanding" &&
    kind !== "supplier-payables"
  );
}

export const OPERATIONAL_REPORT_KINDS: OperationalReportKind[] = [
  "overview",
  "sales-summary",
  "sales-by-payment",
  "sales-by-product",
  "returns",
  "profitability",
  "product-profitability",
  "shifts",
  "cash-variance",
  "inventory-status",
  "inventory-movements",
  "stock-count-variance",
  "purchasing-summary",
  "purchase-outstanding",
  "supplier-purchasing",
  "supplier-payables",
  "expenses-summary",
  "utang-by-product",
];

export function isOperationalReportKind(value: string): value is OperationalReportKind {
  return (OPERATIONAL_REPORT_KINDS as string[]).includes(value);
}

export type ReportHubLink = {
  kind: OperationalReportKind;
  path: string;
  titleKey: string;
};

export type ReportHubGroup = {
  id: string;
  titleKey: string;
  items: ReportHubLink[];
};

export function buildOperationalReportGroups(
  grant: PosSessionGrantFacts | null | undefined,
): ReportHubGroup[] {
  if (!canViewAdvancedReports(grant)) {
    return [];
  }

  const groups: ReportHubGroup[] = [];

  if (canViewReports(grant)) {
    groups.push({
      id: "sales",
      titleKey: "reports.groupSales",
      items: [
        {
          kind: "overview",
          path: "/reports/operational/overview",
          titleKey: "reports.overview",
        },
        {
          kind: "sales-summary",
          path: "/reports/operational/sales-summary",
          titleKey: "reports.salesSummary",
        },
        {
          kind: "sales-by-payment",
          path: "/reports/operational/sales-by-payment",
          titleKey: "reports.salesByPayment",
        },
        {
          kind: "sales-by-product",
          path: "/reports/operational/sales-by-product",
          titleKey: "reports.salesByProduct",
        },
        {
          kind: "returns",
          path: "/reports/operational/returns",
          titleKey: "reports.returns",
        },
        {
          kind: "profitability",
          path: "/reports/operational/profitability",
          titleKey: "reports.profitability",
        },
        {
          kind: "product-profitability",
          path: "/reports/operational/product-profitability",
          titleKey: "reports.productProfitability",
        },
      ],
    });
  }

  if (canViewReports(grant) || canViewShifts(grant)) {
    const candidates: ReportHubLink[] = [
      {
        kind: "shifts",
        path: "/reports/operational/shifts",
        titleKey: "reports.shiftSummary",
      },
      {
        kind: "cash-variance",
        path: "/reports/operational/cash-variance",
        titleKey: "reports.cashVariance",
      },
    ];
    const items = candidates.filter((item) => canAccessOperationalReport(grant, item.kind));
    if (items.length > 0) {
      groups.push({ id: "shifts", titleKey: "reports.groupShifts", items });
    }
  }

  if (canViewReports(grant) || canViewInventory(grant)) {
    const candidates: ReportHubLink[] = [
      {
        kind: "inventory-status",
        path: "/reports/operational/inventory-status",
        titleKey: "reports.inventoryStatus",
      },
      {
        kind: "inventory-movements",
        path: "/reports/operational/inventory-movements",
        titleKey: "reports.inventoryMovements",
      },
      {
        kind: "stock-count-variance",
        path: "/reports/operational/stock-count-variance",
        titleKey: "reports.stockCountVariance",
      },
    ];
    const items = candidates.filter((item) => canAccessOperationalReport(grant, item.kind));
    if (items.length > 0) {
      groups.push({ id: "inventory", titleKey: "reports.groupInventory", items });
    }
  }

  if (canViewReports(grant) || canViewPurchasing(grant)) {
    const candidates: ReportHubLink[] = [
      {
        kind: "purchasing-summary",
        path: "/reports/operational/purchasing-summary",
        titleKey: "reports.purchasingSummary",
      },
      {
        kind: "purchase-outstanding",
        path: "/reports/operational/purchase-outstanding",
        titleKey: "reports.purchaseOutstanding",
      },
      {
        kind: "supplier-purchasing",
        path: "/reports/operational/supplier-purchasing",
        titleKey: "reports.supplierPurchasing",
      },
      {
        kind: "supplier-payables",
        path: "/reports/operational/supplier-payables",
        titleKey: "reports.supplierPayables",
      },
    ];
    const items = candidates.filter((item) => canAccessOperationalReport(grant, item.kind));
    if (items.length > 0) {
      groups.push({ id: "purchasing", titleKey: "reports.groupPurchasing", items });
    }
  }

  if (canViewReports(grant) || canViewExpenses(grant)) {
    const candidates: ReportHubLink[] = [
      {
        kind: "expenses-summary",
        path: "/reports/operational/expenses-summary",
        titleKey: "reports.expenseSummary",
      },
    ];
    const items = candidates.filter((item) => canAccessOperationalReport(grant, item.kind));
    if (items.length > 0) {
      groups.push({ id: "expenses", titleKey: "reports.groupExpenses", items });
    }
  }

  if (canViewReports(grant)) {
    groups.push({
      id: "utang",
      titleKey: "reports.groupUtang",
      items: [
        {
          kind: "utang-by-product",
          path: "/reports/operational/utang-by-product",
          titleKey: "reports.utangByProduct",
        },
      ],
    });
  }

  return groups;
}

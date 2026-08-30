import { listOrganizationBranches } from "@/api/platform/platform-auth-client";
import type { MessageKey } from "@/i18n/messages";

/** Report kinds that accept server-side `branchId` query filtering. */
export type ReportScopeMode = "branch" | "all" | "organization_only";

export type ReportBranchScopeSelection =
  | { mode: "current" }
  | { mode: "all" }
  | { mode: "branch"; branchId: string };

/**
 * Maps classic / operational report kinds to whether branch filtering is truthful.
 * Organization-only kinds must never send branchId pretending to filter.
 */
export function reportScopeModeForClassic(
  kind: "sales" | "utang" | "inventory" | "expenses",
): ReportScopeMode {
  if (kind === "sales") {
    return "branch";
  }
  return "organization_only";
}

export function reportScopeModeForOperational(kind: string): ReportScopeMode {
  switch (kind) {
    case "overview":
    case "sales-summary":
    case "sales-by-payment":
    case "sales-by-product":
    case "returns":
    case "profitability":
    case "product-profitability":
    case "inventory-movements":
      return "branch";
    case "shifts":
    case "cash-variance":
      // Actor/role restricted; CashierShift lacks BranchId — label as organization /
      // actor-scoped without fake branch filter.
      return "organization_only";
    case "inventory-status":
    case "stock-count-variance":
    case "purchasing-summary":
    case "purchase-outstanding":
    case "supplier-purchasing":
    case "supplier-payables":
    case "expenses-summary":
    case "utang-by-product":
      return "organization_only";
    default:
      return "organization_only";
  }
}

/** Dashboard: sale metrics branch-scoped; expenses/utang/low-stock remain org-wide. */
export function reportScopeModeForDashboard(): ReportScopeMode {
  return "branch";
}

export function reportScopeModeForManagementOverview(): ReportScopeMode {
  return "organization_only";
}

export function canSelectAllBranches(opts: {
  isOwner?: boolean;
  isManager?: boolean;
  isReportingUser?: boolean;
  hasOrgManagement?: boolean;
}): boolean {
  return Boolean(
    opts.hasOrgManagement || opts.isOwner || opts.isManager || opts.isReportingUser,
  );
}

/**
 * Resolves query branchId for branch-capable reports.
 * - current → acting branch
 * - all → undefined (organization aggregate)
 * - branch → selected id
 * Organization-only reports always return undefined.
 */
export function resolveReportBranchIdQuery(
  scopeMode: ReportScopeMode,
  selection: ReportBranchScopeSelection,
  currentBranchId: string | null | undefined,
): string | undefined {
  if (scopeMode !== "branch") {
    return undefined;
  }
  if (selection.mode === "all") {
    return undefined;
  }
  if (selection.mode === "branch") {
    return selection.branchId;
  }
  return currentBranchId || undefined;
}

export function scopeLabelKey(
  scopeMode: ReportScopeMode,
  selection: ReportBranchScopeSelection,
): MessageKey {
  if (scopeMode === "organization_only") {
    return "reports.scope.allBranches";
  }
  if (selection.mode === "all") {
    return "reports.scope.allBranches";
  }
  return "reports.scope.currentBranch";
}

export { listOrganizationBranches };

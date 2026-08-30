import type { MessageKey } from "@/i18n/messages";
import type { ReportBranchScopeSelection } from "@/features/reports/report-branch-scope";

/** Explicit dashboard metric scope — never leave implicit. */
export type DashboardMetricScope = "branch" | "organization";

export function resolveDashboardBranchScopeLabel(
  t: (key: MessageKey) => string,
  selection: ReportBranchScopeSelection,
  branchDisplayName: string | null | undefined,
): string {
  if (selection.mode === "all") {
    return t("dashboard.scope.allBranches");
  }

  const name = branchDisplayName?.trim();
  if (name) {
    return t("dashboard.scope.branchNamed").replace("{name}", name);
  }

  return t("dashboard.scope.branch");
}

export function resolveDashboardOrganizationScopeLabel(
  t: (key: MessageKey) => string,
): string {
  return t("dashboard.scope.organization");
}

export function resolveDashboardBranchDisplayName(
  selection: ReportBranchScopeSelection,
  currentBranchId: string | null | undefined,
  currentBranchName: string | null | undefined,
  branches: ReadonlyArray<{ id: string; name: string }>,
): string | null {
  if (selection.mode === "all") {
    return null;
  }
  if (selection.mode === "branch") {
    const named = branches.find((b) => b.id === selection.branchId);
    return named?.name ?? selection.branchId;
  }
  if (currentBranchName?.trim()) {
    return currentBranchName.trim();
  }
  if (currentBranchId) {
    const named = branches.find((b) => b.id === currentBranchId);
    return named?.name ?? currentBranchId;
  }
  return null;
}

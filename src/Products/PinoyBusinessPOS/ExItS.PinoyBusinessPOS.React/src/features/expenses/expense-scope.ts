import type { PosExpenseScopeOptionsDto } from "@/api/pos/pos-expense-client";

export type ExpenseViewScopeKind = "branch" | "allBranches" | "organization" | "allExpenses";

export type ExpenseViewScopeSelection =
  | { kind: "branch"; branchId: string }
  | { kind: "allBranches" }
  | { kind: "organization" }
  | { kind: "allExpenses" };

export type ExpenseCreateTarget =
  | { kind: "branch"; branchId: string }
  | { kind: "organization" };

/** Count of selectable view scopes (used to hide the control when only one). */
export function countExpenseViewScopes(options: PosExpenseScopeOptionsDto): number {
  let count = options.branches.length;
  if (options.canViewAllBranches) {
    count += 1;
  }
  if (options.canViewOrganization) {
    count += 1;
  }
  if (options.canViewAllExpenses) {
    count += 1;
  }
  return count;
}

export function shouldShowExpenseViewScopeSelector(options: PosExpenseScopeOptionsDto): boolean {
  return countExpenseViewScopes(options) > 1;
}

/**
 * Default view scope: current bound branch when authorized; otherwise sole branch;
 * otherwise organization when allowed.
 */
export function defaultExpenseViewScope(
  options: PosExpenseScopeOptionsDto,
  currentBranchId: string | null | undefined,
): ExpenseViewScopeSelection {
  const current = currentBranchId?.trim();
  if (current && options.branches.some((b) => b.branchId === current)) {
    return { kind: "branch", branchId: current };
  }
  if (options.branches.length === 1) {
    return { kind: "branch", branchId: options.branches[0]!.branchId };
  }
  if (options.canViewOrganization) {
    return { kind: "organization" };
  }
  if (options.canViewAllBranches) {
    return { kind: "allBranches" };
  }
  if (options.canViewAllExpenses) {
    return { kind: "allExpenses" };
  }
  if (options.branches.length > 0) {
    return { kind: "branch", branchId: options.branches[0]!.branchId };
  }
  return { kind: "organization" };
}

export function expenseViewScopeToQuery(selection: ExpenseViewScopeSelection): {
  scope: ExpenseViewScopeKind;
  branchId?: string;
} {
  if (selection.kind === "branch") {
    return { scope: "branch", branchId: selection.branchId };
  }
  return { scope: selection.kind };
}

export function expenseViewScopeSelectValue(selection: ExpenseViewScopeSelection): string {
  if (selection.kind === "branch") {
    return `branch:${selection.branchId}`;
  }
  return selection.kind;
}

export function parseExpenseViewScopeSelectValue(value: string): ExpenseViewScopeSelection | null {
  if (value === "allBranches" || value === "organization" || value === "allExpenses") {
    return { kind: value };
  }
  if (value.startsWith("branch:")) {
    const branchId = value.slice("branch:".length).trim();
    return branchId ? { kind: "branch", branchId } : null;
  }
  return null;
}

export function defaultExpenseCreateTarget(
  options: PosExpenseScopeOptionsDto,
  currentBranchId: string | null | undefined,
): ExpenseCreateTarget {
  const current = currentBranchId?.trim();
  if (current && options.branches.some((b) => b.branchId === current)) {
    return { kind: "branch", branchId: current };
  }
  if (options.branches.length === 1) {
    return { kind: "branch", branchId: options.branches[0]!.branchId };
  }
  if (options.canCreateOrganizationWide && options.branches.length === 0) {
    return { kind: "organization" };
  }
  if (options.branches.length > 0) {
    return { kind: "branch", branchId: options.branches[0]!.branchId };
  }
  return { kind: "organization" };
}

export function shouldShowExpenseCreateScopeSelector(options: PosExpenseScopeOptionsDto): boolean {
  const branchChoices = options.branches.length;
  const orgChoice = options.canCreateOrganizationWide ? 1 : 0;
  return branchChoices + orgChoice > 1;
}

export function expenseCreateTargetToBranchId(target: ExpenseCreateTarget): string | null {
  return target.kind === "branch" ? target.branchId : null;
}

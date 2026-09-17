import { describe, expect, it } from "vitest";
import type { PosExpenseScopeOptionsDto } from "@/api/pos/pos-expense-client";
import {
  countExpenseViewScopes,
  defaultExpenseCreateTarget,
  defaultExpenseViewScope,
  expenseViewScopeToQuery,
  shouldShowExpenseCreateScopeSelector,
  shouldShowExpenseViewScopeSelector,
} from "@/features/expenses/expense-scope";

const main = "11111111-1111-1111-1111-111111111111";
const iloilo = "22222222-2222-2222-2222-222222222222";

function options(partial: Partial<PosExpenseScopeOptionsDto>): PosExpenseScopeOptionsDto {
  return {
    canViewOrganization: false,
    canCreateOrganizationWide: false,
    canViewAllBranches: false,
    canViewAllExpenses: false,
    branches: [],
    ...partial,
  };
}

describe("expense-scope", () => {
  it("hides view selector for a single authorized branch", () => {
    const opts = options({
      branches: [{ branchId: main, name: "Main Branch" }],
    });
    expect(countExpenseViewScopes(opts)).toBe(1);
    expect(shouldShowExpenseViewScopeSelector(opts)).toBe(false);
    expect(defaultExpenseViewScope(opts, main)).toEqual({ kind: "branch", branchId: main });
  });

  it("defaults to current branch and shows selector for multi-branch manager", () => {
    const opts = options({
      canViewAllBranches: true,
      branches: [
        { branchId: main, name: "Main Branch" },
        { branchId: iloilo, name: "Iloilo Branch" },
      ],
    });
    expect(shouldShowExpenseViewScopeSelector(opts)).toBe(true);
    expect(defaultExpenseViewScope(opts, main)).toEqual({ kind: "branch", branchId: main });
    expect(expenseViewScopeToQuery({ kind: "allBranches" })).toEqual({ scope: "allBranches" });
  });

  it("owner options include organization and all expenses", () => {
    const opts = options({
      canViewOrganization: true,
      canCreateOrganizationWide: true,
      canViewAllBranches: true,
      canViewAllExpenses: true,
      branches: [
        { branchId: main, name: "Main Branch" },
        { branchId: iloilo, name: "Iloilo Branch" },
      ],
    });
    expect(shouldShowExpenseViewScopeSelector(opts)).toBe(true);
    expect(defaultExpenseViewScope(opts, main)).toEqual({ kind: "branch", branchId: main });
    expect(shouldShowExpenseCreateScopeSelector(opts)).toBe(true);
    expect(defaultExpenseCreateTarget(opts, main)).toEqual({ kind: "branch", branchId: main });
  });
});

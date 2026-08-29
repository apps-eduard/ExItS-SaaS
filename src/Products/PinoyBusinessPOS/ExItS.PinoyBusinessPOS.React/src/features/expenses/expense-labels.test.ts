import { describe, expect, it } from "vitest";
import {
  expenseCategoryStatusLabelKey,
  expensePaymentLabelKey,
  expenseStatusLabelKey,
  formatExpenseDate,
} from "@/features/expenses/expense-labels";

describe("expense-labels", () => {
  it("maps known status and payment codes", () => {
    expect(expenseStatusLabelKey("Recorded")).toBe("expense.status.recorded");
    expect(expenseStatusLabelKey("Voided")).toBe("expense.status.voided");
    expect(expensePaymentLabelKey("Cash")).toBe("expense.payment.cash");
    expect(expensePaymentLabelKey("ManualGCash")).toBe("expense.payment.manualGCash");
    expect(expenseCategoryStatusLabelKey("Active")).toBe("expense.category.active");
    expect(expenseCategoryStatusLabelKey("Inactive")).toBe("expense.category.inactive");
  });

  it("formats expense date without inventing branch ownership", () => {
    expect(formatExpenseDate("2026-08-29")).toMatch(/2026/);
  });
});

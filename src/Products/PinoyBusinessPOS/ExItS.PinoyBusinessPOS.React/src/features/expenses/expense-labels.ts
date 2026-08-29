import type { MessageKey } from "@/i18n/messages";

export function expenseStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Recorded":
      return "expense.status.recorded";
    case "Voided":
      return "expense.status.voided";
    default:
      return "expense.status.unknown";
  }
}

export function expensePaymentLabelKey(method: string): MessageKey {
  switch (method) {
    case "Cash":
      return "expense.payment.cash";
    case "ManualGCash":
      return "expense.payment.manualGCash";
    default:
      return "expense.payment.unknown";
  }
}

export function expenseCategoryStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Active":
      return "expense.category.active";
    case "Inactive":
      return "expense.category.inactive";
    default:
      return "expense.status.unknown";
  }
}

/** Format DateOnly wire `YYYY-MM-DD` for display. */
export function formatExpenseDate(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value.trim());
  if (!match) {
    return value;
  }
  const date = new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    timeZone: "UTC",
  }).format(date);
}

export function todayExpenseDateInput(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

import type { MessageKey } from "@/i18n/messages";

export type StatementEntryFilter = "all" | "credit" | "repayment";

export const STATEMENT_ENTRY_FILTERS: ReadonlyArray<{
  key: StatementEntryFilter;
  labelKey: MessageKey;
}> = [
  { key: "all", labelKey: "customers.statementFilterAll" },
  { key: "credit", labelKey: "customers.statementFilterCredit" },
  { key: "repayment", labelKey: "customers.statementFilterRepayment" },
];

/** Shared statement line fields used by the canonical statement UI. */
export type CustomerStatementLineView = {
  entryId: string;
  entryType: string;
  recordedAtUtc: string;
  amount: number;
  status: string;
  remarks?: string | null;
  sourceSaleId?: string | null;
};

export type CustomerStatementTotalsView = {
  outstandingBalance: number;
  closingBalance: number;
  periodCreditTotal: number;
  periodRepaymentTotal: number;
  lines: CustomerStatementLineView[];
};

export function defaultStatementPeriod() {
  const end = new Date();
  const start = new Date(end);
  start.setDate(start.getDate() - 30);
  const toIsoDate = (value: Date) => value.toISOString().slice(0, 10);
  return { periodStart: toIsoDate(start), periodEnd: toIsoDate(end) };
}

export function statementLineDescription(line: {
  remarks?: string | null;
  status: string;
}): string {
  return line.remarks?.trim() || line.status || "—";
}

export function formatStatementWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

export function matchesStatementEntryFilter(
  entryType: string,
  filter: StatementEntryFilter,
): boolean {
  if (filter === "all") {
    return true;
  }
  return entryType.trim().toLowerCase() === filter;
}

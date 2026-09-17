/** Client-side preview for B2B hybrid payment allocation (server still enforces). */

export type OpenReceivableForAllocation = {
  creditEntryId: string;
  outstandingBalance: number;
  dueDate?: string | null;
  createdAtUtc: string;
  sourceType?: string | null;
  sourceReference?: string | null;
};

export type AllocationPreviewLine = {
  creditEntryId: string;
  amount: number;
  outstandingBefore: number;
  outstandingAfter: number;
  sourceType?: string | null;
  sourceReference?: string | null;
  dueDate?: string | null;
};

function roundMoney(n: number): number {
  return Math.round(n * 100) / 100;
}

export function orderOpenReceivables<T extends OpenReceivableForAllocation>(items: T[]): T[] {
  return [...items].sort((a, b) => {
    const aNull = a.dueDate == null || a.dueDate === "";
    const bNull = b.dueDate == null || b.dueDate === "";
    if (aNull !== bNull) {
      return aNull ? 1 : -1;
    }
    if (!aNull && !bNull && a.dueDate !== b.dueDate) {
      return String(a.dueDate).localeCompare(String(b.dueDate));
    }
    const created = String(a.createdAtUtc).localeCompare(String(b.createdAtUtc));
    if (created !== 0) {
      return created;
    }
    return a.creditEntryId.localeCompare(b.creditEntryId);
  });
}

export function allocatePaymentAutomatically(
  open: OpenReceivableForAllocation[],
  paymentAmount: number,
): AllocationPreviewLine[] {
  let remaining = roundMoney(paymentAmount);
  if (!(remaining > 0)) {
    return [];
  }
  const lines: AllocationPreviewLine[] = [];
  for (const receivable of orderOpenReceivables(open.filter((o) => o.outstandingBalance > 0))) {
    if (remaining <= 0) {
      break;
    }
    const apply = roundMoney(Math.min(remaining, receivable.outstandingBalance));
    if (apply <= 0) {
      continue;
    }
    lines.push({
      creditEntryId: receivable.creditEntryId,
      amount: apply,
      outstandingBefore: receivable.outstandingBalance,
      outstandingAfter: roundMoney(receivable.outstandingBalance - apply),
      sourceType: receivable.sourceType,
      sourceReference: receivable.sourceReference,
      dueDate: receivable.dueDate,
    });
    remaining = roundMoney(remaining - apply);
  }
  return lines;
}

export function validateManualAllocations(args: {
  open: OpenReceivableForAllocation[];
  allocations: { creditEntryId: string; amount: number }[];
  paymentAmount: number;
}): { ok: true; lines: AllocationPreviewLine[] } | { ok: false; error: string } {
  const payment = roundMoney(args.paymentAmount);
  if (!(payment > 0)) {
    return { ok: false, error: "invalid_payment" };
  }
  const byId = new Map(args.open.map((o) => [o.creditEntryId, o]));
  const seen = new Set<string>();
  const lines: AllocationPreviewLine[] = [];
  let sum = 0;
  for (const row of args.allocations) {
    const amount = roundMoney(row.amount);
    if (!(amount > 0)) {
      return { ok: false, error: "invalid_line" };
    }
    if (seen.has(row.creditEntryId)) {
      return { ok: false, error: "duplicate" };
    }
    seen.add(row.creditEntryId);
    const receivable = byId.get(row.creditEntryId);
    if (!receivable) {
      return { ok: false, error: "unknown" };
    }
    if (amount - receivable.outstandingBalance > 1e-9) {
      return { ok: false, error: "exceeds_receivable" };
    }
    lines.push({
      creditEntryId: row.creditEntryId,
      amount,
      outstandingBefore: receivable.outstandingBalance,
      outstandingAfter: roundMoney(receivable.outstandingBalance - amount),
      sourceType: receivable.sourceType,
      sourceReference: receivable.sourceReference,
      dueDate: receivable.dueDate,
    });
    sum = roundMoney(sum + amount);
  }
  if (Math.abs(sum - payment) > 1e-9) {
    return { ok: false, error: "sum_mismatch" };
  }
  return { ok: true, lines };
}

export function countOpenAfterAllocation(
  open: OpenReceivableForAllocation[],
  lines: AllocationPreviewLine[],
): number {
  const applied = new Map(lines.map((l) => [l.creditEntryId, l.amount]));
  return open.filter((o) => {
    const take = applied.get(o.creditEntryId) ?? 0;
    return roundMoney(o.outstandingBalance - take) > 1e-9;
  }).length;
}

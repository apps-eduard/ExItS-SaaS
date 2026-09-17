import type { BusinessReceivable } from "@/api/pos/pos-connected-suppliers-client";
import type { PosSupplierPayableDto } from "@/api/pos/pos-supplier-payables-client";
import { laterPaymentsAmount } from "@/features/purchasing/receive-payment";

export type B2bObligationPerspective = "receivable" | "payable";

export type B2bObligationListFilter = "open" | "overdue" | "paid" | "all";

export const B2B_OBLIGATION_PAGE_SIZE = 10;

/** Canonical obligation row shared by seller Receivables and buyer Payables. */
export type B2bObligationItem = {
  id: string;
  perspective: B2bObligationPerspective;
  status: string;
  isOverdue: boolean;
  sourceType: string;
  sourceId: string | null;
  sourceReference: string | null;
  transactionDateUtc: string;
  originalAmount: number;
  paidAtSourceAmount: number;
  laterPaymentsAmount: number;
  balance: number;
  dueDate: string | null;
};

export function filterB2bObligations<
  T extends { status: string; isOverdue: boolean; balance: number },
>(items: readonly T[], filter: B2bObligationListFilter): T[] {
  switch (filter) {
    case "open":
      return items.filter(
        (p) => (p.status === "Open" || p.status === "PartiallyPaid") && p.balance > 0,
      );
    case "overdue":
      return items.filter(
        (p) =>
          p.isOverdue &&
          p.status !== "Paid" &&
          p.status !== "Voided" &&
          p.status !== "Reversed" &&
          p.balance > 0,
      );
    case "paid":
      return items.filter((p) => p.status === "Paid");
    case "all":
    default:
      return [...items];
  }
}

export function countB2bObligationsByFilter<
  T extends { status: string; isOverdue: boolean; balance: number },
>(items: readonly T[]): Record<B2bObligationListFilter, number> {
  return {
    open: filterB2bObligations(items, "open").length,
    overdue: filterB2bObligations(items, "overdue").length,
    paid: filterB2bObligations(items, "paid").length,
    all: items.length,
  };
}

export function paginateB2bObligations<T>(
  items: readonly T[],
  visibleCount: number,
): { visible: T[]; hasMore: boolean } {
  const limit = Math.max(0, visibleCount);
  return {
    visible: items.slice(0, limit),
    hasMore: items.length > limit,
  };
}

export function mapReceivableToObligation(item: BusinessReceivable): B2bObligationItem {
  const later =
    typeof item.laterPaymentsAmount === "number"
      ? item.laterPaymentsAmount
      : Math.max(0, item.originalAmount - item.outstandingBalance);
  const paidAtSource =
    typeof item.paidAtSourceAmount === "number" ? item.paidAtSourceAmount : 0;
  const status =
    item.status?.trim() ||
    (item.outstandingBalance <= 0
      ? "Paid"
      : later > 0
        ? "PartiallyPaid"
        : "Open");
  return {
    id: item.creditEntryId,
    perspective: "receivable",
    status,
    isOverdue: item.isOverdue === true,
    sourceType: normalizeSourceType(item.sourceType),
    sourceId: item.sourceId?.trim() || null,
    sourceReference: cleanSourceReference(item.sourceReference, item.remarks),
    transactionDateUtc: item.createdAtUtc,
    originalAmount: item.originalAmount,
    paidAtSourceAmount: paidAtSource,
    laterPaymentsAmount: later,
    balance: item.outstandingBalance,
    dueDate: item.dueDate?.trim() || null,
  };
}

export function mapPayableToObligation(item: PosSupplierPayableDto): B2bObligationItem {
  return {
    id: item.payableId,
    perspective: "payable",
    status: item.status,
    isOverdue: item.isOverdue,
    sourceType: normalizeSourceType(item.sourceType),
    sourceId: item.sourceId,
    sourceReference: cleanSourceReference(item.sourceReference, null),
    transactionDateUtc: item.createdAtUtc,
    originalAmount: item.originalAmount,
    paidAtSourceAmount: item.paidAtReceiptAmount,
    laterPaymentsAmount: laterPaymentsAmount(item.paidAmount, item.paidAtReceiptAmount),
    balance: item.balance,
    dueDate: item.dueDate?.trim() || null,
  };
}

/** Align legacy seller labels (PO / DirectPurchase) with payable canonical types. */
export function normalizeSourceType(raw: string | null | undefined): string {
  const value = (raw ?? "").trim();
  const lower = value.toLowerCase();
  if (lower === "po" || lower === "goodsreceipt" || lower === "purchaseorderreceipt") {
    return "GoodsReceipt";
  }
  if (lower === "directpurchase" || lower === "direct" || lower === "directpurchasereceipt") {
    return "DirectPurchaseReceipt";
  }
  if (lower === "sale") {
    return "Sale";
  }
  return value || "Other";
}

/**
 * Strip ugly internal refs such as `sale:<guid>|Product sale …`.
 */
export function cleanSourceReference(
  reference: string | null | undefined,
  remarks: string | null | undefined,
): string | null {
  const candidates = [reference, remarks];
  for (const raw of candidates) {
    const value = raw?.trim();
    if (!value) {
      continue;
    }
    const cleaned = stripInternalSourcePrefix(value);
    if (cleaned) {
      return cleaned;
    }
  }
  return null;
}

function stripInternalSourcePrefix(value: string): string {
  const match = /^(sale|grn|dpr):[0-9a-fA-F-]{36}\|?(.*)$/i.exec(value);
  if (!match) {
    return value;
  }
  const rest = (match[2] ?? "").trim();
  return rest || value;
}

/**
 * Deep-link to canonical source when the current org owns that document.
 * Paths must match `router.tsx` (seller sales live under `/sell/...`).
 */
export function resolveB2bObligationSourceHref(
  perspective: B2bObligationPerspective,
  sourceType: string,
  sourceId: string | null,
): string | null {
  if (!sourceId) {
    return null;
  }
  const type = normalizeSourceType(sourceType);
  if (type === "Sale") {
    return perspective === "receivable"
      ? `/sell/sales/${sourceId}/summary`
      : `/purchasing/direct-purchases/b2b/${sourceId}`;
  }
  if (type === "DirectPurchaseReceipt") {
    // Direct purchase receipts live on the buyer org.
    return perspective === "payable" ? `/purchasing/direct-purchases/${sourceId}` : null;
  }
  if (type === "GoodsReceipt") {
    // GRN id alone has no dedicated detail route; buyer receive list is the owned hub.
    return perspective === "payable" ? "/purchasing/receipts" : null;
  }
  return null;
}

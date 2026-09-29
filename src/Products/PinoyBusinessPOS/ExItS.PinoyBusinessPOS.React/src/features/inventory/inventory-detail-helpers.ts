import type {
  PosInventoryAccountDto,
  PosInventoryBranchRollupDto,
  PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import {
  formatInventoryQty,
  resolveExpiredQuantity,
  resolveInTransitInboundQuantity,
  resolveInTransitOutboundQuantity,
  resolvePendingReturnQuantity,
  resolveSalePolicyBlockedQuantity,
} from "@/features/inventory/inventory-reservation-display";

/** Non-expired quantity beyond the near-expiry warning window (from API totals). */
export function computeGoodQuantity(account: PosInventoryAccountDto): number {
  const sellable = account.sellableQuantity ?? 0;
  const nearExpiry = account.nearExpiryQuantity ?? 0;
  return Math.max(0, sellable - nearExpiry);
}

export function sortLotsByExpiry(lots: PosInventoryLotDto[]): PosInventoryLotDto[] {
  return [...lots].sort((a, b) => a.expirationDate.localeCompare(b.expirationDate));
}

export function formatLotBatchLabel(lotNumber?: string | null): string {
  return lotNumber?.trim() ? lotNumber.trim() : "—";
}

export function canDisableExpirationTracking(account: PosInventoryAccountDto): boolean {
  return account.isTracked && account.onHandQuantity <= 0;
}

/**
 * Tracked product with zero on-hand at the current location and no opening
 * movement for this location yet (hasOpeningStock is branch-scoped from the API).
 */
export function canAddOpeningStock(account: PosInventoryAccountDto): boolean {
  return (
    account.isTracked &&
    account.hasOpeningStock !== true &&
    account.onHandQuantity <= 0
  );
}

export type StockExceptionKind =
  | "damaged"
  | "inspectionHold"
  | "pendingReturn"
  | "expired"
  | "nearExpiry"
  | "saleBlocked"
  | "inTransitOutbound"
  | "inTransitInbound";

export type StockExceptionRow = {
  kind: StockExceptionKind;
  quantity: number;
  testId: string;
};

function nonNegative(value: number | null | undefined): number {
  if (value == null || !Number.isFinite(value)) return 0;
  return Math.max(0, value);
}

/** Exception buckets shown only when quantity is non-zero. */
export function listNonZeroStockExceptions(quantities: {
  damagedQuantity?: number | null;
  inspectionHoldQuantity?: number | null;
  pendingReturnQuantity?: number | null;
  expiredQuantity?: number | null;
  nearExpiryQuantity?: number | null;
  salePolicyBlockedQuantity?: number | null;
  saleBlockedQuantity?: number | null;
  inTransitOutboundQuantity?: number | null;
  inTransitInboundQuantity?: number | null;
}): StockExceptionRow[] {
  const candidates: StockExceptionRow[] = [
    {
      kind: "damaged",
      quantity: nonNegative(quantities.damagedQuantity),
      testId: "inventory-exception-damaged",
    },
    {
      kind: "inspectionHold",
      quantity: nonNegative(quantities.inspectionHoldQuantity),
      testId: "inventory-exception-inspection-hold",
    },
    {
      kind: "pendingReturn",
      quantity: nonNegative(
        quantities.pendingReturnQuantity ?? resolvePendingReturnQuantity(quantities),
      ),
      testId: "inventory-exception-pending-return",
    },
    {
      kind: "expired",
      quantity: nonNegative(quantities.expiredQuantity ?? resolveExpiredQuantity(quantities)),
      testId: "inventory-exception-expired",
    },
    {
      kind: "nearExpiry",
      quantity: nonNegative(quantities.nearExpiryQuantity),
      testId: "inventory-exception-near-expiry",
    },
    {
      kind: "saleBlocked",
      quantity: nonNegative(
        quantities.saleBlockedQuantity ??
          quantities.salePolicyBlockedQuantity ??
          resolveSalePolicyBlockedQuantity(quantities),
      ),
      testId: "inventory-exception-sale-blocked",
    },
    {
      kind: "inTransitOutbound",
      quantity: nonNegative(
        quantities.inTransitOutboundQuantity ??
          resolveInTransitOutboundQuantity(quantities),
      ),
      testId: "inventory-exception-in-transit-out",
    },
    {
      kind: "inTransitInbound",
      quantity: nonNegative(
        quantities.inTransitInboundQuantity ?? resolveInTransitInboundQuantity(quantities),
      ),
      testId: "inventory-exception-incoming",
    },
  ];
  return candidates.filter((row) => row.quantity > 0);
}

export function stockExceptionLabelKey(
  kind: StockExceptionKind,
):
  | "inventory.bucketDamaged"
  | "inventory.bucketInspectionHold"
  | "inventory.productSummary.pendingReturn"
  | "inventory.expiredQty"
  | "inventory.nearExpiryQty"
  | "inventory.saleBlocked"
  | "inventory.inTransitOutbound"
  | "inventory.inTransitInbound" {
  switch (kind) {
    case "damaged":
      return "inventory.bucketDamaged";
    case "inspectionHold":
      return "inventory.bucketInspectionHold";
    case "pendingReturn":
      return "inventory.productSummary.pendingReturn";
    case "expired":
      return "inventory.expiredQty";
    case "nearExpiry":
      return "inventory.nearExpiryQty";
    case "saleBlocked":
      return "inventory.saleBlocked";
    case "inTransitOutbound":
      return "inventory.inTransitOutbound";
    case "inTransitInbound":
      return "inventory.inTransitInbound";
  }
}

/** Compact exception suffix for branch rows, e.g. "· 5 damaged". */
export function formatStockExceptionSuffix(
  kind: StockExceptionKind,
  quantity: number,
  labels: Record<StockExceptionKind, string>,
): string {
  const qty = formatInventoryQty(quantity);
  const label = labels[kind].toLowerCase();
  return `· ${qty} ${label}`;
}

/**
 * Compact branch breakdown: `Name · {onHand} on hand · {available} available`
 * plus non-zero exception suffixes. Uses server available — never recomputes.
 */
export function formatBranchRollupMetricsLine(
  branch: Pick<
    PosInventoryBranchRollupDto,
    | "onHandQuantity"
    | "availableQuantity"
    | "damagedQuantity"
    | "inspectionHoldQuantity"
    | "pendingReturnQuantity"
    | "expiredQuantity"
    | "nearExpiryQuantity"
    | "salePolicyBlockedQuantity"
    | "inTransitOutboundQuantity"
    | "inTransitInboundQuantity"
  >,
  labels: {
    onHand: string;
    available: string;
    exceptions: Record<StockExceptionKind, string>;
  },
): string {
  const parts = [
    `${formatInventoryQty(branch.onHandQuantity)} ${labels.onHand}`,
    `${formatInventoryQty(branch.availableQuantity)} ${labels.available}`,
  ];
  for (const row of listNonZeroStockExceptions(branch)) {
    parts.push(
      formatStockExceptionSuffix(row.kind, row.quantity, labels.exceptions).replace(/^·\s*/, ""),
    );
  }
  return parts.join(" · ");
}

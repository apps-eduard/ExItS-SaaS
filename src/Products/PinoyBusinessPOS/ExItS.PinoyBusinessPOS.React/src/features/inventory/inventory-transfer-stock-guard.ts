/** Client-side stock guards for transfer create. Server remains authoritative. */

export type TransferStockLine = {
  key: string;
  productId: string;
  quantity: number;
  sourceLotId: string | null;
  availableQuantity: number;
  lotAvailableQuantity: number | null;
  tracksExpiration: boolean;
  isTracked: boolean;
};

export type TransferDraftAllocation = {
  lotId: string;
  quantity: number;
  lotAvailableQuantity: number;
};

/** Logical product row on the create draft (may expand to multiple SourceLotId lines). */
export type TransferDraftStockProduct = {
  key: string;
  productId: string;
  quantity: number;
  availableQuantity: number;
  /** Cap from eligible lots when tracksExpiration; otherwise null. */
  eligibleLotQuantity: number | null;
  tracksExpiration: boolean;
  isTracked: boolean;
  allocations: readonly TransferDraftAllocation[];
};

export function productDemandExcludingLine(
  lines: readonly TransferStockLine[],
  productId: string,
  excludeKey?: string,
): number {
  return lines.reduce((sum, line) => {
    if (line.productId !== productId) {
      return sum;
    }
    if (excludeKey && line.key === excludeKey) {
      return sum;
    }
    return sum + (Number.isFinite(line.quantity) ? line.quantity : 0);
  }, 0);
}

export function lotDemandExcludingLine(
  lines: readonly TransferStockLine[],
  sourceLotId: string,
  excludeKey?: string,
): number {
  return lines.reduce((sum, line) => {
    if (line.sourceLotId !== sourceLotId) {
      return sum;
    }
    if (excludeKey && line.key === excludeKey) {
      return sum;
    }
    return sum + (Number.isFinite(line.quantity) ? line.quantity : 0);
  }, 0);
}

export type TransferLineStockIssue =
  | "untracked"
  | "out_of_stock"
  | "over_stock"
  | "lot_out_of_stock"
  | "lot_over_stock"
  | "allocation_mismatch"
  | "invalid_qty";

export function maxTransferableQuantity(product: {
  availableQuantity: number;
  eligibleLotQuantity: number | null;
  tracksExpiration: boolean;
}): number {
  if (product.tracksExpiration && product.eligibleLotQuantity != null) {
    // Physical lot sum is authoritative for expiry-tracked transfers.
    return Math.max(0, product.eligibleLotQuantity);
  }
  return Math.max(0, product.availableQuantity);
}

export function evaluateTransferLineStock(
  line: TransferStockLine,
  lines: readonly TransferStockLine[],
): TransferLineStockIssue | null {
  if (!line.isTracked) {
    return "untracked";
  }
  if (!(line.quantity > 0) || !Number.isFinite(line.quantity)) {
    return "invalid_qty";
  }
  if (line.availableQuantity <= 0) {
    return "out_of_stock";
  }
  const productTotal = productDemandExcludingLine(lines, line.productId, line.key) + line.quantity;
  if (productTotal > line.availableQuantity) {
    return "over_stock";
  }
  if (line.tracksExpiration && line.sourceLotId) {
    const lotAvail = line.lotAvailableQuantity ?? 0;
    if (lotAvail <= 0) {
      return "lot_out_of_stock";
    }
    const lotTotal = lotDemandExcludingLine(lines, line.sourceLotId, line.key) + line.quantity;
    if (lotTotal > lotAvail) {
      return "lot_over_stock";
    }
  }
  return null;
}

/**
 * Stock evaluation for one logical draft product (including multi-lot FEFO / manual allocation).
 */
export function evaluateTransferDraftProduct(
  product: TransferDraftStockProduct,
): TransferLineStockIssue | null {
  if (!product.isTracked) {
    return "untracked";
  }
  if (!(product.quantity > 0) || !Number.isFinite(product.quantity)) {
    return "invalid_qty";
  }
  const maxQty = maxTransferableQuantity(product);
  if (maxQty <= 0) {
    return "out_of_stock";
  }
  if (product.quantity > maxQty) {
    return product.tracksExpiration ? "lot_over_stock" : "over_stock";
  }
  if (!product.tracksExpiration) {
    return null;
  }
  if (product.allocations.length === 0) {
    return "lot_out_of_stock";
  }
  const allocated = product.allocations.reduce(
    (sum, row) => sum + (Number.isFinite(row.quantity) ? row.quantity : 0),
    0,
  );
  if (allocated !== product.quantity) {
    return "allocation_mismatch";
  }
  for (const row of product.allocations) {
    if (!(row.quantity > 0) || !Number.isFinite(row.quantity)) {
      return "invalid_qty";
    }
    if (row.lotAvailableQuantity <= 0) {
      return "lot_out_of_stock";
    }
    if (row.quantity > row.lotAvailableQuantity) {
      return "lot_over_stock";
    }
  }
  return null;
}

export function canAddTransferQuantity(args: {
  quantity: number;
  availableQuantity: number;
  lotAvailableQuantity: number | null;
  tracksExpiration: boolean;
  existingProductDemand: number;
  existingLotDemand: number;
}): TransferLineStockIssue | null {
  const { quantity, availableQuantity, lotAvailableQuantity, tracksExpiration } = args;
  if (!(quantity > 0) || !Number.isFinite(quantity)) {
    return "invalid_qty";
  }
  if (availableQuantity <= 0) {
    return "out_of_stock";
  }
  if (args.existingProductDemand + quantity > availableQuantity) {
    return "over_stock";
  }
  if (tracksExpiration) {
    const lotAvail = lotAvailableQuantity ?? 0;
    if (lotAvail <= 0) {
      return "lot_out_of_stock";
    }
    if (args.existingLotDemand + quantity > lotAvail) {
      return "lot_over_stock";
    }
  }
  return null;
}

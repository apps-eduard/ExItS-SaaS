import { roundMoney, roundQuantity } from "@/cart/sell-cart-helpers";

/** Basket line fields used for request-time cost / retail estimates. */
export type RequestEstimateLine = {
  quantity: number;
  warehouseUnitCost?: number | null;
  branchEffectiveSellingPrice?: number | null;
};

export type RequestLineEstimates = {
  estimatedCost: number | null;
  potentialRetail: number | null;
  potentialGross: number | null;
};

export type RequestBasketTotals = {
  /** Distinct products in the basket — never a mixed physical qty sum. */
  productCount: number;
  estimatedCostTotal: number | null;
  potentialRetailTotal: number | null;
  potentialGross: number | null;
};

function finiteOrNull(value: number | null | undefined): number | null {
  if (value == null || !Number.isFinite(value)) {
    return null;
  }
  return value;
}

/** qty × warehouse unit cost (request-time estimate only). */
export function estimateLineCost(
  quantity: number,
  warehouseUnitCost: number | null | undefined,
): number | null {
  const cost = finiteOrNull(warehouseUnitCost);
  if (cost == null || !Number.isFinite(quantity) || quantity <= 0) {
    return null;
  }
  return roundMoney(quantity * cost);
}

/** qty × branch effective selling price (preview only; not stored on StockRequest). */
export function estimateLineRetail(
  quantity: number,
  branchEffectiveSellingPrice: number | null | undefined,
): number | null {
  const price = finiteOrNull(branchEffectiveSellingPrice);
  if (price == null || !Number.isFinite(quantity) || quantity <= 0) {
    return null;
  }
  return roundMoney(quantity * price);
}

export function estimateLineGross(
  estimatedCost: number | null,
  potentialRetail: number | null,
): number | null {
  if (estimatedCost == null || potentialRetail == null) {
    return null;
  }
  return roundMoney(potentialRetail - estimatedCost);
}

export function estimateRequestLine(line: RequestEstimateLine): RequestLineEstimates {
  const qty = Number.isFinite(line.quantity) ? roundQuantity(line.quantity) : 0;
  const estimatedCost = estimateLineCost(qty, line.warehouseUnitCost);
  const potentialRetail = estimateLineRetail(qty, line.branchEffectiveSellingPrice);
  return {
    estimatedCost,
    potentialRetail,
    potentialGross: estimateLineGross(estimatedCost, potentialRetail),
  };
}

/**
 * Footer totals for the request basket.
 * Does not sum physical quantities across mixed UOMs — only product count + money.
 * Cost / retail totals include only lines where that side is known; potential gross
 * requires both totals to be fully known for every line with quantity.
 */
export function summarizeRequestBasket(
  lines: ReadonlyArray<RequestEstimateLine>,
): RequestBasketTotals {
  const active = lines.filter((l) => Number.isFinite(l.quantity) && l.quantity > 0);
  const productCount = active.length;

  if (productCount === 0) {
    return {
      productCount: 0,
      estimatedCostTotal: null,
      potentialRetailTotal: null,
      potentialGross: null,
    };
  }

  let costSum = 0;
  let costKnownCount = 0;
  let retailSum = 0;
  let retailKnownCount = 0;

  for (const line of active) {
    const { estimatedCost, potentialRetail } = estimateRequestLine(line);
    if (estimatedCost != null) {
      costSum = roundMoney(costSum + estimatedCost);
      costKnownCount += 1;
    }
    if (potentialRetail != null) {
      retailSum = roundMoney(retailSum + potentialRetail);
      retailKnownCount += 1;
    }
  }

  const estimatedCostTotal = costKnownCount > 0 ? costSum : null;
  const potentialRetailTotal = retailKnownCount > 0 ? retailSum : null;
  const bothFullyKnown = costKnownCount === productCount && retailKnownCount === productCount;

  return {
    productCount,
    estimatedCostTotal,
    potentialRetailTotal,
    potentialGross: bothFullyKnown
      ? estimateLineGross(estimatedCostTotal, potentialRetailTotal)
      : null,
  };
}

import type {
  BuyerSupplierProductLink,
  SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";

/** Sentinel for the All category chip. */
export const CONNECTED_PO_CATEGORY_ALL = "all";

/** Sentinel for products without supplier category metadata. */
export const CONNECTED_PO_CATEGORY_OTHER = "other";

export type ConnectedPoReadyProduct = {
  linkId: string;
  buyerProductId: string;
  supplierProductId: string;
  productName: string;
  unitOfMeasure: string;
  supplierSku: string | null;
  unitPurchaseCost: number;
  purchaseUnitId: string | null;
  packageLabel: string | null;
  /** Purchase-unit → base inventory multiplier (default 1). */
  multiplierToBase: number;
  /** When false/undefined before stock loads, treat as unknown (not blocking). */
  stockTracked: boolean | null;
  /** Available quantity in base inventory units when tracked. */
  availableBaseQuantity: number | null;
  /** Supplier category display name when present; otherwise null → Other. */
  categoryName: string | null;
};

export type ConnectedPoCategoryFacet = {
  key: string;
  label: string;
  count: number;
};

export type ConnectedPoDraftLine = {
  productId: string;
  name: string;
  uom: string;
  orderedQty: number;
  unitPurchaseCost: number;
  purchaseUnitId?: string | null;
};

export function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export function lineTotal(orderedQty: number, unitPurchaseCost: number): number {
  return roundMoney(orderedQty * unitPurchaseCost);
}

export function orderSubtotal(lines: ReadonlyArray<{ orderedQty: number; unitPurchaseCost: number }>): number {
  return roundMoney(lines.reduce((sum, line) => sum + lineTotal(line.orderedQty, line.unitPurchaseCost), 0));
}

export function orderUnitCount(lines: ReadonlyArray<{ orderedQty: number }>): number {
  return lines.reduce((sum, line) => sum + line.orderedQty, 0);
}

/** Active linked products with a positive PO price (and optional shared/orderable join). */
export function buildConnectedReadyProducts(
  links: ReadonlyArray<BuyerSupplierProductLink>,
  exposures: ReadonlyArray<SupplierProductExposure> | null = null,
): ConnectedPoReadyProduct[] {
  const exposureBySupplierProduct = new Map(
    (exposures ?? [])
      .filter((x) => x.isExposed && x.isOrderable)
      .map((x) => [x.productId, x] as const),
  );
  const requireExposure = exposures != null && exposures.length > 0;

  const ready: ConnectedPoReadyProduct[] = [];
  for (const link of links) {
    if (!link.isActive || link.buyerProductId === "" || link.lastKnownOrderPrice <= 0) {
      continue;
    }
    const exposure = exposureBySupplierProduct.get(link.supplierProductId);
    if (requireExposure && !exposure) {
      continue;
    }
    const unitPurchaseCost =
      exposure?.effectiveSupplierOrderPrice ??
      exposure?.supplierOrderPrice ??
      link.lastKnownOrderPrice;
    if (unitPurchaseCost <= 0) {
      continue;
    }
    const categoryRaw = exposure?.categoryNameSnapshot?.trim() || null;
    const multiplier =
      typeof link.multiplierToBase === "number" && link.multiplierToBase > 0
        ? link.multiplierToBase
        : 1;
    ready.push({
      linkId: link.linkId,
      buyerProductId: link.buyerProductId,
      supplierProductId: link.supplierProductId,
      productName: link.supplierNameSnapshot,
      unitOfMeasure: link.unitOfMeasureCode,
      supplierSku: link.supplierSkuSnapshot ?? null,
      unitPurchaseCost,
      purchaseUnitId: link.buyerPurchaseUnitId ?? null,
      packageLabel: link.packageLabel ?? null,
      multiplierToBase: multiplier,
      stockTracked: null,
      availableBaseQuantity: null,
      categoryName: categoryRaw,
    });
  }

  return ready.sort((a, b) =>
    a.productName.localeCompare(b.productName, undefined, { sensitivity: "base" }),
  );
}

export function resolveConnectedCategoryKey(categoryName: string | null | undefined): string {
  const trimmed = categoryName?.trim() ?? "";
  return trimmed.length > 0 ? trimmed : CONNECTED_PO_CATEGORY_OTHER;
}

/** Category chips for orderable linked products (All + named + Other). Counts are of the given product set. */
export function buildConnectedCategoryFacets(
  products: ReadonlyArray<ConnectedPoReadyProduct>,
  labels: { all: string; other: string },
): ConnectedPoCategoryFacet[] {
  const named = new Map<string, { label: string; count: number }>();
  let otherCount = 0;
  for (const product of products) {
    const key = resolveConnectedCategoryKey(product.categoryName);
    if (key === CONNECTED_PO_CATEGORY_OTHER) {
      otherCount += 1;
      continue;
    }
    const existing = named.get(key.toLowerCase());
    if (existing) {
      existing.count += 1;
    } else {
      named.set(key.toLowerCase(), { label: key, count: 1 });
    }
  }

  const facets: ConnectedPoCategoryFacet[] = [
    { key: CONNECTED_PO_CATEGORY_ALL, label: labels.all, count: products.length },
  ];
  const sortedNamed = [...named.values()].sort((a, b) =>
    a.label.localeCompare(b.label, undefined, { sensitivity: "base" }),
  );
  for (const entry of sortedNamed) {
    facets.push({ key: entry.label, label: entry.label, count: entry.count });
  }
  if (otherCount > 0) {
    facets.push({
      key: CONNECTED_PO_CATEGORY_OTHER,
      label: labels.other,
      count: otherCount,
    });
  }
  return facets;
}

export function filterConnectedReadyProducts(
  products: ReadonlyArray<ConnectedPoReadyProduct>,
  searchText: string,
  categoryKey: string = CONNECTED_PO_CATEGORY_ALL,
): ConnectedPoReadyProduct[] {
  const query = searchText.trim().toLowerCase();
  const category = categoryKey.trim() || CONNECTED_PO_CATEGORY_ALL;
  return products.filter((product) => {
    if (category !== CONNECTED_PO_CATEGORY_ALL) {
      const productKey = resolveConnectedCategoryKey(product.categoryName);
      if (category === CONNECTED_PO_CATEGORY_OTHER) {
        if (productKey !== CONNECTED_PO_CATEGORY_OTHER) {
          return false;
        }
      } else if (productKey.toLowerCase() !== category.toLowerCase()) {
        return false;
      }
    }
    if (!query) {
      return true;
    }
    const tokens = [
      product.productName,
      product.supplierSku ?? "",
      product.unitOfMeasure,
      product.packageLabel ?? "",
      product.categoryName ?? "",
    ];
    return tokens.some((token) => token.toLowerCase().includes(query));
  });
}

export function applyConnectedQuantityDelta(
  lines: ReadonlyArray<ConnectedPoDraftLine>,
  product: ConnectedPoReadyProduct,
  delta: number,
): ConnectedPoDraftLine[] {
  if (delta === 0) {
    return [...lines];
  }
  const maxQty = maxOrderablePurchaseQty(product);
  const index = lines.findIndex((line) => line.productId === product.buyerProductId);
  if (index < 0) {
    if (delta <= 0) {
      return [...lines];
    }
    if (maxQty != null && maxQty <= 0) {
      return [...lines];
    }
    const initialQty = maxQty == null ? 1 : Math.min(1, maxQty);
    if (initialQty <= 0) {
      return [...lines];
    }
    return [
      ...lines,
      {
        productId: product.buyerProductId,
        name: product.productName,
        uom: product.unitOfMeasure,
        orderedQty: initialQty,
        unitPurchaseCost: product.unitPurchaseCost,
        purchaseUnitId: product.purchaseUnitId,
      },
    ];
  }

  let nextQty = lines[index]!.orderedQty + delta;
  if (maxQty != null && nextQty > maxQty) {
    nextQty = maxQty;
  }
  if (nextQty <= 0) {
    return lines.filter((line) => line.productId !== product.buyerProductId);
  }
  return lines.map((line, i) => (i === index ? { ...line, orderedQty: nextQty } : line));
}

/** Max orderable qty in purchase units for tracked stock; null when untracked/unknown. */
export function maxOrderablePurchaseQty(product: ConnectedPoReadyProduct): number | null {
  if (product.stockTracked !== true || product.availableBaseQuantity == null) {
    return null;
  }
  const multiplier = product.multiplierToBase > 0 ? product.multiplierToBase : 1;
  const raw = product.availableBaseQuantity / multiplier;
  // Floor to avoid ordering a fractional package that exceeds base stock.
  return Math.floor(raw * 1_000_000) / 1_000_000;
}

export type SupplierAvailabilityState =
  | { kind: "unknown" }
  | { kind: "untracked" }
  | { kind: "out_of_stock" }
  | { kind: "available"; quantity: number };

export function resolveSupplierAvailability(product: ConnectedPoReadyProduct): SupplierAvailabilityState {
  if (product.stockTracked == null) {
    return { kind: "unknown" };
  }
  if (product.stockTracked === false) {
    return { kind: "untracked" };
  }
  const max = maxOrderablePurchaseQty(product) ?? 0;
  if (max <= 0) {
    return { kind: "out_of_stock" };
  }
  return { kind: "available", quantity: max };
}

export function mergeConnectedStock(
  products: ReadonlyArray<ConnectedPoReadyProduct>,
  stockBySupplierProductId: ReadonlyMap<string, { isTracked: boolean; availableBaseQuantity: number }>,
): ConnectedPoReadyProduct[] {
  return products.map((product) => {
    const stock = stockBySupplierProductId.get(product.supplierProductId);
    if (!stock) {
      return {
        ...product,
        stockTracked: false,
        availableBaseQuantity: null,
      };
    }
    return {
      ...product,
      stockTracked: stock.isTracked,
      availableBaseQuantity: stock.isTracked ? stock.availableBaseQuantity : null,
    };
  });
}

export function connectedLinesViolateStock(
  lines: ReadonlyArray<ConnectedPoDraftLine>,
  products: ReadonlyArray<ConnectedPoReadyProduct>,
): boolean {
  const byBuyer = new Map(products.map((p) => [p.buyerProductId, p] as const));
  for (const line of lines) {
    const product = byBuyer.get(line.productId);
    if (!product) {
      continue;
    }
    const max = maxOrderablePurchaseQty(product);
    if (max != null && line.orderedQty > max) {
      return true;
    }
  }
  return false;
}

export function retainCompatibleDraftLines(
  lines: ReadonlyArray<ConnectedPoDraftLine>,
  readyProducts: ReadonlyArray<ConnectedPoReadyProduct>,
): ConnectedPoDraftLine[] {
  const allowed = new Set(readyProducts.map((p) => p.buyerProductId));
  return lines.filter((line) => allowed.has(line.productId));
}

export function formatUnitPriceLabel(unitPurchaseCost: number, unitOfMeasure: string): string {
  const unit = unitOfMeasure.trim() || "pc";
  return `${formatCompactPeso(unitPurchaseCost)} / ${unit}`;
}

export function formatLineMath(orderedQty: number, unitPurchaseCost: number): string {
  return `${formatCompactPeso(lineTotal(orderedQty, unitPurchaseCost))} · ${orderedQty} × ${formatCompactPeso(unitPurchaseCost)}`;
}

function formatCompactPeso(amount: number): string {
  return `₱${amount.toLocaleString("en-PH", {
    minimumFractionDigits: amount % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  })}`;
}

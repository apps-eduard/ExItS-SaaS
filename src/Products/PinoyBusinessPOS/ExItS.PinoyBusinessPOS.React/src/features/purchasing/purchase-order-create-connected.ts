import type {
  BuyerSupplierProductLink,
  SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";
import type { ExitsSelectOption } from "@/components/exits/ExitsSelect";

/**
 * UI-only sentinel for the “No category” multi-select option.
 * Never persisted to product / category master data.
 */
export const PO_CATEGORY_NONE_OPTION = "__po_no_category__";

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
  /**
   * Stable category filter id when known (prefer CategoryId).
   * Until exposure DTO carries CategoryId, uses the trimmed category name.
   */
  categoryId: string | null;
  /** Display name; null → eligible for “No category”. */
  categoryName: string | null;
};

export type ConnectedPoCategoryFilter = {
  /** Real category filter ids (never the No-category sentinel). */
  selectedCategoryIds: ReadonlyArray<string>;
  includeNoCategory: boolean;
};

export type ConnectedPoDraftLine = {
  productId: string;
  name: string;
  uom: string;
  orderedQty: number;
  unitPurchaseCost: number;
  purchaseUnitId?: string | null;
};

export function emptyConnectedCategoryFilter(): ConnectedPoCategoryFilter {
  return { selectedCategoryIds: [], includeNoCategory: false };
}

export function isConnectedCategoryFilterActive(filter: ConnectedPoCategoryFilter): boolean {
  return filter.selectedCategoryIds.length > 0 || filter.includeNoCategory;
}

/** Map multi-select values ↔ structured filter (No category kept separate). */
export function connectedCategoryFilterFromValues(
  values: ReadonlyArray<string>,
): ConnectedPoCategoryFilter {
  const includeNoCategory = values.includes(PO_CATEGORY_NONE_OPTION);
  const selectedCategoryIds = values.filter((value) => value !== PO_CATEGORY_NONE_OPTION);
  return { selectedCategoryIds, includeNoCategory };
}

export function connectedCategoryFilterToValues(
  filter: ConnectedPoCategoryFilter,
): string[] {
  const values = [...filter.selectedCategoryIds];
  if (filter.includeNoCategory) {
    values.push(PO_CATEGORY_NONE_OPTION);
  }
  return values;
}

export function resolveConnectedCategoryId(
  categoryId: string | null | undefined,
  categoryName: string | null | undefined,
): string | null {
  const id = categoryId?.trim() ?? "";
  if (id.length > 0) {
    return id;
  }
  const name = categoryName?.trim() ?? "";
  return name.length > 0 ? name : null;
}

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
      categoryId: resolveConnectedCategoryId(null, categoryRaw),
      categoryName: categoryRaw,
    });
  }

  return ready.sort((a, b) =>
    a.productName.localeCompare(b.productName, undefined, { sensitivity: "base" }),
  );
}

export function resolveConnectedCategoryKey(categoryName: string | null | undefined): string | null {
  return resolveConnectedCategoryId(null, categoryName);
}

type CategoryCountable = {
  categoryId?: string | null;
  categoryName?: string | null;
};

/** Multi-select options for the current readiness product set (no “All” option). */
export function buildConnectedCategoryOptions(
  products: ReadonlyArray<CategoryCountable>,
  labels: { noCategory: string },
): ExitsSelectOption[] {
  const named = new Map<string, { id: string; label: string; count: number }>();
  let noCategoryCount = 0;
  for (const product of products) {
    const id = resolveConnectedCategoryId(product.categoryId, product.categoryName);
    if (id == null) {
      noCategoryCount += 1;
      continue;
    }
    const label = product.categoryName?.trim() || id;
    const existing = named.get(id.toLowerCase());
    if (existing) {
      existing.count += 1;
    } else {
      named.set(id.toLowerCase(), { id, label, count: 1 });
    }
  }

  const options: ExitsSelectOption[] = [...named.values()]
    .sort((a, b) => a.label.localeCompare(b.label, undefined, { sensitivity: "base" }))
    .map((entry) => ({
      value: entry.id,
      label: entry.label,
      count: entry.count,
    }));

  if (noCategoryCount > 0) {
    options.push({
      value: PO_CATEGORY_NONE_OPTION,
      label: labels.noCategory,
      count: noCategoryCount,
    });
  }
  return options;
}

export function filterConnectedReadyProducts(
  products: ReadonlyArray<ConnectedPoReadyProduct>,
  searchText: string,
  categoryFilter: ConnectedPoCategoryFilter = emptyConnectedCategoryFilter(),
): ConnectedPoReadyProduct[] {
  const query = searchText.trim().toLowerCase();
  const categoryActive = isConnectedCategoryFilterActive(categoryFilter);
  const selected = new Set(
    categoryFilter.selectedCategoryIds.map((id) => id.trim().toLowerCase()).filter(Boolean),
  );

  return products.filter((product) => {
    if (categoryActive) {
      const productId = resolveConnectedCategoryId(product.categoryId, product.categoryName);
      const matchesNamed =
        productId != null && selected.has(productId.toLowerCase());
      const matchesNone = categoryFilter.includeNoCategory && productId == null;
      if (!matchesNamed && !matchesNone) {
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

/** Filter setup/readiness rows when category metadata is available via map. */
export function filterItemsByConnectedCategory<T>(
  items: ReadonlyArray<T>,
  categoryFilter: ConnectedPoCategoryFilter,
  resolveCategory: (item: T) => { categoryId: string | null; categoryName: string | null },
): T[] {
  if (!isConnectedCategoryFilterActive(categoryFilter)) {
    return [...items];
  }
  const selected = new Set(
    categoryFilter.selectedCategoryIds.map((id) => id.trim().toLowerCase()).filter(Boolean),
  );
  return items.filter((item) => {
    const meta = resolveCategory(item);
    const productId = resolveConnectedCategoryId(meta.categoryId, meta.categoryName);
    const matchesNamed = productId != null && selected.has(productId.toLowerCase());
    const matchesNone = categoryFilter.includeNoCategory && productId == null;
    return matchesNamed || matchesNone;
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
  const index = lines.findIndex((line) => line.productId === product.buyerProductId);
  if (index < 0) {
    if (delta <= 0) {
      return [...lines];
    }
    return [
      ...lines,
      {
        productId: product.buyerProductId,
        name: product.productName,
        uom: product.unitOfMeasure,
        orderedQty: delta,
        unitPurchaseCost: product.unitPurchaseCost,
        purchaseUnitId: product.purchaseUnitId,
      },
    ];
  }

  const nextQty = lines[index]!.orderedQty + delta;
  if (nextQty <= 0) {
    return lines.filter((line) => line.productId !== product.buyerProductId);
  }
  return lines.map((line, i) => (i === index ? { ...line, orderedQty: nextQty } : line));
}

/**
 * Current supplier available qty in purchase units (informational).
 * Not a hard MaxOrderQuantity — buyers may request more.
 */
export function availablePurchaseQty(product: ConnectedPoReadyProduct): number | null {
  if (product.stockTracked !== true || product.availableBaseQuantity == null) {
    return null;
  }
  const multiplier = product.multiplierToBase > 0 ? product.multiplierToBase : 1;
  const raw = product.availableBaseQuantity / multiplier;
  return Math.floor(raw * 1_000_000) / 1_000_000;
}

/** @deprecated Prefer availablePurchaseQty — stock is informational, not a max. */
export function maxOrderablePurchaseQty(product: ConnectedPoReadyProduct): number | null {
  return availablePurchaseQty(product);
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
  const qty = availablePurchaseQty(product) ?? 0;
  if (qty <= 0) {
    return { kind: "out_of_stock" };
  }
  return { kind: "available", quantity: qty };
}

function formatAvailabilityQty(quantity: number): string {
  if (!Number.isFinite(quantity)) {
    return "0";
  }
  if (Math.abs(quantity - Math.trunc(quantity)) < 1e-9) {
    return String(Math.trunc(quantity));
  }
  return String(Math.round(quantity * 1_000_000) / 1_000_000);
}

/** Finder/stock column label: "Available now: {qty} {unit}" or "Out of stock". */
export function formatSupplierAvailabilityLabel(
  product: ConnectedPoReadyProduct,
  t: (key: string) => string,
): string {
  const availability = resolveSupplierAvailability(product);
  if (availability.kind === "out_of_stock") {
    return t("purchasing.supplierOutOfStock");
  }
  if (availability.kind === "available") {
    const unit = formatUnitOfMeasureLabel(product.packageLabel || product.unitOfMeasure || "");
    return t("purchasing.supplierAvailableNow")
      .replace("{qty}", formatAvailabilityQty(availability.quantity))
      .replace("{unit}", unit);
  }
  return t("purchasing.stockNotTracked");
}

/** True when requested qty exceeds current tracked supplier availability (warning only). */
export function requestedExceedsSupplierAvailability(
  product: ConnectedPoReadyProduct,
  orderedQty: number,
): boolean {
  if (!(orderedQty > 0)) {
    return false;
  }
  const availability = resolveSupplierAvailability(product);
  if (availability.kind === "out_of_stock") {
    return true;
  }
  if (availability.kind === "available") {
    return orderedQty > availability.quantity;
  }
  return false;
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

/** @deprecated Over-order is allowed; use requestedExceedsSupplierAvailability for warnings. */
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
    if (requestedExceedsSupplierAvailability(product, line.orderedQty)) {
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

export function formatUnitOfMeasureLabel(unitOfMeasure: string): string {
  const unit = unitOfMeasure.trim();
  if (!unit) {
    return "pc";
  }
  switch (unit.toLowerCase()) {
    case "kilogram":
    case "kilograms":
    case "kg":
      return "Kg";
    case "gram":
    case "grams":
      return "g";
    case "liter":
    case "litre":
    case "liters":
    case "litres":
      return "L";
    case "milliliter":
    case "millilitre":
    case "milliliters":
    case "millilitres":
    case "ml":
      return "mL";
    case "piece":
    case "pieces":
    case "pc":
    case "pcs":
      return "pc";
    default:
      return unit;
  }
}

export function formatUnitPriceLabel(unitPurchaseCost: number, unitOfMeasure: string): string {
  const unit = formatUnitOfMeasureLabel(unitOfMeasure);
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

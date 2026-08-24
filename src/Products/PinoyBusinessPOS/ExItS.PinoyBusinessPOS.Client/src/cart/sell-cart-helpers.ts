import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";

/** Client preview money rounding — server still prices the sale. */
export function roundMoney(amount: number): number {
  const sign = amount < 0 ? -1 : 1;
  return (sign * Math.round(Math.abs(amount) * 100)) / 100;
}

export function roundQuantity(quantity: number): number {
  return Math.round(quantity * 1000) / 1000;
}

export function isByWeightSellingMode(sellingMode: string | null | undefined): boolean {
  return (sellingMode ?? "").trim().toLowerCase() === "byweight";
}

export function cartLineKey(productId: string, productUnitId: string | null): string {
  return `${productId}::${productUnitId ?? "base"}`;
}

export function activeSellUnits(product: PosCatalogProductDto): PosCatalogProductUnitDto[] {
  return (product.units ?? [])
    .filter((unit) => unit.isActive && unit.kind.trim().toLowerCase() === "sell")
    .slice()
    .sort((a, b) => a.sortOrder - b.sortOrder || a.displayName.localeCompare(b.displayName));
}

export function resolveSellUnitPrice(
  product: PosCatalogProductDto,
  unit: PosCatalogProductUnitDto | null | undefined,
): number {
  if (unit?.sellingPrice != null && Number.isFinite(unit.sellingPrice)) {
    return unit.sellingPrice;
  }
  return product.sellingPrice;
}

/** Whole entered quantities when the sell unit does not allow measured/custom qty. */
export function requiresWholeEnteredQuantity(unit: PosCatalogProductUnitDto): boolean {
  return !unit.allowsCustomQuantity;
}

/**
 * Locked sell-floor add decision tree:
 * ByWeight → weight
 * else 1 sell unit + custom → generic custom qty (not weight)
 * else 1 sell unit + !custom → direct add with that unit identity
 * else >1 → unit selector
 * else → base fallback
 */
export type SellAddFlow =
  | { kind: "weight"; unit: PosCatalogProductUnitDto | null }
  | { kind: "customQuantity"; unit: PosCatalogProductUnitDto }
  | { kind: "direct"; unit: PosCatalogProductUnitDto }
  | { kind: "unitSelector"; units: PosCatalogProductUnitDto[] }
  | { kind: "base" };

export function resolveAddFlow(product: PosCatalogProductDto): SellAddFlow {
  if (isByWeightSellingMode(product.sellingMode)) {
    const sellUnits = activeSellUnits(product);
    return { kind: "weight", unit: sellUnits.length === 1 ? sellUnits[0]! : null };
  }

  const sellUnits = activeSellUnits(product);
  if (sellUnits.length === 1) {
    const unit = sellUnits[0]!;
    if (unit.allowsCustomQuantity) {
      return { kind: "customQuantity", unit };
    }
    return { kind: "direct", unit };
  }

  if (sellUnits.length > 1) {
    return { kind: "unitSelector", units: sellUnits };
  }

  return { kind: "base" };
}

/** @deprecated Prefer resolveAddFlow — kept for call sites that only need a dialog cue. */
export function needsSellUnitOrWeightDialog(product: PosCatalogProductDto): boolean {
  const flow = resolveAddFlow(product);
  return flow.kind === "weight" || flow.kind === "customQuantity" || flow.kind === "unitSelector";
}

export type WeightInputUnit = "kg" | "g";

/**
 * Normalize cashier weight input to canonical kilograms (3 dp).
 * Grams must be whole numbers; kilogram input may have at most 3 decimals (not rounded up).
 */
export function normalizeWeightToKilograms(
  rawValue: number,
  unit: WeightInputUnit,
): { kilograms: number } | { error: "zero" | "unit" | "precision" | "invalid" } {
  if (!Number.isFinite(rawValue) || rawValue <= 0) {
    return { error: "zero" };
  }

  if (unit === "g") {
    if (!Number.isInteger(rawValue)) {
      return { error: "precision" };
    }
    return { kilograms: roundQuantity(rawValue / 1000) };
  }

  if (unit !== "kg") {
    return { error: "unit" };
  }

  const scaled = rawValue * 1000;
  if (Math.abs(scaled - Math.round(scaled)) > 1e-9) {
    return { error: "precision" };
  }

  return { kilograms: roundQuantity(rawValue) };
}

/**
 * Normalize measured custom quantity (Liter, Meter, etc.) — at most 3 decimal places, no kg conversion.
 */
export function normalizeCustomQuantity(
  rawValue: number,
): { quantity: number } | { error: "zero" | "precision" | "invalid" } {
  if (!Number.isFinite(rawValue) || rawValue <= 0) {
    return { error: "zero" };
  }

  const scaled = rawValue * 1000;
  if (Math.abs(scaled - Math.round(scaled)) > 1e-9) {
    return { error: "precision" };
  }

  return { quantity: roundQuantity(rawValue) };
}

export function formatQuantityDisplay(value: number): string {
  if (!Number.isFinite(value)) {
    return "0";
  }
  return String(Number(value.toFixed(3)));
}

export function formatLineAmountPreview(
  quantity: number,
  unitPrice: number,
  unitLabel: string,
): string {
  const amount = roundMoney(quantity * unitPrice);
  return `${formatQuantityDisplay(quantity)} ${unitLabel} × ₱${unitPrice.toFixed(2)}/${unitLabel} = ₱${amount.toFixed(2)}`;
}

export type StockHint = {
  tracked: boolean;
  quantity: number;
  unitOfMeasure: string;
  label: "onHand" | "sellable";
};

/**
 * Stock availability for tracked products.
 * Prefer sellableQuantity for expiration-tracked products when present.
 * Server remains authority at checkout (EnsureAvailableForSaleAsync).
 */
export function resolveStockHint(input: {
  isTracked?: boolean | null;
  onHandQuantity?: number | null;
  unitOfMeasure: string;
  tracksExpiration?: boolean | null;
  sellableQuantity?: number | null;
}): StockHint | null {
  if (!input.isTracked) {
    return null;
  }

  if (
    input.tracksExpiration &&
    input.sellableQuantity != null &&
    Number.isFinite(input.sellableQuantity)
  ) {
    return {
      tracked: true,
      quantity: input.sellableQuantity,
      unitOfMeasure: input.unitOfMeasure,
      label: "sellable",
    };
  }

  const onHand = input.onHandQuantity ?? 0;
  return {
    tracked: true,
    quantity: onHand,
    unitOfMeasure: input.unitOfMeasure,
    label: "onHand",
  };
}

export type SellCardStockTone = "ok" | "low" | "out" | "untracked";

export type SellCardStock = {
  tone: SellCardStockTone;
  quantity: number;
  unitOfMeasure: string;
  quantityLabel: "onHand" | "sellable" | null;
};

function normalizeStockStatus(status?: string | null): "in" | "low" | "out" | null {
  const code = (status ?? "").replace(/[\s_-]/g, "").toLowerCase();
  if (code === "outofstock") {
    return "out";
  }
  if (code === "lowstock") {
    return "low";
  }
  if (code === "instock") {
    return "in";
  }
  return null;
}

/** Committed on-hand minus this register's cart. Does not reserve stock for other registers. */
export function remainingQuantityAfterCart(
  committedQuantity: number | null | undefined,
  cartBaseQuantity: number,
): number {
  const committed = Number.isFinite(committedQuantity) ? Number(committedQuantity) : 0;
  const reserved = Number.isFinite(cartBaseQuantity) ? Math.max(0, cartBaseQuantity) : 0;
  return roundQuantity(Math.max(0, committed - reserved));
}

/** Catalog/committed out of stock — hide from browse unless the cashier asks to see them. */
export function isCommittedOutOfStock(input: {
  isTracked?: boolean | null;
  onHandQuantity?: number | null;
  stockStatus?: string | null;
}): boolean {
  if (!input.isTracked) {
    return false;
  }
  if (normalizeStockStatus(input.stockStatus) === "out") {
    return true;
  }
  return (input.onHandQuantity ?? 0) <= 0;
}

/** Tile/dialog stock line: untracked, on-hand, plus low/out when the catalog says so. */
export function resolveSellCardStock(input: {
  isTracked?: boolean | null;
  onHandQuantity?: number | null;
  unitOfMeasure: string;
  tracksExpiration?: boolean | null;
  sellableQuantity?: number | null;
  stockStatus?: string | null;
  isLowStock?: boolean | null;
}): SellCardStock {
  if (!input.isTracked) {
    return {
      tone: "untracked",
      quantity: 0,
      unitOfMeasure: input.unitOfMeasure,
      quantityLabel: null,
    };
  }

  const hint = resolveStockHint(input)!;
  const status = normalizeStockStatus(input.stockStatus);
  let tone: SellCardStockTone = "ok";
  if (hint.quantity <= 0 || status === "out") {
    tone = "out";
  } else if (status === "low" || input.isLowStock === true) {
    tone = "low";
  }

  return {
    tone,
    quantity: hint.quantity,
    unitOfMeasure: hint.unitOfMeasure,
    quantityLabel: hint.label,
  };
}

/** Cart line quantity converted to product base UOM (kg for ByWeight). */
export function lineBaseQuantity(line: {
  quantity: number;
  multiplierToBase: number;
}): number {
  const multiplier =
    Number.isFinite(line.multiplierToBase) && line.multiplierToBase > 0
      ? line.multiplierToBase
      : 1;
  return roundQuantity(line.quantity * multiplier);
}

/** Sum of base quantities already in cart for one product (optionally excluding a line). */
export function sumCartBaseQuantityForProduct(
  lines: ReadonlyArray<{
    productId: string;
    lineKey: string;
    quantity: number;
    multiplierToBase: number;
  }>,
  productId: string,
  excludeLineKey?: string | null,
): number {
  let total = 0;
  for (const line of lines) {
    if (line.productId !== productId) {
      continue;
    }
    if (excludeLineKey != null && line.lineKey === excludeLineKey) {
      continue;
    }
    total = roundQuantity(total + lineBaseQuantity(line));
  }
  return total;
}

export type StockGuardInput = {
  isTracked?: boolean | null;
  onHandQuantity?: number | null;
  unitOfMeasure: string;
  tracksExpiration?: boolean | null;
  sellableQuantity?: number | null;
  sellingMode?: string | null;
};

export type StockGuardResult =
  | { ok: true }
  | {
      ok: false;
      available: number;
      unitOfMeasure: string;
      /** True when message should include the UOM (weighted / measured). */
      includeUnit: boolean;
    };

/**
 * Blocks overselling when the product is inventory-tracked.
 * Compares requested + other cart base qty against authoritative available stock.
 * No org setting currently allows negative stock — default is BLOCK.
 */
export function evaluateStockGuard(input: {
  stock: StockGuardInput;
  /** Quantity being added or set, in the sell-unit / entered UOM (kg for ByWeight). */
  requestedQuantity: number;
  multiplierToBase?: number;
  /** Base qty already in cart for this product (excluding the line being replaced). */
  otherCartBaseQuantity?: number;
}): StockGuardResult {
  const hint = resolveStockHint({
    isTracked: input.stock.isTracked,
    onHandQuantity: input.stock.onHandQuantity,
    unitOfMeasure: input.stock.unitOfMeasure,
    tracksExpiration: input.stock.tracksExpiration,
    sellableQuantity: input.stock.sellableQuantity,
  });

  if (!hint) {
    return { ok: true };
  }

  const multiplier =
    input.multiplierToBase != null &&
    Number.isFinite(input.multiplierToBase) &&
    input.multiplierToBase > 0
      ? input.multiplierToBase
      : 1;
  const requestedBase = roundQuantity(input.requestedQuantity * multiplier);
  const other = roundQuantity(input.otherCartBaseQuantity ?? 0);
  const total = roundQuantity(other + requestedBase);

  if (total <= hint.quantity + 1e-9) {
    return { ok: true };
  }

  const byWeight = isByWeightSellingMode(input.stock.sellingMode);

  return {
    ok: false,
    available: hint.quantity,
    unitOfMeasure: byWeight ? "kg" : hint.unitOfMeasure,
    includeUnit: byWeight,
  };
}

/** Message for blocked adds — matches cashier-facing stock copy. */
export function formatStockUnavailableMessage(result: Extract<StockGuardResult, { ok: false }>): string {
  if (result.includeUnit) {
    const unit =
      result.unitOfMeasure.trim().toLowerCase() === "kilogram" ? "kg" : result.unitOfMeasure;
    return `Only ${result.available.toFixed(2)} ${unit} available.`;
  }
  return `Only ${formatQuantityDisplay(result.available)} available.`;
}

export type CartLineStockIssue = {
  lineKey: string;
  productId: string;
  name: string;
  available: number;
  unitOfMeasure: string;
  includeUnit: boolean;
  message: string;
};

/**
 * Revalidate cart lines against current stock snapshots.
 * Groups by productId so multi-unit lines share one availability pool.
 */
export function findCartStockIssues(
  lines: ReadonlyArray<{
    lineKey: string;
    productId: string;
    name: string;
    quantity: number;
    multiplierToBase: number;
    sellingMode: string;
  }>,
  stockByProductId: ReadonlyMap<string, StockGuardInput>,
): CartLineStockIssue[] {
  const issues: CartLineStockIssue[] = [];
  const seenProducts = new Set<string>();

  for (const line of lines) {
    if (seenProducts.has(line.productId)) {
      continue;
    }
    seenProducts.add(line.productId);

    const stock = stockByProductId.get(line.productId);
    if (!stock) {
      continue;
    }

    const totalBase = sumCartBaseQuantityForProduct(lines, line.productId);
    const check = evaluateStockGuard({
      stock: { ...stock, sellingMode: stock.sellingMode ?? line.sellingMode },
      requestedQuantity: totalBase,
      multiplierToBase: 1,
      otherCartBaseQuantity: 0,
    });

    if (check.ok) {
      continue;
    }

    const productLines = lines.filter((item) => item.productId === line.productId);
    const unit =
      check.unitOfMeasure.trim().toLowerCase() === "kilogram" ? "kg" : check.unitOfMeasure;
    const availableLabel = check.includeUnit
      ? `${check.available.toFixed(2)} ${unit}`
      : formatQuantityDisplay(check.available);

    for (const productLine of productLines) {
      issues.push({
        lineKey: productLine.lineKey,
        productId: productLine.productId,
        name: productLine.name,
        available: check.available,
        unitOfMeasure: check.unitOfMeasure,
        includeUnit: check.includeUnit,
        message: `${productLine.name} exceeds available stock. Available: ${availableLabel}.`,
      });
    }
  }

  return issues;
}

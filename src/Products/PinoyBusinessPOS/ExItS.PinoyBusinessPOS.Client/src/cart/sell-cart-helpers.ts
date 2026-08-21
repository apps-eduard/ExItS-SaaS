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

/** Pack-style units (multiplier ≠ 1, not custom) default to whole entered quantities. */
export function requiresWholeEnteredQuantity(unit: PosCatalogProductUnitDto): boolean {
  return !unit.allowsCustomQuantity && unit.multiplierToBase !== 1;
}

export function needsSellUnitOrWeightDialog(product: PosCatalogProductDto): boolean {
  const sellUnits = activeSellUnits(product);
  if (
    sellUnits.length > 1 ||
    (sellUnits.length === 1 &&
      (sellUnits[0]!.multiplierToBase !== 1 || sellUnits[0]!.allowsCustomQuantity))
  ) {
    return true;
  }
  return isByWeightSellingMode(product.sellingMode);
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
 * Advisory stock hint only — server remains authority at checkout.
 * Prefer sellableQuantity for expiration-tracked products when present.
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

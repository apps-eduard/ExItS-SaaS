import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import {
  formatQuantityDisplay,
  isByWeightSellingMode,
  normalizeWeightToKilograms,
  requiresWholeEnteredQuantity,
  roundQuantity,
  type WeightInputUnit,
} from "@/cart/sell-cart-helpers";

export type ProductionMaterialEntryMode = "weight" | "unit" | "base";

export type ProductionMaterialDraft = {
  materialProductId: string;
  name: string;
  /** Quantity to send to the API (entered qty for unit mode; base kg for weight). */
  quantity: number;
  /** Label shown next to quantity (kg, g, pcs, Pack, …). */
  displayUom: string;
  productUnitId?: string | null;
  /** When weight mode, which input unit the operator last used. */
  weightInputUnit?: WeightInputUnit | null;
};

export function isEligibleProductionMaterial(product: PosCatalogProductDto): boolean {
  return product.canBeUsedAsIngredient === true && product.isTracked === true;
}

export function isWeightMaterial(product: PosCatalogProductDto): boolean {
  if (isByWeightSellingMode(product.sellingMode)) {
    return true;
  }
  const uom = product.unitOfMeasure?.trim().toLowerCase() ?? "";
  return uom === "kilogram" || uom === "kg" || uom === "g" || uom === "gram" || uom === "grams";
}

export function activeMaterialUnits(product: PosCatalogProductDto): PosCatalogProductUnitDto[] {
  return (product.units ?? []).filter((unit) => unit.isActive);
}

export function resolveMaterialEntryMode(product: PosCatalogProductDto): ProductionMaterialEntryMode {
  if (isWeightMaterial(product)) {
    return "weight";
  }
  if (activeMaterialUnits(product).length > 0) {
    return "unit";
  }
  return "base";
}

export function materialBaseUomLabel(product: PosCatalogProductDto): string {
  if (isWeightMaterial(product)) {
    return "kg";
  }
  const uom = product.unitOfMeasure?.trim();
  return uom && uom.length > 0 ? uom : "pcs";
}

/** Informational stock line for setup (does not mutate inventory). */
export function formatMaterialAvailableStock(product: PosCatalogProductDto): {
  qty: number;
  uom: string;
} | null {
  if (product.isTracked === false) {
    return null;
  }
  const qty =
    product.branchAvailableQuantity ??
    product.branchOnHandQuantity ??
    product.onHandQuantity ??
    null;
  if (qty == null || !Number.isFinite(qty)) {
    return null;
  }
  return { qty, uom: materialBaseUomLabel(product) };
}

export function formatMaterialAvailableCaption(
  product: PosCatalogProductDto,
  template: string,
): string | null {
  const stock = formatMaterialAvailableStock(product);
  if (!stock) {
    return null;
  }
  return template
    .replace("{qty}", formatQuantityDisplay(stock.qty))
    .replace("{uom}", stock.uom);
}

export type NormalizedMaterialQuantity =
  | {
      ok: true;
      quantity: number;
      displayUom: string;
      productUnitId?: string | null;
      weightInputUnit?: WeightInputUnit | null;
    }
  | { ok: false; error: "zero" | "precision" | "invalid" | "unit" };

export function normalizeMaterialQuantityInput(input: {
  product: PosCatalogProductDto;
  rawValue: number;
  weightUnit?: WeightInputUnit;
  productUnitId?: string | null;
}): NormalizedMaterialQuantity {
  const mode = resolveMaterialEntryMode(input.product);

  if (mode === "weight") {
    const unit = input.weightUnit ?? "kg";
    const parsed = normalizeWeightToKilograms(input.rawValue, unit);
    if ("error" in parsed) {
      return { ok: false, error: parsed.error };
    }
    return {
      ok: true,
      quantity: parsed.kilograms,
      displayUom: unit,
      productUnitId: null,
      weightInputUnit: unit,
    };
  }

  if (!Number.isFinite(input.rawValue) || input.rawValue <= 0) {
    return { ok: false, error: "zero" };
  }

  if (mode === "unit") {
    const units = activeMaterialUnits(input.product);
    const selected =
      units.find((unit) => unit.unitId === input.productUnitId) ?? units[0] ?? null;
    if (!selected) {
      return { ok: false, error: "unit" };
    }
    if (requiresWholeEnteredQuantity(selected) && !Number.isInteger(input.rawValue)) {
      return { ok: false, error: "precision" };
    }
    return {
      ok: true,
      quantity: requiresWholeEnteredQuantity(selected)
        ? Math.trunc(input.rawValue)
        : roundQuantity(input.rawValue),
      displayUom: selected.shortLabel || selected.displayName || materialBaseUomLabel(input.product),
      productUnitId: selected.unitId,
      weightInputUnit: null,
    };
  }

  const scaled = input.rawValue * 1000;
  if (Math.abs(scaled - Math.round(scaled)) > 1e-9) {
    return { ok: false, error: "precision" };
  }
  return {
    ok: true,
    quantity: roundQuantity(input.rawValue),
    displayUom: materialBaseUomLabel(input.product),
    productUnitId: null,
    weightInputUnit: null,
  };
}

/** Display quantity for an existing draft (weight may show grams). */
export function draftDisplayQuantity(draft: ProductionMaterialDraft): string {
  if (draft.weightInputUnit === "g") {
    return formatQuantityDisplay(roundQuantity(draft.quantity * 1000));
  }
  return formatQuantityDisplay(draft.quantity);
}

export function draftQuantityParts(draft: ProductionMaterialDraft): { qty: string; unit: string } {
  return { qty: draftDisplayQuantity(draft), unit: draft.displayUom };
}

export function draftQuantityLine(draft: ProductionMaterialDraft): string {
  return `${draftDisplayQuantity(draft)} ${draft.displayUom}`;
}

/** Compact availability label for recipe table cells (e.g. "9 kg"). */
export function formatMaterialAvailableCell(product: PosCatalogProductDto | null | undefined): string {
  if (!product) {
    return "—";
  }
  const stock = formatMaterialAvailableStock(product);
  if (!stock) {
    return "—";
  }
  return `${formatQuantityDisplay(stock.qty)} ${stock.uom}`;
}

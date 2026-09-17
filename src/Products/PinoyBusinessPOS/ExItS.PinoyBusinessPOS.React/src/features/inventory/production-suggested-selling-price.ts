/**
 * Suggested selling price from estimated production cost + target gross margin.
 *
 * Primary formula (gross margin, NOT markup):
 *   suggestedRaw = unitCost / (1 - targetMargin)
 *
 * Cost basis today is MATERIAL_ONLY. Future overhead/labor can replace unitCost
 * via {@link ProductionCostBasis} without changing the margin formula.
 */

export const DEFAULT_TARGET_GROSS_MARGIN = 0.3;
export const TARGET_GROSS_MARGIN_PRESETS = [0.2, 0.3, 0.4] as const;
export const LOW_GROSS_MARGIN_WARNING_THRESHOLD = 0.1;

/** Cost input for pricing suggestions — material-only today. */
export type ProductionCostBasis =
  | { kind: "material_only"; unitCost: number; complete: true }
  | { kind: "material_only"; unitCost: number | null; complete: false; reason: "unavailable" | "partial" };

export type SuggestedSellingPrice = {
  raw: number;
  rounded: number;
  targetMargin: number;
  unitCost: number;
};

function roundMoney(value: number): number {
  return Math.round(value * 100) / 100;
}

/**
 * Commercial PHP retail rounding — always round UP to a practical step.
 * Never rounds below the raw suggested price (preserves target margin floor).
 *
 * Steps (documented):
 * - < ₱20   → nearest ₱1
 * - < ₱500  → nearest ₱5
 * - ≥ ₱500  → nearest ₱10
 */
export function roundCommercialPhpSellingPrice(raw: number): number {
  if (!Number.isFinite(raw) || raw <= 0) {
    return 0;
  }
  const step = raw < 20 ? 1 : raw < 500 ? 5 : 10;
  const rounded = Math.ceil(raw / step - Number.EPSILON) * step;
  // Guard floating-point under-round; never go below raw.
  return roundMoney(Math.max(rounded, raw));
}

export function suggestSellingPriceFromUnitCost(
  unitCost: number,
  targetGrossMargin: number = DEFAULT_TARGET_GROSS_MARGIN,
): SuggestedSellingPrice | null {
  if (!Number.isFinite(unitCost) || unitCost < 0) {
    return null;
  }
  if (!Number.isFinite(targetGrossMargin) || targetGrossMargin <= 0 || targetGrossMargin >= 1) {
    return null;
  }
  const raw = roundMoney(unitCost / (1 - targetGrossMargin));
  const rounded = roundCommercialPhpSellingPrice(raw);
  return { raw, rounded, targetMargin: targetGrossMargin, unitCost };
}

export function resolveMaterialCostBasis(input: {
  unitCost: number | null;
  knownLineCount: number;
  missingLineCount: number;
}): ProductionCostBasis {
  if (input.unitCost == null || !Number.isFinite(input.unitCost) || input.knownLineCount === 0) {
    return { kind: "material_only", unitCost: null, complete: false, reason: "unavailable" };
  }
  if (input.missingLineCount > 0) {
    return {
      kind: "material_only",
      unitCost: input.unitCost,
      complete: false,
      reason: "partial",
    };
  }
  return { kind: "material_only", unitCost: input.unitCost, complete: true };
}

/** Gross margin % display: one decimal place (e.g. 30.8). */
export function formatGrossMarginPercent(percent: number): string {
  if (!Number.isFinite(percent)) {
    return "—";
  }
  return (Math.round(percent * 10) / 10).toFixed(1);
}

export function isSellingBelowCost(sellingPrice: number, unitCost: number): boolean {
  return Number.isFinite(sellingPrice) && Number.isFinite(unitCost) && sellingPrice < unitCost;
}

export function isLowGrossMargin(
  sellingPrice: number,
  unitCost: number,
  threshold: number = LOW_GROSS_MARGIN_WARNING_THRESHOLD,
): boolean {
  if (!Number.isFinite(sellingPrice) || sellingPrice <= 0 || !Number.isFinite(unitCost)) {
    return false;
  }
  if (sellingPrice < unitCost) {
    return false;
  }
  const margin = (sellingPrice - unitCost) / sellingPrice;
  return margin < threshold;
}

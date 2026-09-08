import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { listInventoryMovements } from "@/api/pos/pos-inventory-client";

export type RecipeMaterialCostLine = {
  materialProductId: string;
  name: string;
  /** Quantity in base inventory units. */
  baseQuantity: number;
  unitCost: number | null;
  lineCost: number | null;
};

export type RecipeMaterialCostEstimate = {
  lines: RecipeMaterialCostLine[];
  /** Sum of known line costs; null when no line has a cost. */
  batchCost: number | null;
  knownLineCount: number;
  missingLineCount: number;
};

/** Resolve latest known acquisition unit cost from recent stock movements. */
export async function resolveLatestAcquisitionUnitCost(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<number | null> {
  const page = await listInventoryMovements(
    workspace,
    productId,
    { page: 1, pageSize: 40 },
    signal,
  );
  for (const movement of page.items) {
    if (movement.unitCost != null && Number.isFinite(movement.unitCost) && movement.unitCost >= 0) {
      return movement.unitCost;
    }
  }
  return null;
}

export function buildRecipeMaterialCostEstimate(
  lines: Array<{
    materialProductId: string;
    name: string;
    baseQuantity: number;
    unitCost: number | null;
  }>,
): RecipeMaterialCostEstimate {
  const mapped: RecipeMaterialCostLine[] = lines.map((line) => {
    const unitCost =
      line.unitCost != null && Number.isFinite(line.unitCost) ? line.unitCost : null;
    const lineCost =
      unitCost != null && Number.isFinite(line.baseQuantity)
        ? roundMoney(unitCost * line.baseQuantity)
        : null;
    return {
      materialProductId: line.materialProductId,
      name: line.name,
      baseQuantity: line.baseQuantity,
      unitCost,
      lineCost,
    };
  });
  const known = mapped.filter((l) => l.lineCost != null);
  const batchCost =
    known.length === 0
      ? null
      : roundMoney(known.reduce((sum, l) => sum + (l.lineCost ?? 0), 0));
  return {
    lines: mapped,
    batchCost,
    knownLineCount: known.length,
    missingLineCount: mapped.length - known.length,
  };
}

export function estimatedUnitMaterialCost(
  batchCost: number | null,
  standardYieldBaseQty: number,
): number | null {
  if (batchCost == null || !Number.isFinite(standardYieldBaseQty) || standardYieldBaseQty <= 0) {
    return null;
  }
  return roundMoney(batchCost / standardYieldBaseQty);
}

export function estimatedMaterialMargin(
  sellingPrice: number | null,
  unitMaterialCost: number | null,
): { amount: number; percent: number } | null {
  if (
    sellingPrice == null ||
    !Number.isFinite(sellingPrice) ||
    unitMaterialCost == null ||
    !Number.isFinite(unitMaterialCost)
  ) {
    return null;
  }
  const amount = roundMoney(sellingPrice - unitMaterialCost);
  const percent =
    sellingPrice > 0 ? roundMoney((amount / sellingPrice) * 100) : amount === 0 ? 0 : 100;
  return { amount, percent };
}

function roundMoney(value: number): number {
  return Math.round(value * 100) / 100;
}

/** Convert entered recipe qty + optional product unit to base quantity for costing. */
export function toBaseQuantityForCost(
  quantityEntered: number,
  multiplierToBase: number | null | undefined,
): number {
  const mult =
    multiplierToBase != null && Number.isFinite(multiplierToBase) && multiplierToBase > 0
      ? multiplierToBase
      : 1;
  return quantityEntered * mult;
}

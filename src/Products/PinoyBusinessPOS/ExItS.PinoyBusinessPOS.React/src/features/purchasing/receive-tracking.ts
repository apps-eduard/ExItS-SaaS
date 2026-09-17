import { parseNonNegativeQty } from "@/features/purchasing/receive-math";
import { roundMoney } from "@/features/purchasing/receive-payment";

export type UntrackedReceiveLineSummary = {
  productId: string;
  name: string;
  uom: string;
  receivedQty: number;
  unitPurchaseCost: number;
  purchaseAmount: number;
};

/**
 * Lines with good received qty > 0 where inventory is not currently tracked.
 * `isInventoryTracked === false` only — undefined/true are treated as already tracked.
 */
export function selectUntrackedReceivingLines(
  lines: ReadonlyArray<{
    productId: string;
    name: string;
    uom: string;
    unitPurchaseCost: number;
    isInventoryTracked: boolean;
    goodText: string;
  }>,
): UntrackedReceiveLineSummary[] {
  const result: UntrackedReceiveLineSummary[] = [];
  for (const line of lines) {
    if (line.isInventoryTracked !== false) {
      continue;
    }
    const receivedQty = parseNonNegativeQty(line.goodText) ?? 0;
    if (receivedQty <= 0) {
      continue;
    }
    result.push({
      productId: line.productId,
      name: line.name,
      uom: line.uom,
      receivedQty,
      unitPurchaseCost: line.unitPurchaseCost,
      purchaseAmount: roundMoney(receivedQty * line.unitPurchaseCost),
    });
  }
  return result;
}

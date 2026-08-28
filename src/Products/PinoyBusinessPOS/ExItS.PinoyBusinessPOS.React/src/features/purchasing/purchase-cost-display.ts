import type { MessageKey } from "@/i18n/messages";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";

/** Friendly internal labels for stock movement types (not raw enums). */
export function inventoryMovementTypeLabelKey(movementType: string): MessageKey {
  switch (movementType) {
    case "OpeningStock":
      return "inventory.movementType.openingStock";
    case "DirectPurchaseReceipt":
      return "inventory.movementType.directBuy";
    case "PurchaseReceipt":
      return "inventory.movementType.poReceipt";
    case "ManualIncrease":
      return "inventory.movementType.manualIncrease";
    case "ManualDecrease":
      return "inventory.movementType.manualDecrease";
    case "SaleDeduction":
      return "inventory.movementType.sale";
    case "SaleVoidRestoration":
      return "inventory.movementType.saleVoid";
    case "SaleReturnRestock":
      return "inventory.movementType.customerReturn";
    case "TransferOut":
      return "inventory.movementType.transferOut";
    case "TransferIn":
      return "inventory.movementType.transferIn";
    case "TransferCancelRestore":
      return "inventory.movementType.transferCancel";
    case "StockCountVarianceIncrease":
      return "inventory.movementType.stockCountIncrease";
    case "StockCountVarianceDecrease":
      return "inventory.movementType.stockCountDecrease";
    default:
      return "inventory.movementType.other";
  }
}

/**
 * Authoritative stock value when UnitCost is set.
 * Prefer DTO stockValue; else abs(qty) × unitCost for base-unit acquisition display.
 * Null UnitCost → null (never treat as ₱0).
 */
export function resolveMovementStockValue(movement: PosStockMovementDto): number | null {
  if (movement.unitCost == null) {
    return null;
  }
  if (movement.stockValue != null && Number.isFinite(movement.stockValue)) {
    return Math.abs(movement.stockValue);
  }
  return Math.abs(movement.quantityEffect) * movement.unitCost;
}

export function sumGoodsReceiptValue(
  lines: Array<{ lineTotalSnapshot: number }>,
): number {
  return lines.reduce((sum, line) => sum + (line.lineTotalSnapshot || 0), 0);
}

export function sumPurchaseOrderLineTotals(
  lines: Array<{ lineTotal: number }>,
): number {
  return lines.reduce((sum, line) => sum + (line.lineTotal || 0), 0);
}

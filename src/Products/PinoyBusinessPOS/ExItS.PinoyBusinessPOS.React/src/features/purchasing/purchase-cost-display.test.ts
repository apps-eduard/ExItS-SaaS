import { describe, expect, it } from "vitest";
import {
  inventoryMovementTypeLabelKey,
  resolveMovementStockValue,
  sumGoodsReceiptValue,
  sumPurchaseOrderLineTotals,
} from "@/features/purchasing/purchase-cost-display";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";

function movement(partial: Partial<PosStockMovementDto>): PosStockMovementDto {
  return {
    movementId: "11111111-1111-1111-1111-111111111111",
    productId: "22222222-2222-2222-2222-222222222222",
    inventoryAccountId: "33333333-3333-3333-3333-333333333333",
    movementType: "OpeningStock",
    quantityEffect: 24,
    reason: "Opening",
    sourceType: "Opening",
    recordedAtUtc: "2026-08-28T14:15:00Z",
    recordedBy: "44444444-4444-4444-4444-444444444444",
    ...partial,
  };
}

describe("purchase-cost-display", () => {
  it("maps movement types to friendly label keys", () => {
    expect(inventoryMovementTypeLabelKey("OpeningStock")).toBe(
      "inventory.movementType.openingStock",
    );
    expect(inventoryMovementTypeLabelKey("DirectPurchaseReceipt")).toBe(
      "inventory.movementType.directBuy",
    );
    expect(inventoryMovementTypeLabelKey("PurchaseReceipt")).toBe(
      "inventory.movementType.poReceipt",
    );
    expect(inventoryMovementTypeLabelKey("StockUse")).toBe("inventory.movementType.stockUse");
    expect(inventoryMovementTypeLabelKey("ProductionMaterialConsumption")).toBe(
      "inventory.movementType.productionMaterial",
    );
    expect(inventoryMovementTypeLabelKey("ProductionMaterialRestoration")).toBe(
      "inventory.movementType.productionMaterialVoid",
    );
    expect(inventoryMovementTypeLabelKey("ProductionOutput")).toBe(
      "inventory.movementType.productionOutput",
    );
    expect(inventoryMovementTypeLabelKey("ProductionOutputReversal")).toBe(
      "inventory.movementType.productionOutputVoid",
    );
  });

  it("uses stockValue when UnitCost is present", () => {
    expect(
      resolveMovementStockValue(
        movement({ unitCost: 10, quantityEffect: 48, stockValue: 480 }),
      ),
    ).toBe(480);
  });

  it("falls back to abs(qty) × unitCost", () => {
    expect(
      resolveMovementStockValue(movement({ unitCost: 18, quantityEffect: 24, stockValue: null })),
    ).toBe(432);
  });

  it("omits cost when UnitCost is null (never ₱0)", () => {
    expect(
      resolveMovementStockValue(
        movement({ unitCost: null, stockValue: null, movementType: "ManualIncrease" }),
      ),
    ).toBeNull();
  });

  it("sums PO and goods receipt purchase-unit values", () => {
    expect(sumPurchaseOrderLineTotals([{ lineTotal: 480 }, { lineTotal: 120 }])).toBe(600);
    expect(
      sumGoodsReceiptValue([
        { lineTotalSnapshot: 480 },
        { lineTotalSnapshot: 0 },
      ]),
    ).toBe(480);
  });

  it("reconciles case package purchase value (2×240 = 48×10 = 480)", () => {
    const poValue = sumPurchaseOrderLineTotals([{ lineTotal: 2 * 240 }]);
    const receiptValue = sumGoodsReceiptValue([{ lineTotalSnapshot: 2 * 240 }]);
    const movementValue = resolveMovementStockValue(
      movement({
        movementType: "PurchaseReceipt",
        quantityEffect: 48,
        unitCost: 10,
        stockValue: 480,
      }),
    );
    expect(poValue).toBe(480);
    expect(receiptValue).toBe(480);
    expect(movementValue).toBe(480);
  });
});

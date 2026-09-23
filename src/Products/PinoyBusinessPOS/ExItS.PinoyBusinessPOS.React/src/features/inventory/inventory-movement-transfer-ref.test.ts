import { describe, expect, it } from "vitest";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import {
  extractTransferReferenceNumber,
  inventoryTransferDetailPath,
  isInventoryTransferMovement,
  resolveInventoryTransferTransactionId,
} from "@/features/inventory/inventory-movement-transfer-ref";

function movement(
  overrides: Partial<PosStockMovementDto> &
    Pick<PosStockMovementDto, "movementType" | "reason" | "sourceType">,
): PosStockMovementDto {
  return {
    movementId: "m1",
    productId: "p1",
    inventoryAccountId: "a1",
    quantityEffect: -1,
    recordedAtUtc: "2026-09-22T10:00:00Z",
    recordedBy: "actor",
    ...overrides,
  };
}

describe("inventory-movement-transfer-ref", () => {
  it("extracts transfer number from Transfer out reason", () => {
    const m = movement({
      movementType: "TransferOut",
      sourceType: "InventoryTransfer",
      reason: "Transfer out TR-260922-001",
      sourceId: "tid",
    });
    expect(isInventoryTransferMovement(m)).toBe(true);
    expect(extractTransferReferenceNumber(m)).toBe("TR-260922-001");
    expect(inventoryTransferDetailPath("tid")).toBe("/inventory/transfers/tid");
  });

  it("supports legacy unprefixed transfer numbers and Transfer in", () => {
    expect(
      extractTransferReferenceNumber(
        movement({
          movementType: "TransferIn",
          sourceType: "InventoryTransfer",
          reason: "Transfer in 260922-004-R1",
        }),
      ),
    ).toBe("260922-004-R1");
  });

  it("returns null for non-transfer movements", () => {
    expect(
      extractTransferReferenceNumber(
        movement({
          movementType: "SaleDeduction",
          sourceType: "Sale",
          reason: "Sale deduction",
        }),
      ),
    ).toBeNull();
  });

  it("prefers server transactionReference and transactionId over sourceId/reason", () => {
    const m = movement({
      movementType: "TransferDamageHold",
      sourceType: "InventoryTransfer",
      reason: "Transfer damage hold TR-260922-001",
      sourceId: "receipt-line-id",
      transactionType: "InventoryTransfer",
      transactionId: "real-transfer-id",
      transactionReference: "TR-260922-001",
    });
    expect(isInventoryTransferMovement(m)).toBe(true);
    expect(extractTransferReferenceNumber(m)).toBe("TR-260922-001");
    expect(resolveInventoryTransferTransactionId(m)).toBe("real-transfer-id");
    expect(inventoryTransferDetailPath(resolveInventoryTransferTransactionId(m)!)).toBe(
      "/inventory/transfers/real-transfer-id",
    );
  });

  it("strips damage-hold decision suffix when parsing transfer number from reason", () => {
    expect(
      extractTransferReferenceNumber(
        movement({
          movementType: "TransferDamageHold",
          sourceType: "InventoryTransfer",
          reason:
            "Transfer damage hold TR-260922-001 · Return to source · Replacement requested",
          sourceId: "receipt-line-id",
        }),
      ),
    ).toBe("TR-260922-001");
  });

  it("does not treat damage sourceId as transfer id when transactionId is missing", () => {
    expect(
      resolveInventoryTransferTransactionId(
        movement({
          movementType: "TransferDamageHold",
          sourceType: "InventoryTransfer",
          reason: "Transfer damage hold TR-260922-001",
          sourceId: "receipt-line-id",
        }),
      ),
    ).toBeNull();
  });
});

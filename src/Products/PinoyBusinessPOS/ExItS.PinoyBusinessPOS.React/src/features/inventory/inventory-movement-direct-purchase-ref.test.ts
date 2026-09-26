import { describe, expect, it } from "vitest";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import {
  directPurchaseDetailPath,
  extractDirectPurchaseReferenceNumber,
  isDirectPurchaseMovement,
  resolveDirectPurchaseDisplayLabel,
  resolveDirectPurchaseReceiptId,
} from "@/features/inventory/inventory-movement-direct-purchase-ref";

function movement(
  overrides: Partial<PosStockMovementDto> &
    Pick<PosStockMovementDto, "movementType" | "sourceType">,
): PosStockMovementDto {
  return {
    movementId: "mov-1",
    productId: "prod-1",
    inventoryAccountId: "acc-1",
    quantityEffect: 10,
    reason: null,
    sourceId: null,
    recordedAtUtc: "2026-09-26T00:00:00Z",
    recordedBy: "actor-1",
    ...overrides,
  };
}

describe("inventory-movement-direct-purchase-ref", () => {
  it("recognizes DirectPurchaseReceipt and prefers transaction fields", () => {
    const row = movement({
      movementType: "DirectPurchaseReceipt",
      sourceType: "DirectPurchase",
      sourceId: "source-receipt-id",
      transactionType: "DirectPurchase",
      transactionId: "receipt-id",
      transactionReference: "DP-260926-045",
    });

    expect(isDirectPurchaseMovement(row)).toBe(true);
    expect(resolveDirectPurchaseReceiptId(row)).toBe("receipt-id");
    expect(extractDirectPurchaseReferenceNumber(row)).toBe("DP-260926-045");
    expect(resolveDirectPurchaseDisplayLabel(row)).toBe("DP-260926-045");
    expect(directPurchaseDetailPath("receipt-id")).toBe(
      "/purchasing/direct-purchases/receipt-id",
    );
  });

  it("falls back to sourceId when transactionId is missing", () => {
    const row = movement({
      movementType: "DirectPurchaseReceiptReversal",
      sourceType: "DirectPurchase",
      sourceId: "source-receipt-id",
    });

    expect(isDirectPurchaseMovement(row)).toBe(true);
    expect(resolveDirectPurchaseReceiptId(row)).toBe("source-receipt-id");
    expect(extractDirectPurchaseReferenceNumber(row)).toBeNull();
    expect(resolveDirectPurchaseDisplayLabel(row)).toBeNull();
  });

  it("never treats a raw GUID as the display label", () => {
    const row = movement({
      movementType: "DirectPurchaseReceipt",
      sourceType: "DirectPurchase",
      sourceId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      transactionType: "DirectPurchase",
      transactionId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      transactionReference: null,
    });

    expect(resolveDirectPurchaseReceiptId(row)).toBe(
      "752a42f2-bf73-4608-896b-8ca9832d4c8a",
    );
    expect(resolveDirectPurchaseDisplayLabel(row)).toBeNull();
  });

  it("ignores GUID-shaped transactionReference values", () => {
    const row = movement({
      movementType: "DirectPurchaseReceipt",
      sourceType: "DirectPurchase",
      transactionType: "DirectPurchase",
      transactionId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      transactionReference: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
    });

    expect(extractDirectPurchaseReferenceNumber(row)).toBeNull();
    expect(resolveDirectPurchaseDisplayLabel(row)).toBeNull();
  });

  it("parses DP number from reason when server reference is missing", () => {
    const row = movement({
      movementType: "DirectPurchaseReceipt",
      sourceType: "DirectPurchase",
      sourceId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      reason: "Direct purchase receipt DP-260926-003",
    });

    expect(extractDirectPurchaseReferenceNumber(row)).toBe("DP-260926-003");
    expect(resolveDirectPurchaseDisplayLabel(row)).toBe("DP-260926-003");
  });

  it("ignores non-direct-purchase movements", () => {
    const row = movement({
      movementType: "TransferOut",
      sourceType: "InventoryTransfer",
      sourceId: "transfer-id",
      transactionType: "InventoryTransfer",
      transactionId: "transfer-id",
      transactionReference: "TR-1",
    });

    expect(isDirectPurchaseMovement(row)).toBe(false);
    expect(resolveDirectPurchaseReceiptId(row)).toBeNull();
    expect(extractDirectPurchaseReferenceNumber(row)).toBeNull();
  });
});

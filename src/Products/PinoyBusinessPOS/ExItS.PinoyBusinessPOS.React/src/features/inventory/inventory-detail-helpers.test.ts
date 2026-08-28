import { describe, expect, it } from "vitest";
import {
  canDisableExpirationTracking,
  computeGoodQuantity,
  sortLotsByExpiry,
} from "@/features/inventory/inventory-detail-helpers";
import type { PosInventoryAccountDto, PosInventoryLotDto } from "@/api/pos/pos-inventory-client";

function account(partial: Partial<PosInventoryAccountDto> = {}): PosInventoryAccountDto {
  return {
    productId: "p1",
    organizationId: "o1",
    name: "Milk",
    unitOfMeasure: "Piece",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity: 40,
    stockStatus: "InStock",
    isLowStock: false,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    sellableQuantity: 38,
    nearExpiryQuantity: 8,
    expiredQuantity: 2,
    tracksExpiration: true,
    ...partial,
  };
}

describe("inventory-detail-helpers", () => {
  it("computes good quantity from sellable minus near expiry", () => {
    expect(computeGoodQuantity(account())).toBe(30);
  });

  it("sorts lots earliest expiry first", () => {
    const lots: PosInventoryLotDto[] = [
      {
        lotId: "b",
        productId: "p1",
        expirationDate: "2026-10-01",
        quantityOnHand: 12,
        expiryStatus: "Ok",
        createdAtUtc: "",
        updatedAtUtc: "",
      },
      {
        lotId: "a",
        productId: "p1",
        expirationDate: "2026-09-05",
        quantityOnHand: 8,
        expiryStatus: "NearExpiry",
        createdAtUtc: "",
        updatedAtUtc: "",
      },
    ];
    expect(sortLotsByExpiry(lots).map((lot) => lot.lotId)).toEqual(["a", "b"]);
  });

  it("blocks expiration disable when stock remains", () => {
    expect(canDisableExpirationTracking(account({ onHandQuantity: 5 }))).toBe(false);
    expect(canDisableExpirationTracking(account({ onHandQuantity: 0 }))).toBe(true);
  });
});

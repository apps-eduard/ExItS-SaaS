import { describe, expect, it } from "vitest";
import {
  allocateTransferLotsFefo,
  selectTransferEligibleLots,
  sortLotsForTransferFefo,
} from "@/features/inventory/inventory-transfer-fefo-allocate";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";

function lot(
  overrides: Partial<PosInventoryLotDto> &
    Pick<PosInventoryLotDto, "lotId" | "expirationDate" | "quantityOnHand">,
): PosInventoryLotDto {
  return {
    productId: "prod-1",
    expiryStatus: "Ok",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    lotNumber: null,
    ...overrides,
  };
}

describe("inventory-transfer-fefo-allocate", () => {
  const eligible = [
    {
      lotId: "lot-a",
      lotNumber: "LOT-A",
      expirationDate: "2026-09-12",
      quantityOnHand: 50,
    },
    {
      lotId: "lot-b",
      lotNumber: "LOT-B",
      expirationDate: "2026-09-15",
      quantityOnHand: 37,
    },
    {
      lotId: "lot-c",
      lotNumber: "LOT-C",
      expirationDate: "2026-09-30",
      quantityOnHand: 100,
    },
  ];

  it("allocates qty 10 to earliest lot only", () => {
    const result = allocateTransferLotsFefo(eligible, 10);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations).toEqual([
      expect.objectContaining({ lotId: "lot-a", quantity: 10 }),
    ]);
  });

  it("allocates qty 50 exactly to earliest lot", () => {
    const result = allocateTransferLotsFefo(eligible, 50);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations).toHaveLength(1);
    expect(result.allocations[0]).toMatchObject({ lotId: "lot-a", quantity: 50 });
  });

  it("splits qty 60 across first two lots (50 + 10)", () => {
    const result = allocateTransferLotsFefo(eligible, 60);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations).toEqual([
      expect.objectContaining({ lotId: "lot-a", quantity: 50 }),
      expect.objectContaining({ lotId: "lot-b", quantity: 10 }),
    ]);
  });

  it("allocates qty 87 as 50 + 37", () => {
    const result = allocateTransferLotsFefo(eligible, 87);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations.map((a) => [a.lotId, a.quantity])).toEqual([
      ["lot-a", 50],
      ["lot-b", 37],
    ]);
  });

  it("allocates qty 90 as 50 + 37 + 3", () => {
    const result = allocateTransferLotsFefo(eligible, 90);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations.map((a) => [a.lotId, a.quantity])).toEqual([
      ["lot-a", 50],
      ["lot-b", 37],
      ["lot-c", 3],
    ]);
  });

  it("allocates full eligible stock 187", () => {
    const result = allocateTransferLotsFefo(eligible, 187);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations.map((a) => a.quantity)).toEqual([50, 37, 100]);
  });

  it("returns insufficient when qty exceeds eligible stock", () => {
    expect(allocateTransferLotsFefo(eligible, 188)).toEqual({
      ok: false,
      reason: "insufficient",
    });
  });

  it("rejects non-positive quantity", () => {
    expect(allocateTransferLotsFefo(eligible, 0)).toEqual({
      ok: false,
      reason: "invalid_qty",
    });
  });

  it("uses stable lotId order for equal expiry dates", () => {
    const sameDay = [
      {
        lotId: "lot-z",
        lotNumber: "Z",
        expirationDate: "2026-09-12",
        quantityOnHand: 5,
      },
      {
        lotId: "lot-a",
        lotNumber: "A",
        expirationDate: "2026-09-12",
        quantityOnHand: 5,
      },
    ];
    const ordered = sortLotsForTransferFefo(sameDay);
    expect(ordered.map((l) => l.lotId)).toEqual(["lot-a", "lot-z"]);
    const result = allocateTransferLotsFefo(sameDay, 6);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.allocations.map((a) => [a.lotId, a.quantity])).toEqual([
      ["lot-a", 5],
      ["lot-z", 1],
    ]);
  });

  it("ignores depleted and expired lots when selecting eligible", () => {
    const lots = [
      lot({
        lotId: "ok",
        expirationDate: "2026-09-12",
        quantityOnHand: 10,
        expiryStatus: "Ok",
      }),
      lot({
        lotId: "empty",
        expirationDate: "2026-09-10",
        quantityOnHand: 0,
        expiryStatus: "Ok",
      }),
      lot({
        lotId: "expired",
        expirationDate: "2026-01-01",
        quantityOnHand: 40,
        expiryStatus: "Expired",
      }),
    ];
    const eligibleOnly = selectTransferEligibleLots(lots);
    expect(eligibleOnly.map((l) => l.lotId)).toEqual(["ok"]);
  });
});

import { describe, expect, it } from "vitest";
import { resolveRetailWarehouseSupply } from "@/features/warehouse/retail-warehouse-resolve";

const RETAIL = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const WH_A = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const WH_B = "cccccccc-cccc-cccc-cccc-cccccccccccc";

describe("resolveRetailWarehouseSupply", () => {
  it("A: returns no-warehouse when org has no active warehouse", () => {
    expect(
      resolveRetailWarehouseSupply(
        [
          { branchId: RETAIL, name: "Retail", branchType: "Retail", isActive: true },
          { branchId: WH_A, name: "Old WH", branchType: "Warehouse", isActive: false },
        ],
        [{ sourceLocationId: WH_A, destinationLocationId: RETAIL, isActive: true, isPreferred: true }],
        RETAIL,
      ),
    ).toEqual({ kind: "no-warehouse" });
  });

  it("B: returns no-assignment when warehouses exist but no active warehouse route", () => {
    expect(
      resolveRetailWarehouseSupply(
        [
          { branchId: RETAIL, name: "Retail", branchType: "Retail", isActive: true },
          { branchId: WH_A, name: "Panay WH", branchType: "Warehouse", isActive: true },
        ],
        [
          {
            sourceLocationId: WH_A,
            destinationLocationId: RETAIL,
            isActive: false,
            isPreferred: true,
          },
        ],
        RETAIL,
      ),
    ).toEqual({ kind: "no-assignment" });
  });

  it("B: ignores non-warehouse source routes", () => {
    const otherRetail = "dddddddd-dddd-dddd-dddd-dddddddddddd";
    expect(
      resolveRetailWarehouseSupply(
        [
          { branchId: RETAIL, name: "Retail", branchType: "Retail", isActive: true },
          { branchId: otherRetail, name: "Other", branchType: "Retail", isActive: true },
          { branchId: WH_A, name: "Panay WH", branchType: "Warehouse", isActive: true },
        ],
        [
          {
            sourceLocationId: otherRetail,
            destinationLocationId: RETAIL,
            isActive: true,
            isPreferred: true,
          },
        ],
        RETAIL,
      ),
    ).toEqual({ kind: "no-assignment" });
  });

  it("C: prefers preferred active warehouse route", () => {
    expect(
      resolveRetailWarehouseSupply(
        [
          { branchId: RETAIL, name: "Retail", branchType: "Retail", isActive: true },
          { branchId: WH_A, name: "Panay WH", branchType: "Warehouse", isActive: true },
          { branchId: WH_B, name: "Iloilo WH", branchType: "Warehouse", isActive: true },
        ],
        [
          {
            sourceLocationId: WH_A,
            destinationLocationId: RETAIL,
            isActive: true,
            isPreferred: false,
          },
          {
            sourceLocationId: WH_B,
            destinationLocationId: RETAIL,
            isActive: true,
            isPreferred: true,
          },
        ],
        RETAIL,
      ),
    ).toEqual({
      kind: "ready",
      supplyWarehouseId: WH_B,
      supplyWarehouseName: "Iloilo WH",
      isPreferred: true,
    });
  });

  it("C: falls back to first active warehouse route", () => {
    expect(
      resolveRetailWarehouseSupply(
        [
          { branchId: RETAIL, name: "Retail", branchType: "Retail", isActive: true },
          { branchId: WH_A, name: "Panay WH", branchType: "Warehouse", isActive: true },
        ],
        [
          {
            sourceLocationId: WH_A,
            destinationLocationId: RETAIL,
            isActive: true,
            isPreferred: false,
          },
        ],
        RETAIL,
      ),
    ).toEqual({
      kind: "ready",
      supplyWarehouseId: WH_A,
      supplyWarehouseName: "Panay WH",
      isPreferred: false,
    });
  });
});

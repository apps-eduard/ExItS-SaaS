import { describe, expect, it } from "vitest";
import {
  buildManagerAttentionItems,
  buildRetailSnapshotModules,
  buildWarehouseSnapshotModules,
} from "@/features/role/manager-home-data";

describe("buildManagerAttentionItems", () => {
  it("returns empty when all counts are zero", () => {
    expect(
      buildManagerAttentionItems({
        lowStockProductCount: 0,
        expiredLotCount: 0,
        nearExpiryLotCount: 0,
        submittedOrderCount: 0,
        receivablePoCount: 0,
        pendingIncomingTransferCount: 0,
        overdueUtangAmount: 0,
      }),
    ).toEqual([]);
  });

  it("only includes nonzero real conditions", () => {
    const items = buildManagerAttentionItems({
      lowStockProductCount: 12,
      expiredLotCount: 0,
      nearExpiryLotCount: 3,
      submittedOrderCount: 4,
      receivablePoCount: 0,
      pendingIncomingTransferCount: 1,
      overdueUtangAmount: 250.5,
    });
    expect(items.map((i) => i.kind)).toEqual([
      "lowStock",
      "expiry",
      "orders",
      "transfers",
      "utang",
    ]);
    expect(items.find((i) => i.kind === "expiry")?.count).toBe(3);
    expect(items.find((i) => i.kind === "utang")?.amount).toBe(250.5);
  });

  it("omits orders when includeOrders is false", () => {
    const items = buildManagerAttentionItems(
      { submittedOrderCount: 5, lowStockProductCount: 1 },
      { includeOrders: false },
    );
    expect(items.map((i) => i.kind)).toEqual(["lowStock"]);
  });
});

describe("buildRetailSnapshotModules", () => {
  it("caps at four modules and links operational pages", () => {
    const modules = buildRetailSnapshotModules({
      canInventory: true,
      canOrders: true,
      canPurchasing: true,
      canCustomers: true,
      lowStock: 1,
      expiry: 0,
      orderCount: 2,
      receivableCount: 0,
      overdueAmount: 10,
      outstandingAmount: 20,
    });
    expect(modules.length).toBeLessThanOrEqual(4);
    expect(modules.some((m) => m.href === "/devices" || m.href === "/org/branches")).toBe(false);
  });
});

describe("buildWarehouseSnapshotModules", () => {
  it("includes inventory transfers and purchasing without orders/utang", () => {
    const modules = buildWarehouseSnapshotModules({
      canInventory: true,
      canPurchasing: true,
      lowStock: 0,
      expiry: 0,
      receivableCount: 2,
      transferCount: 1,
    });
    expect(modules.map((m) => m.key)).toEqual(["inventory", "transfers", "purchasing"]);
  });
});

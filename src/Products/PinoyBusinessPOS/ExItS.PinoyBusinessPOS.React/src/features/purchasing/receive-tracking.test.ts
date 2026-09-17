import { describe, expect, it } from "vitest";
import { selectUntrackedReceivingLines } from "@/features/purchasing/receive-tracking";

describe("selectUntrackedReceivingLines", () => {
  const base = {
    productId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
    name: "Rice 25kg",
    uom: "Bag",
    unitPurchaseCost: 50,
  };

  it("selects untracked lines with good received qty > 0", () => {
    const selected = selectUntrackedReceivingLines([
      { ...base, isInventoryTracked: false, goodText: "3" },
      {
        ...base,
        productId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        name: "Tracked Oil",
        isInventoryTracked: true,
        goodText: "2",
      },
      {
        ...base,
        productId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        name: "Zero qty",
        isInventoryTracked: false,
        goodText: "0",
      },
    ]);

    expect(selected).toHaveLength(1);
    expect(selected[0]).toMatchObject({
      name: "Rice 25kg",
      receivedQty: 3,
      unitPurchaseCost: 50,
      purchaseAmount: 150,
    });
  });

  it("does not select lines that are already tracked", () => {
    expect(
      selectUntrackedReceivingLines([
        { ...base, isInventoryTracked: true, goodText: "5" },
      ]),
    ).toEqual([]);
  });
});

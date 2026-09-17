import { describe, expect, it } from "vitest";
import {
  maxProducibleFromStock,
  productionScaleFactor,
  scaleProductionQuantity,
} from "@/features/inventory/production-labels";
import {
  findProduceShortages,
  hasProduceShortage,
} from "@/features/inventory/production-run-stock";

describe("production scaling", () => {
  it("scales 100→50 at 0.5 and 100→200 at 2.0", () => {
    expect(productionScaleFactor(100, 50)).toBe(0.5);
    expect(productionScaleFactor(100, 200)).toBe(2);
    expect(scaleProductionQuantity(10, 0.5)).toBe(5);
    expect(scaleProductionQuantity(2, 0.5)).toBe(1);
    expect(scaleProductionQuantity(30, 0.5)).toBe(15);
    expect(scaleProductionQuantity(10, 2)).toBe(20);
  });

  it("rounds AwayFromZero-style to 3 decimals", () => {
    expect(scaleProductionQuantity(1, 1 / 3)).toBe(0.333);
  });
});

describe("production-run-stock", () => {
  it("detects shortages and clears when scaled down", () => {
    const at100 = [
      {
        materialProductId: "a",
        name: "Apple",
        uom: "kg",
        required: 3,
        available: 2,
      },
    ];
    expect(hasProduceShortage(at100)).toBe(true);
    expect(findProduceShortages(at100)[0]?.shortBy).toBe(1);

    const at50 = [
      {
        materialProductId: "a",
        name: "Apple",
        uom: "kg",
        required: 1.5,
        available: 2,
      },
    ];
    expect(hasProduceShortage(at50)).toBe(false);
  });

  it("computes max producible from limiting ingredient", () => {
    expect(
      maxProducibleFromStock({
        definitionOutputQuantity: 100,
        components: [
          { quantityEntered: 10, available: 18 },
          { quantityEntered: 3, available: 2 },
        ],
      }),
    ).toBe(66.667);
  });
});

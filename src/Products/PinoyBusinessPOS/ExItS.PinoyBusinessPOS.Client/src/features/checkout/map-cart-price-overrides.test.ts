import { describe, expect, it } from "vitest";
import {
  exceedsManagerSalePriceLimit,
  mapCartPriceOverridesToRequest,
} from "@/features/checkout/map-cart-price-overrides";
import type { SessionCartLine } from "@/cart/SessionCartProvider";

function line(
  partial: Partial<SessionCartLine> & Pick<SessionCartLine, "lineKey" | "productId">,
): SessionCartLine {
  return {
    sku: null,
    name: "Item",
    sellingMode: "PerItem",
    productUnitId: null,
    unitLabel: "pc",
    multiplierToBase: 1,
    unitPrice: 100,
    quantity: 1,
    baseUnitOfMeasure: "pc",
    allowsCustomQuantity: false,
    ...partial,
  };
}

describe("mapCartPriceOverridesToRequest", () => {
  it("maps pending overrides with 1-based line numbers and expected baseline", () => {
    const intents = mapCartPriceOverridesToRequest([
      line({
        lineKey: "a",
        productId: "11111111-1111-4111-8111-111111111111",
        unitPrice: 100,
        priceOverride: {
          requestedUnitPrice: 90,
          reason: "Match competitor",
          expectedBaselineUnitPrice: 100,
        },
      }),
      line({
        lineKey: "b",
        productId: "22222222-2222-4222-8222-222222222222",
        unitPrice: 50,
      }),
    ]);

    expect(intents).toEqual([
      {
        requestedUnitPrice: 90,
        reason: "Match competitor",
        lineNumber: 1,
        productId: "11111111-1111-4111-8111-111111111111",
        expectedBaselineUnitPrice: 100,
      },
    ]);
  });
});

describe("exceedsManagerSalePriceLimit", () => {
  it("allows inclusive 100% and denies above", () => {
    expect(exceedsManagerSalePriceLimit(100, 90)).toBe(false);
    expect(exceedsManagerSalePriceLimit(100, 200)).toBe(false);
    expect(exceedsManagerSalePriceLimit(100, 200.01)).toBe(true);
    expect(exceedsManagerSalePriceLimit(0, 1)).toBe(true);
  });
});

import { describe, expect, it } from "vitest";
import {
  formatShopCartQtyPriceMeta,
  formatShopCartUnitLabel,
} from "@/features/customer-ordering/shop-cart-line-meta";

describe("shop-cart-line-meta", () => {
  it("title-cases word units for the cart meta line", () => {
    expect(formatShopCartUnitLabel("Bottle")).toBe("Bottle");
    expect(formatShopCartUnitLabel("pack")).toBe("Pack");
    expect(formatShopCartUnitLabel("can")).toBe("Can");
  });

  it("formats qty × price without putting the unit in the stepper", () => {
    expect(formatShopCartQtyPriceMeta("Bottle", 1, 48)).toBe("Bottle · 1×₱48.00");
    expect(formatShopCartQtyPriceMeta("kg", 1.25, 220)).toBe("kg · 1.25×₱220.00");
  });
});

import { describe, expect, it } from "vitest";
import {
  evaluateStockGuard,
  findCartStockIssues,
  formatStockUnavailableMessage,
  sumCartBaseQuantityForProduct,
} from "@/cart/sell-cart-helpers";

const appleStock = {
  isTracked: true,
  onHandQuantity: 1,
  unitOfMeasure: "Kilogram",
  sellingMode: "ByWeight",
};

const unitStock = {
  isTracked: true,
  onHandQuantity: 2,
  unitOfMeasure: "Bottle",
  sellingMode: "PerItem",
};

describe("evaluateStockGuard", () => {
  it("allows adding 1 kg when stock is 1 kg", () => {
    const result = evaluateStockGuard({
      stock: appleStock,
      requestedQuantity: 1,
      otherCartBaseQuantity: 0,
    });
    expect(result).toEqual({ ok: true });
  });

  it("blocks adding 2 kg when stock is 1 kg", () => {
    const result = evaluateStockGuard({
      stock: appleStock,
      requestedQuantity: 2,
      otherCartBaseQuantity: 0,
    });
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(formatStockUnavailableMessage(result)).toBe("Only 1.00 kg available.");
    }
  });

  it("blocks additional 0.5 kg when cart already has 0.7 kg of 1 kg stock", () => {
    const result = evaluateStockGuard({
      stock: appleStock,
      requestedQuantity: 0.5,
      otherCartBaseQuantity: 0.7,
    });
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(formatStockUnavailableMessage(result)).toBe("Only 1.00 kg available.");
    }
  });

  it("allows edit to 0.9 kg when cart already has 0.7 kg of 1 kg stock", () => {
    const result = evaluateStockGuard({
      stock: appleStock,
      requestedQuantity: 0.9,
      otherCartBaseQuantity: 0,
    });
    expect(result).toEqual({ ok: true });
  });

  it("blocks unit qty 3 when stock is 2", () => {
    const result = evaluateStockGuard({
      stock: unitStock,
      requestedQuantity: 3,
      otherCartBaseQuantity: 0,
    });
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(formatStockUnavailableMessage(result)).toBe("Only 2 available.");
    }
  });

  it("skips guard when product is not tracked", () => {
    const result = evaluateStockGuard({
      stock: { ...appleStock, isTracked: false },
      requestedQuantity: 99,
    });
    expect(result).toEqual({ ok: true });
  });

  it("counts pack multipliers toward base stock", () => {
    const result = evaluateStockGuard({
      stock: {
        isTracked: true,
        onHandQuantity: 40,
        unitOfMeasure: "Kilogram",
        sellingMode: "PerItem",
      },
      requestedQuantity: 1,
      multiplierToBase: 50,
      otherCartBaseQuantity: 0,
    });
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(formatStockUnavailableMessage(result)).toBe("Only 40 available.");
    }
  });
});

describe("sumCartBaseQuantityForProduct / findCartStockIssues", () => {
  it("sums base qty across lines and excludes a line key", () => {
    const lines = [
      {
        lineKey: "a::base",
        productId: "a",
        quantity: 0.7,
        multiplierToBase: 1,
      },
      {
        lineKey: "b::base",
        productId: "b",
        quantity: 5,
        multiplierToBase: 1,
      },
    ];
    expect(sumCartBaseQuantityForProduct(lines, "a")).toBe(0.7);
    expect(sumCartBaseQuantityForProduct(lines, "a", "a::base")).toBe(0);
  });

  it("flags cart lines that exceed available stock", () => {
    const lines = [
      {
        lineKey: "apple::base",
        productId: "apple",
        name: "Apple",
        quantity: 2,
        multiplierToBase: 1,
        sellingMode: "ByWeight",
      },
    ];
    const stockByProductId = new Map([["apple", appleStock]]);
    const issues = findCartStockIssues(lines, stockByProductId);
    expect(issues).toHaveLength(1);
    expect(issues[0]!.message).toBe("Apple exceeds available stock. Available: 1.00 kg.");
  });
});

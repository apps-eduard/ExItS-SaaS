import { describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import type { ReactNode } from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import {
  cartLineKey,
  formatLineAmountPreview,
  normalizeCustomQuantity,
  normalizeWeightToKilograms,
  requiresWholeEnteredQuantity,
  resolveAddFlow,
  resolveStockHint,
  resolveSellCardStock,
  remainingQuantityAfterCart,
  isCommittedOutOfStock,
} from "@/cart/sell-cart-helpers";
import { SessionCartProvider, useSessionCart } from "@/cart/SessionCartProvider";

const sampleProduct = (
  id: string,
  price: number,
  name: string,
  extras: Partial<PosCatalogProductDto> = {},
): PosCatalogProductDto => ({
  productId: id,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: price,
  status: "Active",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  ...extras,
});

const bottleSellUnit: PosCatalogProductUnitDto = {
  unitId: "bottle-unit",
  productId: "bottle",
  kind: "Sell",
  displayName: "Bottle",
  shortLabel: "btl",
  multiplierToBase: 1,
  sellingPrice: 95,
  allowsCustomQuantity: false,
  isActive: true,
  sortOrder: 0,
};

const singleNormalSellUnitProduct = sampleProduct("bottle", 100, "Premium Bottle", {
  units: [bottleSellUnit],
});

const riceProduct = sampleProduct("rice", 55, "Rice", {
  unitOfMeasure: "Kilogram",
  units: [
    {
      unitId: "kg-unit",
      productId: "rice",
      kind: "Sell",
      displayName: "Kilogram",
      shortLabel: "kg",
      multiplierToBase: 1,
      sellingPrice: 55,
      allowsCustomQuantity: false,
      isActive: true,
      sortOrder: 0,
    },
    {
      unitId: "sack-unit",
      productId: "rice",
      kind: "Sell",
      displayName: "Sack 50kg",
      shortLabel: "sack",
      multiplierToBase: 50,
      sellingPrice: 2600,
      allowsCustomQuantity: false,
      isActive: true,
      sortOrder: 1,
    },
  ],
});

const literUnit: PosCatalogProductUnitDto = {
  unitId: "liter-unit",
  productId: "oil",
  kind: "Sell",
  displayName: "Liter",
  shortLabel: "L",
  multiplierToBase: 1,
  sellingPrice: 80,
  allowsCustomQuantity: true,
  isActive: true,
  sortOrder: 0,
};

const oilProduct = sampleProduct("oil", 80, "Cooking Oil", {
  unitOfMeasure: "Liter",
  units: [literUnit],
});

const meatProduct = sampleProduct("meat", 60, "Ground Pork", {
  unitOfMeasure: "Kilogram",
  sellingMode: "ByWeight",
});

function wrapper({ children }: { children: ReactNode }) {
  return <SessionCartProvider>{children}</SessionCartProvider>;
}

describe("SessionCartProvider", () => {
  it("adds, increments, decrements, removes, and totals lines", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });

    act(() => {
      result.current.addProduct(sampleProduct("p1", 10, "Item A"));
    });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.quantityTotal).toBe(1);
    expect(result.current.subtotal).toBe(10);

    act(() => {
      result.current.addProduct(sampleProduct("p1", 10, "Item A"));
    });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.quantityTotal).toBe(2);
    expect(result.current.subtotal).toBe(20);

    act(() => {
      result.current.addProduct(sampleProduct("p2", 5, "Item B"), 2);
    });
    expect(result.current.lineCount).toBe(2);
    expect(result.current.quantityTotal).toBe(4);
    expect(result.current.subtotal).toBe(30);

    act(() => {
      result.current.decrementLine(cartLineKey("p2", null));
    });
    expect(result.current.lineCount).toBe(2);
    expect(result.current.quantityTotal).toBe(3);
    expect(result.current.subtotal).toBe(25);

    act(() => {
      result.current.removeLine(cartLineKey("p1", null));
    });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.subtotal).toBe(5);

    act(() => {
      result.current.clear();
    });
    expect(result.current.lines).toEqual([]);
    expect(result.current.lineCount).toBe(0);
    expect(result.current.subtotal).toBe(0);
  });

  it("SINGLE_NORMAL_SELL_UNIT keeps unit identity and unit price 95 not 100", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });

    act(() => {
      result.current.addProduct(singleNormalSellUnitProduct);
    });

    expect(result.current.lineCount).toBe(1);
    expect(result.current.lines[0]?.productUnitId).toBe("bottle-unit");
    expect(result.current.lines[0]?.unitPrice).toBe(95);
    expect(result.current.subtotal).toBe(95);
    expect(result.current.lines[0]?.allowsCustomQuantity).toBe(false);
  });

  it("MULTI_UOM keeps separate lines per sell unit and uses unit prices", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });
    const kg = riceProduct.units![0]!;
    const sack = riceProduct.units![1]!;

    act(() => {
      result.current.addLine(riceProduct, { unit: kg, quantity: 3 });
      result.current.addLine(riceProduct, { unit: sack, quantity: 1 });
    });

    expect(result.current.lineCount).toBe(2);
    expect(result.current.subtotal).toBe(55 * 3 + 2600);
    expect(result.current.lines.map((line) => line.unitLabel).sort()).toEqual(["kg", "sack"]);
  });

  it("NON_CUSTOM rejects decimal quantity 1.5", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });
    const kg = riceProduct.units![0]!;

    act(() => {
      result.current.addLine(riceProduct, { unit: kg, quantity: 1 });
    });
    const key = cartLineKey("rice", "kg-unit");
    expect(result.current.lines[0]?.quantity).toBe(1);

    act(() => {
      result.current.setLineQuantity(key, 1.5);
    });
    expect(result.current.lines[0]?.quantity).toBe(1);

    act(() => {
      result.current.addLine(riceProduct, { unit: kg, quantity: 1.5 });
    });
    expect(result.current.lines[0]?.quantity).toBe(1);
  });

  it("CUSTOM Liter accepts 1.5 L without kg conversion", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });

    act(() => {
      result.current.addLine(oilProduct, { unit: literUnit, quantity: 1.5, replaceQuantity: true });
    });

    expect(result.current.lines[0]?.quantity).toBe(1.5);
    expect(result.current.lines[0]?.unitLabel).toBe("L");
    expect(result.current.lines[0]?.allowsCustomQuantity).toBe(true);
    expect(result.current.subtotal).toBe(120);
  });

  it("BY_WEIGHT stores kilograms and supports replace", () => {
    const { result } = renderHook(() => useSessionCart(), { wrapper });

    act(() => {
      result.current.addLine(meatProduct, { quantity: 2, replaceQuantity: true });
    });
    expect(result.current.lines[0]?.quantity).toBe(2);
    expect(result.current.subtotal).toBe(120);

    act(() => {
      result.current.addLine(meatProduct, { quantity: 1.25, replaceQuantity: true });
    });
    expect(result.current.lines[0]?.quantity).toBe(1.25);
    expect(result.current.subtotal).toBe(75);
  });

  it("clears all lines and keeps cart when category context changes externally", () => {
    const { result, rerender } = renderHook(
      ({ categoryId }: { categoryId: string }) => {
        void categoryId;
        return useSessionCart();
      },
      {
        wrapper,
        initialProps: { categoryId: "all" },
      },
    );

    act(() => {
      result.current.addProduct(sampleProduct("p1", 12, "Stable Item"));
    });

    rerender({ categoryId: "cat-drinks" });
    expect(result.current.lineCount).toBe(1);
    expect(result.current.lines[0]?.name).toBe("Stable Item");

    act(() => {
      result.current.clear();
    });
    expect(result.current.lines).toEqual([]);
  });
});

describe("sell-cart-helpers", () => {
  it("requiresWholeEnteredQuantity follows allowsCustomQuantity only", () => {
    expect(requiresWholeEnteredQuantity(bottleSellUnit)).toBe(true);
    expect(requiresWholeEnteredQuantity(literUnit)).toBe(false);
    expect(
      requiresWholeEnteredQuantity({
        ...bottleSellUnit,
        multiplierToBase: 12,
        allowsCustomQuantity: false,
      }),
    ).toBe(true);
    expect(
      requiresWholeEnteredQuantity({
        ...bottleSellUnit,
        multiplierToBase: 1,
        allowsCustomQuantity: true,
      }),
    ).toBe(false);
  });

  it("resolveAddFlow matches locked decision tree", () => {
    expect(resolveAddFlow(meatProduct).kind).toBe("weight");
    expect(resolveAddFlow(oilProduct)).toEqual({ kind: "customQuantity", unit: literUnit });
    expect(resolveAddFlow(singleNormalSellUnitProduct)).toEqual({
      kind: "direct",
      unit: bottleSellUnit,
    });
    expect(resolveAddFlow(riceProduct).kind).toBe("unitSelector");
    expect(resolveAddFlow(sampleProduct("plain", 10, "Plain")).kind).toBe("base");
  });

  it("normalizes kg and g weight input", () => {
    expect(normalizeWeightToKilograms(2, "kg")).toEqual({ kilograms: 2 });
    expect(normalizeWeightToKilograms(1500, "g")).toEqual({ kilograms: 1.5 });
    expect(normalizeWeightToKilograms(1.2345, "kg")).toEqual({ error: "precision" });
    expect(normalizeWeightToKilograms(1.5, "g")).toEqual({ error: "precision" });
  });

  it("normalizes custom quantity without kg conversion", () => {
    expect(normalizeCustomQuantity(1.5)).toEqual({ quantity: 1.5 });
    expect(normalizeCustomQuantity(1.2345)).toEqual({ error: "precision" });
    expect(normalizeCustomQuantity(0)).toEqual({ error: "zero" });
  });

  it("formats line amount preview and prefers sellable stock for expiry products", () => {
    expect(formatLineAmountPreview(2, 60, "kg")).toContain("2 kg");
    expect(formatLineAmountPreview(2, 60, "kg")).toContain("120.00");

    const sellable = resolveStockHint({
      isTracked: true,
      onHandQuantity: 10,
      unitOfMeasure: "Kilogram",
      tracksExpiration: true,
      sellableQuantity: 7,
    });
    expect(sellable?.label).toBe("sellable");
    expect(sellable?.quantity).toBe(7);
  });

  it("labels untracked, low, and out of stock on sell tiles", () => {
    expect(
      resolveSellCardStock({
        isTracked: false,
        onHandQuantity: 12,
        unitOfMeasure: "Piece",
      }).tone,
    ).toBe("untracked");

    expect(
      resolveSellCardStock({
        isTracked: true,
        onHandQuantity: 4,
        unitOfMeasure: "Kilogram",
        stockStatus: "LowStock",
      }).tone,
    ).toBe("low");

    expect(
      resolveSellCardStock({
        isTracked: true,
        onHandQuantity: 0,
        unitOfMeasure: "Piece",
        stockStatus: "InStock",
      }).tone,
    ).toBe("out");
  });

  it("subtracts this register's cart from remaining on-hand", () => {
    expect(remainingQuantityAfterCart(48, 1)).toBe(47);
    expect(remainingQuantityAfterCart(1, 1)).toBe(0);
    expect(remainingQuantityAfterCart(1, 3)).toBe(0);
  });

  it("treats tracked zero/out status as committed out of stock", () => {
    expect(
      isCommittedOutOfStock({
        isTracked: true,
        onHandQuantity: 0,
      }),
    ).toBe(true);
    expect(
      isCommittedOutOfStock({
        isTracked: true,
        onHandQuantity: 4,
        stockStatus: "OutOfStock",
      }),
    ).toBe(true);
    expect(
      isCommittedOutOfStock({
        isTracked: false,
        onHandQuantity: 0,
      }),
    ).toBe(false);
    expect(
      isCommittedOutOfStock({
        isTracked: true,
        onHandQuantity: 12,
      }),
    ).toBe(false);
  });
});

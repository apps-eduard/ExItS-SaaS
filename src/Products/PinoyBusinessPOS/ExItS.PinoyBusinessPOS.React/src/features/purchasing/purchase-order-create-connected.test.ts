import { describe, expect, it } from "vitest";
import type {
  BuyerSupplierProductLink,
  SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";
import {
  applyConnectedQuantityDelta,
  availablePurchaseQty,
  buildConnectedCategoryOptions,
  buildConnectedReadyProducts,
  connectedCategoryFilterFromValues,
  connectedCategoryFilterToValues,
  emptyConnectedCategoryFilter,
  filterConnectedReadyProducts,
  formatLineMath,
  formatSupplierAvailabilityLabel,
  formatUnitOfMeasureLabel,
  formatUnitPriceLabel,
  isConnectedCategoryFilterActive,
  lineTotal,
  mergeConnectedStock,
  orderSubtotal,
  PO_CATEGORY_NONE_OPTION,
  requestedExceedsSupplierAvailability,
  resolveSupplierAvailability,
  retainCompatibleDraftLines,
} from "@/features/purchasing/purchase-order-create-connected";

const relationshipId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const orgA = "11111111-1111-4111-8111-111111111111";
const orgB = "22222222-2222-4222-8222-222222222222";

function link(
  overrides: Partial<BuyerSupplierProductLink> &
    Pick<BuyerSupplierProductLink, "linkId" | "buyerProductId" | "supplierProductId">,
): BuyerSupplierProductLink {
  return {
    relationshipId,
    buyerOrganizationId: orgA,
    supplierOrganizationId: orgB,
    supplierSkuSnapshot: "PH-BEV-WATER-500",
    supplierNameSnapshot: "Bottled Water 500ml",
    unitOfMeasureCode: "Piece",
    lastKnownOrderPrice: 12,
    isActive: true,
    syncVersion: 1,
    createdAtUtc: "2026-09-01T00:00:00Z",
    updatedAtUtc: "2026-09-01T00:00:00Z",
    buyerPurchaseUnitId: null,
    multiplierToBase: 1,
    packageLabel: null,
    ...overrides,
  };
}

function exposure(
  overrides: Partial<SupplierProductExposure> & Pick<SupplierProductExposure, "exposureId" | "productId">,
): SupplierProductExposure {
  return {
    supplierOrganizationId: orgB,
    nameSnapshot: "Bottled Water 500ml",
    skuSnapshot: "PH-BEV-WATER-500",
    unitOfMeasureCode: "Piece",
    supplierOrderPrice: 12,
    effectiveSupplierOrderPrice: 12,
    isExposed: true,
    isOrderable: true,
    syncVersion: 1,
    createdAtUtc: "2026-09-01T00:00:00Z",
    updatedAtUtc: "2026-09-01T00:00:00Z",
    ...overrides,
  };
}

describe("purchase-order-create-connected", () => {
  it("builds linked shared orderable products and prefers effective PO price", () => {
    const ready = buildConnectedReadyProducts(
      [
        link({
          linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          lastKnownOrderPrice: 10,
        }),
        link({
          linkId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          buyerProductId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
          supplierProductId: "99999999-9999-4999-8999-999999999999",
          isActive: false,
        }),
      ],
      [
        exposure({
          exposureId: "10101010-1010-4010-8010-101010101010",
          productId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          effectiveSupplierOrderPrice: 12,
        }),
      ],
    );
    expect(ready).toHaveLength(1);
    expect(ready[0]?.unitPurchaseCost).toBe(12);
    expect(ready[0]?.buyerProductId).toBe("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
  });

  it("filters by search without requiring search to show products", () => {
    const products = buildConnectedReadyProducts([
      link({
        linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      }),
    ]);
    expect(filterConnectedReadyProducts(products, "")).toHaveLength(1);
    expect(filterConnectedReadyProducts(products, "water")).toHaveLength(1);
    expect(filterConnectedReadyProducts(products, "soap")).toHaveLength(0);
  });

  it("builds searchable multi-select options with No category and OR filters", () => {
    const products = buildConnectedReadyProducts(
      [
        link({
          linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          supplierNameSnapshot: "Bottled Water 500ml",
        }),
        link({
          linkId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          buyerProductId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
          supplierProductId: "99999999-9999-4999-8999-999999999999",
          supplierNameSnapshot: "Rice 1kg",
          supplierSkuSnapshot: "PH-RICE-1KG",
          lastKnownOrderPrice: 50,
        }),
        link({
          linkId: "10101010-1010-4010-8010-101010101010",
          buyerProductId: "12121212-1212-4212-8212-121212121212",
          supplierProductId: "13131313-1313-4313-8313-131313131313",
          supplierNameSnapshot: "Mystery Pack",
          supplierSkuSnapshot: "PH-MISC-1",
          lastKnownOrderPrice: 8,
        }),
        link({
          linkId: "17171717-1717-4717-8717-171717171717",
          buyerProductId: "18181818-1818-4818-8818-181818181818",
          supplierProductId: "19191919-1919-4919-8919-191919191919",
          supplierNameSnapshot: "Chips",
          supplierSkuSnapshot: "PH-SNACK-1",
          lastKnownOrderPrice: 20,
        }),
      ],
      [
        exposure({
          exposureId: "14141414-1414-4414-8414-141414141414",
          productId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          categoryNameSnapshot: "Beverages",
        }),
        exposure({
          exposureId: "15151515-1515-4515-8515-151515151515",
          productId: "99999999-9999-4999-8999-999999999999",
          nameSnapshot: "Rice 1kg",
          skuSnapshot: "PH-RICE-1KG",
          supplierOrderPrice: 50,
          effectiveSupplierOrderPrice: 50,
          categoryNameSnapshot: "Staples",
        }),
        exposure({
          exposureId: "16161616-1616-4616-8616-161616161616",
          productId: "13131313-1313-4313-8313-131313131313",
          nameSnapshot: "Mystery Pack",
          skuSnapshot: "PH-MISC-1",
          supplierOrderPrice: 8,
          effectiveSupplierOrderPrice: 8,
          categoryNameSnapshot: null,
        }),
        exposure({
          exposureId: "20202020-2020-4020-8020-202020202020",
          productId: "19191919-1919-4919-8919-191919191919",
          nameSnapshot: "Chips",
          skuSnapshot: "PH-SNACK-1",
          supplierOrderPrice: 20,
          effectiveSupplierOrderPrice: 20,
          categoryNameSnapshot: "Snacks",
        }),
      ],
    );

    const options = buildConnectedCategoryOptions(products, { noCategory: "No category" });
    expect(options).toEqual([
      { value: "Beverages", label: "Beverages", count: 1 },
      { value: "Snacks", label: "Snacks", count: 1 },
      { value: "Staples", label: "Staples", count: 1 },
      { value: PO_CATEGORY_NONE_OPTION, label: "No category", count: 1 },
    ]);
    expect(options.some((o) => o.value === "all" || o.label === "All")).toBe(false);
    expect(options.some((o) => o.label === "Other")).toBe(false);

    expect(isConnectedCategoryFilterActive(emptyConnectedCategoryFilter())).toBe(false);
    expect(
      filterConnectedReadyProducts(products, "", emptyConnectedCategoryFilter()),
    ).toHaveLength(4);

    const beveragesAndSnacks = connectedCategoryFilterFromValues(["Beverages", "Snacks"]);
    expect(filterConnectedReadyProducts(products, "", beveragesAndSnacks)).toHaveLength(2);
    expect(
      filterConnectedReadyProducts(products, "water", beveragesAndSnacks).map((p) => p.productName),
    ).toEqual(["Bottled Water 500ml"]);

    const noneOnly = connectedCategoryFilterFromValues([PO_CATEGORY_NONE_OPTION]);
    expect(noneOnly.includeNoCategory).toBe(true);
    expect(noneOnly.selectedCategoryIds).toEqual([]);
    expect(filterConnectedReadyProducts(products, "", noneOnly)[0]?.productName).toBe(
      "Mystery Pack",
    );
    expect(connectedCategoryFilterToValues(noneOnly)).toEqual([PO_CATEGORY_NONE_OPTION]);
  });

  it("uses supplier exposure category for linked products, never buyer local category", () => {
    const ready = buildConnectedReadyProducts(
      [
        link({
          linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          supplierNameSnapshot: "Apple",
          lastKnownOrderPrice: 40,
        }),
        link({
          linkId: "10101010-1010-4010-8010-101010101010",
          buyerProductId: "12121212-1212-4212-8212-121212121212",
          supplierProductId: "13131313-1313-4313-8313-131313131313",
          supplierNameSnapshot: "Banana",
          lastKnownOrderPrice: 25,
        }),
        link({
          linkId: "17171717-1717-4717-8717-171717171717",
          buyerProductId: "18181818-1818-4818-8818-181818181818",
          supplierProductId: "19191919-1919-4919-8919-191919191919",
          supplierNameSnapshot: "Uncategorized Snack",
          lastKnownOrderPrice: 10,
        }),
      ],
      [
        exposure({
          exposureId: "14141414-1414-4414-8414-141414141414",
          productId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          nameSnapshot: "Apple",
          categoryNameSnapshot: "Fruits",
          supplierOrderPrice: 40,
          effectiveSupplierOrderPrice: 40,
        }),
        exposure({
          exposureId: "15151515-1515-4515-8515-151515151515",
          productId: "13131313-1313-4313-8313-131313131313",
          nameSnapshot: "Banana",
          categoryNameSnapshot: "Fruits",
          supplierOrderPrice: 25,
          effectiveSupplierOrderPrice: 25,
        }),
        exposure({
          exposureId: "16161616-1616-4616-8616-161616161616",
          productId: "19191919-1919-4919-8919-191919191919",
          nameSnapshot: "Uncategorized Snack",
          categoryNameSnapshot: null,
          supplierOrderPrice: 10,
          effectiveSupplierOrderPrice: 10,
        }),
      ],
    );

    expect(ready.map((p) => [p.productName, p.categoryName])).toEqual([
      ["Apple", "Fruits"],
      ["Banana", "Fruits"],
      ["Uncategorized Snack", null],
    ]);
    // Buyer local category names must not appear on Create PO rows.
    expect(ready.every((p) => p.categoryName !== "Fresh Produce")).toBe(true);
    expect(ready.every((p) => p.categoryName !== "Produce")).toBe(true);

    const options = buildConnectedCategoryOptions(ready, { noCategory: "No category" });
    expect(options).toEqual([
      { value: "Fruits", label: "Fruits", count: 2 },
      { value: PO_CATEGORY_NONE_OPTION, label: "No category", count: 1 },
    ]);

    const fruitsOnly = connectedCategoryFilterFromValues(["Fruits"]);
    expect(filterConnectedReadyProducts(ready, "", fruitsOnly).map((p) => p.productName)).toEqual([
      "Apple",
      "Banana",
    ]);
  });

  it("supports + Add, +/- qty, line total, and qty 0 remove", () => {
    const product = buildConnectedReadyProducts([
      link({
        linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        lastKnownOrderPrice: 12,
      }),
    ])[0]!;

    let lines = applyConnectedQuantityDelta([], product, 1);
    expect(lines).toEqual([
      expect.objectContaining({ productId: product.buyerProductId, orderedQty: 1, unitPurchaseCost: 12 }),
    ]);
    expect(lineTotal(1, 12)).toBe(12);
    expect(formatLineMath(1, 12)).toContain("1 ×");

    lines = applyConnectedQuantityDelta(lines, product, 4);
    expect(lines[0]?.orderedQty).toBe(5);
    expect(lineTotal(5, 12)).toBe(60);
    expect(orderSubtotal(lines)).toBe(60);

    lines = applyConnectedQuantityDelta(lines, product, -5);
    expect(lines).toEqual([]);
  });

  it("allows over-order and surfaces availability labels / warnings", () => {
    const t = (key: string) => {
      if (key === "purchasing.supplierOutOfStock") return "Out of stock";
      if (key === "purchasing.supplierAvailableNow") return "Available now: {qty} {unit}";
      if (key === "purchasing.stockNotTracked") return "Stock not tracked";
      return key;
    };
    const base = buildConnectedReadyProducts([
      link({
        linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        lastKnownOrderPrice: 12,
      }),
    ])[0]!;

    const out = { ...base, stockTracked: true, availableBaseQuantity: 0 };
    expect(resolveSupplierAvailability(out)).toEqual({ kind: "out_of_stock" });
    expect(formatSupplierAvailabilityLabel(out, t)).toBe("Out of stock");
    let lines = applyConnectedQuantityDelta([], out, 1);
    expect(lines[0]?.orderedQty).toBe(1);
    expect(requestedExceedsSupplierAvailability(out, 1)).toBe(true);

    const limited = {
      ...base,
      stockTracked: true,
      availableBaseQuantity: 5,
      unitOfMeasure: "Kg",
      packageLabel: "Kg",
    };
    expect(resolveSupplierAvailability(limited)).toEqual({ kind: "available", quantity: 5 });
    expect(formatSupplierAvailabilityLabel(limited, t)).toBe("5");
    expect(requestedExceedsSupplierAvailability(limited, 3)).toBe(false);
    expect(requestedExceedsSupplierAvailability(limited, 5)).toBe(false);
    expect(requestedExceedsSupplierAvailability(limited, 8)).toBe(true);
    lines = applyConnectedQuantityDelta([], limited, 1);
    lines = applyConnectedQuantityDelta(lines, limited, 7);
    expect(lines[0]?.orderedQty).toBe(8);

    const thousands = {
      ...base,
      stockTracked: true,
      availableBaseQuantity: 12500,
      unitOfMeasure: "Piece",
      packageLabel: "Piece",
    };
    expect(formatSupplierAvailabilityLabel(thousands, t)).toBe("12,500");

    const cases = {
      ...base,
      multiplierToBase: 12,
      stockTracked: true,
      availableBaseQuantity: 24,
      unitOfMeasure: "Pack",
      packageLabel: "Pack",
    };
    expect(availablePurchaseQty(cases)).toBe(2);
    expect(formatSupplierAvailabilityLabel(cases, t)).toBe("2");
    lines = applyConnectedQuantityDelta([], cases, 1);
    lines = applyConnectedQuantityDelta(lines, cases, 5);
    expect(lines[0]?.orderedQty).toBe(6);
    expect(requestedExceedsSupplierAvailability(cases, 6)).toBe(true);

    const untracked = { ...base, stockTracked: false, availableBaseQuantity: null };
    expect(resolveSupplierAvailability(untracked)).toEqual({ kind: "untracked" });
    lines = applyConnectedQuantityDelta([], untracked, 1);
    lines = applyConnectedQuantityDelta(lines, untracked, 99);
    expect(lines[0]?.orderedQty).toBe(100);
    expect(requestedExceedsSupplierAvailability(untracked, 100)).toBe(false);
  });

  it("merges supplier-branch stock onto ready products", () => {
    const products = buildConnectedReadyProducts([
      link({
        linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      }),
    ]);
    const merged = mergeConnectedStock(
      products,
      new Map([["dddddddd-dddd-4ddd-8ddd-dddddddddddd", { isTracked: true, availableBaseQuantity: 7 }]]),
    );
    expect(merged[0]?.stockTracked).toBe(true);
    expect(merged[0]?.availableBaseQuantity).toBe(7);
  });

  it("clears incompatible lines when supplier catalog changes", () => {
    const kept = buildConnectedReadyProducts([
      link({
        linkId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        supplierProductId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      }),
    ]);
    const lines = [
      {
        productId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        name: "Water",
        uom: "Piece",
        orderedQty: 2,
        unitPurchaseCost: 12,
      },
      {
        productId: "gone-product",
        name: "Gone",
        uom: "Piece",
        orderedQty: 1,
        unitPurchaseCost: 1,
      },
    ];
    expect(retainCompatibleDraftLines(lines, kept)).toEqual([lines[0]]);
  });

  it("formats unit labels for display", () => {
    expect(formatUnitOfMeasureLabel("Kilogram")).toBe("Kg");
    expect(formatUnitPriceLabel(12, "Piece")).toMatch(/₱\s*12/);
  });
});

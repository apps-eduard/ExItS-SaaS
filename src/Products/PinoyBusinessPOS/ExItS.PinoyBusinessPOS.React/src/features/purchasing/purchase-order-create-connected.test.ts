import { describe, expect, it } from "vitest";
import type {
  BuyerSupplierProductLink,
  SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";
import {
  applyConnectedQuantityDelta,
  buildConnectedCategoryFacets,
  buildConnectedReadyProducts,
  connectedLinesViolateStock,
  CONNECTED_PO_CATEGORY_ALL,
  CONNECTED_PO_CATEGORY_OTHER,
  filterConnectedReadyProducts,
  formatLineMath,
  lineTotal,
  maxOrderablePurchaseQty,
  mergeConnectedStock,
  orderSubtotal,
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

  it("builds category facets with Other and filters with search together", () => {
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
      ],
    );

    const facets = buildConnectedCategoryFacets(products, { all: "All", other: "Other" });
    expect(facets).toEqual([
      { key: CONNECTED_PO_CATEGORY_ALL, label: "All", count: 3 },
      { key: "Beverages", label: "Beverages", count: 1 },
      { key: "Staples", label: "Staples", count: 1 },
      { key: CONNECTED_PO_CATEGORY_OTHER, label: "Other", count: 1 },
    ]);

    expect(filterConnectedReadyProducts(products, "", "Beverages")).toHaveLength(1);
    expect(filterConnectedReadyProducts(products, "", CONNECTED_PO_CATEGORY_OTHER)[0]?.productName).toBe(
      "Mystery Pack",
    );
    expect(filterConnectedReadyProducts(products, "water", "Beverages")).toHaveLength(1);
    expect(filterConnectedReadyProducts(products, "water", "Staples")).toHaveLength(0);
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

  it("enforces supplier stock max and blocks out-of-stock add", () => {
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
    expect(applyConnectedQuantityDelta([], out, 1)).toEqual([]);

    const limited = { ...base, stockTracked: true, availableBaseQuantity: 10 };
    expect(resolveSupplierAvailability(limited)).toEqual({ kind: "available", quantity: 10 });
    let lines = applyConnectedQuantityDelta([], limited, 1);
    lines = applyConnectedQuantityDelta(lines, limited, 20);
    expect(lines[0]?.orderedQty).toBe(10);
    expect(connectedLinesViolateStock(lines, [limited])).toBe(false);
    expect(connectedLinesViolateStock([{ ...lines[0]!, orderedQty: 11 }], [limited])).toBe(true);

    const cases = {
      ...base,
      multiplierToBase: 12,
      stockTracked: true,
      availableBaseQuantity: 24,
    };
    expect(maxOrderablePurchaseQty(cases)).toBe(2);
    lines = applyConnectedQuantityDelta([], cases, 1);
    lines = applyConnectedQuantityDelta(lines, cases, 5);
    expect(lines[0]?.orderedQty).toBe(2);

    const untracked = { ...base, stockTracked: false, availableBaseQuantity: null };
    expect(resolveSupplierAvailability(untracked)).toEqual({ kind: "untracked" });
    lines = applyConnectedQuantityDelta([], untracked, 1);
    lines = applyConnectedQuantityDelta(lines, untracked, 99);
    expect(lines[0]?.orderedQty).toBe(100);
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
        productId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        name: "Other",
        uom: "Piece",
        orderedQty: 1,
        unitPurchaseCost: 5,
      },
    ];
    expect(retainCompatibleDraftLines(lines, kept)).toEqual([lines[0]]);
  });
});

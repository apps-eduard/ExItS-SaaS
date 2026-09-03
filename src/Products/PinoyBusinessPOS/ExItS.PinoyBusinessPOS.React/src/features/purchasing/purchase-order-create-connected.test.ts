import { describe, expect, it } from "vitest";
import type {
  BuyerSupplierProductLink,
  SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";
import {
  applyConnectedQuantityDelta,
  buildConnectedReadyProducts,
  filterConnectedReadyProducts,
  formatLineMath,
  lineTotal,
  orderSubtotal,
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

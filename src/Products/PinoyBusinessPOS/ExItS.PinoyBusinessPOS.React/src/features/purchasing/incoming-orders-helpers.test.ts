import { describe, expect, it } from "vitest";
import type { ConnectedPurchaseOrder } from "@/api/pos/pos-connected-suppliers-client";
import {
  countIncomingLines,
  countIncomingOrdersByUiFilter,
  countIncomingUnits,
  filterIncomingOrdersBySearch,
  filterIncomingOrdersByUiStatus,
  formatIncomingLineMath,
  uiFilterToApiStatus,
} from "@/features/purchasing/incoming-orders-helpers";

function order(overrides: Partial<ConnectedPurchaseOrder> = {}): ConnectedPurchaseOrder {
  return {
    connectedPurchaseOrderId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    relationshipId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    buyerOrganizationId: "11111111-1111-4111-8111-111111111111",
    supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
    buyerPurchaseOrderId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    buyerPoNumber: "PO-000123",
    orderDate: "2026-09-04",
    notes: null,
    status: "New",
    totalAmount: 490,
    createdAtUtc: "2026-09-04T00:00:00Z",
    updatedAtUtc: "2026-09-04T00:00:00Z",
    lines: [
      {
        productId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        nameSnapshot: "Bottled Water 500ml",
        skuSnapshot: "PH-BEV-WATER-500",
        qty: 20,
        unitPriceSnapshot: 12,
        lineTotal: 240,
        unitOfMeasureCode: "Piece",
        availability: "Pending",
        proposedLineTotal: 0,
        confirmedLineTotal: 0,
      },
      {
        productId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        nameSnapshot: "Bath Soap Bar",
        skuSnapshot: "PH-SOAP-1",
        qty: 10,
        unitPriceSnapshot: 25,
        lineTotal: 250,
        unitOfMeasureCode: "Piece",
        availability: "Pending",
        proposedLineTotal: 0,
        confirmedLineTotal: 0,
      },
    ],
    displayStatus: "New",
    buyerDisplayName: "Paul Store",
    paymentTerm: "Cash",
    paymentTermLabel: "Cash",
    proposedTotalAmount: 0,
    confirmedTotalAmount: 0,
    ...overrides,
  };
}

describe("incoming-orders-helpers", () => {
  it("maps UI filters to domain statuses", () => {
    expect(uiFilterToApiStatus("pending")).toBe("New");
    expect(uiFilterToApiStatus("completed")).toBe("Fulfilled");
    expect(uiFilterToApiStatus("all")).toBeUndefined();
  });

  it("counts products/units and formats line math from PO snapshots", () => {
    const po = order();
    expect(countIncomingLines(po)).toBe(2);
    expect(countIncomingUnits(po)).toBe(30);
    expect(formatIncomingLineMath(20, 12, 240)).toBe("20 × ₱12 = ₱240");
  });

  it("filters by buyer name and PO number", () => {
    const rows = [
      order(),
      order({
        connectedPurchaseOrderId: "99999999-9999-4999-8999-999999999999",
        buyerPoNumber: "PO-000999",
        buyerDisplayName: "Other Mart",
      }),
    ];
    expect(filterIncomingOrdersBySearch(rows, "paul")).toHaveLength(1);
    expect(filterIncomingOrdersBySearch(rows, "000123")).toHaveLength(1);
    expect(filterIncomingOrdersBySearch(rows, "missing")).toHaveLength(0);
  });

  it("counts status tabs and filters by UI status", () => {
    const rows = [
      order({ status: "New" }),
      order({
        connectedPurchaseOrderId: "11111111-1111-4111-8111-111111111111",
        status: "New",
      }),
      order({
        connectedPurchaseOrderId: "22222222-2222-4222-8222-222222222222",
        status: "Accepted",
      }),
      order({
        connectedPurchaseOrderId: "33333333-3333-4333-8333-333333333333",
        status: "Preparing",
      }),
      order({
        connectedPurchaseOrderId: "44444444-4444-4444-8444-444444444444",
        status: "Fulfilled",
      }),
      order({
        connectedPurchaseOrderId: "55555555-5555-4555-8555-555555555555",
        status: "Declined",
      }),
      order({
        connectedPurchaseOrderId: "66666666-6666-4666-8666-666666666666",
        status: "Withdrawn",
      }),
    ];
    expect(countIncomingOrdersByUiFilter(rows)).toEqual({
      pending: 2,
      accepted: 1,
      preparing: 1,
      completed: 1,
      declined: 1,
    });
    expect(filterIncomingOrdersByUiStatus(rows, "pending")).toHaveLength(2);
    expect(filterIncomingOrdersByUiStatus(rows, "all")).toHaveLength(7);
  });
});

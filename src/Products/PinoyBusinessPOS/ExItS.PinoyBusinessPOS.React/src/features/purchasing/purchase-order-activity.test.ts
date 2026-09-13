import { describe, expect, it } from "vitest";
import {
  buildPurchaseOrderActivityEvents,
} from "@/features/purchasing/purchase-order-activity";
import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";

const purchaseOrderId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const orgId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const productId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

function basePo(overrides: Partial<PosPurchaseOrderDto> = {}): PosPurchaseOrderDto {
  return {
    purchaseOrderId,
    organizationId: orgId,
    supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
    supplierName: "ABC Trading",
    poNumber: "PO-0001",
    status: "PartiallyReceived",
    orderDate: "2026-08-27",
    orderedBy: "11111111-1111-4111-8111-111111111111",
    createdAtUtc: "2026-08-27T08:00:00Z",
    orderedAtUtc: "2026-08-27T09:00:00Z",
    updatedAtUtc: "2026-08-28T11:40:00Z",
    lines: [
      {
        lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        productId,
        lineNumber: 1,
        nameSnapshot: "Bath Soap",
        uomSnapshot: "Case",
        orderedQty: 10,
        unitPurchaseCost: 100,
        lineTotal: 1000,
        receivedQty: 4,
        outstandingQty: 6,
      },
    ],
    ...overrides,
  } as PosPurchaseOrderDto;
}

function receipt(
  id: string,
  grn: string,
  at: string,
  overrides: Partial<PosGoodsReceiptDto> = {},
): PosGoodsReceiptDto {
  return {
    goodsReceiptId: id,
    organizationId: orgId,
    purchaseOrderId,
    supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
    grnNumber: grn,
    receivedDate: at.slice(0, 10),
    deliveryReference: null,
    notes: null,
    receivedAtUtc: at,
    receivedBy: "22222222-2222-4222-8222-222222222222",
    status: "Posted",
    lines: [
      {
        lineId: `${id}-line`,
        purchaseOrderLineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        productId,
        lineNumber: 1,
        nameSnapshot: "Bath Soap",
        uomSnapshot: "Case",
        quantityReceived: 2,
        unitPurchaseCostSnapshot: 100,
        lineTotalSnapshot: 200,
        shortClosedQty: 0,
      },
    ],
    ...overrides,
  } as PosGoodsReceiptDto;
}

describe("buildPurchaseOrderActivityEvents", () => {
  it("orders multiple receipts chronologically", () => {
    const events = buildPurchaseOrderActivityEvents({
      po: basePo(),
      receipts: [
        receipt("r2", "GRN-2", "2026-08-29T10:00:00Z"),
        receipt("r1", "GRN-1", "2026-08-28T10:00:00Z"),
      ],
    });

    const receiptEvents = events.filter((e) => e.kind === "receipt");
    expect(receiptEvents.map((e) => e.grnNumber)).toEqual(["GRN-1", "GRN-2"]);
    expect(events.findIndex((e) => e.kind === "created")).toBeLessThan(
      events.findIndex((e) => e.kind === "submitted"),
    );
  });

  it("emits receipt_reversed when a receipt is voided", () => {
    const events = buildPurchaseOrderActivityEvents({
      po: basePo(),
      receipts: [
        receipt("r1", "GRN-1", "2026-08-28T10:00:00Z", {
          status: "Voided",
          voidedAtUtc: "2026-08-30T12:00:00Z",
          voidedByUserId: "11111111-1111-4111-8111-111111111111",
          voidReason: "Wrong qty",
        }),
      ],
    });

    expect(events.some((e) => e.kind === "receipt" && e.receiptResult === "reversed")).toBe(true);
    const reversed = events.find((e) => e.kind === "receipt_reversed");
    expect(reversed?.grnNumber).toBe("GRN-1");
    expect(reversed?.atUtc).toBe("2026-08-30T12:00:00Z");
  });

  it("emits completed when PO status is Received", () => {
    const events = buildPurchaseOrderActivityEvents({
      po: basePo({
        status: "Received",
        lines: [
          {
            lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            productId,
            lineNumber: 1,
            nameSnapshot: "Bath Soap",
            uomSnapshot: "Case",
            orderedQty: 10,
            unitPurchaseCost: 100,
            lineTotal: 1000,
            receivedQty: 10,
            outstandingQty: 0,
          },
        ],
      }),
      receipts: [
        receipt("r1", "GRN-1", "2026-08-28T10:00:00Z", {
          lines: [
            {
              lineId: "r1-line",
              purchaseOrderLineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
              productId,
              lineNumber: 1,
              nameSnapshot: "Bath Soap",
              uomSnapshot: "Case",
              quantityReceived: 10,
              unitPurchaseCostSnapshot: 100,
              lineTotalSnapshot: 1000,
            },
          ],
        }),
      ],
    });

    expect(events.some((e) => e.kind === "completed")).toBe(true);
    const lastReceipt = events.find((e) => e.kind === "receipt");
    expect(lastReceipt?.receiptResult).toBe("fully_received");
  });
});

import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  IDEMPOTENCY_KEY_HEADER,
  OPERATION_TYPE_HEADER,
  OFFLINE_OPERATION_TYPES,
  PAYLOAD_HASH_HEADER,
} from "@/api/pos/pos-mutation-idempotency";
import {
  assertNotStockTouchingUrl,
  cancelPurchaseOrder,
  createPurchaseOrder,
  listGoodsReceiptsForPurchaseOrder,
  NON_STOCK_PURCHASE_ORDER_METHODS,
  receivePurchaseOrder,
  STOCK_TOUCHING_PURCHASE_ORDER_METHODS,
  submitPurchaseOrder,
} from "@/api/pos/pos-purchase-orders-client";
import { createDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import {
  buildReceivePlan,
  outstandingAfterPrior,
  parseNonNegativeQty,
} from "@/features/purchasing/receive-math";
import {
  canManageInventory,
  canManagePurchasing,
  canViewPurchasing,
} from "@/access/pos-capabilities";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const poId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const supplierId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const productId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const lineId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const grnId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function poDto(overrides: Record<string, unknown> = {}) {
  return {
    purchaseOrderId: poId,
    organizationId: workspace.organizationId,
    poNumber: "PO-1",
    supplierId,
    status: "Draft",
    orderDate: "2026-08-21",
    expectedDeliveryDate: null,
    supplierReference: null,
    notes: null,
    orderedAtUtc: null,
    orderedBy: null,
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
    lines: [
      {
        lineId,
        productId,
        lineNumber: 1,
        nameSnapshot: "Rice",
        uomSnapshot: "kg",
        orderedQty: 10,
        unitPurchaseCost: 50,
        lineTotal: 500,
        receivedQty: 0,
        outstandingQty: 10,
        lineNotes: null,
        closedShortQty: 0,
      },
    ],
    displayStatus: "Draft",
    canReceiveConnected: true,
    supplierName: "Island Wholesale",
    ...overrides,
  };
}

describe("RMAP-17 purchasing clients", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("documents receive as the only stock-touching PO client method", () => {
    expect(STOCK_TOUCHING_PURCHASE_ORDER_METHODS).toEqual(["receivePurchaseOrder"]);
    expect(NON_STOCK_PURCHASE_ORDER_METHODS).not.toContain("receivePurchaseOrder");
  });

  it("create/submit/cancel never call stock-touching URLs", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(jsonResponse(poDto(), 201))
      .mockResolvedValueOnce(jsonResponse(poDto({ status: "Ordered" })))
      .mockResolvedValueOnce(jsonResponse(poDto({ status: "Cancelled" })));

    await createPurchaseOrder(workspace, {
      purchaseOrderId: poId,
      supplierId,
      orderDate: "2026-08-21",
      lines: [{ productId, orderedQty: 10, unitPurchaseCost: 50 }],
    });
    await submitPurchaseOrder(workspace, poId);
    await cancelPurchaseOrder(workspace, poId);

    const urls = vi.mocked(fetch).mock.calls.map((c) => String(c[0]));
    expect(urls[0]).toContain("/purchase-orders");
    expect(urls[1]).toContain(`/purchase-orders/${poId}/submit`);
    expect(urls[2]).toContain(`/purchase-orders/${poId}/cancel`);
    for (const url of urls) {
      assertNotStockTouchingUrl(url);
    }

    const createInit = vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit;
    const createHeaders = new Headers(createInit.headers);
    expect(createHeaders.get(IDEMPOTENCY_KEY_HEADER)).toBe(poId.replace(/-/g, ""));
    expect(createHeaders.get(OPERATION_TYPE_HEADER)).toBe(OFFLINE_OPERATION_TYPES.PurchaseOrderCreate);
  });

  it("create sends purchase_order.create idempotency headers with client purchaseOrderId", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(jsonResponse(poDto(), 201));
    await createPurchaseOrder(workspace, {
      purchaseOrderId: poId,
      supplierId,
      orderDate: "2026-08-21",
      lines: [{ productId, orderedQty: 10, unitPurchaseCost: 50 }],
    });
    const init = vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get(IDEMPOTENCY_KEY_HEADER)).toBe(poId.replace(/-/g, ""));
    expect(headers.get(PAYLOAD_HASH_HEADER)).toMatch(/^[a-f0-9]{64}$/);
    expect(headers.get(OPERATION_TYPE_HEADER)).toBe(OFFLINE_OPERATION_TYPES.PurchaseOrderCreate);
    const body = JSON.parse(String(init.body));
    expect(body.purchaseOrderId).toBe(poId);
  });

  it("submit sends purchase_order.submit idempotency headers", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(jsonResponse(poDto({ status: "Ordered" })));
    await submitPurchaseOrder(workspace, poId);
    const init = vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get(IDEMPOTENCY_KEY_HEADER)).toBe(poId.replace(/-/g, ""));
    expect(headers.get(PAYLOAD_HASH_HEADER)).toMatch(/^[a-f0-9]{64}$/);
    expect(headers.get(OPERATION_TYPE_HEADER)).toBe(OFFLINE_OPERATION_TYPES.PurchaseOrderSubmit);
  });

  it("receive is the stock-touching path and sends goodsReceiptId idempotency", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        {
          goodsReceiptId: grnId,
          organizationId: workspace.organizationId,
          purchaseOrderId: poId,
          supplierId,
          grnNumber: "GRN-1",
          receivedDate: "2026-08-21",
          deliveryReference: null,
          notes: null,
          receivedAtUtc: "2026-08-21T01:00:00Z",
          receivedBy: "99999999-9999-4999-8999-999999999999",
          lines: [
            {
              lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
              purchaseOrderLineId: lineId,
              productId,
              lineNumber: 1,
              nameSnapshot: "Rice",
              uomSnapshot: "kg",
              quantityReceived: 4,
              unitPurchaseCostSnapshot: 50,
              lineTotalSnapshot: 200,
              inventoryMovementId: "11111111-2222-4333-8444-555555555555",
            },
          ],
        },
        201,
      ),
    );

    await receivePurchaseOrder(workspace, poId, {
      goodsReceiptId: grnId,
      lines: [
        {
          productId,
          receiveQty: 4,
          expiryDate: "2027-12-30",
          lotNumber: "LOT-A123",
        },
      ],
    });

    const url = String(vi.mocked(fetch).mock.calls[0]?.[0]);
    expect(url).toContain(`/purchase-orders/${poId}/receive`);
    const init = vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get(IDEMPOTENCY_KEY_HEADER)).toBe(grnId.replace(/-/g, ""));
    expect(headers.get(OPERATION_TYPE_HEADER)).toBe(OFFLINE_OPERATION_TYPES.PurchaseOrderReceive);
    const body = JSON.parse(String(init.body)) as {
      goodsReceiptId: string;
      lines: Array<{ expiryDate?: string; lotNumber?: string; receiveQty: number }>;
    };
    expect(body.goodsReceiptId).toBe(grnId);
    expect(body.lines[0]?.receiveQty).toBe(4);
    expect(body.lines[0]?.expiryDate).toBe("2027-12-30");
    expect(body.lines[0]?.lotNumber).toBe("LOT-A123");
  });

  it("includes enableTrackingIfNeeded in receive body when true", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        {
          goodsReceiptId: grnId,
          organizationId: workspace.organizationId,
          purchaseOrderId: poId,
          supplierId,
          grnNumber: "GRN-2",
          receivedDate: "2026-08-21",
          deliveryReference: null,
          notes: null,
          receivedAtUtc: "2026-08-21T01:00:00Z",
          receivedBy: "99999999-9999-4999-8999-999999999999",
          lines: [
            {
              lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
              purchaseOrderLineId: lineId,
              productId,
              lineNumber: 1,
              nameSnapshot: "Rice",
              uomSnapshot: "kg",
              quantityReceived: 4,
              unitPurchaseCostSnapshot: 50,
              lineTotalSnapshot: 200,
              inventoryTrackingEnabled: true,
              previousTrackedStock: null,
              newTrackedStock: 4,
            },
          ],
        },
        201,
      ),
    );

    const receipt = await receivePurchaseOrder(workspace, poId, {
      goodsReceiptId: grnId,
      enableTrackingIfNeeded: true,
      lines: [{ productId, receiveQty: 4 }],
    });

    const init = vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit;
    const body = JSON.parse(String(init.body)) as {
      enableTrackingIfNeeded?: boolean;
    };
    expect(body.enableTrackingIfNeeded).toBe(true);
    expect(receipt.lines[0]?.inventoryTrackingEnabled).toBe(true);
    expect(receipt.lines[0]?.newTrackedStock).toBe(4);
  });

  it("omits enableTrackingIfNeeded from receive body when false or unset", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        {
          goodsReceiptId: grnId,
          organizationId: workspace.organizationId,
          purchaseOrderId: poId,
          supplierId,
          grnNumber: "GRN-3",
          receivedDate: "2026-08-21",
          receivedAtUtc: "2026-08-21T01:00:00Z",
          receivedBy: "99999999-9999-4999-8999-999999999999",
          lines: [
            {
              lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
              purchaseOrderLineId: lineId,
              productId,
              lineNumber: 1,
              nameSnapshot: "Rice",
              uomSnapshot: "kg",
              quantityReceived: 4,
              unitPurchaseCostSnapshot: 50,
              lineTotalSnapshot: 200,
            },
          ],
        },
        201,
      ),
    );

    await receivePurchaseOrder(workspace, poId, {
      goodsReceiptId: grnId,
      enableTrackingIfNeeded: false,
      lines: [{ productId, receiveQty: 4 }],
    });

    const init = vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit;
    const body = JSON.parse(String(init.body)) as Record<string, unknown>;
    expect(body).not.toHaveProperty("enableTrackingIfNeeded");
  });

  it("lists goods receipts for a purchase order", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse([
        {
          goodsReceiptId: grnId,
          organizationId: workspace.organizationId,
          purchaseOrderId: poId,
          supplierId,
          grnNumber: "GRN-1",
          receivedDate: "2026-08-21",
          receivedAtUtc: "2026-08-21T01:00:00Z",
          receivedBy: "99999999-9999-4999-8999-999999999999",
          lines: [
            {
              lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
              purchaseOrderLineId: lineId,
              productId,
              lineNumber: 1,
              nameSnapshot: "Rice",
              uomSnapshot: "kg",
              quantityReceived: 4,
              unitPurchaseCostSnapshot: 50,
              lineTotalSnapshot: 200,
            },
          ],
        },
      ]),
    );

    const receipts = await listGoodsReceiptsForPurchaseOrder(workspace, poId);
    expect(receipts).toHaveLength(1);
    expect(receipts[0]?.grnNumber).toBe("GRN-1");
    expect(String(vi.mocked(fetch).mock.calls[0]?.[0])).toContain(
      `purchaseOrderId=${poId}`,
    );
    expect(NON_STOCK_PURCHASE_ORDER_METHODS).toContain("listGoodsReceiptsForPurchaseOrder");
  });

  it("direct purchase create sends body idempotencyKey", async () => {
    const key = "12121212-1212-4121-8121-121212121212";
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        {
          directPurchaseReceiptId: "13131313-1313-4131-8131-131313131313",
          organizationId: workspace.organizationId,
          receiptNumber: "DPR-1",
          purchaseDate: "2026-08-21",
          supplierId: null,
          sourceNameSnapshot: "Market",
          referenceNumber: null,
          notes: null,
          totalCost: 100,
          createdByUserId: "99999999-9999-4999-8999-999999999999",
          createdAtUtc: "2026-08-21T02:00:00Z",
          lines: [
            {
              lineId: "14141414-1414-4141-8141-141414141414",
              productId,
              lineNumber: 1,
              productNameSnapshot: "Rice",
              skuSnapshot: null,
              unitOfMeasure: "kg",
              quantity: 2,
              unitCost: 50,
              lineTotal: 100,
              expiryDate: "2026-12-01",
              lotNumber: "L1",
              inventoryMovementId: "15151515-1515-4151-8151-151515151515",
            },
          ],
        },
        201,
      ),
    );

    await createDirectPurchaseReceipt(workspace, {
      purchaseDate: "2026-08-21",
      sourceName: "Market",
      idempotencyKey: key,
      lines: [
        {
          productId,
          quantity: 2,
          unitCost: 50,
          expiryDate: "2026-12-01",
          lotNumber: "L1",
        },
      ],
    });

    const url = String(vi.mocked(fetch).mock.calls[0]?.[0]);
    expect(url).toContain("/direct-purchase-receipts");
    const body = JSON.parse(String((vi.mocked(fetch).mock.calls[0]?.[1] as RequestInit).body));
    expect(body.idempotencyKey).toBe(key);
    expect(body.lines[0].expiryDate).toBe("2026-12-01");
  });
});

describe("partial receive math", () => {
  it("computes outstanding and denies over-receipt", () => {
    expect(outstandingAfterPrior(10, 4)).toBe(6);
    expect(parseNonNegativeQty("")).toBe(0);
    expect(parseNonNegativeQty("-1")).toBeNull();

    const over = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 4,
        damagedQty: 2,
        closeRemaining: false,
      },
    ]);
    expect(over.ok).toBe(false);
    if (!over.ok) {
      expect(over.error).toBe("over_receive");
    }

    const ok = buildReceivePlan([
      {
        productId,
        outstandingQty: 10,
        goodQty: 4,
        damagedQty: 1,
        closeRemaining: true,
      },
    ]);
    expect(ok.ok).toBe(true);
    if (ok.ok) {
      expect(ok.lines[0]?.receiveQty).toBe(4);
      expect(ok.lines[0]?.damagedQty).toBe(1);
      expect(ok.lines[0]?.shortClosedQty).toBe(5);
      expect(ok.lines[0]?.discrepancyKind).toBe("Damaged");
    }
  });
});

describe("purchasing capabilities", () => {
  function grant(role: string) {
    return {
      productAccessAllowed: true,
      mappedPosRoleCode: role,
      productLocalRoleCode: role,
      membershipRole: role === "Owner" ? "OrganizationOwner" : "OrganizationMember",
      organizationManagementAuthority: role === "Owner",
    };
  }

  it("gates view/manage purchasing and inventory receive", () => {
    expect(canViewPurchasing(grant("Owner"))).toBe(true);
    expect(canViewPurchasing(grant("StoreManager"))).toBe(true);
    expect(canViewPurchasing(grant("Cashier"))).toBe(false);
    expect(canManagePurchasing(grant("InventoryStaff"))).toBe(true);
    expect(canManagePurchasing(grant("ReportingUser"))).toBe(false);
    expect(canManageInventory(grant("Owner"))).toBe(true);
    expect(canManageInventory(grant("Cashier"))).toBe(false);
  });
});

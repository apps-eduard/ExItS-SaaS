import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createSaleReturn,
  estimateLineRefundAmount,
  estimateTotalRefundAmount,
  formatRefundMethodLabel,
  getRefundableSale,
  getSaleReturn,
  isCashShiftRequiredError,
  isStaleReturnConflict,
  listSaleReturns,
} from "@/api/pos/pos-sale-returns-client";
import { PosApiError } from "@/api/pos/pos-http";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const saleId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const returnId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const saleLineId = "99999999-9999-4999-8999-999999999999";
const productId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const actorId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

function refundableJson(extra: Record<string, unknown> = {}) {
  return {
    saleId,
    saleNumber: "S-9001",
    paymentMethod: "Cash",
    status: "Completed",
    lines: [
      {
        saleLineId,
        productId,
        productNameSnapshot: "Coke",
        unitOfMeasure: "Piece",
        sellingMode: "PerItem",
        originalQuantity: 10,
        unitPriceSnapshot: 10,
        originalLineTotal: 80,
        previouslyReturnedQuantity: 0,
        refundableQuantity: 10,
        previouslyRefundedAmount: 0,
        refundableAmount: 80,
      },
    ],
    ...extra,
  };
}

function returnJson(extra: Record<string, unknown> = {}) {
  return {
    returnId,
    organizationId: workspace.organizationId,
    returnNumber: "R-1001",
    saleId,
    refundMethod: "Cash",
    status: "Completed",
    returnDate: "2026-08-21",
    reason: "Customer changed mind",
    notes: null,
    totalRefundAmount: 40,
    createdAtUtc: "2026-08-21T03:00:00Z",
    createdBy: actorId,
    completedAtUtc: "2026-08-21T03:00:00Z",
    cashierShiftId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    lines: [
      {
        saleReturnLineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        saleLineId,
        productId,
        productNameSnapshot: "Coke",
        unitOfMeasure: "Piece",
        quantityReturned: 5,
        unitPriceSnapshot: 10,
        refundAmount: 40,
        restockDisposition: "ReturnToStock",
        lineReason: null,
        inventoryMovementId: null,
      },
    ],
    ...extra,
  };
}

describe("pos-sale-returns-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("gets refundable sale snapshot", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify(refundableJson()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const refundable = await getRefundableSale(workspace, saleId);
    expect(refundable.saleNumber).toBe("S-9001");
    expect(refundable.lines[0]?.refundableQuantity).toBe(10);

    const [url] = vi.mocked(fetch).mock.calls[0]!;
    expect(String(url)).toContain(`/api/v1/pos/sale-returns/refundable/${saleId}`);
  });

  it("posts create return with returnId and dispositions", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify(returnJson()), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const created = await createSaleReturn(workspace, {
      saleId,
      reason: "Damaged packaging",
      notes: "Shelf check",
      returnId,
      lines: [
        {
          saleLineId,
          quantity: 5,
          restockDisposition: "DoNotRestock",
          lineReason: "Opened",
        },
      ],
    });

    expect(created.returnNumber).toBe("R-1001");
    expect(created.totalRefundAmount).toBe(40);

    const [, init] = vi.mocked(fetch).mock.calls[0]!;
    const body = JSON.parse(String(init?.body));
    expect(body).toEqual({
      saleId,
      reason: "Damaged packaging",
      notes: "Shelf check",
      returnId,
      lines: [
        {
          saleLineId,
          quantity: 5,
          restockDisposition: "DoNotRestock",
          lineReason: "Opened",
        },
      ],
    });
  });

  it("lists and gets returns", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            items: [returnJson()],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(returnJson({ refundMethod: "ManualGCash" })), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );

    const page = await listSaleReturns(workspace, { saleId, page: 1 });
    expect(page.items).toHaveLength(1);

    const detail = await getSaleReturn(workspace, returnId);
    expect(formatRefundMethodLabel(detail.refundMethod)).toBe("GCash");
    expect(detail.refundMethod).toBe("ManualGCash");
  });

  it("estimates cumulative NET refund without UnitPrice", () => {
    // Discounted 10 qty @ net 80 — half return → 40
    expect(
      estimateLineRefundAmount({
        originalQuantity: 10,
        originalLineTotal: 80,
        previouslyReturnedQuantity: 0,
        previouslyRefundedAmount: 0,
        requestedQty: 5,
      }),
    ).toBe(40);

    // Final slice absorbs remainder (backend domain tests pattern)
    const first = estimateLineRefundAmount({
      originalQuantity: 3,
      originalLineTotal: 10,
      previouslyReturnedQuantity: 0,
      previouslyRefundedAmount: 0,
      requestedQty: 1,
    });
    expect(first).toBe(3.33);

    const second = estimateLineRefundAmount({
      originalQuantity: 3,
      originalLineTotal: 10,
      previouslyReturnedQuantity: 1,
      previouslyRefundedAmount: 3.33,
      requestedQty: 1,
    });
    expect(second).toBe(3.34);

    const final = estimateLineRefundAmount({
      originalQuantity: 3,
      originalLineTotal: 10,
      previouslyReturnedQuantity: 2,
      previouslyRefundedAmount: 6.67,
      requestedQty: 1,
    });
    expect(final).toBe(3.33);

    expect(
      estimateTotalRefundAmount([
        {
          originalQuantity: 10,
          originalLineTotal: 80,
          previouslyReturnedQuantity: 0,
          previouslyRefundedAmount: 0,
          requestedQty: 5,
        },
      ]),
    ).toBe(40);
  });

  it("detects stale conflict and cash-shift errors", () => {
    expect(
      isStaleReturnConflict(
        new PosApiError(409, { errorCode: "pos.concurrency_conflict", detail: "Conflict" }),
      ),
    ).toBe(true);
    expect(
      isStaleReturnConflict(
        new PosApiError(400, {
          errorCode: "pos.sale_return.quantity.exceeds_refundable",
          detail: "Qty",
        }),
      ),
    ).toBe(true);
    expect(
      isCashShiftRequiredError(
        new PosApiError(409, {
          errorCode: "pos.cashier_shift.no_open_shift",
          detail: "Cash refunds require an open cashier shift",
        }),
      ),
    ).toBe(true);
    expect(
      isStaleReturnConflict(
        new PosApiError(409, {
          errorCode: "pos.cashier_shift.no_open_shift",
          detail: "Cash refunds require an open cashier shift",
        }),
      ),
    ).toBe(false);
  });
});

import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  acceptReturnBatch,
  finalizeReturnBatch,
  getReturnBatchReviewPreview,
  listReturnBatches,
} from "@/api/pos/pos-return-batches-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const saleId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const returnBatchId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const lineId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const productId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";

function batchJson(overrides: Record<string, unknown> = {}) {
  return {
    returnBatchId,
    organizationId: workspace.organizationId,
    saleId,
    branchId: workspace.branchId,
    batchNumber: "RB-1001",
    status: "PendingInspection",
    refundStatus: "RefundDue",
    acceptedReturnValue: 40,
    refundDueAmount: 40,
    refundedAmount: 0,
    saleReturnId: null,
    reason: "Damaged packaging",
    notes: null,
    createdAtUtc: "2026-09-18T08:00:00Z",
    createdBy: "ffffffff-ffff-4fff-8fff-ffffffffffff",
    updatedAtUtc: "2026-09-18T08:00:00Z",
    finalizedAtUtc: null,
    finalizedBy: null,
    financialSummary: {
      originalSaleTotal: 100,
      acceptedReturnValue: 40,
      amountPreviouslyPaid: 100,
      obligationReduced: 0,
      remainingAmountDue: 0,
      refundDue: 40,
      refunded: 0,
      refundRemaining: 40,
      returnedQuantity: 2,
      sellableQuantity: 0,
      damagedQuantity: 0,
    },
    lines: [
      {
        returnBatchLineId: lineId,
        saleLineId: "99999999-9999-4999-8999-999999999999",
        productId,
        productNameSnapshot: "Coke",
        unitOfMeasure: "Piece",
        unitPriceSnapshot: 20,
        lineTotalSnapshot: 40,
        acceptedQuantity: 2,
        refundAmountSnapshot: 40,
        sellableQuantity: null,
        damagedQuantity: null,
        inspectionNote: null,
        classifiedAtUtc: null,
        classifiedBy: null,
      },
    ],
    refunds: [],
    ...overrides,
  };
}

describe("pos-return-batches-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("accepts a return batch", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify(batchJson()), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const result = await acceptReturnBatch(workspace, {
      saleId,
      reason: "Damaged packaging",
      returnBatchId,
      lines: [{ saleLineId: "99999999-9999-4999-8999-999999999999", acceptedQuantity: 2 }],
    });
    expect(result.returnBatchId).toBe(returnBatchId);
  });

  it("lists and reviews return batches by sale", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        new Response(JSON.stringify([batchJson()]), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(batchJson()), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );

    const listed = await listReturnBatches(workspace, { saleId });
    expect(listed).toHaveLength(1);

    const review = await getReturnBatchReviewPreview(workspace, returnBatchId);
    expect(review.batchNumber).toBe("RB-1001");
  });

  it("finalizes return batch", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify(
          batchJson({
            status: "Finalized",
            finalizedAtUtc: "2026-09-18T09:00:00Z",
          }),
        ),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    const finalized = await finalizeReturnBatch(workspace, returnBatchId, {
      expectedUpdatedAtUtc: "2026-09-18T08:00:00Z",
    });
    expect(finalized.status).toBe("Finalized");
  });
});

import { describe, expect, it } from "vitest";
import {
  buildWarehouseAttentionItems,
  classifyTransferBuckets,
  stockRequestStatusCounts,
  summarizeMovementTypes,
  topDestinationsFromOutgoing,
  topMovedProductsFromRows,
} from "@/features/reports/warehouse-dashboard-helpers";

describe("warehouse-dashboard-helpers", () => {
  it("classifies outgoing and incoming transfer buckets", () => {
    const outgoing = classifyTransferBuckets(
      [
        { transferId: "1", sourceBranchId: "a", destinationBranchId: "b", status: "Draft", totalSentQty: 1 },
        { transferId: "2", sourceBranchId: "a", destinationBranchId: "b", status: "InTransit", totalSentQty: 2 },
      ],
      "outgoing",
    );
    expect(outgoing.awaitingDispatch).toBe(1);
    expect(outgoing.inTransit).toBe(1);

    const incoming = classifyTransferBuckets(
      [
        { transferId: "3", sourceBranchId: "a", destinationBranchId: "b", status: "InTransit", totalSentQty: 3 },
        { transferId: "4", sourceBranchId: "a", destinationBranchId: "b", status: "PartiallyReceived", totalSentQty: 4 },
      ],
      "incoming",
    );
    expect(incoming.incomingToReceive).toBe(2);
    expect(incoming.partiallyReceived).toBe(1);
  });

  it("ranks destinations from outgoing transfer qty", () => {
    const top = topDestinationsFromOutgoing([
      {
        transferId: "1",
        sourceBranchId: "w",
        destinationBranchId: "r1",
        destinationBranchName: "Pac Passi",
        status: "Received",
        totalSentQty: 100,
      },
      {
        transferId: "2",
        sourceBranchId: "w",
        destinationBranchId: "r1",
        destinationBranchName: "Pac Passi",
        status: "InTransit",
        totalSentQty: 50,
      },
      {
        transferId: "3",
        sourceBranchId: "w",
        destinationBranchId: "r2",
        destinationBranchName: "Pac Sara",
        status: "Cancelled",
        totalSentQty: 999,
      },
    ]);
    expect(top[0]).toMatchObject({ name: "Pac Passi", units: 150 });
    expect(top.some((d) => d.name === "Pac Sara")).toBe(false);
  });

  it("summarizes movement types without inventing sales", () => {
    const summary = summarizeMovementTypes([
      { movementType: "PurchaseReceive", quantityTotal: 40, count: 2 },
      { movementType: "TransferOut", quantityTotal: -12, count: 1 },
      { movementType: "Sale", quantityTotal: -999, count: 9 },
    ]);
    expect(summary.receivedFromSuppliers).toBe(40);
    expect(summary.transferOut).toBe(-12);
  });

  it("aggregates top moved products by direction", () => {
    const outbound = topMovedProductsFromRows(
      [
        { productId: "p1", productName: "Rice", quantityEffect: -10 },
        { productId: "p1", productName: "Rice", quantityEffect: -5 },
        { productId: "p2", productName: "Water", quantityEffect: 8 },
      ],
      "outbound",
    );
    expect(outbound).toEqual([{ productId: "p1", productName: "Rice", quantity: 15 }]);
  });

  it("counts stock request statuses and builds non-zero attention only", () => {
    expect(
      stockRequestStatusCounts([
        { stockRequestId: "1", status: "Pending", lineCount: 1 },
        { stockRequestId: "2", status: "Fulfilled", lineCount: 1 },
      ]),
    ).toEqual({ pending: 1, inProgress: 0, partiallyFulfilled: 0, fulfilled: 1 });

    expect(
      buildWarehouseAttentionItems({
        lowStock: 0,
        expiry: 0,
        awaitingDispatch: 0,
        incomingToReceive: 0,
        receivablePos: 0,
        pendingStockRequests: 0,
      }),
    ).toEqual([]);

    expect(
      buildWarehouseAttentionItems({
        lowStock: 2,
        expiry: 0,
        awaitingDispatch: 0,
        incomingToReceive: 0,
        receivablePos: 0,
        pendingStockRequests: 3,
      }).map((i) => i.key),
    ).toEqual(["lowStock", "stockRequests"]);
  });
});

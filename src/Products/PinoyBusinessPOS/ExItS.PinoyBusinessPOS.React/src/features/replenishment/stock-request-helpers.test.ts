import { describe, expect, it } from "vitest";
import {
  canCancelStockRequestAsDestination,
  canDispatchRemainingStockRequest,
  canFulfillRemaining,
  canPrepareTransfer,
  filterStockRequestsByTab,
  findOpenCoveringTransfer,
  hasConfiguredInternalSource,
  openCoveringTransferMessage,
  pickPreferredSourceId,
  prepareTransferPrimaryLabelKey,
  remainingRequestQty,
  stockRequestMatchesTab,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
  totalRemainingToDispatch,
} from "@/features/replenishment/stock-request-helpers";

describe("stock-request-helpers", () => {
  it("prefers preferred active source and falls back to first active", () => {
    expect(
      pickPreferredSourceId([
        { sourceLocationId: "a", isPreferred: false, isActive: true },
        { sourceLocationId: "b", isPreferred: true, isActive: true },
      ]),
    ).toBe("b");
    expect(
      pickPreferredSourceId([{ sourceLocationId: "a", isPreferred: false, isActive: true }]),
    ).toBe("a");
    expect(pickPreferredSourceId([{ sourceLocationId: "a", isPreferred: true, isActive: false }])).toBe(
      null,
    );
  });

  it("computes remaining to dispatch as approved − received − open in transit − waived", () => {
    expect(remainingRequestQty(100, 70, 30)).toBe(0);
    expect(remainingRequestQty(100, 70, 0)).toBe(30);
    expect(remainingRequestQty(100, 70, 0, 30)).toBe(0);
    expect(remainingRequestQty(10, 0, 6)).toBe(4);
    expect(remainingRequestQty(10, 6, 0)).toBe(4);
    expect(remainingRequestQty(10, 10, 0)).toBe(0);
  });

  it("allows prepare when remaining is positive even with open in-transit cover", () => {
    const lines = [{ remainingToDispatchQuantity: 30 }];
    expect(canPrepareTransfer("PartiallyFulfilled", lines, false)).toBe(true);
    expect(canPrepareTransfer("PartiallyFulfilled", lines, true)).toBe(true);
    expect(canPrepareTransfer("PartiallyFulfilled", [{ remainingToDispatchQuantity: 0 }], true)).toBe(
      false,
    );
    expect(canFulfillRemaining("PartiallyFulfilled", lines, false)).toBe(true);
    expect(canFulfillRemaining("Approved", lines, false)).toBe(false);
    expect(
      canDispatchRemainingStockRequest("PartiallyFulfilled", [{ remainingToDispatchQuantity: 0 }]),
    ).toBe(false);
    expect(canDispatchRemainingStockRequest("Fulfilled", [{ remainingToDispatchQuantity: 10 }])).toBe(
      false,
    );
  });

  it("sums remaining to dispatch and picks prepare button labels", () => {
    expect(
      totalRemainingToDispatch([
        { remainingToDispatchQuantity: 10 },
        { remainingToDispatchQuantity: 5 },
      ]),
    ).toBe(15);
    expect(prepareTransferPrimaryLabelKey("Approved", false)).toBe("stockRequest.reviewPrepareTransfer");
    expect(prepareTransferPrimaryLabelKey("PartiallyFulfilled", false)).toBe(
      "stockRequest.fulfillRemaining",
    );
    expect(prepareTransferPrimaryLabelKey("PartiallyFulfilled", true)).toBe(
      "stockRequest.continueTransferPreparation",
    );
  });

  it("findOpenCoveringTransfer exposes transfer id for view link", () => {
    expect(
      findOpenCoveringTransfer([
        {
          transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          transferNumber: "TR-1",
          status: "InTransit",
          totalOutstandingQty: 12,
        },
      ]),
    ).toEqual({
      transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferLabel: "TR-1",
      outstandingQty: 12,
    });
  });

  it("surfaces open covering transfer for replacement guard copy", () => {
    expect(
      openCoveringTransferMessage([
        {
          transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          transferNumber: "260922-001",
          status: "PartiallyReceived",
          totalOutstandingQty: 30,
        },
      ]),
    ).toEqual({ transferLabel: "260922-001", outstandingQty: 30 });
  });

  it("detects no configured internal source", () => {
    expect(hasConfiguredInternalSource([])).toBe(false);
    expect(hasConfiguredInternalSource([{ isActive: false }])).toBe(false);
    expect(hasConfiguredInternalSource([{ isActive: true }])).toBe(true);
  });

  it("maps retail tabs to statuses", () => {
    expect(stockRequestMatchesTab("Pending", "submitted", "retail")).toBe(true);
    expect(stockRequestMatchesTab("Approved", "inProgress", "retail")).toBe(true);
    expect(stockRequestMatchesTab("Preparing", "inProgress", "retail")).toBe(true);
    expect(stockRequestMatchesTab("InProgress", "inProgress", "retail")).toBe(true);
    expect(stockRequestMatchesTab("InTransit", "inTransit", "retail")).toBe(true);
    expect(stockRequestMatchesTab("Fulfilled", "completed", "retail")).toBe(true);
    expect(stockRequestMatchesTab("Rejected", "completed", "retail")).toBe(true);
    expect(stockRequestMatchesTab("Pending", "all", "retail")).toBe(true);
    expect(stockRequestMatchesTab("Pending", "inTransit", "retail")).toBe(false);
  });

  it("maps warehouse tabs to statuses", () => {
    expect(stockRequestMatchesTab("Pending", "incoming", "warehouse")).toBe(true);
    expect(stockRequestMatchesTab("Approved", "preparing", "warehouse")).toBe(true);
    expect(stockRequestMatchesTab("InTransit", "dispatched", "warehouse")).toBe(true);
    expect(stockRequestMatchesTab("PartiallyFulfilled", "dispatched", "warehouse")).toBe(true);
    expect(stockRequestMatchesTab("PartiallyFulfilled", "history", "warehouse")).toBe(false);
    expect(stockRequestMatchesTab("Cancelled", "history", "warehouse")).toBe(true);
    expect(stockRequestMatchesTab("Pending", "history", "warehouse")).toBe(false);
  });

  it("filters list items by tab", () => {
    const items = [
      { stockRequestId: "1", status: "Pending" },
      { stockRequestId: "2", status: "Preparing" },
      { stockRequestId: "3", status: "InTransit" },
      { stockRequestId: "4", status: "Fulfilled" },
    ];
    expect(filterStockRequestsByTab(items, "submitted", "retail").map((i) => i.stockRequestId)).toEqual([
      "1",
    ]);
    expect(filterStockRequestsByTab(items, "preparing", "warehouse").map((i) => i.stockRequestId)).toEqual([
      "2",
    ]);
    expect(filterStockRequestsByTab(items, "all", "retail")).toHaveLength(4);
  });

  it("provides status label keys and tones", () => {
    expect(stockRequestStatusLabelKey("InProgress")).toBe("stockRequest.status.preparing");
    expect(stockRequestStatusTone("Pending")).toBe("warning");
    expect(stockRequestStatusTone("Fulfilled")).toBe("success");
    expect(stockRequestStatusTone("Rejected")).toBe("danger");
  });

  it("allows destination cancel only before dispatch", () => {
    expect(canCancelStockRequestAsDestination("Pending")).toBe(true);
    expect(canCancelStockRequestAsDestination("Approved")).toBe(true);
    expect(canCancelStockRequestAsDestination("Preparing")).toBe(true);
    expect(canCancelStockRequestAsDestination("InTransit")).toBe(false);
  });
});

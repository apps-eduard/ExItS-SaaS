import { describe, expect, it } from "vitest";
import {
  canCancelStockRequestAsDestination,
  filterStockRequestsByTab,
  hasConfiguredInternalSource,
  pickPreferredSourceId,
  remainingRequestQty,
  stockRequestMatchesTab,
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
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

  it("computes remaining qty from fulfilled and in-progress", () => {
    expect(remainingRequestQty(10, 0, 6)).toBe(4);
    expect(remainingRequestQty(10, 6, 0)).toBe(4);
    expect(remainingRequestQty(10, 10, 0)).toBe(0);
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

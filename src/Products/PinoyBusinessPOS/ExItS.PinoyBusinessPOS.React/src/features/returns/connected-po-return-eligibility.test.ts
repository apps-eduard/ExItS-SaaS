import { describe, expect, it } from "vitest";
import { describeConnectedPoReturnLineEligibility } from "@/features/returns/connected-po-return-eligibility";

describe("describeConnectedPoReturnLineEligibility", () => {
  it("shows eligible quantity and expiry", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 6,
      returnsAllowed: true,
      earliestReturnExpiresAtUtc: "2026-09-28T00:00:00Z",
      latestReturnExpiresAtUtc: "2026-09-28T00:00:00Z",
      lineBlockedReason: null,
    });
    expect(view.status).toBe("eligible");
    expect(view.availableQty).toBe(6);
    expect(view.showReturnAction).toBe(true);
    expect(view.expiresLabel).toContain("2026");
  });

  it("marks non-returnable with delivery-issue helper", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: false,
      lineBlockedReason: "non_returnable",
    });
    expect(view.status).toBe("non_returnable");
    expect(view.showReturnAction).toBe(false);
    expect(view.helper).toMatch(/Delivery problems/i);
  });

  it("marks expired returns without return action", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: true,
      latestReturnExpiresAtUtc: "2026-09-21T00:00:00Z",
      lineBlockedReason: "window_expired",
    });
    expect(view.status).toBe("expired");
    expect(view.showReturnAction).toBe(false);
  });
});

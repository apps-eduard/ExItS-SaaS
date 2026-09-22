import { describe, expect, it } from "vitest";
import { describeConnectedPoReturnLineEligibility } from "@/features/returns/connected-po-return-eligibility";

describe("describeConnectedPoReturnLineEligibility", () => {
  it("eligible product shows return action with available qty", () => {
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
  });

  it("displays return expiry for eligible lines", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 2,
      returnsAllowed: true,
      earliestReturnExpiresAtUtc: "2026-09-28T12:00:00Z",
      latestReturnExpiresAtUtc: "2026-10-02T12:00:00Z",
      lineBlockedReason: null,
    });
    expect(view.expiresLabel).toBeTruthy();
    expect(view.expiresLabel).toContain("2026");
  });

  it("expired item removes normal return action", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: true,
      latestReturnExpiresAtUtc: "2026-09-21T00:00:00Z",
      lineBlockedReason: "window_expired",
    });
    expect(view.status).toBe("expired");
    expect(view.showReturnAction).toBe(false);
  });

  it("NonReturnable displays correctly", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: false,
      lineBlockedReason: "non_returnable",
    });
    expect(view.status).toBe("non_returnable");
    expect(view.availableQty).toBe(0);
    expect(view.showReturnAction).toBe(false);
  });

  it("NonReturnable helper explains delivery issues remain separate", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: false,
      lineBlockedReason: "non_returnable",
    });
    expect(view.helper).toMatch(/Delivery problems/i);
  });

  it("returnsAllowed false without blocked reason still treats as non-returnable", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 5,
      returnsAllowed: false,
      lineBlockedReason: null,
    });
    expect(view.status).toBe("non_returnable");
    expect(view.showReturnAction).toBe(false);
  });

  it("already returned / nothing returnable hides return action", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: true,
      lineBlockedReason: "already_returned",
    });
    expect(view.showReturnAction).toBe(false);
    expect(view.status).toBe("nothing_returnable");
  });

  it("zero qty without special reason is not actionable", () => {
    const view = describeConnectedPoReturnLineEligibility({
      returnableQuantity: 0,
      returnsAllowed: true,
      lineBlockedReason: null,
    });
    expect(view.showReturnAction).toBe(false);
  });
});

import { describe, expect, it } from "vitest";
import { evaluateCheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import type { PosCashierShiftDto } from "@/api/pos/pos-shifts-client";
import { DEFERRED_POS_DEVICE_CONTEXT } from "@/workspace/pos-device-context";

function shift(partial: Partial<PosCashierShiftDto>): PosCashierShiftDto {
  return {
    shiftId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    organizationId: "11111111-1111-1111-1111-111111111111",
    shiftNumber: "S-1",
    status: "Open",
    actorId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    registerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    registerCode: "REG-1",
    registerName: "Front",
    businessDate: "2026-08-21",
    openingCashAmount: 100,
    openingCashCounted: true,
    effectiveCashCountMode: "Required",
    openedAtUtc: "2026-08-21T01:00:00Z",
    openedBy: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    createdAtUtc: "2026-08-21T01:00:00Z",
    updatedAtUtc: "2026-08-21T01:00:00Z",
    ...partial,
  };
}

describe("evaluateCheckoutShiftReadiness", () => {
  it("blocks when no open shift", () => {
    const result = evaluateCheckoutShiftReadiness({
      loading: false,
      canViewShifts: true,
      currentShift: null,
      posDevice: DEFERRED_POS_DEVICE_CONTEXT,
    });
    expect(result.status).toBe("blocked_no_shift");
    expect(result.shiftGateReady).toBe(false);
    expect(result.moneyPostReady).toBe(false);
  });

  it("is ready for shift gate when open shift has register, without inventing device", () => {
    const result = evaluateCheckoutShiftReadiness({
      loading: false,
      canViewShifts: true,
      currentShift: shift({ status: "Open" }),
      posDevice: DEFERRED_POS_DEVICE_CONTEXT,
    });
    expect(result.status).toBe("ready");
    expect(result.shiftGateReady).toBe(true);
    expect(result.moneyPostReady).toBe(false);
    expect(result.registerId).toBe("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  });

  it("blocks closed shift", () => {
    const result = evaluateCheckoutShiftReadiness({
      loading: false,
      canViewShifts: true,
      currentShift: shift({ status: "Closed" }),
      posDevice: DEFERRED_POS_DEVICE_CONTEXT,
    });
    expect(result.status).toBe("blocked_closed");
    expect(result.shiftGateReady).toBe(false);
  });

  it("blocks open shift without register", () => {
    const result = evaluateCheckoutShiftReadiness({
      loading: false,
      canViewShifts: true,
      currentShift: shift({ registerId: null }),
      posDevice: DEFERRED_POS_DEVICE_CONTEXT,
    });
    expect(result.status).toBe("blocked_no_register");
  });

  it("blocks when shift view is denied", () => {
    const result = evaluateCheckoutShiftReadiness({
      loading: false,
      canViewShifts: false,
      currentShift: shift({}),
      posDevice: DEFERRED_POS_DEVICE_CONTEXT,
    });
    expect(result.status).toBe("blocked_denied");
  });
});

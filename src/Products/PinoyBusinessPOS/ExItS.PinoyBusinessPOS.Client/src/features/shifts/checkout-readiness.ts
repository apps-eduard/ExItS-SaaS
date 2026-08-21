import type { PosCashierShiftDto } from "@/api/pos/pos-shifts-client";
import { isOpenCashierShift } from "@/api/pos/pos-shifts-client";
import type { PosDeviceContext } from "@/workspace/pos-device-context";
import { isPosDeviceReadyForMoney } from "@/workspace/pos-device-context";

/**
 * RMAP-10 shift gate for checkout readiness (prepares RMAP-11).
 * Does not invent PosDevice — money POST remains separately gated.
 */
export type CheckoutShiftReadinessStatus =
  | "loading"
  | "blocked_denied"
  | "blocked_no_shift"
  | "blocked_closed"
  | "blocked_no_register"
  | "ready";

export type CheckoutShiftReadiness = {
  status: CheckoutShiftReadinessStatus;
  shiftId: string | null;
  registerId: string | null;
  /** True when open shift + register satisfy the sale domain shift gate. */
  shiftGateReady: boolean;
  /**
   * True only when shift gate is ready AND a contracted PosDevice exists.
   * Always false while browser device identity remains deferred (RMAP-03).
   */
  moneyPostReady: boolean;
};

export type EvaluateCheckoutShiftReadinessInput = {
  loading: boolean;
  canViewShifts: boolean;
  currentShift: PosCashierShiftDto | null;
  posDevice?: PosDeviceContext | null;
};

export function evaluateCheckoutShiftReadiness(
  input: EvaluateCheckoutShiftReadinessInput,
): CheckoutShiftReadiness {
  if (input.loading) {
    return {
      status: "loading",
      shiftId: null,
      registerId: null,
      shiftGateReady: false,
      moneyPostReady: false,
    };
  }

  if (!input.canViewShifts) {
    return {
      status: "blocked_denied",
      shiftId: null,
      registerId: null,
      shiftGateReady: false,
      moneyPostReady: false,
    };
  }

  const shift = input.currentShift;
  if (!shift) {
    return {
      status: "blocked_no_shift",
      shiftId: null,
      registerId: null,
      shiftGateReady: false,
      moneyPostReady: false,
    };
  }

  if (!isOpenCashierShift(shift)) {
    return {
      status: "blocked_closed",
      shiftId: shift.shiftId,
      registerId: shift.registerId ?? null,
      shiftGateReady: false,
      moneyPostReady: false,
    };
  }

  const registerId = shift.registerId?.trim() ? shift.registerId : null;
  if (!registerId) {
    return {
      status: "blocked_no_register",
      shiftId: shift.shiftId,
      registerId: null,
      shiftGateReady: false,
      moneyPostReady: false,
    };
  }

  const deviceReady = isPosDeviceReadyForMoney(input.posDevice);
  return {
    status: "ready",
    shiftId: shift.shiftId,
    registerId,
    shiftGateReady: true,
    moneyPostReady: deviceReady,
  };
}

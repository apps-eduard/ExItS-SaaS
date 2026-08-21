import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { isPosDeviceReadyForMoney, type PosDeviceContext } from "@/workspace/pos-device-context";

/**
 * Pre-Sell readiness (Device → Shift → Sell). UI gate only — server remains authority.
 */
export type SellEntryReadinessKind = "loading" | "device_required" | "shift_required" | "ready";

export type SellEntryReadiness = {
  kind: SellEntryReadinessKind;
  deviceReady: boolean;
  shiftReady: boolean;
  moneyPostReady: boolean;
};

export function evaluateSellEntryReadiness(input: {
  posDevice: PosDeviceContext | null | undefined;
  shiftReadiness: CheckoutShiftReadiness;
}): SellEntryReadiness {
  const deviceLoading =
    input.posDevice == null ||
    input.posDevice.status === "loading" ||
    input.posDevice.registrationStatus === "loading";
  const shiftLoading = input.shiftReadiness.status === "loading";

  if (deviceLoading || shiftLoading) {
    return {
      kind: "loading",
      deviceReady: false,
      shiftReady: false,
      moneyPostReady: false,
    };
  }

  const deviceReady = isPosDeviceReadyForMoney(input.posDevice);
  if (!deviceReady) {
    return {
      kind: "device_required",
      deviceReady: false,
      shiftReady: input.shiftReadiness.shiftGateReady,
      moneyPostReady: false,
    };
  }

  if (!input.shiftReadiness.shiftGateReady) {
    return {
      kind: "shift_required",
      deviceReady: true,
      shiftReady: false,
      moneyPostReady: false,
    };
  }

  return {
    kind: "ready",
    deviceReady: true,
    shiftReady: true,
    moneyPostReady: input.shiftReadiness.moneyPostReady,
  };
}

/** Mid-session loss after Sell opened — compact cart warning, preserve cart. */
export type MidSessionSellBlock =
  { kind: "none" } | { kind: "device_lost" } | { kind: "shift_lost" };

export function evaluateMidSessionSellBlock(input: {
  posDevice: PosDeviceContext | null | undefined;
  shiftReadiness: CheckoutShiftReadiness;
}): MidSessionSellBlock {
  if (input.shiftReadiness.status === "loading") {
    return { kind: "none" };
  }
  if (!isPosDeviceReadyForMoney(input.posDevice)) {
    return { kind: "device_lost" };
  }
  if (!input.shiftReadiness.shiftGateReady) {
    return { kind: "shift_lost" };
  }
  return { kind: "none" };
}

import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { isPosDeviceReadyForMoney, type PosDeviceContext } from "@/workspace/pos-device-context";

/**
 * Pre-Sell readiness (Device → Shift → Sell). UI gate only — server remains authority.
 * `view_only` allows catalog browsing when the endpoint is not registered for POS sales.
 */
export type SellEntryReadinessKind =
  "loading" | "device_required" | "view_only" | "shift_required" | "ready";

export type SellEntryReadiness = {
  kind: SellEntryReadinessKind;
  deviceReady: boolean;
  shiftReady: boolean;
  moneyPostReady: boolean;
};

export function evaluateSellEntryReadiness(input: {
  posDevice: PosDeviceContext | null | undefined;
  shiftReadiness: CheckoutShiftReadiness;
  /** When true, unregistered endpoints may browse Sell in view-only mode. */
  allowViewOnlyWithoutDevice?: boolean;
  /** When false, temporary PWA pause — device gate does not block money UX. */
  deviceEnforcementEnabled?: boolean | null;
}): SellEntryReadiness {
  const deviceLoading =
    input.deviceEnforcementEnabled !== false &&
    (input.posDevice == null ||
      input.posDevice.status === "loading" ||
      input.posDevice.registrationStatus === "loading");
  const shiftLoading = input.shiftReadiness.status === "loading";

  if (deviceLoading || shiftLoading) {
    return {
      kind: "loading",
      deviceReady: false,
      shiftReady: false,
      moneyPostReady: false,
    };
  }

  const deviceReady = isPosDeviceReadyForMoney(input.posDevice, {
    enforcementEnabled: input.deviceEnforcementEnabled,
  });
  if (!deviceReady) {
    if (input.allowViewOnlyWithoutDevice !== false) {
      return {
        kind: "view_only",
        deviceReady: false,
        shiftReady: input.shiftReadiness.shiftGateReady,
        moneyPostReady: false,
      };
    }
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
  deviceEnforcementEnabled?: boolean | null;
}): MidSessionSellBlock {
  if (input.shiftReadiness.status === "loading") {
    return { kind: "none" };
  }
  if (
    !isPosDeviceReadyForMoney(input.posDevice, {
      enforcementEnabled: input.deviceEnforcementEnabled,
    })
  ) {
    return { kind: "device_lost" };
  }
  if (!input.shiftReadiness.shiftGateReady) {
    return { kind: "shift_lost" };
  }
  return { kind: "none" };
}

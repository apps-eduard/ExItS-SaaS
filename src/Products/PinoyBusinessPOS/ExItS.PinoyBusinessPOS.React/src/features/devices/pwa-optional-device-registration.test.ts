import { describe, expect, it } from "vitest";
import { evaluateSellEntryReadiness } from "@/features/sell/sell-readiness";
import { evaluateCheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { isPosDeviceReadyForMoney, type PosDeviceContext } from "@/workspace/pos-device-context";

const unregisteredDevice: PosDeviceContext = {
  status: "unregistered",
  durableIdentityAvailable: true,
  registrationStatus: "unregistered",
  installationDeviceId: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
  posDeviceId: null,
  registeredBranchId: null,
  authorizedForSelectedBranch: false,
  detail: "Browser is not registered.",
};

describe("PWA optional device registration", () => {
  it("treats unregistered browser as money-ready when enforcement is paused", () => {
    expect(
      isPosDeviceReadyForMoney(unregisteredDevice, { enforcementEnabled: false }),
    ).toBe(true);
  });

  it("still requires registration when enforcement is enabled", () => {
    expect(
      isPosDeviceReadyForMoney(unregisteredDevice, { enforcementEnabled: true }),
    ).toBe(false);
  });

  it("allows Sell entry without device registration when enforcement is paused", () => {
    const result = evaluateSellEntryReadiness({
      posDevice: unregisteredDevice,
      shiftReadiness: {
        status: "ready",
        shiftId: "s1",
        registerId: "r1",
        shiftGateReady: true,
        moneyPostReady: true,
      },
      deviceEnforcementEnabled: false,
    });
    expect(result.kind).toBe("ready");
    expect(result.moneyPostReady).toBe(true);
  });

  it("keeps cash-register/shift gate independent of device pause", () => {
    const result = evaluateCheckoutShiftReadiness({
      loading: false,
      canViewShifts: true,
      currentShift: null,
      posDevice: unregisteredDevice,
      deviceEnforcementEnabled: false,
    });
    expect(result.status).toBe("blocked_no_shift");
    expect(result.shiftGateReady).toBe(false);
    expect(result.moneyPostReady).toBe(false);
  });
});

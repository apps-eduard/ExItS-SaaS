import { describe, expect, it } from "vitest";
import {
  evaluateMidSessionSellBlock,
  evaluateSellEntryReadiness,
} from "@/features/sell/sell-readiness";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import {
  authorizedPosDeviceContext,
  INITIAL_POS_DEVICE_CONTEXT,
  unregisteredPosDeviceContext,
} from "@/workspace/pos-device-context";

const readyShift: CheckoutShiftReadiness = {
  status: "ready",
  shiftGateReady: true,
  moneyPostReady: true,
  registerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  shiftId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
};

const blockedNoShift: CheckoutShiftReadiness = {
  status: "blocked_no_shift",
  shiftGateReady: false,
  moneyPostReady: false,
  registerId: null,
  shiftId: null,
};

const loadingShift: CheckoutShiftReadiness = {
  status: "loading",
  shiftGateReady: false,
  moneyPostReady: false,
  registerId: null,
  shiftId: null,
};

const authorizedDevice = authorizedPosDeviceContext({
  installationDeviceId: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
  posDeviceId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
  registeredBranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
});

describe("evaluateSellEntryReadiness", () => {
  it("returns loading while device or shift is loading", () => {
    expect(
      evaluateSellEntryReadiness({
        posDevice: INITIAL_POS_DEVICE_CONTEXT,
        shiftReadiness: readyShift,
      }).kind,
    ).toBe("loading");

    expect(
      evaluateSellEntryReadiness({
        posDevice: authorizedDevice,
        shiftReadiness: loadingShift,
      }).kind,
    ).toBe("loading");
  });

  it("uses view_only for unregistered devices so catalog browsing can continue", () => {
    const result = evaluateSellEntryReadiness({
      posDevice: unregisteredPosDeviceContext(
        "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
        "unregistered",
      ),
      shiftReadiness: readyShift,
    });
    expect(result.kind).toBe("view_only");
    expect(result.deviceReady).toBe(false);
    expect(result.moneyPostReady).toBe(false);
  });

  it("keeps device_required when view-only is disabled", () => {
    const result = evaluateSellEntryReadiness({
      posDevice: unregisteredPosDeviceContext(
        "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
        "unregistered",
      ),
      shiftReadiness: readyShift,
      allowViewOnlyWithoutDevice: false,
    });
    expect(result.kind).toBe("device_required");
  });

  it("requires open shift after device is ready", () => {
    const result = evaluateSellEntryReadiness({
      posDevice: authorizedDevice,
      shiftReadiness: blockedNoShift,
    });
    expect(result.kind).toBe("shift_required");
    expect(result.deviceReady).toBe(true);
    expect(result.shiftReady).toBe(false);
  });

  it("treats device gate as ready when enforcement is paused", () => {
    const result = evaluateSellEntryReadiness({
      posDevice: unregisteredPosDeviceContext(
        "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
        "unregistered",
      ),
      shiftReadiness: readyShift,
      deviceEnforcementEnabled: false,
    });
    expect(result.kind).toBe("ready");
    expect(result.deviceReady).toBe(true);
    expect(result.moneyPostReady).toBe(true);
  });

  it("is ready when device and shift gate pass", () => {
    const result = evaluateSellEntryReadiness({
      posDevice: authorizedDevice,
      shiftReadiness: readyShift,
    });
    expect(result.kind).toBe("ready");
    expect(result.moneyPostReady).toBe(true);
  });
});

describe("evaluateMidSessionSellBlock", () => {
  it("returns none while shift readiness is loading", () => {
    expect(
      evaluateMidSessionSellBlock({
        posDevice: authorizedDevice,
        shiftReadiness: loadingShift,
      }).kind,
    ).toBe("none");
  });

  it("reports device_lost when authorization is gone", () => {
    expect(
      evaluateMidSessionSellBlock({
        posDevice: unregisteredPosDeviceContext("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee", "revoked"),
        shiftReadiness: readyShift,
      }).kind,
    ).toBe("device_lost");
  });

  it("does not report device_lost when enforcement is paused", () => {
    expect(
      evaluateMidSessionSellBlock({
        posDevice: unregisteredPosDeviceContext("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee", "revoked"),
        shiftReadiness: readyShift,
        deviceEnforcementEnabled: false,
      }).kind,
    ).toBe("none");
  });

  it("reports shift_lost when shift gate falls", () => {
    expect(
      evaluateMidSessionSellBlock({
        posDevice: authorizedDevice,
        shiftReadiness: blockedNoShift,
      }).kind,
    ).toBe("shift_lost");
  });

  it("returns none when still ready", () => {
    expect(
      evaluateMidSessionSellBlock({
        posDevice: authorizedDevice,
        shiftReadiness: readyShift,
      }).kind,
    ).toBe("none");
  });
});

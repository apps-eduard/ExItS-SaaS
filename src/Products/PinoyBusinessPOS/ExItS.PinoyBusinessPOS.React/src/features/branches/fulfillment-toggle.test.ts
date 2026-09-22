import { describe, expect, it } from "vitest";
import { resolveFulfillmentToggle } from "@/features/branches/fulfillment-toggle";

describe("resolveFulfillmentToggle org Offer Delivery guard", () => {
  it("allows OFF→ON when global Offer Delivery is ON", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: false,
      ready: true,
      canUseDelivery: true,
      orgOfferDelivery: true,
    });
    expect(decision.enableBlocked).toBe(false);
    expect(decision.disabled).toBe(false);
    expect(decision.blockReason).toBeNull();
  });

  it("allows ON→OFF when global Offer Delivery is ON", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: true,
      ready: true,
      canUseDelivery: true,
      orgOfferDelivery: true,
    });
    expect(decision.checked).toBe(true);
    expect(decision.enableBlocked).toBe(false);
    expect(decision.disabled).toBe(false);
  });

  it("blocks OFF→ON when global Offer Delivery is OFF but keeps switch clickable", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: false,
      ready: true,
      canUseDelivery: true,
      orgOfferDelivery: false,
    });
    expect(decision.enableBlocked).toBe(true);
    expect(decision.blockReason).toBe("orgOffer");
    expect(decision.disabled).toBe(false);
    expect(decision.hintKey).toBe("branches.toggle.enableOfferDeliveryFirst");
  });

  it("keeps ON when global Offer Delivery is OFF (globally paused)", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: true,
      ready: true,
      canUseDelivery: true,
      orgOfferDelivery: false,
    });
    expect(decision.checked).toBe(true);
    expect(decision.enableBlocked).toBe(false);
    expect(decision.disabled).toBe(false);
  });

  it("allows turning a globally-paused ON branch OFF", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: true,
      ready: true,
      canUseDelivery: true,
      orgOfferDelivery: false,
    });
    expect(decision.checked).toBe(true);
    expect(decision.disabled).toBe(false);
  });
});

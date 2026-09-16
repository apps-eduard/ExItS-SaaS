import { describe, expect, it } from "vitest";
import {
  buildPoFulfillmentReadinessView,
  buildSupplierReadinessSummary,
} from "@/features/branches/po-fulfillment-readiness";

const base = {
  pickupEnabled: false,
  deliveryEnabled: false,
  pickupReady: false,
  deliveryReady: false,
  branchDetailsComplete: false,
  pickupSectionsComplete: 0,
  pickupSectionsTotal: 2,
  deliverySectionsComplete: 0,
  deliverySectionsTotal: 5,
};

describe("buildPoFulfillmentReadinessView", () => {
  it("both methods OFF → Setup required with enable-method + branch info only", () => {
    const view = buildPoFulfillmentReadinessView(base);
    expect(view.ready).toBe(false);
    expect(view.statusKey).toBe("branches.poFulfillment.status.setupRequired");
    expect(view.checklist.map((c) => c.id)).toEqual(["enableMethod", "branchInfo"]);
    expect(view.methods.every((m) => !m.enabled)).toBe(true);
    // Disabled methods must not invent detail-progress checklist rows.
    expect(view.checklist.some((c) => c.id === "pickupProgress")).toBe(false);
    expect(view.checklist.some((c) => c.id === "deliveryProgress")).toBe(false);
  });

  it("Pickup ready only → Ready (Delivery may stay Off)", () => {
    const view = buildPoFulfillmentReadinessView({
      ...base,
      pickupEnabled: true,
      pickupReady: true,
      branchDetailsComplete: true,
      pickupSectionsComplete: 2,
      deliverySectionsComplete: 0,
    });
    expect(view.ready).toBe(true);
    expect(view.checklist.find((c) => c.id === "pickupProgress")).toMatchObject({
      done: true,
      progressLabel: "2/2",
    });
    expect(view.methods.find((m) => m.channel === "delivery")?.enabled).toBe(false);
  });

  it("Delivery ready only → Ready", () => {
    const view = buildPoFulfillmentReadinessView({
      ...base,
      deliveryEnabled: true,
      deliveryReady: true,
      branchDetailsComplete: true,
      deliverySectionsComplete: 5,
      pickupSectionsComplete: 2,
    });
    expect(view.ready).toBe(true);
    expect(view.checklist.find((c) => c.id === "deliveryProgress")?.done).toBe(true);
  });

  it("enabled but incomplete → Setup required with that method progress only", () => {
    const view = buildPoFulfillmentReadinessView({
      ...base,
      pickupEnabled: true,
      pickupReady: false,
      branchDetailsComplete: true,
      pickupSectionsComplete: 1,
    });
    expect(view.ready).toBe(false);
    expect(view.checklist.map((c) => c.id)).toEqual(["pickupProgress"]);
    expect(view.checklist[0]?.progressLabel).toBe("1/2");
    expect(view.checklist.some((c) => c.id === "deliveryProgress")).toBe(false);
  });

  it("incomplete branch info shown even when a method is enabled", () => {
    const view = buildPoFulfillmentReadinessView({
      ...base,
      deliveryEnabled: true,
      deliveryReady: false,
      branchDetailsComplete: false,
      deliverySectionsComplete: 2,
    });
    expect(view.checklist.map((c) => c.id)).toEqual(["branchInfo", "deliveryProgress"]);
  });
});

describe("buildSupplierReadinessSummary", () => {
  it("deep-links fulfillment to branch overview and others to canonical pages", () => {
    const items = buildSupplierReadinessSummary({
      branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      fulfillmentReady: true,
      catalogOk: false,
      paymentsOk: true,
      contactOk: false,
    });
    expect(items.find((i) => i.key === "fulfillment")?.href).toBe(
      "/org/branches/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/fulfillment",
    );
    expect(items.find((i) => i.key === "catalog")?.href).toBe("/customers?kind=businesses");
    expect(items.find((i) => i.key === "payments")?.href).toBe("/org/payment-methods");
    expect(items.find((i) => i.key === "contact")?.ok).toBe(false);
  });
});

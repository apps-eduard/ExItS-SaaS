import { describe, expect, it } from "vitest";
import {
  buildNeedsAttentionAlerts,
  formatNeedsAttentionBadge,
  groupNeedsAttentionAlerts,
  hasEnabledPoPaymentMethod,
  requirementIsMissing,
} from "@/features/shell/needs-attention";

describe("buildNeedsAttentionAlerts", () => {
  it("returns empty when all counts are zero", () => {
    expect(
      buildNeedsAttentionAlerts({
        inventory: {
          lowStockProductCount: 0,
          outOfStockProductCount: 0,
          expiredLotCount: 0,
          nearExpiryLotCount: 0,
        },
        commerce: {
          incompleteSupplierConnectionIds: [],
          paymentSetupIncomplete: false,
          creditIncompleteConnectionIds: [],
        },
        branch: {
          branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          deliveryEnabled: false,
          pickupEnabled: false,
          deliveryReady: true,
          pickupReady: true,
          customerOrderingEnabled: false,
          branchDetailsComplete: true,
        },
      }),
    ).toEqual([]);
  });

  it("aggregates inventory low stock and expiry without listing products", () => {
    const alerts = buildNeedsAttentionAlerts({
      inventory: {
        lowStockProductCount: 8,
        outOfStockProductCount: 2,
        expiredLotCount: 1,
        nearExpiryLotCount: 2,
      },
    });

    expect(alerts.map((a) => a.kind)).toEqual(["lowStock", "outOfStock", "expiringSoon"]);
    expect(alerts.find((a) => a.kind === "lowStock")).toMatchObject({
      count: 8,
      href: "/inventory?lowStock=1",
    });
    expect(alerts.find((a) => a.kind === "outOfStock")).toMatchObject({
      count: 2,
      href: "/inventory?stockStatus=OutOfStock",
    });
    expect(alerts.find((a) => a.kind === "expiringSoon")).toMatchObject({
      count: 3,
      href: "/inventory/expiration",
    });
  });

  it("aggregates supplier readiness and deep-links a single connection", () => {
    const one = buildNeedsAttentionAlerts({
      commerce: {
        incompleteSupplierConnectionIds: ["cccccccc-cccc-cccc-cccc-cccccccccccc"],
      },
    });
    expect(one).toHaveLength(1);
    expect(one[0]).toMatchObject({
      kind: "supplierReadiness",
      count: 1,
      href: "/customers/business/cccccccc-cccc-cccc-cccc-cccccccccccc",
      group: "connectedCommerce",
    });

    const many = buildNeedsAttentionAlerts({
      commerce: {
        incompleteSupplierConnectionIds: ["a", "b"],
      },
    });
    expect(many[0]?.href).toBe("/customers?kind=businesses");
    expect(many[0]?.count).toBe(2);
  });

  it("emits payment and credit alerts into the correct groups", () => {
    const alerts = buildNeedsAttentionAlerts({
      commerce: {
        paymentSetupIncomplete: true,
        creditIncompleteConnectionIds: ["c1"],
      },
    });
    expect(alerts.find((a) => a.kind === "paymentSetup")).toMatchObject({
      group: "connectedCommerce",
      href: "/org/payment-methods",
    });
    expect(alerts.find((a) => a.kind === "creditSetup")).toMatchObject({
      group: "creditConfiguration",
      href: "/customers/business/c1",
    });
  });

  it("emits delivery, pickup, missing fulfillment, and branch info alerts with deep-links", () => {
    const branchId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    const alerts = buildNeedsAttentionAlerts({
      branch: {
        branchId,
        orgOfferDelivery: true,
        deliveryEnabled: true,
        pickupEnabled: true,
        deliveryReady: false,
        pickupReady: false,
        customerOrderingEnabled: true,
        branchDetailsComplete: false,
        deliveryLocationComplete: false,
        deliveryPolicyComplete: true,
        deliveryAreasComplete: true,
      },
    });

    expect(alerts.map((a) => a.kind)).toEqual([
      "deliveryReadiness",
      "pickupReadiness",
      "branchInfoIncomplete",
    ]);
    expect(alerts.find((a) => a.kind === "deliveryReadiness")?.href).toBe(
      `/org/branches/${branchId}/fulfillment?tab=location`,
    );
    expect(alerts.find((a) => a.kind === "pickupReadiness")?.href).toBe(
      `/org/branches/${branchId}/fulfillment?tab=location`,
    );
    expect(alerts.find((a) => a.kind === "branchInfoIncomplete")?.href).toBe(
      `/org/branches/${branchId}/fulfillment?tab=details`,
    );

    const missing = buildNeedsAttentionAlerts({
      branch: {
        branchId,
        customerOrderingEnabled: true,
        pickupEnabled: false,
        deliveryEnabled: false,
        orgOfferDelivery: false,
        branchDetailsComplete: true,
      },
    });
    expect(missing.map((a) => a.kind)).toEqual(["missingFulfillment"]);
    expect(missing[0]?.href).toBe(`/org/branches/${branchId}/fulfillment`);
  });

  it("does not alert delivery gaps when organization Offer Delivery is OFF", () => {
    const alerts = buildNeedsAttentionAlerts({
      branch: {
        branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        orgOfferDelivery: false,
        deliveryEnabled: true,
        deliveryReady: false,
        pickupEnabled: true,
        pickupReady: true,
        branchDetailsComplete: true,
      },
    });
    expect(alerts.map((a) => a.kind)).toEqual([]);
  });

  it("Mica-like zero-config connected supplier surfaces fulfillment, payment, and branch info", () => {
    const branchId = "f8e580b1-aa12-4a01-9ba0-c624fb24a3a4";
    const before = buildNeedsAttentionAlerts({
      inventory: {
        lowStockProductCount: 0,
        outOfStockProductCount: 0,
        expiredLotCount: 0,
        nearExpiryLotCount: 0,
      },
      commerce: {
        // Old hook skipped payment when store-payment-management was absent.
        paymentSetupIncomplete: false,
        incompleteSupplierConnectionIds: [],
        creditIncompleteConnectionIds: [],
      },
      branch: {
        branchId,
        // Real Mica Main Branch fulfillment-readiness snapshot
        customerOrderingEnabled: false,
        pickupEnabled: false,
        deliveryEnabled: false,
        deliveryReady: false,
        pickupReady: false,
        branchDetailsComplete: false,
        deliveryLocationComplete: false,
        deliveryPolicyComplete: false,
        deliveryAreasComplete: false,
        // Old builder had no supplierCommerceEligible → missing fulfillment skipped
      },
    });
    // Before: only branch-info could fire; delivery/pickup details correctly stay quiet
    // while methods are disabled. Missing fulfillment + payment did not.
    expect(before.map((a) => a.kind)).toEqual(["branchInfoIncomplete"]);

    const after = buildNeedsAttentionAlerts({
      inventory: {
        lowStockProductCount: 0,
        outOfStockProductCount: 1,
        expiredLotCount: 0,
        nearExpiryLotCount: 0,
      },
      commerce: {
        incompleteSupplierConnectionIds: [
          "e0c2582c-672f-4412-b0b9-8d7b200e409a",
          "2346d48b-9159-4a4a-93e1-3848242d8903",
        ],
        paymentSetupIncomplete: true,
        creditIncompleteConnectionIds: [],
      },
      branch: {
        branchId,
        supplierCommerceEligible: true,
        customerOrderingEnabled: false,
        pickupEnabled: false,
        deliveryEnabled: false,
        deliveryReady: false,
        pickupReady: false,
        branchDetailsComplete: false,
        deliveryLocationComplete: false,
        deliveryPolicyComplete: false,
        deliveryAreasComplete: false,
      },
    });

    expect(after.map((a) => a.kind)).toEqual([
      "outOfStock",
      "supplierReadiness",
      "paymentSetup",
      "missingFulfillment",
      "branchInfoIncomplete",
    ]);
    expect(after.find((a) => a.kind === "supplierReadiness")?.count).toBe(2);
    expect(after.find((a) => a.kind === "missingFulfillment")?.href).toBe(
      `/org/branches/${branchId}/fulfillment`,
    );
    expect(after.find((a) => a.kind === "missingFulfillment")?.reasonKey).toBe(
      "shell.needsAttention.missingFulfillmentReason",
    );
    expect(after.find((a) => a.kind === "paymentSetup")?.href).toBe("/org/payment-methods");
    // Delivery/Pickup detail rows must stay absent while methods are disabled.
    expect(after.some((a) => a.kind === "deliveryReadiness")).toBe(false);
    expect(after.some((a) => a.kind === "pickupReadiness")).toBe(false);
  });

  it("does not treat missing fulfillment as required for non-supplier branches with ordering off", () => {
    const alerts = buildNeedsAttentionAlerts({
      branch: {
        branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        supplierCommerceEligible: false,
        customerOrderingEnabled: false,
        pickupEnabled: false,
        deliveryEnabled: false,
        branchDetailsComplete: true,
      },
    });
    expect(alerts).toEqual([]);
  });

  it("groups alerts in canonical order", () => {
    const groups = groupNeedsAttentionAlerts(
      buildNeedsAttentionAlerts({
        inventory: { lowStockProductCount: 1 },
        commerce: { paymentSetupIncomplete: true },
        branch: {
          branchId: "b",
          orgOfferDelivery: true,
          deliveryEnabled: true,
          deliveryReady: false,
          deliveryPolicyComplete: false,
          deliveryLocationComplete: true,
          deliveryAreasComplete: true,
        },
      }),
    );
    expect(groups.map((g) => g.id)).toEqual([
      "inventory",
      "connectedCommerce",
      "branchFulfillment",
    ]);
  });
});

describe("formatNeedsAttentionBadge", () => {
  it("hides zero and caps at 9+", () => {
    expect(formatNeedsAttentionBadge(0)).toBeNull();
    expect(formatNeedsAttentionBadge(3)).toBe("3");
    expect(formatNeedsAttentionBadge(10)).toBe("9+");
  });
});

describe("payment and requirement helpers", () => {
  it("detects enabled PO payment methods", () => {
    expect(
      hasEnabledPoPaymentMethod([
        {
          methodCode: "Cash",
          entitled: true,
          isEnabled: false,
          comingSoon: false,
        },
      ]),
    ).toBe(false);
    expect(
      hasEnabledPoPaymentMethod([
        {
          methodCode: "BankTransfer",
          entitled: true,
          isEnabled: true,
          comingSoon: false,
        },
      ]),
    ).toBe(true);
  });

  it("detects Missing requirements", () => {
    expect(
      requirementIsMissing(
        [{ code: "CreditPolicy", status: "Missing" }],
        "CreditPolicy",
      ),
    ).toBe(true);
    expect(
      requirementIsMissing(
        [{ code: "CreditPolicy", status: "Complete" }],
        "CreditPolicy",
      ),
    ).toBe(false);
  });
});

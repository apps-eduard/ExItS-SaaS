import { describe, expect, it } from "vitest";
import {
  buildBuyerPoNotReadyCopy,
  normalizeBuyerPoBlockerCategories,
} from "@/features/purchasing/buyer-po-readiness-banner";
import type { MessageKey } from "@/i18n/messages";

const EN: Partial<Record<MessageKey, string>> = {
  "purchasing.supplierNotReadyBody": "Generic fallback.",
  "purchasing.supplierNotReady.issuePrefix": "Issue:",
  "purchasing.supplierNotReady.issuesPrefix": "Issues:",
  "purchasing.supplierNotReady.issue.fulfillment": "Fulfillment setup",
  "purchasing.supplierNotReady.issue.noUsableMethod": "Pickup or Delivery",
  "purchasing.supplierNotReady.issue.payment": "Payment",
  "purchasing.supplierNotReady.issue.catalog": "Catalog",
  "purchasing.supplierNotReady.issue.contact": "Contact",
  "purchasing.supplierNotReady.issue.credit": "Credit",
  "purchasing.supplierNotReady.phrase.fulfillment": "fulfillment",
  "purchasing.supplierNotReady.phrase.noUsableMethod": "fulfillment methods",
  "purchasing.supplierNotReady.phrase.payment": "payment",
  "purchasing.supplierNotReady.phrase.catalog": "catalog",
  "purchasing.supplierNotReady.phrase.contact": "contact",
  "purchasing.supplierNotReady.phrase.credit": "credit",
  "purchasing.supplierNotReady.reason.fulfillment":
    "Purchase orders are currently unavailable because this supplier has not completed its fulfillment setup. Please contact the supplier and ask them to review their purchase-order fulfillment settings.",
  "purchasing.supplierNotReady.reason.noUsableMethod":
    "This supplier currently has no Pickup or Delivery method available for purchase orders. Please contact the supplier.",
  "purchasing.supplierNotReady.reason.payment":
    "This supplier has not configured an accepted payment method for purchase orders.",
  "purchasing.supplierNotReady.reason.catalog":
    "This supplier currently has no products available for purchase ordering.",
  "purchasing.supplierNotReady.reason.contact":
    "The supplier must complete its business contact setup before accepting purchase orders.",
  "purchasing.supplierNotReady.reason.credit":
    "Credit purchasing is currently unavailable for this business relationship.",
  "purchasing.supplierNotReady.reason.multiple":
    "Purchase orders are currently unavailable due to {reasons} setup. Please contact your supplier.",
};

function t(key: MessageKey): string {
  return EN[key] ?? key;
}

describe("normalizeBuyerPoBlockerCategories", () => {
  it("keeps known categories in stable order and drops unknown codes", () => {
    expect(
      normalizeBuyerPoBlockerCategories(["Credit", "SellingBranch", "Payment", "Fulfillment"]),
    ).toEqual(["Fulfillment", "Payment", "Credit"]);
  });
});

describe("buildBuyerPoNotReadyCopy", () => {
  it("uses fulfillment body for a single fulfillment blocker", () => {
    const copy = buildBuyerPoNotReadyCopy(["Fulfillment"], t);
    expect(t(copy.bodyKey)).toMatch(/fulfillment setup/i);
    expect(copy.issueLine).toBe("Issue: Fulfillment setup");
    expect(copy.listPhrase).toBeNull();
  });

  it("uses payment body for a payment blocker", () => {
    const copy = buildBuyerPoNotReadyCopy(["Payment"], t);
    expect(t(copy.bodyKey)).toMatch(/accepted payment method/i);
  });

  it("uses catalog body for a catalog blocker", () => {
    const copy = buildBuyerPoNotReadyCopy(["Catalog"], t);
    expect(t(copy.bodyKey)).toMatch(/no products available/i);
  });

  it("summarizes multiple blockers without exposing internal codes", () => {
    const copy = buildBuyerPoNotReadyCopy(["Payment", "Fulfillment"], t);
    expect(copy.bodyKey).toBe("purchasing.supplierNotReady.reason.multiple");
    expect(copy.listPhrase).toBe("fulfillment and payment");
    expect(copy.issueLine).toBe("Issues: Fulfillment setup · Payment");
    const rendered = t(copy.bodyKey).replace("{reasons}", copy.listPhrase!);
    expect(rendered).toMatch(/fulfillment and payment setup/i);
    expect(rendered).not.toMatch(/SellingBranch|PickupConfig|PaymentMethods/i);
  });

  it("falls back when categories are empty", () => {
    const copy = buildBuyerPoNotReadyCopy([], t);
    expect(copy.bodyKey).toBe("purchasing.supplierNotReadyBody");
    expect(copy.issueLine).toBeNull();
  });
});

import { describe, expect, it } from "vitest";
import {
  BUSINESS_CREDIT_POLICY_ANCHOR,
  BUSINESS_RELATIONSHIP_CONTACT_ANCHOR,
  DEFAULT_SUPPLIER_COMMERCE_READINESS_FILTER,
  countSupplierCommerceReadinessFilters,
  filterSupplierCommerceRequirements,
  firstIncompleteSupplierCommerceRequirement,
  resolveSupplierCommerceReadinessPath,
} from "@/features/customers/supplier-commerce-readiness-nav";

const connectionId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

const requirements = [
  { code: "SellingBranch", status: "Complete" },
  { code: "SharedCatalog", status: "Missing" },
  { code: "ResponsibleContact", status: "Missing" },
  { code: "CreditPolicy", status: "NotApplicable" },
];

describe("supplier-commerce-readiness-nav", () => {
  it("defaults filter to Needs setup", () => {
    expect(DEFAULT_SUPPLIER_COMMERCE_READINESS_FILTER).toBe("needsSetup");
  });

  it("counts visible requirements and hides NotApplicable", () => {
    expect(countSupplierCommerceReadinessFilters(requirements)).toEqual({
      needsSetup: 2,
      complete: 1,
      all: 3,
    });
  });

  it("filters Needs setup, Complete, and All", () => {
    expect(filterSupplierCommerceRequirements(requirements, "needsSetup").map((r) => r.code)).toEqual([
      "SharedCatalog",
      "ResponsibleContact",
    ]);
    expect(filterSupplierCommerceRequirements(requirements, "complete").map((r) => r.code)).toEqual([
      "SellingBranch",
    ]);
    expect(filterSupplierCommerceRequirements(requirements, "all").map((r) => r.code)).toEqual([
      "SellingBranch",
      "SharedCatalog",
      "ResponsibleContact",
    ]);
  });

  it("picks the first incomplete requirement", () => {
    expect(firstIncompleteSupplierCommerceRequirement(requirements)?.code).toBe("SharedCatalog");
  });

  it("resolves deep-links for incomplete and complete rows", () => {
    const ctx = { connectionId, supplierBranchId: branchId };

    expect(resolveSupplierCommerceReadinessPath("SellingBranch", ctx)).toBe(
      `/org/branches/${branchId}`,
    );
    expect(resolveSupplierCommerceReadinessPath("FulfillmentMethod", ctx)).toBe(
      `/org/branches/${branchId}/fulfillment`,
    );
    expect(resolveSupplierCommerceReadinessPath("PickupConfig", ctx)).toBe(
      `/org/branches/${branchId}/fulfillment`,
    );
    expect(resolveSupplierCommerceReadinessPath("DeliveryConfig", ctx)).toBe(
      `/org/branches/${branchId}/fulfillment?tab=policy`,
    );
    expect(resolveSupplierCommerceReadinessPath("PaymentMethods", ctx)).toBe("/org/payment-methods");
    expect(resolveSupplierCommerceReadinessPath("SharedCatalog", ctx)).toBe(
      `/suppliers/connected/buyers/${connectionId}/shared-products`,
    );
    expect(resolveSupplierCommerceReadinessPath("ResponsibleContact", ctx)).toBe(
      `/customers/business/${connectionId}#${BUSINESS_RELATIONSHIP_CONTACT_ANCHOR}`,
    );
    expect(
      resolveSupplierCommerceReadinessPath("ResponsibleContact", {
        ...ctx,
        openContactEditor: true,
      }),
    ).toBe(
      `/customers/business/${connectionId}?editContact=1#${BUSINESS_RELATIONSHIP_CONTACT_ANCHOR}`,
    );
    expect(resolveSupplierCommerceReadinessPath("CreditPolicy", ctx)).toBe(
      `/customers/business/${connectionId}#${BUSINESS_CREDIT_POLICY_ANCHOR}`,
    );
  });
});

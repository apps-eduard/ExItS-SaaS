import { describe, expect, it } from "vitest";
import type { PersonalProductAffiliationDto } from "@/api/platform/product-affiliations-client";
import {
  affiliatedBusinesses,
  isProductCommerciallyReady,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function row(overrides: Partial<PersonalProductAffiliationDto>): PersonalProductAffiliationDto {
  return {
    productCode: "pinoy-business-pos",
    productDisplayName: "PinoyBusinessPOS",
    organizationId: null,
    organizationDisplayName: null,
    membershipRole: null,
    roleDisplay: null,
    planKey: null,
    planDisplayName: null,
    subscriptionStatus: null,
    trialEndUtc: null,
    canManageBilling: false,
    ...overrides,
  };
}

describe("product portfolio capability", () => {
  it("keeps start and open ready only for POS", () => {
    expect(isProductCommerciallyReady("pinoy-business-pos")).toBe(true);
    for (const code of [
      "pinoy-loan-manager",
      "pinoy-service-pro",
      "pinoy-pawn-manager",
      "pinoy-buy-now-pay-later",
    ]) {
      expect(isProductCommerciallyReady(code)).toBe(false);
      const capability = resolveProductPortfolioCapability(row({ productCode: code }));
      expect(capability.canStartBusiness).toBe(false);
      expect(capability.canOpenProduct).toBe(false);
      expect(capability.availability).toBe("coming-soon");
      expect(capability.startRoute).toBeNull();
    }
  });

  it("offers Start Business only when POS has no organization", () => {
    const capability = resolveProductPortfolioCapability(row({ canManageBilling: false }));
    expect(capability.canStartBusiness).toBe(true);
    expect(capability.startRoute).toBe("/personal/explore-pos");
    expect(capability.canOpenProduct).toBe(false);
  });

  it("opens and manages an active POS owner subscription", () => {
    const capability = resolveProductPortfolioCapability(
      row({
        organizationId: orgId,
        organizationDisplayName: "ABC Grocery",
        roleDisplay: "Owner",
        planDisplayName: "Pro",
        subscriptionStatus: "Active",
        canManageBilling: true,
      }),
    );
    expect(capability.canStartBusiness).toBe(false);
    expect(capability.canOpenProduct).toBe(true);
    expect(capability.canManageSubscription).toBe(true);
    expect(capability.manageRoute).toBe("/org/subscription");
  });

  it("reuses the organization when POS has no active subscription", () => {
    const capability = resolveProductPortfolioCapability(
      row({
        organizationId: orgId,
        canManageBilling: true,
        subscriptionStatus: null,
      }),
    );
    expect(capability.canStartBusiness).toBe(false);
    expect(capability.canChoosePlan).toBe(true);
    expect(capability.canOpenProduct).toBe(false);
    expect(capability.choosePlanRoute).toBe("/personal/explore-pos");
  });

  it("lets non-billing staff open without managing the subscription", () => {
    const capability = resolveProductPortfolioCapability(
      row({
        organizationId: orgId,
        roleDisplay: "Cashier",
        subscriptionStatus: "Active",
        canManageBilling: false,
      }),
    );
    expect(capability.canOpenProduct).toBe(true);
    expect(capability.canManageSubscription).toBe(false);
    expect(capability.canChoosePlan).toBe(false);
  });

  it("keeps one row per product", () => {
    const rows = affiliatedBusinesses([
      row({ productCode: "pinoy-business-pos", organizationId: orgId }),
      row({ productCode: "pinoy-business-pos", organizationId: orgId }),
      row({
        productCode: "pinoy-loan-manager",
        organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      }),
      row({ productCode: "pinoy-service-pro" }),
    ]);
    expect(rows.map((item) => item.productCode)).toEqual([
      "pinoy-business-pos",
      "pinoy-loan-manager",
    ]);
  });
});

import { describe, expect, it } from "vitest";
import type { OrganizationOnlineSupplierPaymentsCapability } from "@/api/platform/organization-online-supplier-payments-client";
import type { PaymentMethodSettingDto } from "@/api/pos/pos-payment-methods-client";
import {
  hasReadyOnlineSupplierPaymentMethod,
  resolveBuyerSupplierPaymentCta,
} from "@/features/suppliers/buyer-supplier-payment-gate";

function capability(
  status: OrganizationOnlineSupplierPaymentsCapability["status"],
): OrganizationOnlineSupplierPaymentsCapability {
  return {
    organizationId: "11111111-1111-1111-1111-111111111111",
    status,
    updatedAtUtc: null,
    updatedByActorReference: null,
    reason: null,
  };
}

function method(
  overrides: Partial<PaymentMethodSettingDto> = {},
): PaymentMethodSettingDto {
  return {
    methodCode: "OnlineGCash",
    requiredCapability: "OnlinePayments",
    integrationMode: "Online",
    settlementMode: "ExternalConfirmation",
    availability: "ComingSoon",
    entitled: true,
    isEnabled: false,
    isCheckoutEligible: false,
    comingSoon: true,
    requireReference: false,
    branchScope: "AllBranches",
    selectedBranchIds: [],
    canConfigure: false,
    ...overrides,
  };
}

describe("hasReadyOnlineSupplierPaymentMethod", () => {
  it("is false when all OnlinePayments methods are ComingSoon", () => {
    expect(hasReadyOnlineSupplierPaymentMethod([method()])).toBe(false);
  });

  it("is true when a non-ComingSoon OnlinePayments method is entitled", () => {
    expect(
      hasReadyOnlineSupplierPaymentMethod([
        method({
          methodCode: "OnlineGCash",
          availability: "Configurable",
          comingSoon: false,
          entitled: true,
        }),
      ]),
    ).toBe(true);
  });
});

describe("resolveBuyerSupplierPaymentCta", () => {
  const base = {
    payableEligible: true,
    allowManage: true,
    online: true,
    paymentMethods: [method()],
  };

  it("hides CTA when platform status is Disabled", () => {
    expect(
      resolveBuyerSupplierPaymentCta({
        ...base,
        platformCapability: capability("Disabled"),
      }),
    ).toBe("hidden");
  });

  it("hides CTA when platform status is Suspended", () => {
    expect(
      resolveBuyerSupplierPaymentCta({
        ...base,
        platformCapability: capability("Suspended"),
      }),
    ).toBe("hidden");
  });

  it("shows unavailable when Available but only ComingSoon online methods", () => {
    expect(
      resolveBuyerSupplierPaymentCta({
        ...base,
        platformCapability: capability("Available"),
        paymentMethods: [method()],
      }),
    ).toBe("unavailable");
  });

  it("shows pay_now when Available and a ready online method exists", () => {
    expect(
      resolveBuyerSupplierPaymentCta({
        ...base,
        platformCapability: capability("Available"),
        paymentMethods: [
          method({
            availability: "Configurable",
            comingSoon: false,
            entitled: true,
          }),
        ],
      }),
    ).toBe("pay_now");
  });

  it("hides when manage is denied even if Available", () => {
    expect(
      resolveBuyerSupplierPaymentCta({
        ...base,
        allowManage: false,
        platformCapability: capability("Available"),
      }),
    ).toBe("hidden");
  });
});

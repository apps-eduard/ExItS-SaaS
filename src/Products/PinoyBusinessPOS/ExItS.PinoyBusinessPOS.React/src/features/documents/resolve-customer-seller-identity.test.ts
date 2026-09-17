import { describe, expect, it } from "vitest";
import type { LinkedCustomerSaleReceipt } from "@/api/pos/pos-linked-customers-client";
import { resolveCustomerSellerIdentity } from "@/features/documents/resolve-customer-seller-identity";

function baseReceipt(
  overrides: Partial<LinkedCustomerSaleReceipt> = {},
): LinkedCustomerSaleReceipt {
  return {
    organizationId: "11111111-1111-1111-1111-111111111111",
    platformBusinessCustomerId: "22222222-2222-4222-8222-222222222222",
    posCustomerId: "33333333-3333-4333-8333-333333333333",
    saleId: "44444444-4444-4444-8444-444444444444",
    receiptNumber: "SALE-1",
    occurredAtUtc: "2026-09-14T10:00:00Z",
    status: "Completed",
    paymentMethod: "Cash",
    currency: "PHP",
    subtotal: 100,
    taxAmount: 0,
    total: 100,
    lines: [],
    ...overrides,
  };
}

describe("resolveCustomerSellerIdentity", () => {
  it("prefers sale-time snapshot over public profile and merchant fallback", () => {
    const { identity, headerVisibility } = resolveCustomerSellerIdentity({
      receipt: baseReceipt({
        merchantDisplayName: "Old Merchant",
        sellerDocumentIdentity: {
          businessName: "Mica store",
          address: "Iloilo City",
          phone: "0917",
          email: "mica@gmail.com",
          branchName: "Main Branch",
          showLogo: true,
          showBusinessName: true,
          showBusinessAddress: true,
          showBusinessPhone: true,
          showBusinessEmail: true,
          showBranchName: true,
          showBranchAddress: false,
          identitySource: "saleSnapshot",
        },
      }),
      publicProfile: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        displayName: "Mica Supermarket",
        publicOrganizationId: "ORG1",
        logoUrl: null,
        businessPhone: "0999",
        businessEmail: "new@example.com",
        addressLine1: "New Address",
        addressLine2: null,
        city: null,
        region: null,
        postalCode: null,
        countryCode: null,
      },
    });

    expect(identity.businessName).toBe("Mica store");
    expect(identity.address).toBe("Iloilo City");
    expect(identity.phone).toBe("0917");
    expect(identity.email).toBe("mica@gmail.com");
    expect(identity.branchName).toBe("Main Branch");
    expect(headerVisibility.showBusinessEmail).toBe(true);
  });

  it("falls back to public profile then merchant name, Store only last", () => {
    const withPublic = resolveCustomerSellerIdentity({
      receipt: baseReceipt({ merchantDisplayName: null }),
      publicProfile: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        displayName: "Paul Coffee",
        publicOrganizationId: null,
        logoUrl: "https://cdn.example/logo.png",
        businessPhone: "0917",
        businessEmail: "a@b.com",
        addressLine1: "Iloilo",
        addressLine2: null,
        city: null,
        region: null,
        postalCode: null,
        countryCode: "PH",
      },
    });
    expect(withPublic.identity.businessName).toBe("Paul Coffee");
    expect(withPublic.identity.logoUrl).toContain("logo.png");

    const withMerchant = resolveCustomerSellerIdentity({
      receipt: baseReceipt({ merchantDisplayName: "Kizy Store" }),
      publicProfile: null,
    });
    expect(withMerchant.identity.businessName).toBe("Kizy Store");

    const lastResort = resolveCustomerSellerIdentity({
      receipt: baseReceipt({ merchantDisplayName: null }),
      publicProfile: null,
    });
    expect(lastResort.identity.businessName).toBe("Store");
  });

  it("honors snapshot visibility flags for address/phone/email", () => {
    const { headerVisibility } = resolveCustomerSellerIdentity({
      receipt: baseReceipt({
        sellerDocumentIdentity: {
          businessName: "Mica store",
          showLogo: true,
          showBusinessName: true,
          showBusinessAddress: true,
          showBusinessPhone: true,
          showBusinessEmail: false,
          showBranchName: true,
          showBranchAddress: false,
          identitySource: "saleSnapshot",
        },
      }),
    });
    expect(headerVisibility.showBusinessAddress).toBe(true);
    expect(headerVisibility.showBusinessPhone).toBe(true);
    expect(headerVisibility.showBusinessEmail).toBe(false);
  });

  it("gap-fills branch-only snapshot from public profile (checkout race)", () => {
    const { identity, headerVisibility } = resolveCustomerSellerIdentity({
      receipt: baseReceipt({
        merchantDisplayName: null,
        sellerDocumentIdentity: {
          branchName: "Main Branch",
          showLogo: true,
          showBusinessName: true,
          showBusinessAddress: true,
          showBusinessPhone: true,
          showBusinessEmail: true,
          showBranchName: true,
          showBranchAddress: false,
          identitySource: "saleSnapshot",
        },
      }),
      publicProfile: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        displayName: "Mica store",
        publicOrganizationId: "ORG421278",
        logoUrl: null,
        businessPhone: null,
        businessEmail: "mica@gmail.com",
        addressLine1: null,
        addressLine2: null,
        city: null,
        region: null,
        postalCode: null,
        countryCode: "PH",
      },
    });

    expect(identity.businessName).toBe("Mica store");
    expect(identity.email).toBe("mica@gmail.com");
    expect(identity.address).toBe("PH");
    expect(identity.branchName).toBe("Main Branch");
    expect(headerVisibility.showBusinessEmail).toBe(true);
  });
});

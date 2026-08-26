import type { OfflinePriceAuthority } from "@/api/pos/pos-offline-price-authority-client";
import type { CheckoutSaleLineRequest } from "@/api/pos/pos-sales-client";
import { roundMoney } from "@/cart/sell-cart-helpers";

/**
 * Test doubles for the offline price leases the server issues (RMAP-21 Review Repair 01).
 *
 * The signature here is a placeholder: only the real server can produce one that verifies, which
 * is the property under test everywhere else. These helpers exist so client-side tests can build
 * the shape of a leased line without pretending to hold the signing key.
 */
export const MOCK_AUTHORITY_SIGNATURE = "a".repeat(64);

export function mockPriceAuthority(
  overrides: Partial<OfflinePriceAuthority> & Pick<OfflinePriceAuthority, "productId">,
): OfflinePriceAuthority {
  const issuedAt = new Date();
  const expiresAt = new Date(issuedAt.getTime() + 8 * 60 * 60 * 1000);
  return {
    authorityId: "99999999-9999-4999-8999-999999999999",
    organizationId: "11111111-1111-4111-8111-111111111111",
    branchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    sellingUnitId: null,
    unitPrice: 25,
    unitOfMeasure: "Piece",
    sellingMode: "PerItem",
    issuedAtUtc: issuedAt.toISOString(),
    expiresAtUtc: expiresAt.toISOString(),
    signature: MOCK_AUTHORITY_SIGNATURE,
    ...overrides,
  };
}

/** Builds the leased checkout line the offline cart would queue for one authority. */
export function mockLeasedCheckoutLine(
  authority: OfflinePriceAuthority,
  quantity: number,
): CheckoutSaleLineRequest {
  return {
    productId: authority.productId,
    quantity,
    ...(authority.sellingUnitId
      ? { sellingUnitId: authority.sellingUnitId, enteredQuantity: quantity }
      : {}),
    unitPriceSnapshot: authority.unitPrice,
    unitOfMeasure: authority.unitOfMeasure,
    sellingMode: authority.sellingMode,
    lineTotal: roundMoney(authority.unitPrice * quantity),
    offlinePriceAuthority: {
      authorityId: authority.authorityId,
      organizationId: authority.organizationId,
      productId: authority.productId,
      signature: authority.signature,
      issuedAtUtc: authority.issuedAtUtc,
      expiresAtUtc: authority.expiresAtUtc,
      unitPrice: authority.unitPrice,
      unitOfMeasure: authority.unitOfMeasure,
      sellingMode: authority.sellingMode,
      branchId: authority.branchId ?? null,
      sellingUnitId: authority.sellingUnitId ?? null,
    },
  };
}

import { afterEach, describe, expect, it } from "vitest";
import { EMPTY_PERSONAL_MERCHANT_CART } from "@/features/customer-ordering/personal-merchant-cart";
import {
  clearPersonalMerchantCartStorage,
  loadPersonalMerchantCartFromStorage,
  parsePersistedPersonalMerchantCart,
  PERSONAL_MERCHANT_CART_SCHEMA_VERSION,
  personalMerchantCartStorageKey,
  savePersonalMerchantCartToStorage,
} from "@/features/customer-ordering/personal-merchant-cart-storage";

const USER_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const USER_B = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const SELLER = "11111111-1111-4111-8111-111111111111";
const PRODUCT = "22222222-2222-4222-8222-222222222222";

describe("personal-merchant-cart-storage", () => {
  afterEach(() => {
    window.localStorage.clear();
  });

  it("persists and restores cart for an account key", () => {
    savePersonalMerchantCartToStorage(USER_A, {
      sellerOrganizationId: SELLER,
      organizationDisplayName: "Kizy Store",
      lines: [
        {
          productId: PRODUCT,
          name: "Rice",
          sku: "R1",
          unitOfMeasure: "kg",
          unitPrice: 100,
          quantity: 2,
        },
      ],
    });

    const restored = loadPersonalMerchantCartFromStorage(USER_A);
    expect(restored.sellerOrganizationId).toBe(SELLER);
    expect(restored.organizationDisplayName).toBe("Kizy Store");
    expect(restored.lines).toHaveLength(1);
    expect(restored.lines[0]?.quantity).toBe(2);
    expect(restored.lines[0]?.unitPrice).toBe(100);
  });

  it("isolates carts across accounts", () => {
    savePersonalMerchantCartToStorage(USER_A, {
      sellerOrganizationId: SELLER,
      organizationDisplayName: "Store A",
      lines: [
        {
          productId: PRODUCT,
          name: "Rice",
          sku: null,
          unitOfMeasure: "pc",
          unitPrice: 50,
          quantity: 1,
        },
      ],
    });

    expect(loadPersonalMerchantCartFromStorage(USER_B)).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
    expect(loadPersonalMerchantCartFromStorage(USER_A).lines).toHaveLength(1);
  });

  it("fails safe on malformed storage", () => {
    expect(parsePersistedPersonalMerchantCart(null)).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
    expect(parsePersistedPersonalMerchantCart({ version: 99 })).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
    expect(
      parsePersistedPersonalMerchantCart({
        version: PERSONAL_MERCHANT_CART_SCHEMA_VERSION,
        sellerOrganizationId: "not-a-guid",
        lines: [],
      }),
    ).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
    expect(
      parsePersistedPersonalMerchantCart({
        version: PERSONAL_MERCHANT_CART_SCHEMA_VERSION,
        sellerOrganizationId: SELLER,
        lines: [{ productId: PRODUCT, name: "X", unitOfMeasure: "pc", unitPrice: 1, quantity: -1 }],
      }),
    ).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
    expect(
      parsePersistedPersonalMerchantCart({
        version: PERSONAL_MERCHANT_CART_SCHEMA_VERSION,
        sellerOrganizationId: SELLER,
        lines: [{ productId: PRODUCT, name: "X", unitOfMeasure: "pc", unitPrice: Number.NaN, quantity: 1 }],
      }),
    ).toEqual(EMPTY_PERSONAL_MERCHANT_CART);

    window.localStorage.setItem(personalMerchantCartStorageKey(USER_A), "{not-json");
    expect(loadPersonalMerchantCartFromStorage(USER_A)).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
  });

  it("clears storage when cart is emptied", () => {
    savePersonalMerchantCartToStorage(USER_A, {
      sellerOrganizationId: SELLER,
      organizationDisplayName: "Store",
      lines: [
        {
          productId: PRODUCT,
          name: "Rice",
          sku: null,
          unitOfMeasure: "pc",
          unitPrice: 10,
          quantity: 1,
        },
      ],
    });
    clearPersonalMerchantCartStorage(USER_A);
    expect(window.localStorage.getItem(personalMerchantCartStorageKey(USER_A))).toBeNull();
    expect(loadPersonalMerchantCartFromStorage(USER_A)).toEqual(EMPTY_PERSONAL_MERCHANT_CART);
  });
});

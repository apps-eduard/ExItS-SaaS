import { describe, expect, it } from "vitest";
import { mapCartLinesToCheckoutRequest } from "@/features/checkout/map-cart-to-checkout";
import type { SessionCartLine } from "@/cart/SessionCartProvider";
import { PosApiError } from "@/api/pos/pos-http";
import { mapCheckoutSaleErrorKey } from "@/features/checkout/checkout-sale-errors";

function line(
  partial: Partial<SessionCartLine> & Pick<SessionCartLine, "productId">,
): SessionCartLine {
  return {
    lineKey: partial.lineKey ?? `${partial.productId}:base`,
    productId: partial.productId,
    sku: partial.sku ?? null,
    name: partial.name ?? "Item",
    sellingMode: partial.sellingMode ?? "PerItem",
    productUnitId: partial.productUnitId ?? null,
    unitLabel: partial.unitLabel ?? "pc",
    multiplierToBase: partial.multiplierToBase ?? 1,
    unitPrice: partial.unitPrice ?? 10,
    quantity: partial.quantity ?? 1,
    baseUnitOfMeasure: partial.baseUnitOfMeasure ?? "pc",
    allowsCustomQuantity: partial.allowsCustomQuantity ?? false,
  };
}

describe("mapCartLinesToCheckoutRequest", () => {
  it("maps base lines without snapshots or unit fields", () => {
    const mapped = mapCartLinesToCheckoutRequest([
      line({ productId: "11111111-1111-4111-8111-111111111111", quantity: 3 }),
    ]);
    expect(mapped).toEqual([{ productId: "11111111-1111-4111-8111-111111111111", quantity: 3 }]);
  });

  it("maps unit lines with sellingUnitId and enteredQuantity", () => {
    const unitId = "22222222-2222-4222-8222-222222222222";
    const mapped = mapCartLinesToCheckoutRequest([
      line({
        productId: "11111111-1111-4111-8111-111111111111",
        productUnitId: unitId,
        quantity: 2,
        multiplierToBase: 50,
      }),
    ]);
    expect(mapped).toEqual([
      {
        productId: "11111111-1111-4111-8111-111111111111",
        quantity: 2,
        sellingUnitId: unitId,
        enteredQuantity: 2,
      },
    ]);
  });
});

describe("mapCheckoutSaleErrorKey", () => {
  it("maps known error codes", () => {
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(401, { errorCode: "application.auth.session_expired" }),
      ),
    ).toBe("checkout.errorSession");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(403, { errorCode: "application.auth.product_access_denied" }),
      ),
    ).toBe("checkout.errorProductAccess");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(409, { errorCode: "pos.cashier_shift.no_open_shift" }),
      ),
    ).toBe("checkout.errorNoShift");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(403, { errorCode: "application.pos_device.not_authorized" }),
      ),
    ).toBe("checkout.errorDeviceUnregistered");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(403, { errorCode: "application.pos_device.revoked" }),
      ),
    ).toBe("checkout.errorDeviceRevoked");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(409, {
          errorCode: "application.pos_device.branch_conflict",
          detail: "Wrong branch",
        }),
      ),
    ).toBe("checkout.errorDeviceWrongBranch");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(409, { errorCode: "pos.inventory.insufficient_stock" }),
      ),
    ).toBe("checkout.errorInsufficientStock");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(400, { errorCode: "pos.sale.amount_tendered.below_total" }),
      ),
    ).toBe("checkout.errorInsufficientTender");
    expect(
      mapCheckoutSaleErrorKey(
        new PosApiError(403, {
          errorCode: "application.auth.capability.denied",
          detail: "ApplyCommercialDiscount is required.",
        }),
      ),
    ).toBe("checkout.errorDiscountDenied");
  });
});

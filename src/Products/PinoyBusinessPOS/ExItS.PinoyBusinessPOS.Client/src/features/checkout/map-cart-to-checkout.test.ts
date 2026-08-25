import { describe, expect, it } from "vitest";
import type { OfflinePriceAuthority } from "@/api/pos/pos-offline-price-authority-client";
import type { SessionCartLine } from "@/cart/SessionCartProvider";
import {
  mapCartLinesToCheckoutRequest,
  mapCartLinesToOfflineCheckoutRequest,
} from "@/features/checkout/map-cart-to-checkout";
import { priceAuthorityLeaseKey } from "@/offline/price-authority-cache";
import { mockPriceAuthority } from "@/test/mock-price-authority";

const riceId = "11111111-1111-4111-8111-111111111111";
const sodaId = "22222222-2222-4222-8222-222222222222";
const packUnitId = "33333333-3333-4333-8333-333333333333";

function line(
  partial: Partial<SessionCartLine> & Pick<SessionCartLine, "lineKey" | "productId">,
): SessionCartLine {
  return {
    sku: null,
    name: "Item",
    sellingMode: "PerItem",
    productUnitId: null,
    unitLabel: "pc",
    multiplierToBase: 1,
    unitPrice: 100,
    quantity: 1,
    baseUnitOfMeasure: "pc",
    allowsCustomQuantity: false,
    ...partial,
  };
}

function lookup(...authorities: OfflinePriceAuthority[]) {
  return new Map(
    authorities.map((authority) => [
      priceAuthorityLeaseKey(authority.productId, authority.sellingUnitId),
      authority,
    ]),
  );
}

describe("mapCartLinesToOfflineCheckoutRequest", () => {
  it("prices every line from its lease and totals the cart from those amounts", () => {
    const rice = mockPriceAuthority({
      productId: riceId,
      unitPrice: 62,
      unitOfMeasure: "Kilogram",
      sellingMode: "ByWeight",
    });
    const soda = mockPriceAuthority({ productId: sodaId, unitPrice: 8.5 });

    const mapped = mapCartLinesToOfflineCheckoutRequest(
      [
        line({ lineKey: "a", productId: riceId, quantity: 1.5, unitPrice: 999 }),
        line({ lineKey: "b", productId: sodaId, quantity: 3, unitPrice: 999 }),
      ],
      lookup(rice, soda),
    );

    expect(mapped.ok).toBe(true);
    if (!mapped.ok) {
      return;
    }
    // 62.00 × 1.5 = 93.00 and 8.50 × 3 = 25.50 — the cart's own unitPrice is ignored entirely.
    expect(mapped.lines[0]).toMatchObject({ unitPriceSnapshot: 62, lineTotal: 93 });
    expect(mapped.lines[1]).toMatchObject({ unitPriceSnapshot: 8.5, lineTotal: 25.5 });
    expect(mapped.total).toBe(118.5);
    expect(mapped.lines[0]?.offlinePriceAuthority?.signature).toBe(rice.signature);
    expect(mapped.lines[0]?.unitOfMeasure).toBe("Kilogram");
    expect(mapped.lines[0]?.sellingMode).toBe("ByWeight");
  });

  it("carries the sell unit so the server bills the leased pack price", () => {
    const pack = mockPriceAuthority({
      productId: sodaId,
      sellingUnitId: packUnitId,
      unitPrice: 55,
    });

    const mapped = mapCartLinesToOfflineCheckoutRequest(
      [line({ lineKey: "a", productId: sodaId, productUnitId: packUnitId, quantity: 2 })],
      lookup(pack),
    );

    expect(mapped.ok).toBe(true);
    if (!mapped.ok) {
      return;
    }
    expect(mapped.lines[0]).toMatchObject({
      sellingUnitId: packUnitId,
      enteredQuantity: 2,
      lineTotal: 110,
    });
    expect(mapped.total).toBe(110);
  });

  it("refuses the whole cart when any line has no lease", () => {
    const mapped = mapCartLinesToOfflineCheckoutRequest(
      [line({ lineKey: "a", productId: riceId }), line({ lineKey: "b", productId: sodaId })],
      lookup(mockPriceAuthority({ productId: riceId })),
    );

    expect(mapped).toEqual({ ok: false, unleasedLineKeys: ["b"] });
  });

  it("refuses a lease for the base unit when the cashier picked a pack", () => {
    const mapped = mapCartLinesToOfflineCheckoutRequest(
      [line({ lineKey: "a", productId: sodaId, productUnitId: packUnitId })],
      lookup(mockPriceAuthority({ productId: sodaId })),
    );

    expect(mapped).toEqual({ ok: false, unleasedLineKeys: ["a"] });
  });

  it("refuses a lease whose window closed while the cart was open", () => {
    const issued = new Date(Date.now() - 30 * 60 * 60 * 1000);
    const stale = mockPriceAuthority({
      productId: riceId,
      issuedAtUtc: issued.toISOString(),
      expiresAtUtc: new Date(issued.getTime() + 8 * 60 * 60 * 1000).toISOString(),
    });

    const mapped = mapCartLinesToOfflineCheckoutRequest(
      [line({ lineKey: "a", productId: riceId })],
      lookup(stale),
    );

    expect(mapped).toEqual({ ok: false, unleasedLineKeys: ["a"] });
  });
});

describe("mapCartLinesToCheckoutRequest", () => {
  it("still sends no prices at all when online", () => {
    const [mapped] = mapCartLinesToCheckoutRequest([
      line({ lineKey: "a", productId: riceId, quantity: 2, unitPrice: 100 }),
    ]);

    expect(mapped).toEqual({ productId: riceId, quantity: 2 });
  });
});

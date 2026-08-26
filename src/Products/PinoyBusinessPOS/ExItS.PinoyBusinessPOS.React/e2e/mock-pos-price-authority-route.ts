import type { Page } from "@playwright/test";
import { E2E_BRANCH_ID, E2E_ORG_ID } from "./mock-bound-session";
import { mockCatalogProducts } from "./mock-pos-catalog";

/**
 * Mock of POST /api/v1/pos/offline-price-authorities (RMAP-21 Review Repair 01).
 *
 * Stands in for the half of the contract the browser can see: the server hands out signed leases
 * for the products a device just browsed. The signature is opaque here on purpose — the client
 * must replay it untouched, and only the real server can tell a good one from a forged one.
 */

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
const LEASE_HOURS = 8;

export type MockPriceAuthorityApi = {
  /** Simulates the owner editing the shelf price after leases were already issued. */
  setCatalogPrice(productId: string, unitPrice: number): void;
  currentCatalogPrice(productId: string): number;
  issuedCount(): number;
};

type FixtureProduct = {
  productId: string;
  sellingPrice: number;
  unitOfMeasure: string;
  sellingMode: string;
  units?: Array<{ unitId: string; sellingPrice?: number }>;
};

const fixtures = mockCatalogProducts as ReadonlyArray<FixtureProduct>;

function defaultCatalogPrices(): Map<string, number> {
  const prices = new Map<string, number>();
  for (const product of fixtures) {
    prices.set(product.productId, product.sellingPrice);
    for (const unit of product.units ?? []) {
      prices.set(`${product.productId}::${unit.unitId}`, unit.sellingPrice ?? product.sellingPrice);
    }
  }
  return prices;
}

function unitFacts(productId: string): { unitOfMeasure: string; sellingMode: string } {
  const product = fixtures.find((entry) => entry.productId === productId);
  return {
    unitOfMeasure: product?.unitOfMeasure ?? "Piece",
    sellingMode: product?.sellingMode ?? "PerItem",
  };
}

export async function mockPosPriceAuthorityApi(page: Page): Promise<MockPriceAuthorityApi> {
  const prices = defaultCatalogPrices();
  let issued = 0;

  await page.route("**/pos-api/api/v1/pos/offline-price-authorities**", async (route) => {
    if (route.request().method() !== "POST") {
      return route.fallback();
    }

    const body = route.request().postDataJSON() as {
      productIds?: string[];
      sellingUnitIds?: string[];
    };
    const productIds = body.productIds ?? [];
    const sellingUnitIds = body.sellingUnitIds ?? [];

    const issuedAtUtc = new Date();
    const expiresAtUtc = new Date(issuedAtUtc.getTime() + LEASE_HOURS * 60 * 60 * 1000);

    const authorities = productIds.map((productId, index) => {
      const rawUnitId = sellingUnitIds[index];
      const sellingUnitId = rawUnitId && rawUnitId !== EMPTY_GUID ? rawUnitId : null;
      const priceKey = sellingUnitId ? `${productId}::${sellingUnitId}` : productId;
      const facts = unitFacts(productId);
      issued += 1;
      return {
        authorityId: `${index.toString(16).padStart(8, "0")}-0000-4000-8000-${Date.now().toString(16).padStart(12, "0").slice(-12)}`,
        organizationId: E2E_ORG_ID,
        branchId: E2E_BRANCH_ID,
        productId,
        sellingUnitId,
        unitPrice: prices.get(priceKey) ?? 0,
        unitOfMeasure: facts.unitOfMeasure,
        sellingMode: facts.sellingMode,
        issuedAtUtc: issuedAtUtc.toISOString(),
        expiresAtUtc: expiresAtUtc.toISOString(),
        signature: "e".repeat(64),
      };
    });

    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        authorities,
        issuedAtUtc: issuedAtUtc.toISOString(),
        expiresAtUtc: expiresAtUtc.toISOString(),
      }),
    });
  });

  return {
    setCatalogPrice(productId, unitPrice) {
      prices.set(productId, unitPrice);
    },
    currentCatalogPrice(productId) {
      return prices.get(productId) ?? 0;
    },
    issuedCount() {
      return issued;
    },
  };
}

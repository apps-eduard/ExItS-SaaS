import type { Page } from "@playwright/test";
import {
  filterMockProducts,
  mockCatalogCategories,
  mockChipsProduct,
  mockCokeProduct,
  mockMeatProduct,
  mockRiceProduct,
  MOCK_MEAT_PRODUCT_ID,
  MOCK_RICE_PRODUCT_ID,
} from "./mock-pos-catalog";

export async function mockPosCatalogApi(page: Page, options?: { productDelayMs?: number }) {
  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/pos/operational-branch") && method === "PUT") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: "11111111-1111-1111-1111-111111111111",
          branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          name: "Main Branch",
          deviceMatchesSelectedBranch: false,
          deviceBoundBranchId: null,
          openCashierShiftPresent: false,
        }),
      });
    }

    if (url.includes(`/api/v1/pos/inventory/${MOCK_MEAT_PRODUCT_ID}`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          productId: MOCK_MEAT_PRODUCT_ID,
          organizationId: "11111111-1111-1111-1111-111111111111",
          name: "Ground Pork",
          unitOfMeasure: "Kilogram",
          productStatus: "Active",
          isTracked: true,
          onHandQuantity: 12.5,
          stockStatus: "InStock",
          isLowStock: false,
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
          tracksExpiration: true,
          sellableQuantity: 10,
          expiredQuantity: 2.5,
          nearExpiryQuantity: 0,
        }),
      });
    }

    if (url.includes(`/api/v1/pos/inventory/${MOCK_RICE_PRODUCT_ID}`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          productId: MOCK_RICE_PRODUCT_ID,
          organizationId: "11111111-1111-1111-1111-111111111111",
          name: "Rice",
          unitOfMeasure: "Kilogram",
          productStatus: "Active",
          isTracked: true,
          onHandQuantity: 500,
          stockStatus: "InStock",
          isLowStock: false,
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
        }),
      });
    }

    if (!method || method !== "GET") {
      return route.fulfill({ status: 405, body: "" });
    }

    if (url.includes("/api/v1/pos/catalog/categories")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockCatalogCategories),
      });
    }

    if (url.includes("/by-barcode/4006381333931")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockCokeProduct),
      });
    }

    if (url.includes("/by-barcode/4800012345678")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockRiceProduct),
      });
    }

    if (url.includes("/by-barcode/4800098765432")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockMeatProduct),
      });
    }

    if (url.includes("/by-barcode/")) {
      return route.fulfill({
        status: 404,
        contentType: "application/json",
        body: JSON.stringify({ detail: "Product was not found." }),
      });
    }

    if (url.includes("/by-sku/COKE-330")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockCokeProduct),
      });
    }

    if (url.includes("/by-sku/")) {
      return route.fulfill({
        status: 404,
        contentType: "application/json",
        body: JSON.stringify({ detail: "Product was not found." }),
      });
    }

    if (url.includes("/api/v1/pos/catalog/products")) {
      if (options?.productDelayMs) {
        await new Promise((resolve) => setTimeout(resolve, options.productDelayMs));
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(filterMockProducts(url)),
      });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

export { mockChipsProduct, mockCokeProduct, mockMeatProduct, mockRiceProduct };

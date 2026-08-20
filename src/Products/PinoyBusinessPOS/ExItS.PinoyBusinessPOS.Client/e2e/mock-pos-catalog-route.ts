import type { Page } from "@playwright/test";
import {
  filterMockProducts,
  mockCatalogCategories,
  mockChipsProduct,
  mockCokeProduct,
} from "./mock-pos-catalog";

export async function mockPosCatalogApi(page: Page, options?: { productDelayMs?: number }) {
  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

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

export { mockChipsProduct, mockCokeProduct };

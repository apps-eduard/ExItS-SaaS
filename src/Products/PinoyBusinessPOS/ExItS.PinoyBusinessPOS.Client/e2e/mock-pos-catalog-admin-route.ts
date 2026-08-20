import type { Page } from "@playwright/test";
import {
  MOCK_CHIPS_PRODUCT_ID,
  MOCK_COKE_PRODUCT_ID,
  MOCK_DRINKS_CATEGORY_ID,
  MOCK_SNACKS_CATEGORY_ID,
  mockCatalogCategories,
  mockChipsProduct,
  mockCokeProduct,
} from "./mock-pos-catalog";
import { E2E_BRANCH_ID, E2E_ORG_ID } from "./mock-bound-session";

type MutableCategory = (typeof mockCatalogCategories.items)[number];
type MutableProduct = typeof mockCokeProduct;

function filterProducts(url: string, products: MutableProduct[]) {
  const parsed = new URL(url, "http://127.0.0.1");
  const categoryId = parsed.searchParams.get("categoryId");
  const search = parsed.searchParams.get("search")?.toLowerCase();
  let items = [...products];
  if (categoryId) {
    items = items.filter((product) => product.categoryId === categoryId);
  }
  if (search) {
    items = items.filter(
      (product) =>
        product.name.toLowerCase().includes(search) || product.sku?.toLowerCase().includes(search),
    );
  }
  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 50,
  };
}

/**
 * Mutable in-memory catalog for RMAP-04 admin e2e.
 * Register after mockBound*Session so this handler wins for pos-api.
 */
export async function mockPosCatalogAdminApi(page: Page) {
  const categories: MutableCategory[] = structuredClone(mockCatalogCategories.items);
  const products: MutableProduct[] = structuredClone([mockCokeProduct, mockChipsProduct]);

  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const parsed = new URL(url);

    if (url.includes("/api/v1/pos/operational-branch") && method === "PUT") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: E2E_ORG_ID,
          branchId: E2E_BRANCH_ID,
          name: "Main Branch",
          deviceMatchesSelectedBranch: false,
          deviceBoundBranchId: null,
          openCashierShiftPresent: false,
        }),
      });
    }

    if (url.includes("/api/v1/pos/catalog/categories") && method === "GET") {
      if (/\/categories\/[0-9a-f-]{36}$/i.test(parsed.pathname)) {
        const id = parsed.pathname.split("/").pop()!;
        const category = categories.find((item) => item.categoryId === id);
        if (!category) {
          return route.fulfill({
            status: 404,
            contentType: "application/json",
            body: JSON.stringify({ detail: "Category was not found." }),
          });
        }
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(category),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: categories,
          totalCount: categories.length,
          page: 1,
          pageSize: 100,
        }),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/categories") &&
      method === "POST" &&
      !url.includes("/deactivate") &&
      !url.includes("/reactivate")
    ) {
      const body = route.request().postDataJSON() as { name?: string };
      const created: MutableCategory = {
        categoryId: crypto.randomUUID(),
        organizationId: E2E_ORG_ID,
        name: body.name?.trim() || "Untitled",
        status: "Active",
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: new Date().toISOString(),
      };
      categories.push(created);
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(created),
      });
    }

    if (url.includes("/api/v1/pos/catalog/categories/") && method === "PUT") {
      const id = parsed.pathname.split("/").pop()!;
      const body = route.request().postDataJSON() as {
        name?: string;
        expectedUpdatedAtUtc?: string | null;
      };
      const category = categories.find((item) => item.categoryId === id);
      if (!category) {
        return route.fulfill({
          status: 404,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Category was not found." }),
        });
      }
      if (body.expectedUpdatedAtUtc && body.expectedUpdatedAtUtc !== category.updatedAtUtc) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Category was modified by another user." }),
        });
      }
      category.name = body.name?.trim() || category.name;
      category.updatedAtUtc = new Date().toISOString();
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(category),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/categories/") &&
      url.includes("/deactivate") &&
      method === "POST"
    ) {
      const id = parsed.pathname.split("/").slice(-2)[0]!;
      const category = categories.find((item) => item.categoryId === id);
      if (category) {
        category.status = "Inactive";
        category.updatedAtUtc = new Date().toISOString();
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(category),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/categories/") &&
      url.includes("/reactivate") &&
      method === "POST"
    ) {
      const id = parsed.pathname.split("/").slice(-2)[0]!;
      const category = categories.find((item) => item.categoryId === id);
      if (category) {
        category.status = "Active";
        category.updatedAtUtc = new Date().toISOString();
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(category),
      });
    }

    if (url.includes("/api/v1/pos/catalog/products") && method === "GET") {
      if (/\/products\/[0-9a-f-]{36}$/i.test(parsed.pathname)) {
        const id = parsed.pathname.split("/").pop()!;
        const product = products.find((item) => item.productId === id);
        if (!product) {
          return route.fulfill({
            status: 404,
            contentType: "application/json",
            body: JSON.stringify({ detail: "Product was not found." }),
          });
        }
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(product),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(filterProducts(url, products)),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/products") &&
      method === "POST" &&
      !url.includes("/deactivate") &&
      !url.includes("/reactivate") &&
      !url.includes("/prices")
    ) {
      const body = route.request().postDataJSON() as {
        name?: string;
        sku?: string | null;
        barcode?: string | null;
        categoryId?: string | null;
        unitOfMeasure?: string;
        sellingPrice?: number;
        sellingMode?: string;
        canBeSold?: boolean;
        units?: unknown[];
      };
      if (body.sku && products.some((p) => p.sku === body.sku)) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({ detail: "SKU already exists." }),
        });
      }
      const created = {
        productId: crypto.randomUUID(),
        organizationId: E2E_ORG_ID,
        name: body.name?.trim() || "Untitled",
        sku: body.sku ?? null,
        barcode: body.barcode ?? null,
        categoryId: body.categoryId ?? MOCK_DRINKS_CATEGORY_ID,
        unitOfMeasure: body.unitOfMeasure ?? "Piece",
        sellingMode: body.sellingMode ?? "PerItem",
        sellingPrice: body.sellingPrice ?? 0,
        status: "Active",
        canBeSold: body.canBeSold !== false,
        units: body.units ?? [],
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: new Date().toISOString(),
      } as MutableProduct;
      products.push(created);
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(created),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/products/") &&
      method === "PUT" &&
      !url.includes("/image")
    ) {
      const id = parsed.pathname.split("/").pop()!;
      const body = route.request().postDataJSON() as {
        name?: string;
        sku?: string | null;
        barcode?: string | null;
        categoryId?: string | null;
        expectedUpdatedAtUtc?: string | null;
        canBeSold?: boolean;
      };
      const product = products.find((item) => item.productId === id);
      if (!product) {
        return route.fulfill({
          status: 404,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Product was not found." }),
        });
      }
      if (body.expectedUpdatedAtUtc && body.expectedUpdatedAtUtc !== product.updatedAtUtc) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Product was modified by another user." }),
        });
      }
      if (body.sku && products.some((p) => p.sku === body.sku && p.productId !== id)) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({ detail: "SKU already exists." }),
        });
      }
      product.name = body.name?.trim() || product.name;
      product.sku = body.sku ?? product.sku;
      product.barcode = body.barcode ?? product.barcode;
      product.categoryId = body.categoryId ?? product.categoryId;
      if (typeof body.canBeSold === "boolean") {
        product.canBeSold = body.canBeSold;
      }
      product.updatedAtUtc = new Date().toISOString();
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(product),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/products/") &&
      url.includes("/deactivate") &&
      method === "POST"
    ) {
      const id = parsed.pathname.split("/").slice(-2)[0]!;
      const product = products.find((item) => item.productId === id);
      if (product) {
        product.status = "Inactive";
        product.updatedAtUtc = new Date().toISOString();
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(product),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/products/") &&
      url.includes("/reactivate") &&
      method === "POST"
    ) {
      const id = parsed.pathname.split("/").slice(-2)[0]!;
      const product = products.find((item) => item.productId === id);
      if (product) {
        product.status = "Active";
        product.updatedAtUtc = new Date().toISOString();
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(product),
      });
    }

    if (
      url.includes("/api/v1/pos/catalog/products/") &&
      url.includes("/image") &&
      method === "PUT"
    ) {
      const parts = parsed.pathname.split("/");
      const id = parts[parts.indexOf("products") + 1]!;
      const product = products.find((item) => item.productId === id);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(product ?? mockCokeProduct),
      });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

export { MOCK_CHIPS_PRODUCT_ID, MOCK_COKE_PRODUCT_ID, MOCK_DRINKS_CATEGORY_ID, MOCK_SNACKS_CATEGORY_ID };

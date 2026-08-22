import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const session = {
  sessionId: "11111111-1111-1111-1111-111111111111",
  userId: "22222222-2222-2222-2222-222222222222",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia@example.test",
  expiresAtUtc: "2026-08-19T12:00:00Z",
  absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
  selectedOrganizationId: null,
  selectedOrganizationDisplayName: null,
  organizationSelectionState: "None",
  activeOrganizationCount: 0,
  accountClass: "Platform",
};

const categoryId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const productId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const businessTypeId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

type MockOptions = {
  permissions?: string[];
  categoryPutConflict?: boolean;
  productPutConflict?: boolean;
};

type MockState = {
  antiforgeryRequested: boolean;
  mutationCalls: Array<{ method: string; path: string; csrfHeader: string | null }>;
  categoryListRequests: string[];
  productListRequests: string[];
  categoryDetailGets: number;
  productDetailGets: number;
  categories: Array<Record<string, unknown>>;
  products: Array<Record<string, unknown>>;
};

function basePermissions(): string[] {
  return [
    "platform.permission.view_portfolio",
    "platform.permission.view_global_catalog",
    "platform.permission.manage_global_categories",
    "platform.permission.manage_global_products",
  ];
}

async function mockGlobalCatalog(page: Page, options: MockOptions = {}): Promise<MockState> {
  const permissions = options.permissions ?? basePermissions();
  const state: MockState = {
    antiforgeryRequested: false,
    mutationCalls: [],
    categoryListRequests: [],
    productListRequests: [],
    categoryDetailGets: 0,
    productDetailGets: 0,
    categories: [
      {
        id: categoryId,
        name: "Beverages",
        parentId: null,
        sortOrder: 10,
        status: "Active",
        businessTypes: ["sari-sari"],
        businessTypeIds: [businessTypeId],
        createdAtUtc: "2026-01-01T08:00:00Z",
        updatedAtUtc: "2026-08-01T08:00:00Z",
      },
    ],
    products: [
      {
        id: productId,
        name: "Bottled Water",
        sku: "BW-500",
        barcode: "4800123456789",
        brand: "Refresh",
        globalCategoryId: categoryId,
        unit: "Bottle",
        sellingMode: "PerItem",
        costPrice: 8,
        sellingPrice: 15,
        status: "Active",
        searchTags: ["water"],
        businessTypes: ["sari-sari"],
        businessTypeIds: [businessTypeId],
        hasImage: true,
        imageVersion: 1,
        createdAtUtc: "2026-01-02T08:00:00Z",
        updatedAtUtc: "2026-08-02T08:00:00Z",
      },
    ],
  };

  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: session.email,
        actorType: "PlatformUser",
        platformUserId: session.userId,
        organizationId: null,
        permissions,
      },
    });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    state.antiforgeryRequested = true;
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" } });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 100 } });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/users*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 5 } });
  });
  await page.route("**/api/v1/platform/audit*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 8 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/api/v1/platform/global-catalog/business-types*", async (route) => {
    await route.fulfill({
      json: {
        items: [
          {
            id: businessTypeId,
            code: "sari-sari",
            name: "Sari-Sari Store",
            status: "Active",
            sortOrder: 1,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 100,
      },
    });
  });

  await page.route("**/api/v1/platform/global-catalog/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();
    const csrfHeader = request.headers()["x-xsrf-token"] ?? null;

    if (method !== "GET") {
      state.mutationCalls.push({ method, path, csrfHeader });
    }

    const categoryDetailMatch = path.match(/\/categories\/([0-9a-f-]{36})$/i);
    if (categoryDetailMatch && method === "GET") {
      state.categoryDetailGets += 1;
      const match = state.categories.find((item) => item.id === categoryDetailMatch[1]);
      if (!match) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      await route.fulfill({ json: match });
      return;
    }

    if (path.endsWith("/global-catalog/categories") && method === "GET") {
      state.categoryListRequests.push(url.toString());
      await route.fulfill({
        json: {
          items: state.categories,
          totalCount: state.categories.length,
          page: Number(url.searchParams.get("page") ?? "1"),
          pageSize: 20,
        },
      });
      return;
    }

    if (path.endsWith("/global-catalog/categories") && method === "POST") {
      const body = request.postDataJSON() as Record<string, unknown>;
      const created = {
        id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        name: body.name,
        parentId: body.parentId ?? null,
        sortOrder: body.sortOrder ?? 0,
        status: "Active",
        businessTypes: [],
        businessTypeIds: body.businessTypeIds ?? [],
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
      };
      state.categories = [created, ...state.categories];
      await route.fulfill({ status: 201, json: created });
      return;
    }

    if (categoryDetailMatch && method === "PUT") {
      if (options.categoryPutConflict) {
        await route.fulfill({
          status: 409,
          json: {
            title: "Conflict",
            status: 409,
            detail: "Category was updated by another operator.",
            errorCode: "application.concurrency_conflict",
          },
        });
        return;
      }
      const body = request.postDataJSON() as Record<string, unknown>;
      const existing = state.categories.find((item) => item.id === categoryDetailMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        name: body.name,
        updatedAtUtc: "2026-08-22T08:05:00Z",
      };
      state.categories = state.categories.map((item) =>
        item.id === categoryDetailMatch[1] ? updated : item,
      );
      await route.fulfill({ json: updated });
      return;
    }

    const categoryStatusMatch = path.match(/\/categories\/([0-9a-f-]{36})\/status$/i);
    if (categoryStatusMatch && method === "PATCH") {
      const body = request.postDataJSON() as Record<string, unknown>;
      const existing = state.categories.find((item) => item.id === categoryStatusMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = { ...existing, status: body.status, updatedAtUtc: "2026-08-22T08:06:00Z" };
      state.categories = state.categories.map((item) =>
        item.id === categoryStatusMatch[1] ? updated : item,
      );
      await route.fulfill({ json: updated });
      return;
    }

    const productDetailMatch = path.match(/\/products\/([0-9a-f-]{36})$/i);
    if (productDetailMatch && method === "GET") {
      state.productDetailGets += 1;
      const match = state.products.find((item) => item.id === productDetailMatch[1]);
      if (!match) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      await route.fulfill({ json: match });
      return;
    }

    if (path.endsWith("/global-catalog/products") && method === "GET") {
      state.productListRequests.push(url.toString());
      await route.fulfill({
        json: {
          items: state.products,
          totalCount: state.products.length,
          page: Number(url.searchParams.get("page") ?? "1"),
          pageSize: 20,
        },
      });
      return;
    }

    if (path.endsWith("/global-catalog/products") && method === "POST") {
      const body = request.postDataJSON() as Record<string, unknown>;
      const created = {
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        name: body.name,
        sku: body.sku,
        barcode: body.barcode ?? null,
        brand: body.brand,
        globalCategoryId: body.globalCategoryId,
        unit: body.unit,
        sellingMode: body.sellingMode ?? "PerItem",
        status: "Draft",
        businessTypes: [],
        businessTypeIds: body.businessTypeIds ?? [],
        hasImage: false,
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
      };
      state.products = [created, ...state.products];
      await route.fulfill({ status: 201, json: created });
      return;
    }

    if (productDetailMatch && method === "PUT") {
      if (options.productPutConflict) {
        await route.fulfill({
          status: 409,
          json: {
            title: "Conflict",
            status: 409,
            detail: "Product was updated by another operator.",
            errorCode: "application.concurrency_conflict",
          },
        });
        return;
      }
      const body = request.postDataJSON() as Record<string, unknown>;
      const existing = state.products.find((item) => item.id === productDetailMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        name: body.name,
        updatedAtUtc: "2026-08-22T08:05:00Z",
      };
      state.products = state.products.map((item) =>
        item.id === productDetailMatch[1] ? updated : item,
      );
      await route.fulfill({ json: updated });
      return;
    }

    const productImageMatch = path.match(/\/products\/([0-9a-f-]{36})\/image(?:\/(thumb|medium))?$/i);
    if (productImageMatch && method === "GET") {
      await route.fulfill({
        status: 200,
        contentType: "image/webp",
        body: Buffer.from("fake-image-bytes"),
      });
      return;
    }
    if (productImageMatch && method === "PUT") {
      const existing = state.products.find((item) => item.id === productImageMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        hasImage: true,
        imageVersion: Number(existing.imageVersion ?? 0) + 1,
        updatedAtUtc: "2026-08-22T08:07:00Z",
      };
      state.products = state.products.map((item) =>
        item.id === productImageMatch[1] ? updated : item,
      );
      await route.fulfill({ json: updated });
      return;
    }
    if (productImageMatch && method === "DELETE") {
      const existing = state.products.find((item) => item.id === productImageMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        hasImage: false,
        imageVersion: null,
        updatedAtUtc: "2026-08-22T08:08:00Z",
      };
      state.products = state.products.map((item) =>
        item.id === productImageMatch[1] ? updated : item,
      );
      await route.fulfill({ status: 204, body: "" });
      return;
    }

    await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
  });

  return state;
}

async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
}

test.describe("global catalog navigation and permissions", () => {
  test("shows Global Catalog nav links and opens categories and products routes", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await mockGlobalCatalog(page);
    await page.goto("/admin");
    await expect(page.getByRole("link", { name: "Categories" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Global Products" })).toBeVisible();
    await page.getByRole("link", { name: "Categories" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/categories$/);
    await expect(page.getByRole("heading", { name: "Categories" })).toBeVisible();
    await page.getByRole("link", { name: "Global Products" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/products$/);
    await expect(page.getByRole("heading", { name: "Global Products" })).toBeVisible();
  });

  test("view-only permission hides mutation controls but keeps lists readable", async ({ page }) => {
    await mockGlobalCatalog(page, {
      permissions: ["platform.permission.view_portfolio", "platform.permission.view_global_catalog"],
    });
    await page.goto("/admin/global-catalog/categories");
    await expect(page.getByRole("heading", { name: "Categories" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Create category" })).toHaveCount(0);
    await page.goto("/admin/global-catalog/products");
    await expect(page.getByRole("heading", { name: "Global Products" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Create product" })).toHaveCount(0);
  });

  test("manageGlobalCategories unlocks category mutations only", async ({ page }) => {
    await mockGlobalCatalog(page, {
      permissions: [
        "platform.permission.view_portfolio",
        "platform.permission.view_global_catalog",
        "platform.permission.manage_global_categories",
      ],
    });
    await page.goto("/admin/global-catalog/categories");
    await expect(page.getByRole("link", { name: "Create category" })).toBeVisible();
    await page.goto("/admin/global-catalog/products");
    await expect(page.getByRole("link", { name: "Create product" })).toHaveCount(0);
  });

  test("manageGlobalProducts unlocks product mutations only", async ({ page }) => {
    await mockGlobalCatalog(page, {
      permissions: [
        "platform.permission.view_portfolio",
        "platform.permission.view_global_catalog",
        "platform.permission.manage_global_products",
      ],
    });
    await page.goto("/admin/global-catalog/products");
    await expect(page.getByRole("link", { name: "Create product" })).toBeVisible();
    await page.goto("/admin/global-catalog/categories");
    await expect(page.getByRole("link", { name: "Create category" })).toHaveCount(0);
  });
});

test.describe("global catalog category browser flows", () => {
  test("lists, searches, filters, and opens category detail", async ({ page }) => {
    const mock = await mockGlobalCatalog(page);
    await page.goto("/admin/global-catalog/categories");
    await expect(page.getByRole("link", { name: "Beverages" })).toBeVisible();
    await page.getByLabel("Search").fill("bev");
    await page.getByRole("button", { name: "Search" }).click();
    await expect
      .poll(() => mock.categoryListRequests.some((url) => url.includes("search=bev")))
      .toBe(true);
    await page.selectOption("#gc-category-status", "Active");
    await expect
      .poll(() => mock.categoryListRequests.some((url) => url.includes("status=Active")))
      .toBe(true);
    await page.getByRole("link", { name: "Beverages" }).click();
    await expect(page).toHaveURL(new RegExp(`/admin/global-catalog/categories/${categoryId}$`));
    await expect(page.getByRole("heading", { name: "Beverages" })).toBeVisible();
  });
});

test.describe("global catalog product browser flows", () => {
  test("lists, searches, filters by sku and barcode query, and opens detail", async ({ page }) => {
    const mock = await mockGlobalCatalog(page);
    await page.goto("/admin/global-catalog/products");
    await expect(page.getByRole("link", { name: "Bottled Water" })).toBeVisible();
    await page.getByLabel("Search").fill("water");
    await page.getByRole("button", { name: "Search" }).click();
    await expect
      .poll(() => mock.productListRequests.some((url) => url.includes("search=water")))
      .toBe(true);
    await page.fill("#gc-product-sku", "BW-500");
    await expect
      .poll(() => mock.productListRequests.some((url) => url.includes("sku=BW-500")))
      .toBe(true);
    await page.goto("/admin/global-catalog/products?barcode=4800123456789");
    await expect
      .poll(() => mock.productListRequests.some((url) => url.includes("barcode=4800123456789")))
      .toBe(true);
    await page.getByRole("link", { name: "Bottled Water" }).click();
    await expect(page).toHaveURL(new RegExp(`/admin/global-catalog/products/${productId}$`));
    await expect(page.getByRole("heading", { name: "Bottled Water" })).toBeVisible();
  });
});

test.describe("global catalog mutations and conflicts", () => {
  test("category create uses antiforgery and POST path", async ({ page }) => {
    const mock = await mockGlobalCatalog(page);
    await page.goto("/admin/global-catalog/categories/new");
    await page.getByLabel("Name").fill("Snacks");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/categories\//);
    await expect.poll(() => mock.antiforgeryRequested).toBe(true);
    await expect
      .poll(() =>
        mock.mutationCalls.some(
          (call) =>
            call.method === "POST" &&
            call.path.endsWith("/global-catalog/categories") &&
            call.csrfHeader === "test-antiforgery-token",
        ),
      )
      .toBe(true);
  });

  test("product create uses antiforgery and POST path", async ({ page }) => {
    const mock = await mockGlobalCatalog(page);
    await page.goto("/admin/global-catalog/products/new");
    await page.getByLabel("Name").fill("Instant Noodles");
    await page.getByLabel("SKU").fill("IN-001");
    await page.getByLabel("Brand").fill("Quick");
    await page.getByLabel("Category").selectOption(categoryId);
    await page.getByLabel("Unit").selectOption("Pack");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/products\//);
    await expect
      .poll(() =>
        mock.mutationCalls.some(
          (call) =>
            call.method === "POST" &&
            call.path.endsWith("/global-catalog/products") &&
            call.csrfHeader === "test-antiforgery-token",
        ),
      )
      .toBe(true);
  });

  test("409 conflict on category edit refetches detail and shows truthful message", async ({ page }) => {
    const mock = await mockGlobalCatalog(page, { categoryPutConflict: true });
    await page.goto(`/admin/global-catalog/categories/${categoryId}/edit`);
    await page.getByLabel("Name").fill("Beverages Updated");
    const beforeGets = mock.categoryDetailGets;
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByText("Category was updated by another operator.")).toBeVisible();
    expect(mock.categoryDetailGets).toBeGreaterThan(beforeGets);
    await expect(page.getByLabel("Name")).toHaveValue("Beverages Updated");
  });
});

test.describe("global catalog image browser flow", () => {
  test("loads preview and supports upload and remove endpoints", async ({ page }) => {
    const mock = await mockGlobalCatalog(page);
    await page.goto(`/admin/global-catalog/products/${productId}`);
    await expect(page.getByRole("heading", { name: "Product image" })).toBeVisible();
    await expect(page.getByRole("img", { name: "Bottled Water" })).toBeVisible();
    await page.locator('input[type="file"]').setInputFiles({
      name: "sample.webp",
      mimeType: "image/webp",
      buffer: Buffer.from("RIFF....WEBP"),
    });
    await expect
      .poll(() =>
        mock.mutationCalls.some(
          (call) => call.method === "PUT" && call.path.includes(`/products/${productId}/image`),
        ),
      )
      .toBe(true);
    await page.getByRole("button", { name: "Remove image" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Remove image" }).click();
    await expect
      .poll(() =>
        mock.mutationCalls.some(
          (call) => call.method === "DELETE" && call.path.includes(`/products/${productId}/image`),
        ),
      )
      .toBe(true);
  });
});

test.describe("global catalog accessibility and responsive layout", () => {
  test("categories page has no serious axe violations at desktop and tablet", async ({ page }) => {
    await mockGlobalCatalog(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/admin/global-catalog/categories");
    await expect(page.getByRole("link", { name: "Beverages" })).toBeVisible();
    let accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    await assertNoHorizontalOverflow(page);

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto("/admin/global-catalog/categories");
    await expect(page.getByRole("link", { name: "Beverages" })).toBeVisible();
    accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    await assertNoHorizontalOverflow(page);
  });

  test("product detail has no serious axe violations at desktop and tablet", async ({ page }) => {
    await mockGlobalCatalog(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(`/admin/global-catalog/products/${productId}`);
    await expect(page.getByRole("heading", { name: "Bottled Water" })).toBeVisible();
    let accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    await assertNoHorizontalOverflow(page);

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(`/admin/global-catalog/products/${productId}`);
    await expect(page.getByRole("heading", { name: "Bottled Water" })).toBeVisible();
    accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    await assertNoHorizontalOverflow(page);
  });
});

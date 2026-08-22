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

const draftId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const publishedId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const archivedId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const businessTypeId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

type MockState = {
  antiforgeryRequested: boolean;
  mutationCalls: Array<{ method: string; path: string; csrfHeader: string | null }>;
  listRequests: string[];
  templates: Array<Record<string, unknown>>;
  availableProducts: Array<Record<string, unknown>>;
};

function basePermissions(): string[] {
  return [
    "platform.permission.view_portfolio",
    "platform.permission.view_global_catalog",
    "platform.permission.manage_catalog_templates",
    "platform.permission.publish_catalog_templates",
  ];
}

async function mockTemplates(
  page: Page,
  options: { permissions?: string[]; updateConflict?: boolean } = {},
) {
  const permissions = options.permissions ?? basePermissions();
  const state: MockState = {
    antiforgeryRequested: false,
    mutationCalls: [],
    listRequests: [],
    availableProducts: [
      {
        id: "11111111-1111-1111-1111-111111111111",
        name: "Canned Tuna",
        sku: "TUNA-001",
        brand: "Blue Sea",
        status: "Active",
        unit: "Can",
        sellingMode: "PerItem",
      },
      {
        id: "22222222-2222-2222-2222-222222222222",
        name: "Instant Noodles",
        sku: "NOOD-001",
        brand: "Quick Meal",
        status: "Active",
        unit: "Pack",
        sellingMode: "PerItem",
      },
    ],
    templates: [
      {
        id: draftId,
        name: "Sari-Sari Starter",
        slug: "sari-sari-starter",
        description: "Starter catalog",
        iconReference: "store",
        primaryBusinessType: "sari-sari",
        primaryBusinessTypeId: businessTypeId,
        status: "Draft",
        defaultBatchSize: 20,
        selectionMode: "Curated",
        productCount: 1,
        firstBatchCount: 1,
        createdAtUtc: "2026-01-01T08:00:00Z",
        updatedAtUtc: "2026-08-01T08:00:00Z",
        products: [
          {
            id: "99999999-9999-9999-9999-999999999991",
            globalProductId: "11111111-1111-1111-1111-111111111111",
            sortOrder: 1,
            isFeatured: true,
            isFirstBatch: true,
            productName: "Canned Tuna",
            sku: "TUNA-001",
          },
        ],
      },
      {
        id: publishedId,
        name: "Mini Grocery Essentials",
        slug: "mini-grocery-essentials",
        description: "Published template",
        primaryBusinessType: "sari-sari",
        primaryBusinessTypeId: businessTypeId,
        status: "Published",
        defaultBatchSize: 25,
        selectionMode: "Hybrid",
        publishedAtUtc: "2026-07-01T08:00:00Z",
        productCount: 1,
        firstBatchCount: 1,
        createdAtUtc: "2026-02-01T08:00:00Z",
        updatedAtUtc: "2026-08-02T08:00:00Z",
        products: [
          {
            id: "99999999-9999-9999-9999-999999999992",
            globalProductId: "11111111-1111-1111-1111-111111111111",
            sortOrder: 1,
            isFeatured: false,
            isFirstBatch: true,
            productName: "Canned Tuna",
            sku: "TUNA-001",
          },
        ],
      },
      {
        id: archivedId,
        name: "Legacy Template",
        slug: "legacy-template",
        description: "Archived template",
        primaryBusinessType: "sari-sari",
        primaryBusinessTypeId: businessTypeId,
        status: "Archived",
        defaultBatchSize: 10,
        selectionMode: "Auto",
        productCount: 0,
        firstBatchCount: 0,
        createdAtUtc: "2026-03-01T08:00:00Z",
        updatedAtUtc: "2026-08-03T08:00:00Z",
        products: [],
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
  await page.route("**/api/v1/platform/global-catalog/categories*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/api/v1/platform/global-catalog/products/imports*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/api/v1/platform/global-catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/api/v1/platform/global-catalog/business-types**", async (route) => {
    await route.fulfill({
      json: {
        items: [
          {
            id: businessTypeId,
            code: "sari-sari",
            name: "Sari-Sari Store",
            status: "Active",
            sortOrder: 1,
            createdAtUtc: "2026-01-01T08:00:00Z",
            updatedAtUtc: "2026-08-01T08:00:00Z",
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 100,
      },
    });
  });

  await page.route("**/api/v1/platform/global-catalog/templates**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();
    const csrfHeader = request.headers()["x-xsrf-token"] ?? null;

    if (method !== "GET") {
      state.mutationCalls.push({ method, path, csrfHeader });
    }

    const availableMatch = path.match(/\/templates\/([0-9a-f-]{36})\/available-products$/i);
    if (availableMatch && method === "GET") {
      const template = state.templates.find((item) => item.id === availableMatch[1]);
      const assigned = new Set(
        ((template?.products as Array<Record<string, unknown>>) ?? []).map(
          (product) => product.globalProductId,
        ),
      );
      const items = state.availableProducts.filter((product) => !assigned.has(product.id));
      await route.fulfill({
        json: {
          items,
          totalCount: items.length,
          page: 1,
          pageSize: 10,
        },
      });
      return;
    }

    const lifecycleMatch = path.match(/\/templates\/([0-9a-f-]{36})\/(publish|unpublish|archive)$/i);
    const detailMatch = path.match(/\/templates\/([0-9a-f-]{36})$/i);
    const productMatch = path.match(/\/templates\/([0-9a-f-]{36})\/products(?:\/([0-9a-f-]{36}))?$/i);

    if (path.endsWith("/global-catalog/templates") && method === "GET") {
      state.listRequests.push(url.toString());
      let filtered = [...state.templates];
      const status = url.searchParams.get("status");
      if (status) {
        filtered = filtered.filter((item) => item.status === status);
      }
      await route.fulfill({
        json: {
          items: filtered.map((item) => {
            const copy = { ...item };
            delete copy.products;
            return copy;
          }),
          totalCount: filtered.length,
          page: Number(url.searchParams.get("page") ?? "1"),
          pageSize: 20,
        },
      });
      return;
    }

    if (path.endsWith("/global-catalog/templates") && method === "POST") {
      const body = request.postDataJSON() as Record<string, unknown>;
      const created = {
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        name: body.name,
        slug: body.slug ?? "new-template",
        description: body.description ?? null,
        iconReference: body.iconReference ?? null,
        primaryBusinessType: "sari-sari",
        primaryBusinessTypeId: body.primaryBusinessTypeId ?? businessTypeId,
        status: "Draft",
        defaultBatchSize: body.defaultBatchSize ?? 20,
        selectionMode: body.selectionMode ?? "Curated",
        productCount: 0,
        firstBatchCount: 0,
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
        products: [],
      };
      state.templates = [created, ...state.templates];
      await route.fulfill({ status: 201, json: created });
      return;
    }

    if (detailMatch && method === "GET") {
      const match = state.templates.find((item) => item.id === detailMatch[1]);
      if (!match) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      await route.fulfill({ json: match });
      return;
    }

    if (detailMatch && method === "PUT") {
      if (options.updateConflict) {
        await route.fulfill({
          status: 409,
          json: {
            title: "Conflict",
            status: 409,
            detail: "Catalog template was updated by another operator.",
            errorCode: "application.concurrency_conflict",
          },
        });
        return;
      }
      const body = request.postDataJSON() as Record<string, unknown>;
      const existing = state.templates.find((item) => item.id === detailMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        name: body.name,
        slug: body.slug,
        description: body.description,
        defaultBatchSize: body.defaultBatchSize,
        selectionMode: body.selectionMode,
        updatedAtUtc: "2026-08-22T08:05:00Z",
      };
      state.templates = state.templates.map((item) => (item.id === detailMatch[1] ? updated : item));
      await route.fulfill({ json: updated });
      return;
    }

    if (lifecycleMatch && method === "POST") {
      const existing = state.templates.find((item) => item.id === lifecycleMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const action = lifecycleMatch[2];
      const updated = {
        ...existing,
        status:
          action === "publish" ? "Published" : action === "unpublish" ? "Draft" : "Archived",
        publishedAtUtc: action === "publish" ? "2026-08-22T08:06:00Z" : existing.publishedAtUtc,
        updatedAtUtc: "2026-08-22T08:06:00Z",
      };
      state.templates = state.templates.map((item) => (item.id === lifecycleMatch[1] ? updated : item));
      await route.fulfill({ json: updated });
      return;
    }

    if (productMatch && method === "POST" && path.endsWith("/products")) {
      const existing = state.templates.find((item) => item.id === productMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const body = request.postDataJSON() as Record<string, unknown>;
      const source = state.availableProducts.find((product) => product.id === body.globalProductId);
      const products = [
        ...((existing.products as Array<Record<string, unknown>>) ?? []),
        {
          id: "99999999-9999-9999-9999-999999999999",
          globalProductId: body.globalProductId,
          sortOrder: 1,
          isFeatured: false,
          isFirstBatch: false,
          productName: source?.name,
          sku: source?.sku,
        },
      ];
      const updated = {
        ...existing,
        products,
        productCount: products.length,
        updatedAtUtc: "2026-08-22T08:07:00Z",
      };
      state.templates = state.templates.map((item) => (item.id === productMatch[1] ? updated : item));
      await route.fulfill({ json: updated });
      return;
    }

    await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
  });

  return state;
}

test.describe("global catalog templates", () => {
  test("navigates to templates list", async ({ page }) => {
    await mockTemplates(page);
    await page.goto("/admin");
    await page.getByRole("link", { name: "Templates" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/templates$/);
    await expect(page.getByRole("heading", { name: "Templates" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Sari-Sari Starter" })).toBeVisible();
  });

  test("creates template with antiforgery", async ({ page }) => {
    const mock = await mockTemplates(page);
    await page.goto("/admin/global-catalog/templates/new");
    await page.getByLabel("Name").fill("Neighborhood Essentials");
    await page.getByLabel("Primary business type").selectOption(businessTypeId);
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/templates\//);
    await expect.poll(() => mock.antiforgeryRequested).toBe(true);
    await expect
      .poll(() =>
        mock.mutationCalls.some(
          (call) =>
            call.method === "POST" &&
            call.path.endsWith("/global-catalog/templates") &&
            call.csrfHeader === "test-antiforgery-token",
        ),
      )
      .toBe(true);
  });

  test("edit and detail routes work", async ({ page }) => {
    await mockTemplates(page);
    await page.goto(`/admin/global-catalog/templates/${draftId}/edit`);
    await page.getByLabel("Name").fill("Updated Starter");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page).toHaveURL(new RegExp(`/admin/global-catalog/templates/${draftId}$`));
    await expect(page.getByRole("heading", { name: "Updated Starter" })).toBeVisible();
  });

  test("publish, unpublish, and archive lifecycle", async ({ page }) => {
    await mockTemplates(page);
    await page.goto(`/admin/global-catalog/templates/${draftId}`);
    await page.getByRole("button", { name: "Publish" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Publish" }).click();
    await expect(page.getByRole("button", { name: "Unpublish" })).toBeVisible();
    await page.getByRole("button", { name: "Unpublish" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Unpublish" }).click();
    await expect(page.getByRole("button", { name: "Publish" })).toBeVisible();
    await page.getByRole("button", { name: "Archive" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Archive" }).click();
    await expect(page.getByRole("button", { name: "Publish" })).toHaveCount(0);
  });

  test("composition assigns available product", async ({ page }) => {
    await mockTemplates(page, {
      permissions: basePermissions(),
    });
    await page.goto(`/admin/global-catalog/templates/${draftId}`);
    await page.getByRole("button", { name: "Assign" }).first().click();
    await expect(page.getByText("Instant Noodles")).toBeVisible();
  });

  test("view-only hides mutation controls", async ({ page }) => {
    await mockTemplates(page, {
      permissions: ["platform.permission.view_portfolio", "platform.permission.view_global_catalog"],
    });
    await page.goto(`/admin/global-catalog/templates/${draftId}`);
    await expect(page.getByRole("heading", { name: "Sari-Sari Starter" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Edit" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Publish" })).toHaveCount(0);
    await expect(page.getByText("Available products")).toHaveCount(0);
  });

  test("legacy catalog templates route redirects", async ({ page }) => {
    await mockTemplates(page);
    await page.goto("/admin/catalog/templates");
    await expect(page).toHaveURL(/\/admin\/global-catalog\/templates$/);
  });

  test("passes axe at desktop and tablet", async ({ page }) => {
    await mockTemplates(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/admin/global-catalog/templates");
    await expect(page.getByRole("heading", { name: "Templates" })).toBeVisible();
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(`/admin/global-catalog/templates/${draftId}`);
    await expect(page.getByRole("heading", { name: "Sari-Sari Starter" })).toBeVisible();
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  });
});

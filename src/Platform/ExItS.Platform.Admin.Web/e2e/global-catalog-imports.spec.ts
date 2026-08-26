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

const validatedId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const failedId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeee01";

type MockState = {
  antiforgeryRequested: boolean;
  uploadCalls: Array<{ csrfHeader: string | null; idempotencyKey: string | null }>;
  confirmCalls: Array<{ csrfHeader: string | null }>;
  listRequests: string[];
  templateRequests: number;
  jobs: Array<Record<string, unknown>>;
};

function basePermissions(): string[] {
  return [
    "platform.permission.view_portfolio",
    "platform.permission.view_global_catalog",
    "platform.permission.import_global_products",
  ];
}

async function mockImports(page: Page, options: { permissions?: string[] } = {}) {
  const permissions = options.permissions ?? basePermissions();
  const state: MockState = {
    antiforgeryRequested: false,
    uploadCalls: [],
    confirmCalls: [],
    listRequests: [],
    templateRequests: 0,
    jobs: [
      {
        id: validatedId,
        fileName: "validated-import.csv",
        fileFormat: "Csv",
        fileSizeBytes: 1024,
        fileSha256: "a".repeat(64),
        requestedBy: session.email,
        status: "Validated",
        totalCount: 2,
        processedCount: 0,
        importedCount: 0,
        skippedCount: 0,
        failedCount: 0,
        pendingCount: 2,
        validProductCount: 2,
        existingCategoriesReferencedCount: 1,
        newCategoriesToCreateCount: 1,
        warningCount: 1,
        previewSummary: "2 valid products, 1 new category will be created.",
        createdAtUtc: "2026-08-20T08:00:00Z",
        updatedAtUtc: "2026-08-20T08:01:00Z",
        previewItems: [
          {
            id: "11111111-1111-1111-1111-111111111101",
            rowNumber: 2,
            name: "Sardines",
            sku: "SAR-1",
            categoryName: "Pantry",
            unit: "Piece",
            status: "Valid",
            willCreateCategory: false,
          },
        ],
      },
      {
        id: failedId,
        fileName: "failed-import.csv",
        fileFormat: "Csv",
        fileSizeBytes: 640,
        fileSha256: "e".repeat(64),
        requestedBy: session.email,
        status: "Failed",
        totalCount: 2,
        processedCount: 2,
        importedCount: 0,
        skippedCount: 0,
        failedCount: 2,
        pendingCount: 0,
        validProductCount: 0,
        existingCategoriesReferencedCount: 0,
        newCategoriesToCreateCount: 0,
        warningCount: 0,
        errorSummary: "All rows failed validation.",
        createdAtUtc: "2026-08-16T08:00:00Z",
        updatedAtUtc: "2026-08-16T08:05:00Z",
        completedAtUtc: "2026-08-16T08:05:00Z",
        previewItems: [],
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
  await page.route("**/api/v1/platform/global-catalog/products**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();

    if (path.endsWith("/imports/template.csv") && method === "GET") {
      state.templateRequests += 1;
      await route.fulfill({
        contentType: "text/csv",
        body: "name,unit,sku,brand\nSample,Piece,SKU-1,Brand\n",
        headers: {
          "Content-Disposition": 'attachment; filename="exits-global-product-import-template.csv"',
        },
      });
      return;
    }

    if (path.endsWith("/products/imports") && method === "GET") {
      state.listRequests.push(url.toString());
      let filtered = [...state.jobs];
      const status = url.searchParams.get("status");
      if (status) {
        filtered = filtered.filter((job) => job.status === status);
      }
      await route.fulfill({
        json: {
          items: filtered.map((job) => ({ ...job, previewItems: undefined })),
          totalCount: filtered.length,
          page: Number(url.searchParams.get("page") ?? "1"),
          pageSize: 20,
        },
      });
      return;
    }

    if (path.endsWith("/products/imports") && method === "POST") {
      state.uploadCalls.push({
        csrfHeader: request.headers()["x-xsrf-token"] ?? null,
        idempotencyKey: request.headers()["idempotency-key"] ?? null,
      });
      const created = {
        id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        fileName: "uploaded.csv",
        fileFormat: "Csv",
        fileSizeBytes: 128,
        fileSha256: "f".repeat(64),
        requestedBy: session.email,
        status: "Validated",
        totalCount: 1,
        processedCount: 0,
        importedCount: 0,
        skippedCount: 0,
        failedCount: 0,
        pendingCount: 1,
        validProductCount: 1,
        existingCategoriesReferencedCount: 1,
        newCategoriesToCreateCount: 0,
        warningCount: 0,
        previewSummary: "1 valid product.",
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
        previewItems: [],
      };
      state.jobs = [created, ...state.jobs];
      await route.fulfill({ status: 201, json: created });
      return;
    }

    const confirmMatch = path.match(/\/imports\/([0-9a-f-]{36})\/confirm$/i);
    if (confirmMatch && method === "POST") {
      state.confirmCalls.push({ csrfHeader: request.headers()["x-xsrf-token"] ?? null });
      const existing = state.jobs.find((job) => job.id === confirmMatch[1]);
      const updated = { ...existing, status: "Queued", currentStage: "Queued" };
      state.jobs = state.jobs.map((job) => (job.id === confirmMatch[1] ? updated : job));
      await route.fulfill({ json: updated });
      return;
    }

    const errorsMatch = path.match(/\/imports\/([0-9a-f-]{36})\/errors$/i);
    if (errorsMatch && method === "GET") {
      await route.fulfill({
        json: {
          items: [
            {
              id: "22222222-2222-2222-2222-000000000001",
              rowNumber: 2,
              name: "Bad Product",
              sku: "BAD-1",
              status: "Failed",
              errorCode: "application.catalog_import.validation",
              errorMessage: "Invalid row.",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        },
      });
      return;
    }

    const detailMatch = path.match(/\/imports\/([0-9a-f-]{36})$/i);
    if (detailMatch && method === "GET") {
      const match = state.jobs.find((job) => job.id === detailMatch[1]);
      if (!match) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      await route.fulfill({ json: match });
      return;
    }

    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/api/v1/platform/global-catalog/business-types*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });

  return state;
}

test.describe("global catalog imports", () => {
  test("navigates to imports list", async ({ page }) => {
    await mockImports(page);
    await page.goto("/admin");
    await page.getByRole("link", { name: "Imports" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/imports$/);
    await expect(page.getByRole("heading", { name: "Imports" })).toBeVisible();
    await expect(page.getByRole("link", { name: "validated-import.csv" })).toBeVisible();
  });

  test("status filter hits list API", async ({ page }) => {
    const mock = await mockImports(page);
    await page.goto("/admin/global-catalog/imports");
    await page.selectOption("#gc-import-status", "Failed");
    await expect.poll(() => mock.listRequests.some((url) => url.includes("status=Failed"))).toBe(true);
  });

  test("downloads template from server endpoint", async ({ page }) => {
    const mock = await mockImports(page);
    await page.goto("/admin/global-catalog/imports");
    await page.getByRole("button", { name: "Download CSV template" }).click();
    await expect.poll(() => mock.templateRequests).toBeGreaterThan(0);
  });

  test("uploads multipart without CSRF and confirm uses CSRF", async ({ page }) => {
    const mock = await mockImports(page);
    await page.goto("/admin/global-catalog/imports");
    await page.setInputFiles("#gc-import-file", {
      name: "products.csv",
      mimeType: "text/csv",
      buffer: Buffer.from("name,unit,sku,brand\nA,Piece,SKU-1,Brand\n"),
    });
    await page.getByRole("button", { name: "Upload and validate" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/imports\//);
    expect(mock.uploadCalls.some((call) => call.csrfHeader === null)).toBe(true);

    await page.goto(`/admin/global-catalog/imports/${validatedId}`);
    await page.getByRole("button", { name: "Confirm import" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Confirm import" }).click();
    await expect.poll(() => mock.confirmCalls.length).toBeGreaterThan(0);
    expect(mock.antiforgeryRequested).toBe(true);
    expect(mock.confirmCalls.some((call) => call.csrfHeader === "test-antiforgery-token")).toBe(true);
  });

  test("shows import detail preview and errors", async ({ page }) => {
    await mockImports(page);
    await page.goto(`/admin/global-catalog/imports/${validatedId}`);
    await expect(page.getByRole("heading", { name: "validated-import.csv" })).toBeVisible();
    await expect(page.getByText("Sardines")).toBeVisible();
    await page.goto(`/admin/global-catalog/imports/${failedId}`);
    await expect(page.getByText("Bad Product")).toBeVisible();
  });

  test("redirects legacy catalog imports route", async ({ page }) => {
    await mockImports(page);
    await page.goto("/admin/catalog/imports");
    await expect(page).toHaveURL(/\/admin\/global-catalog\/imports$/);
  });

  test("view-only permission hides imports nav and route", async ({ page }) => {
    await mockImports(page, {
      permissions: ["platform.permission.view_portfolio", "platform.permission.view_global_catalog"],
    });
    await page.goto("/admin");
    await expect(page.getByRole("link", { name: "Imports" })).toHaveCount(0);
    await page.goto("/admin/global-catalog/imports");
    await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
  });

  test("axe and viewport checks", async ({ page }) => {
    await mockImports(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/admin/global-catalog/imports");
    let accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    const desktopOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(desktopOverflow).toBe(false);

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(`/admin/global-catalog/imports/${validatedId}`);
    accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    const tabletOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(tabletOverflow).toBe(false);
  });
});

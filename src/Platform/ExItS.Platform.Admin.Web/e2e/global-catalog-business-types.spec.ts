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

const activeId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const inactiveId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

type MockState = {
  antiforgeryRequested: boolean;
  mutationCalls: Array<{ method: string; path: string; csrfHeader: string | null }>;
  listRequests: string[];
  detailGets: number;
  items: Array<Record<string, unknown>>;
};

function basePermissions(): string[] {
  return [
    "platform.permission.view_portfolio",
    "platform.permission.view_global_catalog",
    "platform.permission.manage_global_categories",
  ];
}

async function mockBusinessTypes(page: Page, options: { permissions?: string[]; updateConflict?: boolean } = {}) {
  const permissions = options.permissions ?? basePermissions();
  const state: MockState = {
    antiforgeryRequested: false,
    mutationCalls: [],
    listRequests: [],
    detailGets: 0,
    items: [
      {
        id: activeId,
        code: "sari-sari",
        name: "Sari-Sari Store",
        description: "Neighborhood store",
        status: "Active",
        sortOrder: 1,
        iconReference: "store",
        createdAtUtc: "2026-01-01T08:00:00Z",
        updatedAtUtc: "2026-08-01T08:00:00Z",
      },
      {
        id: inactiveId,
        code: "mini-grocery",
        name: "Mini Grocery",
        description: "Small grocery",
        status: "Inactive",
        sortOrder: 2,
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
  await page.route("**/api/v1/platform/global-catalog/categories*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/api/v1/platform/global-catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });

  await page.route("**/api/v1/platform/global-catalog/business-types**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();
    const csrfHeader = request.headers()["x-xsrf-token"] ?? null;

    if (method !== "GET") {
      state.mutationCalls.push({ method, path, csrfHeader });
    }

    const detailMatch = path.match(/\/business-types\/([0-9a-f-]{36})$/i);
    if (detailMatch && method === "GET") {
      state.detailGets += 1;
      const match = state.items.find((item) => item.id === detailMatch[1]);
      if (!match) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      await route.fulfill({ json: match });
      return;
    }

    if (path.endsWith("/global-catalog/business-types") && method === "GET") {
      state.listRequests.push(url.toString());
      let filtered = [...state.items];
      const status = url.searchParams.get("status");
      if (status) {
        filtered = filtered.filter((item) => item.status === status);
      }
      const search = url.searchParams.get("search");
      if (search) {
        const needle = search.toLowerCase();
        filtered = filtered.filter(
          (item) =>
            String(item.name).toLowerCase().includes(needle) ||
            String(item.code).toLowerCase().includes(needle),
        );
      }
      await route.fulfill({
        json: {
          items: filtered,
          totalCount: filtered.length,
          page: Number(url.searchParams.get("page") ?? "1"),
          pageSize: 20,
        },
      });
      return;
    }

    if (path.endsWith("/global-catalog/business-types") && method === "POST") {
      const body = request.postDataJSON() as Record<string, unknown>;
      const created = {
        id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        code: body.code,
        name: body.name,
        description: body.description ?? null,
        status: "Active",
        sortOrder: body.sortOrder ?? 0,
        iconReference: body.iconReference ?? null,
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
      };
      state.items = [created, ...state.items];
      await route.fulfill({ status: 201, json: created });
      return;
    }

    if (detailMatch && method === "PUT") {
      if (options.updateConflict) {
        await route.fulfill({
          status: 409,
          json: {
            title: "Conflict",
            status: 409,
            detail: "Business type was updated by another operator.",
            errorCode: "application.concurrency_conflict",
          },
        });
        return;
      }
      const body = request.postDataJSON() as Record<string, unknown>;
      const existing = state.items.find((item) => item.id === detailMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        name: body.name,
        description: body.description,
        sortOrder: body.sortOrder,
        iconReference: body.iconReference,
        updatedAtUtc: "2026-08-22T08:05:00Z",
      };
      state.items = state.items.map((item) => (item.id === detailMatch[1] ? updated : item));
      await route.fulfill({ json: updated });
      return;
    }

    const statusMatch = path.match(/\/business-types\/([0-9a-f-]{36})\/status$/i);
    if (statusMatch && method === "POST") {
      const body = request.postDataJSON() as Record<string, unknown>;
      const existing = state.items.find((item) => item.id === statusMatch[1]);
      if (!existing) {
        await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
        return;
      }
      const updated = {
        ...existing,
        status: body.status,
        updatedAtUtc: "2026-08-22T08:06:00Z",
      };
      state.items = state.items.map((item) => (item.id === statusMatch[1] ? updated : item));
      await route.fulfill({ json: updated });
      return;
    }

    await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
  });

  return state;
}

test.describe("global catalog business types", () => {
  test("navigates to business types list", async ({ page }) => {
    await mockBusinessTypes(page);
    await page.goto("/admin");
    await page.getByRole("link", { name: "Business Types" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/business-types$/);
    await expect(page.getByRole("heading", { name: "Business Types" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Sari-Sari Store" })).toBeVisible();
  });

  test("search and status filters hit list API", async ({ page }) => {
    const mock = await mockBusinessTypes(page);
    await page.goto("/admin/global-catalog/business-types");
    await page.getByLabel("Search").fill("mini");
    await page.getByRole("button", { name: "Search" }).click();
    await expect.poll(() => mock.listRequests.some((url) => url.includes("search=mini"))).toBe(true);
    await page.selectOption("#gc-bt-status", "Inactive");
    await expect.poll(() => mock.listRequests.some((url) => url.includes("status=Inactive"))).toBe(true);
  });

  test("creates business type with antiforgery", async ({ page }) => {
    const mock = await mockBusinessTypes(page);
    await page.goto("/admin/global-catalog/business-types/new");
    await page.getByLabel("Code").fill("cafe");
    await page.getByLabel("Name").fill("Cafe");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page).toHaveURL(/\/admin\/global-catalog\/business-types\//);
    await expect.poll(() => mock.antiforgeryRequested).toBe(true);
    await expect
      .poll(() =>
        mock.mutationCalls.some(
          (call) =>
            call.method === "POST" &&
            call.path.endsWith("/global-catalog/business-types") &&
            call.csrfHeader === "test-antiforgery-token",
        ),
      )
      .toBe(true);
  });

  test("edit keeps code read-only and PUT succeeds", async ({ page }) => {
    await mockBusinessTypes(page);
    await page.goto(`/admin/global-catalog/business-types/${activeId}/edit`);
    await expect(page.getByLabel("Code")).toBeDisabled();
    await expect(page.getByLabel("Code")).toHaveValue("sari-sari");
    await page.getByLabel("Name").fill("Sari-Sari Updated");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page).toHaveURL(new RegExp(`/admin/global-catalog/business-types/${activeId}$`));
    await expect(page.getByRole("heading", { name: "Sari-Sari Updated" })).toBeVisible();
  });

  test("lifecycle deactivate and reactivate", async ({ page }) => {
    await mockBusinessTypes(page);
    await page.goto(`/admin/global-catalog/business-types/${activeId}`);
    await page.getByRole("button", { name: "Deactivate" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Deactivate" }).click();
    await expect(page.getByText("Inactive")).toBeVisible();
    await page.getByRole("button", { name: "Activate" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Activate" }).click();
    await expect(page.getByText("Active")).toBeVisible();
  });

  test("archive and reactivate from archived", async ({ page }) => {
    await mockBusinessTypes(page);
    await page.goto(`/admin/global-catalog/business-types/${activeId}`);
    await page.getByRole("button", { name: "Archive" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Archive" }).click();
    await expect(page.getByText("Archived")).toBeVisible();
    await page.getByRole("button", { name: "Activate", exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Activate", exact: true }).click();
    await expect(page.getByText("Active")).toBeVisible();
  });

  test("409 conflict on edit refetches detail", async ({ page }) => {
    const mock = await mockBusinessTypes(page, { updateConflict: true });
    await page.goto(`/admin/global-catalog/business-types/${activeId}/edit`);
    const before = mock.detailGets;
    await page.getByLabel("Name").fill("Stale");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByText("Business type was updated by another operator.")).toBeVisible();
    expect(mock.detailGets).toBeGreaterThan(before);
  });

  test("view-only permission hides mutation controls", async ({ page }) => {
    await mockBusinessTypes(page, {
      permissions: ["platform.permission.view_portfolio", "platform.permission.view_global_catalog"],
    });
    await page.goto("/admin/global-catalog/business-types");
    await expect(page.getByRole("link", { name: "Create business type" })).toHaveCount(0);
    await page.goto(`/admin/global-catalog/business-types/${activeId}`);
    await expect(page.getByRole("link", { name: "Edit" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Deactivate" })).toHaveCount(0);
  });

  test("axe and viewport checks", async ({ page }) => {
    await mockBusinessTypes(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/admin/global-catalog/business-types");
    let accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    const desktopOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(desktopOverflow).toBe(false);

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(`/admin/global-catalog/business-types/${activeId}`);
    accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter((v) => v.impact === "serious" || v.impact === "critical")).toEqual([]);
    const tabletOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(tabletOverflow).toBe(false);
  });
});

import { expect, test, type Page } from "@playwright/test";

const samplePermission = {
  code: "platform.permission.view_portfolio",
  description: "view portfolio",
  area: "platform",
};

const builtinRole = {
  id: "11111111-1111-1111-1111-111111111111",
  code: "PlatformAdministrator",
  name: "Platform Administrator",
  description: "Built-in admin",
  kind: "BuiltIn",
  status: "Active",
  permissions: ["platform.permission.manage_platform_users"],
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
  version: 1,
};

const customRole = {
  id: "22222222-2222-2222-2222-222222222222",
  code: "OpsViewer",
  name: "Ops Viewer",
  description: "Custom ops",
  kind: "Custom",
  status: "Active",
  permissions: ["platform.permission.view_portfolio"],
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
  version: 3,
};

async function mockSession(page: Page, permissions: string[]) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      json: {
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
      },
    });
  });
  await page.route("**/api/v1/platform/authorization/me**", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: "olivia@example.test",
        actorType: "PlatformUser",
        platformUserId: "22222222-2222-2222-2222-222222222222",
        organizationId: null,
        permissions,
      },
    });
  });
  await page.route("**/api/v1/platform/antiforgery/token**", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" } });
  });
}

async function mockRolesApi(page: Page, options?: { mutationFail?: boolean }) {
  let role = { ...customRole };

  await page.route("**/api/v1/platform/authorization/permissions**", async (route) => {
    await route.fulfill({ json: [samplePermission] });
  });

  await page.route("**/api/v1/platform/authorization/role-definitions**", async (route) => {
    const url = route.request().url();
    const method = route.request().method().toUpperCase();

    if (method === "POST" && /role-definitions\/?(\?.*)?$/.test(new URL(url).pathname)) {
      const body = route.request().postDataJSON() as {
        code: string;
        name: string;
        description?: string | null;
        permissions?: string[];
      };
      await route.fulfill({
        status: 201,
        json: {
          ...customRole,
          id: "33333333-3333-3333-3333-333333333333",
          code: body.code,
          name: body.name,
          description: body.description ?? null,
          permissions: body.permissions ?? [],
          version: 1,
        },
      });
      return;
    }

    if (url.includes(`/role-definitions/${role.id}/activate`) && method === "POST") {
      if (options?.mutationFail) {
        await route.fulfill({ status: 500, json: { detail: "activate failed", status: 500 } });
        return;
      }
      role = { ...role, status: "Active", version: role.version + 1 };
      await route.fulfill({ json: role });
      return;
    }
    if (url.includes(`/role-definitions/${role.id}/deactivate`) && method === "POST") {
      if (options?.mutationFail) {
        await route.fulfill({ status: 500, json: { detail: "deactivate failed", status: 500 } });
        return;
      }
      role = { ...role, status: "Inactive", version: role.version + 1 };
      await route.fulfill({ json: role });
      return;
    }
    if (url.includes(`/role-definitions/${role.id}/retire`) && method === "POST") {
      if (options?.mutationFail) {
        await route.fulfill({ status: 500, json: { detail: "retire failed", status: 500 } });
        return;
      }
      role = { ...role, status: "Retired", version: role.version + 1 };
      await route.fulfill({ json: role });
      return;
    }
    if (url.includes(`/role-definitions/${role.id}`) && method === "PUT") {
      if (options?.mutationFail) {
        await route.fulfill({ status: 500, json: { detail: "save failed", status: 500 } });
        return;
      }
      const body = route.request().postDataJSON() as {
        name: string;
        description?: string | null;
        permissions?: string[];
      };
      role = {
        ...role,
        name: body.name,
        description: body.description ?? null,
        permissions: body.permissions ?? role.permissions,
        version: role.version + 1,
      };
      await route.fulfill({ json: role });
      return;
    }
    if (url.includes(`/role-definitions/${builtinRole.id}`)) {
      await route.fulfill({ json: builtinRole });
      return;
    }
    if (url.includes(`/role-definitions/${role.id}`)) {
      await route.fulfill({ json: role });
      return;
    }
    if (url.includes("role-definitions/33333333-3333-3333-3333-333333333333")) {
      await route.fulfill({
        json: {
          ...customRole,
          id: "33333333-3333-3333-3333-333333333333",
          code: "NewRole",
          name: "New Role",
        },
      });
      return;
    }
    if (url.includes("/role-definitions")) {
      await route.fulfill({
        json: { items: [builtinRole, role], totalCount: 2, page: 1, pageSize: 20 },
      });
      return;
    }

    await route.fulfill({ status: 404, json: { detail: "not found" } });
  });
}

test("platform roles list filters and open detail", async ({ page }) => {
  await mockSession(page, [
    "platform.permission.manage_platform_users",
    "platform.permission.view_portfolio",
  ]);
  await mockRolesApi(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/platform-roles");
  await expect(page.getByTestId("platform-roles-list-page")).toBeVisible();
  await expect(page.getByText("OpsViewer")).toBeVisible();
  await page.getByLabel("Search").fill("Ops");
  await page.getByLabel("Kind").selectOption("Custom");
  await page.getByRole("button", { name: "Apply" }).click();
  await expect(page).toHaveURL(/search=Ops/);
  await page.getByRole("link", { name: "OpsViewer" }).click();
  await expect(page.getByTestId("platform-role-detail-page")).toBeVisible();
  await expect(page.getByTestId("platform-roles-pos-warning")).toBeVisible();
});

test("create custom role navigates to detail", async ({ page }) => {
  await mockSession(page, ["platform.permission.manage_platform_users"]);
  await mockRolesApi(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/platform-roles");
  await page.getByTestId("platform-roles-toggle-create").click();
  await page.getByTestId("platform-role-new-code").fill("NewRole");
  await page.getByTestId("platform-role-new-name").fill("New Role");
  await page.getByRole("checkbox").check();
  await page.getByTestId("platform-role-create-submit").click();
  await expect(page.getByTestId("platform-role-detail-page")).toBeVisible();
  await expect(page).toHaveURL(/\/admin\/platform-roles\/33333333-3333-3333-3333-333333333333/);
});

test("edit permissions, deactivate, activate, retire", async ({ page }) => {
  await mockSession(page, ["platform.permission.manage_platform_users"]);
  await mockRolesApi(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  page.on("dialog", (dialog) => void dialog.accept());
  await page.goto(`/admin/platform-roles/${customRole.id}`);
  await expect(page.getByTestId("platform-role-manage")).toBeVisible();
  await page.getByTestId("platform-role-edit-name").fill("Ops Viewer Updated");
  await page.getByTestId("platform-role-save").click();
  await expect(page.getByTestId("platform-role-mutation-success")).toBeVisible();
  await page.getByTestId("platform-role-deactivate").click();
  await expect(page.getByTestId("platform-role-activate")).toBeVisible();
  await page.getByTestId("platform-role-activate").click();
  await expect(page.getByTestId("platform-role-deactivate")).toBeVisible();
  await page.getByTestId("platform-role-retire").click();
  await expect(page.getByTestId("platform-role-manage")).toHaveCount(0);
});

test("forbidden without manage_platform_users", async ({ page }) => {
  await mockSession(page, ["platform.permission.view_portfolio"]);
  await page.goto("/admin/platform-roles");
  await expect(page.getByRole("heading", { name: /not found/i })).toBeVisible();
  await expect(page.getByTestId("platform-roles-list-page")).toHaveCount(0);
});

test("failed mutation remains truthful", async ({ page }) => {
  await mockSession(page, ["platform.permission.manage_platform_users"]);
  await mockRolesApi(page, { mutationFail: true });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/platform-roles/${customRole.id}`);
  await page.getByTestId("platform-role-save").click();
  await expect(page.getByTestId("platform-role-mutation-error")).toBeVisible();
  await expect(page.getByText("save failed")).toBeVisible();
  await expect(page.getByTestId("platform-role-mutation-success")).toHaveCount(0);
});

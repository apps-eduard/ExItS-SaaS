import { expect, test, type Page } from "@playwright/test";

const olivia = {
  key: "olivia",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia.mendoza@exits.local",
  listLabel: "Olivia Mendoza — Platform Administration",
};

async function mockUnauthenticated(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      status: 401,
      json: { status: 401, errorCode: "application.auth.session_invalid" },
    });
  });
}

test("production-shaped config keeps the Test User selector hidden", async ({ page }) => {
  await page.route("**/config.js", async (route) => {
    await route.fulfill({
      contentType: "application/javascript",
      body: 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"http://127.0.0.1:8091",localValidationToolsEnabled:false};',
    });
  });
  await mockUnauthenticated(page);
  await page.route("**/api/v1/platform/local-validation/enabled", async (route) => {
    await route.fulfill({ json: true });
  });
  await page.goto("/admin/login");
  await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  await expect(page.getByText("Local Validation")).toHaveCount(0);
  await expect(page.getByText("Development Tools")).toHaveCount(0);
  await expect(page.locator("#dev-test-user")).toHaveCount(0);
});

test("missing runtime flag keeps the Test User selector hidden in production", async ({ page }) => {
  await page.route("**/config.js", async (route) => {
    await route.fulfill({
      contentType: "application/javascript",
      body: 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"http://127.0.0.1:8091"};',
    });
  });
  await mockUnauthenticated(page);
  await page.goto("/admin/login");
  await expect(page.locator("#dev-test-user")).toHaveCount(0);
});

test("runtime flag true still hides when the backend Local Validation flag is false", async ({
  page,
}) => {
  await page.route("**/config.js", async (route) => {
    await route.fulfill({
      contentType: "application/javascript",
      body: 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"http://127.0.0.1:8091",localValidationToolsEnabled:true};',
    });
  });
  await mockUnauthenticated(page);
  await page.route("**/api/v1/platform/local-validation/enabled", async (route) => {
    await route.fulfill({ json: false });
  });
  await page.goto("/admin/login");
  await expect(page.locator("#dev-test-user")).toHaveCount(0);
});

test("runtime flag true still hides when the backend Local Validation API fails", async ({
  page,
}) => {
  await page.route("**/config.js", async (route) => {
    await route.fulfill({
      contentType: "application/javascript",
      body: 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"http://127.0.0.1:8091",localValidationToolsEnabled:true};',
    });
  });
  await mockUnauthenticated(page);
  await page.route("**/api/v1/platform/local-validation/enabled", async (route) => {
    await route.abort("failed");
  });
  await page.goto("/admin/login");
  await expect(page.locator("#dev-test-user")).toHaveCount(0);
});

test("runtime flag true and backend enabled shows Test User, fills email only, and uses real login", async ({
  page,
}) => {
  let loginCalls = 0;
  let authenticated = false;
  await page.route("**/config.js", async (route) => {
    await route.fulfill({
      contentType: "application/javascript",
      body: 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"http://127.0.0.1:8091",localValidationToolsEnabled:true};',
    });
  });
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    if (!authenticated) {
      await route.fulfill({
        status: 401,
        json: { status: 401, errorCode: "application.auth.session_invalid" },
      });
      return;
    }
    await route.fulfill({
      json: {
        sessionId: "11111111-1111-1111-1111-111111111111",
        userId: "22222222-2222-2222-2222-222222222222",
        username: "olivia",
        displayName: "Olivia Mendoza",
        email: olivia.email,
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
  await page.route("**/api/v1/platform/local-validation/enabled", async (route) => {
    await route.fulfill({ json: true });
  });
  await page.route("**/api/v1/platform/local-validation/quick-login-identities", async (route) => {
    await route.fulfill({ json: [olivia] });
  });
  await page.route("**/api/v1/platform/auth/login", async (route) => {
    loginCalls += 1;
    const body = route.request().postDataJSON() as { usernameOrEmail?: string; password?: string };
    expect(body.usernameOrEmail).toBe(olivia.email);
    expect(body.password).toBe("typed-by-tester");
    authenticated = true;
    await route.fulfill({
      json: {
        sessionId: "11111111-1111-1111-1111-111111111111",
        userId: "22222222-2222-2222-2222-222222222222",
        username: "olivia",
        displayName: "Olivia Mendoza",
        email: olivia.email,
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
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: olivia.email,
        actorType: "PlatformUser",
        platformUserId: "22222222-2222-2222-2222-222222222222",
        organizationId: null,
        permissions: ["platform.permission.view_portfolio"],
      },
    });
  });
  await page.route("**/api/v1/platform/organizations**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/subscriptions**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/users**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 5 } });
  });
  await page.route("**/api/v1/platform/audit**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 8 } });
  });
  await page.route("**/health**", async (route) => {
    await route.fulfill({ body: "Healthy" });
  });

  await page.goto("/admin/login");
  const selector = page.getByLabel("Test User — Local Validation");
  await expect(selector).toBeVisible();
  await selector.selectOption("olivia");
  await expect(page.getByLabel("Email")).toHaveValue(olivia.email);
  await expect(page.locator("#sign-in-password")).toHaveValue("");
  expect(loginCalls).toBe(0);

  await page.locator("#sign-in-password").fill("typed-by-tester");
  await page.getByRole("button", { name: "Sign In" }).click();
  await expect(page).toHaveURL(/\/admin$/);
  expect(loginCalls).toBe(1);
});

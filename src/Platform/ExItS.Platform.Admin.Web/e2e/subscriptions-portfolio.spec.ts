import { expect, test } from "@playwright/test";

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

const authorization = {
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  platformUserId: session.userId,
  organizationId: null,
  permissions: [
    "platform.permission.view_portfolio",
    "platform.permission.manage_subscriptions",
    "platform.permission.manage_manual_payments",
  ],
};

const subscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationDisplayName: "Northwind Market",
  productCode: "pinoy-business-pos",
  planId: "22222222-2222-2222-2222-222222222222",
  status: "Active",
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Starter",
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({
      json: {
        items: [
          {
            id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
            code: "pinoy-business-pos",
            displayName: "Pinoy Business POS",
            status: "Active",
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      },
    });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "csrf-token" } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("subscription portfolio list, filters, detail link, error retry", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  let fail = false;
  await page.route("**/api/v1/platform/subscriptions**", async (route) => {
    const url = route.request().url();
    if (url.match(/\/subscriptions\/[^/?]+$/)) {
      await route.fulfill({ json: subscription });
      return;
    }
    if (fail) {
      await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
      return;
    }
    expect(url).toMatch(/pageSize=20/);
    await route.fulfill({
      json: { items: [subscription], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
  await page.goto("/admin/subscriptions");
  await expect(page.getByRole("heading", { name: "Subscriptions", exact: true, level: 1 })).toBeVisible();
  await expect(page.getByText("Northwind Market").first()).toBeVisible();
  await page.locator("#sub-portfolio-status").selectOption("Active");
  await expect(page).toHaveURL(/status=Active/);
  await page
    .locator(`a[href="/admin/subscriptions/${subscription.id}"]`)
    .first()
    .click();
  await expect(page).toHaveURL(/\/admin\/subscriptions\/11111111-1111-1111-1111-111111111111/);
  await page.goto("/admin/subscriptions");
  fail = true;
  await page.reload();
  await expect(page.getByRole("heading", { name: "Unable to load subscriptions.", level: 2 })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("Northwind Market").first()).toBeVisible();
});

test("subscription portfolio empty state", async ({ page }) => {
  await mockCore(page);
  await page.route("**/api/v1/platform/subscriptions**", async (route) => {
    if (route.request().url().match(/\/subscriptions\/[^/?]+$/)) {
      await route.fulfill({ status: 404, json: { title: "Not Found", status: 404 } });
      return;
    }
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.goto("/admin/subscriptions");
  await expect(page.getByText("No subscriptions")).toBeVisible();
});

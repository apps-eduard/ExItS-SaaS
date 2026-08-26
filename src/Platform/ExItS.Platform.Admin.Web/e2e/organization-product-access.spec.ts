import { expect, test } from "@playwright/test";

const organization = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const assignment = {
  id: "55555555-5555-5555-5555-555555555555",
  userId: "22222222-2222-2222-2222-222222222222",
  organizationId: organization.id,
  membershipId: "11111111-1111-1111-1111-111111111111",
  productCode: "pinoy-business-pos",
  status: "Active",
  grantedAtUtc: "2026-08-22T10:00:00Z",
  grantedByActor: "olivia@example.test",
};

async function mockCore(page: import("@playwright/test").Page) {
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
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: "olivia@example.test",
        actorType: "PlatformUser",
        platformUserId: "22222222-2222-2222-2222-222222222222",
        organizationId: null,
        permissions: [
          "platform.permission.view_portfolio",
          "platform.permission.manage_organizations",
          "platform.permission.manage_product_access",
        ],
      },
    });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({ json: { subscriptions: [], payments: [], latestEntitlements: [] } });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "csrf-token" } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("product access list error shows retry", async ({ page }) => {
  let fail = true;
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/product-access")) {
      if (fail) {
        await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
        return;
      }
      await route.fulfill({
        json: { items: [assignment], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: organization });
      return;
    }
    await route.fulfill({ json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 } });
  });
  await page.goto(`/admin/organizations/${organization.id}/product-access`);
  await expect(page.getByRole("heading", { name: "Unable to load product access." })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("pinoy-business-pos")).toBeVisible();
});

test("grant product access", async ({ page }) => {
  const assignments: typeof assignment[] = [];
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/product-access") && route.request().method() === "POST") {
      assignments.push(assignment);
      await route.fulfill({ json: assignment });
      return;
    }
    if (url.includes("/product-access")) {
      await route.fulfill({
        json: { items: assignments, totalCount: assignments.length, page: 1, pageSize: 20 },
      });
      return;
    }
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: organization });
      return;
    }
    await route.fulfill({ json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 } });
  });
  await page.goto(`/admin/organizations/${organization.id}/product-access`);
  await page.locator("#grant-user").fill(assignment.userId);
  await page.locator("#grant-product").fill(assignment.productCode);
  await page.getByRole("button", { name: "Grant access" }).click();
  await expect(page.getByText("pinoy-business-pos")).toBeVisible();
});

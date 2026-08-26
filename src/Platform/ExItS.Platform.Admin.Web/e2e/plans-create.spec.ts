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

const createdPlanId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

async function mockPlansCreate(page: import("@playwright/test").Page) {
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
        permissions: [
          "platform.permission.view_portfolio",
          "platform.permission.manage_catalog",
        ],
      },
    });
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
  await page.route("**/api/v1/platform/catalog/plans?**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.route("**/api/v1/platform/catalog/products/pinoy-business-pos/plans", async (route) => {
    if (route.request().method() === "POST") {
      await route.fulfill({
        json: {
          id: createdPlanId,
          productCode: "pinoy-business-pos",
          code: "starter-plus",
          displayName: "Starter Plus",
          status: "Inactive",
          currencyCode: "PHP",
        },
      });
      return;
    }
    await route.continue();
  });
  await page.route(`**/api/v1/platform/catalog/plans/${createdPlanId}`, async (route) => {
    await route.fulfill({
      json: {
        id: createdPlanId,
        productCode: "pinoy-business-pos",
        code: "starter-plus",
        displayName: "Starter Plus",
        status: "Inactive",
        currencyCode: "PHP",
        maxBranches: 1,
        maxActiveStaff: 1,
        maxActivePosDevices: 1,
        maxActiveBusinessTypes: 1,
        customerCreditEnabled: false,
        advancedReportsEnabled: false,
        exportEnabled: false,
        trialAllowed: false,
        defaultTrialDays: 0,
        monthlyPrice: 0,
        annualPrice: 0,
        sortOrder: 100,
      },
    });
  });
  await page.route(
    `**/api/v1/platform/catalog/products/pinoy-business-pos/plans/${createdPlanId}/versions`,
    async (route) => {
      await route.fulfill({ json: [] });
    },
  );
  await page.route("**/api/v1/platform/catalog/products/pinoy-business-pos/features", async (route) => {
    await route.fulfill({ json: [] });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "csrf-token" } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("create plan from plans page", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockPlansCreate(page);
  await page.goto("/admin/plans");
  await page.getByRole("button", { name: "Create plan" }).click();
  await page.getByLabel("Plan code").fill("starter-plus");
  await page.getByLabel("Display name").fill("Starter Plus");
  await page.getByRole("button", { name: "Create plan" }).last().click();
  await expect(page).toHaveURL(new RegExp(`/admin/plans/${createdPlanId}$`));
  await expect(page.getByRole("heading", { name: "Starter Plus", level: 1 })).toBeVisible();
});

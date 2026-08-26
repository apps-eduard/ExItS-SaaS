import { expect, test } from "@playwright/test";
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

const manageCatalogPermissions = [
  "platform.permission.view_portfolio",
  "platform.permission.manage_catalog",
];

const growthPlanId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

let growthPlan = {
  id: growthPlanId,
  productCode: "pinoy-business-pos",
  code: "growth",
  displayName: "Growth",
  status: "Active",
  maxBranches: 3,
  maxActiveStaff: 10,
  maxActivePosDevices: 3,
  maxActiveBusinessTypes: 3,
  customerCreditEnabled: true,
  advancedReportsEnabled: true,
  exportEnabled: true,
  trialAllowed: true,
  defaultTrialDays: 14,
  monthlyPrice: 699,
  annualPrice: 6990,
  currencyCode: "PHP",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

async function mockPlanCommercial(page: import("@playwright/test").Page) {
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
        permissions: manageCatalogPermissions,
      },
    });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "test-token" } });
  });
  await page.route(`**/api/v1/platform/catalog/plans/${growthPlanId}`, async (route) => {
    await route.fulfill({ json: growthPlan });
  });
  await page.route("**/api/v1/platform/catalog/plans?**", async (route) => {
    await route.fulfill({
      json: { items: [growthPlan], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
  await page.route(
    `**/api/v1/platform/catalog/products/pinoy-business-pos/plans/${growthPlanId}/versions`,
    async (route) => {
      await route.fulfill({ json: [] });
    },
  );
  await page.route("**/api/v1/platform/catalog/products/pinoy-business-pos/features", async (route) => {
    await route.fulfill({
      json: [
        {
          productCode: "pinoy-business-pos",
          featureCode: "store-customer-ordering",
          displayName: "Customer ordering",
          valueType: "Boolean",
          status: "Active",
        },
        {
          productCode: "pinoy-business-pos",
          featureCode: "store-delivery-orders",
          displayName: "Delivery orders",
          valueType: "Boolean",
          status: "Active",
        },
      ],
    });
  });
  await page.route(
    `**/api/v1/platform/catalog/products/pinoy-business-pos/plans/${growthPlanId}/commercial`,
    async (route) => {
      if (route.request().method() !== "PATCH") {
        await route.fallback();
        return;
      }
      const body = route.request().postDataJSON() as typeof growthPlan;
      growthPlan = { ...growthPlan, ...body, updatedAtUtc: new Date().toISOString() };
      await route.fulfill({ json: growthPlan });
    },
  );
}

test.describe("Plan commercial editing", () => {
  test("Growth maxActivePosDevices persists after save and refetch", async ({ page }) => {
    await mockPlanCommercial(page);
    await page.goto(`/admin/plans/${growthPlanId}`);

    await expect(page.getByRole("heading", { name: "Growth" })).toBeVisible();
    const posDevices = page.getByLabel("Max active POS devices");
    await posDevices.fill("5");
    await page.getByRole("button", { name: "Save commercial package" }).click();

    await expect(page.getByText("Commercial package saved.")).toBeVisible();
    await expect(posDevices).toHaveValue("5");
  });

  test("plan detail passes axe on desktop", async ({ page }) => {
    await mockPlanCommercial(page);
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto(`/admin/plans/${growthPlanId}`);
    await expect(page.getByRole("heading", { name: "Growth" })).toBeVisible();
    const results = await new AxeBuilder({ page }).analyze();
    expect(results.violations).toEqual([]);
  });
});

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

const authorization = {
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  platformUserId: session.userId,
  organizationId: null,
  permissions: ["platform.permission.view_portfolio", "platform.permission.manage_organizations"],
};

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const planId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const productDetail = {
  id: productId,
  code: "future-product-x",
  displayName: "Future Product X",
  status: "Active",
  createdAtUtc: "2026-01-01T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

const planDetail = {
  id: planId,
  productCode: "future-product-x",
  productId,
  productDisplayName: "Future Product X",
  code: "starter",
  displayName: "Starter",
  status: "Active",
  monthlyPrice: 999,
  annualPrice: 9990,
  currencyCode: "PHP",
  trialAllowed: true,
  defaultTrialDays: 14,
};

const plansPage = {
  items: [planDetail],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

async function mockCatalog(
  page: import("@playwright/test").Page,
  permissions = authorization.permissions,
  options?: {
    productStatus?: number;
    planStatus?: number;
    plansEmpty?: boolean;
    productPlansEmpty?: boolean;
  },
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: { ...authorization, permissions } });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}`, async (route) => {
    await route.fulfill({
      status: options?.productStatus ?? 200,
      json: productDetail,
    });
  });
  await page.route("**/api/v1/platform/catalog/products/future-product-x/plans", async (route) => {
    await route.fulfill({
      json: options?.productPlansEmpty ? [] : [planDetail],
    });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    const url = route.request().url();
    if (url.includes(`/catalog/products/${productId}`)) {
      return;
    }
    if (url.includes("/plans")) {
      return;
    }
    await route.fulfill({
      json: {
        items: [productDetail],
        totalCount: 1,
        page: 1,
        pageSize: 100,
      },
    });
  });
  await page.route(`**/api/v1/platform/catalog/plans/${planId}`, async (route) => {
    await route.fulfill({
      status: options?.planStatus ?? 200,
      json: planDetail,
    });
  });
  await page.route("**/api/v1/platform/catalog/plans*", async (route) => {
    const url = route.request().url();
    if (url.includes(`/catalog/plans/${planId}`)) {
      return;
    }
    await route.fulfill({
      json: options?.plansEmpty ? { items: [], totalCount: 0, page: 1, pageSize: 20 } : plansPage,
    });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("authorized product detail is read-only", async ({ page }) => {
  await mockCatalog(page);
  await page.goto(`/admin/products/${productId}`);
  await expect(page.getByRole("heading", { name: "Future Product X" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Starter" })).toBeVisible();
  await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);
});

test("invalid product GUID shows not found", async ({ page }) => {
  await mockCatalog(page);
  await page.goto("/admin/products/not-a-guid");
  await expect(page.getByRole("heading", { name: "Product not found" })).toBeVisible();
});

test("product 404 shows not found", async ({ page }) => {
  await mockCatalog(page, authorization.permissions, { productStatus: 404 });
  await page.goto(`/admin/products/${productId}`);
  await expect(page.getByRole("heading", { name: "Product not found" })).toBeVisible();
});

test("product 403 fail-closes", async ({ page }) => {
  await mockCatalog(page, authorization.permissions, { productStatus: 403 });
  await page.goto(`/admin/products/${productId}`);
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
});

test("authorized plans list is read-only", async ({ page }) => {
  await mockCatalog(page);
  await page.goto("/admin/plans");
  await expect(page.getByRole("heading", { name: "Plans & Pricing", exact: true })).toBeVisible();
  await expect(
    page.getByRole("table").getByRole("link", { name: "Starter", exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);
});

test("plans list unauthorized fail-closes", async ({ page }) => {
  await mockCatalog(page, []);
  await page.goto("/admin/plans");
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
});

test("plan detail shows returned pricing fields", async ({ page }) => {
  await mockCatalog(page);
  await page.goto(`/admin/plans/${planId}`);
  await expect(page.getByRole("heading", { name: "Starter" })).toBeVisible();
  await expect(page.getByText("₱999.00")).toBeVisible();
});

test("plan 404 shows not found", async ({ page }) => {
  await mockCatalog(page, authorization.permissions, { planStatus: 404 });
  await page.goto(`/admin/plans/${planId}`);
  await expect(page.getByRole("heading", { name: "Plan not found" })).toBeVisible();
});

test("plan 403 fail-closes", async ({ page }) => {
  await mockCatalog(page, authorization.permissions, { planStatus: 403 });
  await page.goto(`/admin/plans/${planId}`);
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
});

test("empty product plans shows truthful empty state", async ({ page }) => {
  await mockCatalog(page, authorization.permissions, { productPlansEmpty: true });
  await page.goto(`/admin/products/${productId}`);
  await expect(page.getByText("No plans were returned for this product.")).toBeVisible();
});

test("plans mobile and axe", async ({ page }) => {
  await mockCatalog(page);
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin/plans");
  await expect(page.getByRole("link", { name: "Starter" })).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);
});

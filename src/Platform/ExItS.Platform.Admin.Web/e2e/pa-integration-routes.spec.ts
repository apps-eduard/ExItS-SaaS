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
    "platform.permission.manage_organizations",
    "platform.permission.manage_memberships",
    "platform.permission.manage_product_access",
    "platform.permission.manage_subscriptions",
    "platform.permission.manage_manual_payments",
    "platform.permission.manage_entitlement_overrides",
    "platform.permission.manage_platform_users",
    "platform.permission.view_global_catalog",
    "platform.permission.manage_global_categories",
    "platform.permission.manage_global_products",
    "platform.permission.import_global_products",
    "platform.permission.manage_catalog_templates",
    "platform.permission.publish_catalog_templates",
    "platform.permission.view_audit_records",
    "platform.permission.view_privacy_compliance",
  ],
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/api/v1/platform/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/auth/me") || url.includes("/authorization/me")) {
      await route.fallback();
      return;
    }
    if (url.includes("/operations/system-health")) {
      await route.fulfill({
        json: {
          generatedAtUtc: "2026-08-22T12:00:00Z",
          overallStatus: "Healthy",
          services: [],
        },
      });
      return;
    }
    if (url.includes("/global-catalog/")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    if (url.includes("/organizations")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    if (url.includes("/catalog/products") || url.includes("/subscriptions") || url.includes("/payments")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    if (url.includes("/users") || url.includes("/platform-roles") || url.includes("/audit")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    if (url.includes("/privacy-compliance")) {
      await route.fulfill({ json: { categories: [], requirements: [], systems: [] } });
      return;
    }
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
}

const routes: Array<{ path: string; heading: string | RegExp }> = [
  { path: "/admin/organizations", heading: /Organizations|All Organizations/i },
  { path: "/admin/products", heading: /Products/i },
  { path: "/admin/plans", heading: /Plans/i },
  { path: "/admin/subscriptions", heading: /Subscriptions/i },
  { path: "/admin/payments", heading: /Payments/i },
  { path: "/admin/entitlements", heading: /Entitlements/i },
  { path: "/admin/users", heading: /Accounts|Users|All Accounts/i },
  { path: "/admin/platform-roles", heading: /Roles|Platform roles/i },
  { path: "/admin/audit", heading: /Audit/i },
  { path: "/admin/privacy-compliance", heading: /Privacy|Compliance/i },
  { path: "/admin/system-health", heading: /System health|Health/i },
  { path: "/admin/global-catalog/business-types", heading: /Business types/i },
  { path: "/admin/global-catalog/categories", heading: /Categories/i },
  { path: "/admin/global-catalog/products", heading: /Global products|Products/i },
  { path: "/admin/global-catalog/imports", heading: /Imports/i },
  { path: "/admin/global-catalog/templates", heading: /Templates/i },
];

test("PA-INTEGRATION-01 reconciled App routes remain reachable", async ({ page }) => {
  await mockCore(page);
  for (const route of routes) {
    await page.goto(route.path);
    await expect(page).not.toHaveURL(/\/admin\/login/);
    await expect(page.getByRole("heading", { name: route.heading }).first()).toBeVisible({
      timeout: 10_000,
    });
  }
});

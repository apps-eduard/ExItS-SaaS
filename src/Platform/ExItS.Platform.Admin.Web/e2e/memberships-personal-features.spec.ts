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
  permissions: [
    "platform.permission.view_portfolio",
    "platform.permission.manage_organizations",
    "platform.permission.manage_memberships",
    "platform.permission.manage_catalog",
    "platform.permission.manage_platform_users",
  ],
};

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

const feature = {
  featureCode: "personal-ad-free",
  displayName: "Ad-free Personal",
  isActive: true,
  rewardPointsPrice: 100,
  defaultEntitlementDurationDays: 30,
  isRewardRedeemable: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
};

async function mockSession(
  page: import("@playwright/test").Page,
  permissions = authorization.permissions,
  options?: { orgsEmpty?: boolean; orgsFail?: boolean; featuresEmpty?: boolean; featuresFail?: boolean },
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: { ...authorization, permissions } });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" } });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 100 } });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    if (options?.orgsFail) {
      await route.fulfill({
        status: 500,
        json: { title: "Error", detail: "orgs failed" },
      });
      return;
    }
    await route.fulfill({
      json: options?.orgsEmpty
        ? { items: [], totalCount: 0, page: 1, pageSize: 20 }
        : {
            items: [
              { id: orgId, displayName: "Acme Trading", slug: "acme", status: "Active" },
            ],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          },
    });
  });
  await page.route("**/api/v1/platform/personal/features**", async (route) => {
    if (options?.featuresFail) {
      await route.fulfill({ status: 500, json: { title: "Error", detail: "features failed" } });
      return;
    }
    await route.fulfill({ json: options?.featuresEmpty ? [] : [feature] });
  });
  await page.route("**/api/v1/platform/personal/features/personal-ad-free", async (route) => {
    if (route.request().method() === "PATCH") {
      await route.fulfill({
        json: { ...feature, displayName: "Ad-free Plus", updatedAtUtc: "2026-08-02T00:00:00Z" },
      });
      return;
    }
    await route.fulfill({ json: feature });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("memberships hub success and open people", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await page.goto("/admin/organization-users");
  await expect(page.getByTestId("memberships-hub-page")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Memberships" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Acme Trading" })).toHaveAttribute(
    "href",
    `/admin/organizations/${orgId}/people`,
  );
  await expect(page.getByRole("heading", { name: "Under development" })).toHaveCount(0);
});

test("memberships empty and forbidden and error", async ({ page }) => {
  await mockSession(page, authorization.permissions, { orgsEmpty: true });
  await page.goto("/admin/organization-users");
  await expect(page.getByRole("heading", { name: "No organizations" })).toBeVisible();

  await mockSession(page, ["platform.permission.view_portfolio"]);
  await page.goto("/admin/organization-users");
  await expect(page.getByTestId("forbidden-state")).toBeVisible();

  await mockSession(page, authorization.permissions, { orgsFail: true });
  await page.goto("/admin/organization-users");
  await expect(page.getByText("Unable to load organizations for memberships.")).toBeVisible();
  await expect(page.getByRole("button", { name: /retry/i })).toBeVisible();
});

test("personal features list success empty forbidden error", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await page.goto("/admin/personal-features");
  await expect(page.getByTestId("personal-features-list-page")).toBeVisible();
  await expect(page.getByText("Ad-free Personal")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Under development" })).toHaveCount(0);

  await mockSession(page, authorization.permissions, { featuresEmpty: true });
  await page.goto("/admin/personal-features");
  await expect(page.getByText("No personal features were returned.")).toBeVisible();

  await mockSession(page, ["platform.permission.manage_organizations"]);
  await page.goto("/admin/personal-features");
  await expect(page.getByTestId("forbidden-state")).toBeVisible();

  await mockSession(page, authorization.permissions, { featuresFail: true });
  await page.goto("/admin/personal-features");
  await expect(page.getByText("Unable to load personal features.")).toBeVisible();
});

test("personal feature detail save", async ({ page }) => {
  await mockSession(page);
  await page.goto("/admin/personal-features/personal-ad-free");
  await expect(page.getByTestId("personal-feature-detail-page")).toBeVisible();
  await page.getByTestId("personal-features-edit-name").fill("Ad-free Plus");
  await page.getByTestId("personal-features-save").click();
  await expect(page.getByTestId("personal-features-save-success")).toBeVisible();
});

test("memberships and personal features have no overflow or serious axe issues", async ({
  page,
}) => {
  await mockSession(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/organization-users");
  await expect(page.getByTestId("memberships-hub-page")).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.setViewportSize({ width: 375, height: 812 });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    ),
  ).toBe(false);

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/personal-features");
  await expect(page.getByTestId("personal-features-list-page")).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

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
  ],
};

const organization = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
  profile: { legalName: "Northwind LLC" },
  branding: { brandDisplayName: "Northwind" },
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
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

test("overview profile save and suspend lifecycle", async ({ page }) => {
  let status = organization.status;
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/suspend") && route.request().method() === "POST") {
      status = "Suspended";
      await route.fulfill({ json: { ...organization, status } });
      return;
    }
    if (route.request().method() === "PUT" && url.includes(organization.id) && !url.includes("/branding")) {
      await route.fulfill({ json: { ...organization, profile: { legalName: "Northwind Holdings" } } });
      return;
    }
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: { ...organization, status } });
      return;
    }
    await route.fulfill({ json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 } });
  });
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
  await page.getByLabel("Legal name").fill("Northwind Holdings");
  await page.getByRole("button", { name: "Save profile" }).click();
  await expect(page.getByText("Profile saved.")).toBeVisible();
  await page.getByRole("button", { name: "Suspend" }).click();
  await page.getByRole("button", { name: "Suspend organization" }).click();
  await expect(page.getByText("Organization suspended.")).toBeVisible();
});

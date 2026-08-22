import { expect, test } from "@playwright/test";

const organization = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const authorization = {
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  platformUserId: "22222222-2222-2222-2222-222222222222",
  organizationId: null,
  permissions: [
    "platform.permission.view_portfolio",
    "platform.permission.manage_organizations",
    "platform.permission.manage_memberships",
  ],
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      json: {
        sessionId: "11111111-1111-1111-1111-111111111111",
        userId: authorization.platformUserId,
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

test("invite success and error surfaces in dialog", async ({ page }) => {
  let failInvite = true;
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/invitations") && route.request().method() === "POST") {
      if (failInvite) {
        await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "invite failed" } });
        return;
      }
      await route.fulfill({
        json: {
          id: "33333333-3333-3333-3333-333333333333",
          organizationId: organization.id,
          email: "invitee@example.test",
          role: "OrganizationMember",
          status: "Pending",
          invitationStatus: "Sent",
        },
      });
      return;
    }
    if (url.includes("/members")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    if (url.includes("/invitations")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: organization });
      return;
    }
    await route.fulfill({ json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 } });
  });
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await page.getByRole("button", { name: "Invite" }).click();
  await page.getByLabel("Contact").fill("invitee@example.test");
  await page.getByRole("button", { name: "Send invitation" }).click();
  await expect(page.getByText("invite failed")).toBeVisible();
  failInvite = false;
  await page.getByRole("button", { name: "Send invitation" }).click();
  await expect(page.getByRole("button", { name: "Send invitation" })).toHaveCount(0);
});

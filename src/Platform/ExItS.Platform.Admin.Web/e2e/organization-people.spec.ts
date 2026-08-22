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
    "platform.permission.manage_subscriptions",
    "platform.permission.view_audit_records",
    "platform.permission.view_global_catalog",
    "platform.permission.view_privacy_compliance",
    "platform.permission.manage_platform_users",
    "platform.permission.manage_memberships",
    "platform.permission.manage_manual_payments",
    "platform.permission.manage_entitlement_overrides",
  ],
};

const organization = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const member = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: organization.id,
  userId: session.userId,
  role: "OrganizationMember",
  status: "Active",
  displayName: "Ana Cruz",
  email: "ana@org.test",
  roleDisplay: "Staff",
};

const invitation = {
  id: "33333333-3333-3333-3333-333333333333",
  organizationId: organization.id,
  email: "invitee@example.test",
  role: "OrganizationMember",
  status: "Pending",
  invitationStatus: "Sent",
  roleDisplay: "Staff",
  acceptToken: "super-secret-accept-token",
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
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/users*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/audit*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 8 } });
  });
  await page.route("**/health/ready", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/health", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

function mockOrganizationRoutes(
  page: import("@playwright/test").Page,
  options?: {
    members?: unknown[];
    memberTotal?: number;
    invitations?: unknown[];
    memberStatus?: number;
    invitationStatus?: number;
    memberDetail?: string;
    invitationDetail?: string;
  },
) {
  return page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/members")) {
      expect(url).not.toMatch(/[?&](search|sortBy)=/);
      await route.fulfill({
        status: options?.memberStatus ?? 200,
        json:
          options?.memberStatus && options.memberStatus >= 400
            ? { title: "Error", status: options.memberStatus, detail: options.memberDetail }
            : {
                items: options?.members ?? [member],
                totalCount: options?.memberTotal ?? 1,
                page: 1,
                pageSize: 20,
              },
      });
      return;
    }
    if (url.includes("/invitations")) {
      expect(url).not.toMatch(/[?&](search|sortBy)=/);
      await route.fulfill({
        status: options?.invitationStatus ?? 200,
        json:
          options?.invitationStatus && options.invitationStatus >= 400
            ? {
                title: "Error",
                status: options.invitationStatus,
                detail: options.invitationDetail,
              }
            : {
                items: options?.invitations ?? [invitation],
                totalCount: (options?.invitations ?? [invitation]).length,
                page: 1,
                pageSize: 20,
              },
      });
      return;
    }
    if (url.includes("/branches")) {
      await route.fulfill({ json: [] });
      return;
    }
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: organization });
      return;
    }
    await route.fulfill({
      json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
}

test("Overview and People navigation and deep link work", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await mockOrganizationRoutes(page);
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
  const workspaceNav = page.getByRole("navigation", { name: "Organization workspace" });
  await workspaceNav.getByRole("link", { name: "People" }).click();
  await expect(page).toHaveURL(
    /\/admin\/organizations\/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\/people/,
  );
  await expect(page.getByRole("heading", { name: "People", exact: true, level: 1 })).toBeVisible();
  await expect(page.getByText("Ana Cruz")).toBeVisible();
  await expect(page.getByRole("button", { name: /invite/i })).toHaveCount(0);
  await workspaceNav.getByRole("link", { name: "Overview" }).click();
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
});

test("direct people deep link, tablet, and 375 have no overflow", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationRoutes(page);
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await expect(page.getByRole("heading", { name: "People", exact: true, level: 1 })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toContainText("People");
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("invitations tab never renders accept tokens", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationRoutes(page);
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await page.getByRole("tab", { name: "Invitations" }).click();
  await expect(page.getByText("invitee@example.test")).toBeVisible();
  await expect(page.getByText("super-secret-accept-token")).toHaveCount(0);
  await expect(page.getByRole("button", { name: /resend/i })).toHaveCount(0);
});

test("empty members and invitations stay truthful", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationRoutes(page, { members: [], memberTotal: 0, invitations: [] });
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await expect(page.getByText("No members")).toBeVisible();
  await page.getByRole("tab", { name: "Invitations" }).click();
  await expect(page.getByText("No invitations")).toBeVisible();
});

test("member list error stays in region with retry and copy diagnostics", async ({ page }) => {
  await mockCore(page);
  let fail = true;
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/members")) {
      if (fail) {
        await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
        return;
      }
      await route.fulfill({
        json: { items: [member], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    if (url.includes("/invitations")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await expect(page.getByRole("heading", { name: "Unable to load members." })).toBeVisible();
  await expect(page.getByRole("button", { name: "Copy error details" })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("Ana Cruz")).toBeVisible();
});

test("forbidden members fail-closes without leaking payload", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationRoutes(page, {
    memberStatus: 403,
    memberDetail: "member-secret",
    invitations: [invitation],
  });
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await expect(page.getByText("This list is not available.")).toBeVisible();
  await expect(page.getByText("member-secret")).toHaveCount(0);
  await page.getByRole("tab", { name: "Invitations" }).click();
  await expect(page.getByText("invitee@example.test")).toBeVisible();
});

test("people localize, theme, density, and axe", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await mockOrganizationRoutes(page);
  await page.goto(`/admin/organizations/${organization.id}/people`);
  await expect(page.getByRole("heading", { name: "People", exact: true, level: 1 })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(page.getByRole("heading", { name: "Mga Tao", exact: true, level: 1 })).toBeVisible();
  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Madilim/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});

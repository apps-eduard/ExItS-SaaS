import { expect, test, type Page } from "@playwright/test";

const sampleAudit = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  occurredAtUtc: "2026-08-19T12:00:00Z",
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  actionCode: "platform.auth.signed_in",
  targetType: "AuthSession",
  targetId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  productCode: "POS",
  correlationId: "corr-1",
  outcome: "Succeeded",
  reason: null,
  summary: "Signed in",
};

async function mockSession(page: Page) {
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
  await page.route("**/api/v1/platform/authorization/me**", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: "olivia@example.test",
        actorType: "PlatformUser",
        platformUserId: "22222222-2222-2222-2222-222222222222",
        organizationId: null,
        permissions: [
          "platform.permission.view_audit_records",
          "platform.permission.view_portfolio",
          "platform.permission.manage_organizations",
        ],
      },
    });
  });
}

test("audit list filters, detail fields, and organization link", async ({ page }) => {
  await mockSession(page);
  await page.route("**/api/v1/platform/audit**", async (route) => {
    const url = route.request().url();
    if (url.includes(sampleAudit.id)) {
      await route.fulfill({ json: sampleAudit });
      return;
    }
    await route.fulfill({
      json: { items: [sampleAudit], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
  await page.route("**/api/v1/platform/organizations/**", async (route) => {
    await route.fulfill({
      json: {
        id: sampleAudit.organizationId,
        displayName: "Demo Org",
        status: "Active",
      },
    });
  });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/audit");
  await expect(page.getByTestId("audit-list-page")).toBeVisible();
  await expect(page.getByText(sampleAudit.actionCode)).toBeVisible();

  await page.getByLabel("Actor").fill("olivia");
  await page.getByLabel("Outcome").selectOption("Succeeded");
  await page.getByRole("button", { name: "Apply filters" }).click();
  await expect(page).toHaveURL(/actor=olivia/);
  await expect(page).toHaveURL(/outcome=Succeeded/);

  await page.getByRole("link", { name: /19 Aug 2026|Aug 19/i }).click();
  await expect(page.getByTestId("audit-detail-page")).toBeVisible();
  await expect(page.getByText(sampleAudit.actorIdentifier)).toBeVisible();
  await expect(page.getByText(sampleAudit.correlationId)).toBeVisible();
  await expect(page.getByRole("link", { name: sampleAudit.organizationId })).toHaveAttribute(
    "href",
    `/admin/organizations/${sampleAudit.organizationId}`,
  );
});

test("audit list stays usable on phone viewport", async ({ page }) => {
  await mockSession(page);
  await page.route("**/api/v1/platform/audit**", async (route) => {
    await route.fulfill({
      json: { items: [sampleAudit], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin/audit");
  await expect(page.getByTestId("audit-list-page")).toBeVisible();
  await expect(page.getByText(sampleAudit.actionCode)).toBeVisible();
  const overflow = await page.evaluate(() => {
    const root = document.documentElement;
    return root.scrollWidth > root.clientWidth + 1;
  });
  expect(overflow).toBe(false);
});

test("audit API failure shows ErrorState with Retry", async ({ page }) => {
  await mockSession(page);
  await page.route("**/api/v1/platform/audit**", async (route) => {
    await route.fulfill({
      status: 503,
      json: { status: 503, title: "Service Unavailable", errorCode: "platform.unavailable" },
    });
  });
  await page.goto("/admin/audit");
  await expect(page.getByText("Unable to load platform audit.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Retry" })).toBeVisible();
  await expect(page.getByText("No audit records")).toHaveCount(0);
});

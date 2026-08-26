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
    "platform.permission.manage_manual_payments",
  ],
};

const payment = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  productCode: "pinoy-business-pos",
  amount: 499,
  currencyCode: "PHP",
  method: "Manual",
  status: "Confirmed",
  createdAtUtc: "2026-08-01T00:00:00Z",
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
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
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "csrf-token" } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("payments portfolio defaults to Confirmed and never issues unfiltered GET", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);

  const paymentGets: string[] = [];
  await page.route("**/api/v1/platform/payments**", async (route) => {
    const url = route.request().url();
    if (route.request().method() === "GET" && url.includes("/payments")) {
      paymentGets.push(url);
      const u = new URL(url);
      const hasFilter =
        Boolean(u.searchParams.get("status")) ||
        Boolean(u.searchParams.get("productCode")) ||
        Boolean(u.searchParams.get("organizationId")) ||
        Boolean(u.searchParams.get("reference"));
      expect(hasFilter, `unfiltered payments GET: ${url}`).toBe(true);
    }
    await route.fulfill({
      json: { items: [payment], totalCount: 1, page: 1, pageSize: 20 },
    });
  });

  await page.goto("/admin/payments");
  await expect(page.getByRole("heading", { name: /Payments/i })).toBeVisible();
  await expect(page.getByLabel("Status", { exact: true })).toHaveValue("Confirmed");
  await expect
    .poll(() => paymentGets.some((u) => u.includes("status=Confirmed")))
    .toBe(true);
  expect(
    paymentGets.every((u) => {
      const parsed = new URL(u);
      return (
        Boolean(parsed.searchParams.get("status")) ||
        Boolean(parsed.searchParams.get("productCode"))
      );
    }),
  ).toBe(true);

  const productSelect = page.getByLabel("Product", { exact: true });
  await expect(productSelect.locator('option[value="pinoy-business-pos"]')).toHaveCount(1, {
    timeout: 10000,
  });
  await productSelect.selectOption("pinoy-business-pos");
  await expect(page.getByLabel("Status", { exact: true }).locator('option[value=""]')).toHaveCount(1);

  await page.getByRole("button", { name: /Reset filters/i }).click();
  await expect(page).toHaveURL(/status=Confirmed/);
  await expect(page.getByLabel("Status", { exact: true })).toHaveValue("Confirmed");
  await expect(page.getByLabel("Product", { exact: true })).toHaveValue("");
});

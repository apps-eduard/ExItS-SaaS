import { expect, test } from "@playwright/test";

test.describe("auth session", () => {
  test("mocked login happy path keeps sessionToken and Bearer out of storage", async ({ page }) => {
    const orgId = "11111111-1111-1111-1111-111111111111";
    const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    await page.route("**/platform-api/**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
        });
      }

      if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }

      if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            sessionId: "22222222-2222-2222-2222-222222222222",
            username: "cashier",
            displayName: "Cashier One",
            sessionToken: "must-not-persist",
          }),
        });
      }

      if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            {
              organizationId: orgId,
              displayName: "Kizy Store",
              slug: "kizy-store",
            },
          ]),
        });
      }

      if (url.includes(`/organizations/${orgId}/branches`) && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: branchId,
              organizationId: orgId,
              code: "MAIN",
              name: "Main Branch",
              isPrimary: true,
              status: "Active",
            },
          ]),
        });
      }

      if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
        return route.fulfill({ status: 204, body: "" });
      }

      if (url.includes(`/organizations/${orgId}/branch-context`) && method === "PUT") {
        return route.fulfill({ status: 204, body: "" });
      }

      if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            accessToken: "in-memory-only-access-token",
            productAccessAllowed: true,
          }),
        });
      }

      if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
        return route.fulfill({ status: 204, body: "" });
      }

      return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });

    await page.goto("/sign-in");
    await page.getByLabel("Email or username").fill("cashier");
    await page.getByLabel("Password").fill("secret");
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page.getByRole("heading", { name: "Workspace ready" })).toBeVisible();
    await expect(page.getByRole("banner").getByText("Kizy Store · Main Branch")).toBeVisible();

    const storageScan = await page.evaluate(() => {
      const values: string[] = [];
      for (const storage of [window.localStorage, window.sessionStorage]) {
        for (let index = 0; index < storage.length; index += 1) {
          const key = storage.key(index);
          if (key) {
            values.push(`${key}=${storage.getItem(key) ?? ""}`);
          }
        }
      }
      return values.join("\n");
    });

    expect(storageScan.toLowerCase()).not.toMatch(/sessiontoken/);
    expect(storageScan).not.toMatch(/Bearer /i);
    expect(storageScan).not.toMatch(/in-memory-only-access-token/);
  });
});

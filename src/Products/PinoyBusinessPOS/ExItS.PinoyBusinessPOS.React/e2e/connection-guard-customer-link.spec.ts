import { expect, test } from "@playwright/test";
import { chooseExitsCustomerCreate } from "./helpers";
import {
  clientNavigate,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";

test.describe("EXITS-CONNECTION-GUARD-HARDENING-01 customer link eligibility", () => {
  test.use({ serviceWorkers: "block" });

  test("owner EX-ID eligibility hides Save and invite", async ({ page }) => {
    await mockBoundOwnerSession(page);

    await page.route("**/platform-api/**/resolve-public-id", async (route) => {
      if (route.request().method() !== "POST") {
        return route.fallback();
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          publicUserId: "EX-9000-0001",
          userIdentityId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          displayName: "Owner Person",
          maskedEmail: "o***@example.com",
          status: "Active",
          isSelf: true,
        }),
      });
    });

    await page.route("**/platform-api/**/customers/link-eligibility", async (route) => {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          status: "OwnerOfOrganization",
          message: "You're already the owner of this business.",
          publicUserId: "EX-9000-0001",
          displayName: "Owner Person",
          userIdentityId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        }),
      });
    });

    await page.route("**/pos-api/**/customers/checkout-search**", async (route) => {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      });
    });

    await signInAndBindOwner(page);
    await page
      .getByTestId("workspace-destination-operations")
      .waitFor({ state: "visible", timeout: 15000 });
    await page.getByTestId("workspace-destination-operations").click();
    await expect(page.getByTestId("open-customers")).toBeVisible({ timeout: 15000 });
    await clientNavigate(page, "/customers/new");
    await expect(page.getByTestId("customer-form-page")).toBeVisible({ timeout: 15000 });
    await chooseExitsCustomerCreate(page);
    await expect(page.getByTestId("customer-personal-link-panel")).toBeVisible();

    await page.getByTestId("qr-manual-id").fill("EX-9000-0001");
    await page.getByTestId("qr-manual-submit").click();

    await expect(page.getByTestId("customer-link-eligibility-OwnerOfOrganization")).toBeVisible({
      timeout: 15000,
    });
    await expect(page.getByTestId("customer-save")).toHaveCount(0);
  });
});

import { expect, test } from "@playwright/test";
import {
  clientNavigate,
  mockBoundCashierSession,
  mockBoundManagerSession,
  mockBoundOrgAdminSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindManager,
  signInAndBindOrgAdmin,
  signInAndBindOwner,
} from "./mock-bound-session";

test.describe("RMAP-02R role / experience reconciliation", () => {
  test.use({ serviceWorkers: "block" });

  test("Owner workspace chooser keeps security role Owner across experiences", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
    await expect(page.getByTestId("workspace-destination-manage_business")).toBeVisible();
    await expect(page.getByTestId("workspace-destination-operations")).toBeVisible();
    await expect(page.getByTestId("workspace-destination-start_selling")).toBeVisible();

    await page.getByTestId("workspace-destination-operations").click();
    await expect(page.getByRole("heading", { name: "Manager home" })).toBeVisible();
    await expect(page.getByTestId("security-role-label")).toContainText("Owner");

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Switch experience" }).click();
    await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
    await page.getByTestId("workspace-destination-start_selling").click();
    await expect(page.getByTestId("sell-floor")).toBeVisible();
  });

  test("Owner can open Manage Business without inventing a branch", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await page.getByTestId("workspace-destination-manage_business").click();
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await expect(page.getByRole("link", { name: "Invite staff" })).toBeVisible();
  });

  test("Manager sees Operations and Start Selling chooser", async ({ page }) => {
    await mockBoundManagerSession(page);
    await page.goto("/sign-in");
    await page.getByLabel("Email or staff login").fill("manager");
    await page.getByLabel("Password").fill("secret");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
    await expect(page.getByTestId("workspace-destination-manage_business")).toHaveCount(0);
    await expect(page.getByTestId("workspace-destination-operations")).toBeVisible();
    await expect(page.getByTestId("workspace-destination-start_selling")).toBeVisible();

    await page.getByTestId("workspace-destination-operations").click();
    await expect(page.getByRole("heading", { name: "Manager home" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Start selling" })).toBeVisible();

    await clientNavigate(page, "/org");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();

    await clientNavigate(page, "/role/owner");
    await expect(page.getByTestId("owner-role-denied")).toBeVisible();
  });

  test("Cashier auto-routes Start Selling and cannot open management", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "Sell floor" })).toBeVisible();

    await clientNavigate(page, "/role/manager");
    await expect(page.getByTestId("manager-role-denied")).toBeVisible();

    await clientNavigate(page, "/org");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();

    await clientNavigate(page, "/org/staff/invite");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();
  });

  test("OrganizationAdministrator reaches Manage Business without branch force", async ({
    page,
  }) => {
    await mockBoundOrgAdminSession(page);
    await signInAndBindOrgAdmin(page);
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await expect(page.getByRole("link", { name: "Invite staff" })).toHaveCount(0);

    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("sell-access-denied")).toBeVisible();
  });

  for (const viewport of [
    { width: 375, height: 812, name: "375x812" },
    { width: 768, height: 1024, name: "768x1024" },
    { width: 1024, height: 768, name: "1024x768" },
    { width: 1440, height: 900, name: "1440x900" },
  ] as const) {
    test(`Owner workspace experience chooser usable at ${viewport.name}`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await mockBoundOwnerSession(page);
      await signInAndBindOwner(page);
      await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
      const manage = page.getByTestId("workspace-destination-manage_business");
      await expect(manage).toBeVisible();
      const box = await manage.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.height).toBeGreaterThanOrEqual(40);
      await expect(page.getByTestId("workspace-destination-operations")).toBeVisible();
      await expect(page.getByTestId("workspace-destination-start_selling")).toBeVisible();
      const overflow = await page.evaluate(() => {
        const root = document.documentElement;
        return root.scrollWidth > root.clientWidth + 1;
      });
      expect(overflow).toBe(false);
    });
  }
});

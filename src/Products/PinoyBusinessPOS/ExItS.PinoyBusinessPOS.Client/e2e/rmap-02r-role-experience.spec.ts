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

  test("Owner experience chooser keeps security role Owner", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await expect(page.getByRole("heading", { name: "Owner home" })).toBeVisible();
    await expect(page.getByTestId("owner-experience-chooser")).toBeVisible();
    await expect(page.getByTestId("security-role-label")).toContainText("Owner");

    await page.getByRole("link", { name: "Operations" }).click();
    await expect(page.getByRole("heading", { name: "Manager home" })).toBeVisible();
    await expect(page.getByTestId("security-role-label")).toContainText("Owner");

    await clientNavigate(page, "/role/owner");
    await page.getByRole("button", { name: "Start selling" }).click();
    await expect(page.getByTestId("sell-floor")).toBeVisible();
  });

  test("Owner can open admin experience and invite CTA", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await page.getByRole("link", { name: "Manage business" }).click();
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await expect(page.getByRole("link", { name: "Invite staff" })).toBeVisible();
  });

  test("Manager can sell but cannot open admin or owner home", async ({ page }) => {
    await mockBoundManagerSession(page);
    await signInAndBindManager(page);
    await expect(page.getByRole("heading", { name: "Manager home" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Start selling" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Invite staff" })).toHaveCount(0);

    await page.getByRole("button", { name: "Start selling" }).click();
    await expect(page.getByTestId("sell-floor")).toBeVisible();

    await clientNavigate(page, "/org");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();

    await clientNavigate(page, "/role/owner");
    await expect(page.getByTestId("owner-role-denied")).toBeVisible();
  });

  test("Cashier cannot open manager or admin experiences", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "Cashier home" })).toBeVisible();

    await clientNavigate(page, "/role/manager");
    await expect(page.getByTestId("manager-role-denied")).toBeVisible();

    await clientNavigate(page, "/org");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();

    await clientNavigate(page, "/org/staff/invite");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();
  });

  test("OrganizationAdministrator reaches org essentials without sell floor", async ({ page }) => {
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
    test(`Owner experience chooser usable at ${viewport.name}`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await mockBoundOwnerSession(page);
      await signInAndBindOwner(page);
      const chooser = page.getByTestId("owner-experience-chooser");
      await expect(chooser).toBeVisible();
      const box = await chooser.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.width).toBeLessThanOrEqual(viewport.width + 1);
      await expect(page.getByRole("link", { name: "Manage business" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Start selling" })).toBeVisible();
      const overflow = await page.evaluate(() => {
        const root = document.documentElement;
        return root.scrollWidth > root.clientWidth + 1;
      });
      expect(overflow).toBe(false);
    });
  }
});

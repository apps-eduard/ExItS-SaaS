import { expect, test } from "@playwright/test";
import { mockBoundManagerSession, signInAndBindManager } from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";

async function signInOpsReady(page: import("@playwright/test").Page) {
  await seedInstallationId(page);
  await mockBoundManagerSession(page);
  await mockAuthorizedPosDevice(page);
  await mockPosCatalogApi(page);
  await mockPosRegisterShiftApi(page, { openShift: true });
  await signInAndBindManager(page);
  // Stay on the bound manager home — full page.goto clears in-memory workspace bind.
}

test.describe("Responsive org bottom navigation", () => {
  test.use({ serviceWorkers: "block" });

  test("shows bottom nav on phone with primary destinations", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await signInOpsReady(page);
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    await expect(page.getByTestId("org-nav-home")).toBeVisible();
    await expect(page.getByTestId("org-nav-sell")).toBeVisible();
    await expect(page.getByTestId("org-nav-catalog")).toBeVisible();
    await expect(page.getByTestId("org-nav-orders")).toBeVisible();
    await expect(page.getByTestId("org-nav-more")).toBeVisible();
  });

  test("selected state follows route and More opens hub", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await signInOpsReady(page);
    await page.getByTestId("org-nav-sell").click();
    await expect(page.getByTestId("org-nav-sell")).toHaveAttribute("aria-current", "page");
    await page.getByTestId("org-nav-more").click();
    await expect(page).toHaveURL(/\/more$/);
    await expect(page.getByTestId("org-more-page")).toBeVisible();
    await expect(page.getByTestId("org-nav-more")).toHaveAttribute("aria-current", "page");
  });

  test("hides bottom nav on desktop while keeping top bar", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await signInOpsReady(page);
    await expect(page.getByTestId("org-bottom-nav")).toBeAttached();
    await expect(page.getByTestId("org-bottom-nav")).toBeHidden();
    await expect(page.getByRole("banner")).toBeVisible();
  });

  test("shows balanced bottom nav on tablet portrait; hides at lg landscape", async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await signInOpsReady(page);
    await expect(page.getByTestId("org-bottom-nav")).toBeAttached();
    await expect(page.getByTestId("org-bottom-nav")).toBeHidden();

    await page.setViewportSize({ width: 768, height: 1024 });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    const box = await page
      .getByTestId("org-bottom-nav")
      .locator(".org-bottom-nav-inner")
      .boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeLessThanOrEqual(768);
  });
});

import { expect, test } from "@playwright/test";
import { mockBoundManagerSession, signInAndBindManager } from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";
import path from "node:path";
import fs from "node:fs";

const outDir = path.resolve(
  process.cwd(),
  "../../../../docs/Mobile-React/Reports/impl-pos-react-responsive-bottom-nav",
);

async function signInOpsReady(page: import("@playwright/test").Page) {
  await seedInstallationId(page);
  await mockBoundManagerSession(page);
  await mockAuthorizedPosDevice(page);
  await mockPosCatalogApi(page);
  await mockPosRegisterShiftApi(page, { openShift: true });
  await signInAndBindManager(page);
}

test.describe("Bottom nav visual validation screenshots", () => {
  test.use({ serviceWorkers: "block" });

  test("capture phone / tablet / desktop shells", async ({ page }) => {
    fs.mkdirSync(outDir, { recursive: true });

    await page.setViewportSize({ width: 390, height: 844 });
    await signInOpsReady(page);
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    await page.screenshot({
      path: path.join(outDir, "phone-390x844.png"),
      fullPage: false,
    });

    await page.getByTestId("org-nav-sell").click();
    await expect(page.getByTestId("org-nav-sell")).toHaveAttribute("aria-current", "page");
    await page.screenshot({
      path: path.join(outDir, "phone-sell-390x844.png"),
      fullPage: false,
    });

    await page.setViewportSize({ width: 768, height: 1024 });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    await page.screenshot({
      path: path.join(outDir, "tablet-portrait-768x1024.png"),
      fullPage: false,
    });

    await page.setViewportSize({ width: 1024, height: 768 });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    await page.screenshot({
      path: path.join(outDir, "tablet-landscape-1024x768.png"),
      fullPage: false,
    });

    await page.setViewportSize({ width: 1440, height: 900 });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    await page.screenshot({
      path: path.join(outDir, "desktop-1440x900.png"),
      fullPage: false,
    });
  });
});

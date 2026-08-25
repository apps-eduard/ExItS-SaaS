import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  chooseOwnerManageBusiness,
  chooseOwnerOperations,
  clientNavigate,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

async function signInOwnerManage(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-manage_business")
    .waitFor({ state: "visible", timeout: 15000 });
  await chooseOwnerManageBusiness(page);
  await expect(page.getByTestId("org-essentials-page")).toBeVisible({ timeout: 15000 });
}

async function signInOwnerOps(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-operations")
    .waitFor({ state: "visible", timeout: 15000 });
  await chooseOwnerOperations(page);
  await expect(page.getByTestId("open-shifts").first()).toBeVisible({ timeout: 15000 });
}

test.describe("I18N-01 language + cash handling", () => {
  test.use({ serviceWorkers: "block" });

  test("language selection persists across reload", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await page.goto("/settings/preferences");
    await expect(
      page.getByRole("heading", { name: /Preferences|Mga setting|Preferensiya/i }),
    ).toBeVisible();
    await page.getByRole("radio", { name: /Bisaya \(Cebuano\)/i }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "ceb-PH");
    await page.reload();
    await expect(page.locator("html")).toHaveAttribute("lang", "ceb-PH");
  });

  test("cash denomination defaults exclude 0.01 and support add/remove/empty", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const api = await mockPosRegisterShiftApi(page, {
      openShift: false,
      openingCashCountMode: "Optional",
      closingCashCountMode: "Optional",
    });
    await signInOwnerManage(page);
    await clientNavigate(page, "/org/cash-handling");
    await expect(page.getByTestId("cash-handling-page")).toBeVisible();
    await expect(page.getByTestId("cash-handling-denom-0.01")).toHaveCount(0);
    await expect(page.getByTestId("cash-handling-denom-1000")).toBeVisible();
    await expect(page.getByTestId("cash-handling-denom-0.05")).toBeVisible();

    await page.getByTestId("cash-handling-add-value").fill("0.50");
    await page.getByTestId("cash-handling-add").click();
    await expect(page.getByTestId("cash-handling-denom-0.5")).toBeVisible();

    await page.getByTestId("cash-handling-remove-0.5").click();
    await expect(page.getByTestId("cash-handling-denom-0.5")).toHaveCount(0);

    api.setState({ denominations: [] });
    await clientNavigate(page, "/org");
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await clientNavigate(page, "/org/cash-handling");
    await expect(page.getByTestId("cash-handling-denoms-empty")).toBeVisible();
    await expect(
      page.getByText(/No cash denominations configured|Walang|Walay|Awan|Wala/i),
    ).toBeVisible();
  });

  test("opening required blocks skip; closing required blocks skip close", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPosRegisterShiftApi(page, {
      openShift: false,
      openingCashCountMode: "Required",
      closingCashCountMode: "Required",
      denominations: [],
    });
    await signInOwnerOps(page);
    await page.getByTestId("open-shift-open").first().click();
    await expect(page.getByTestId("shift-open-page")).toBeVisible();
    await expect(page.getByTestId("opening-denom-empty")).toBeVisible();
    await expect(page.getByTestId("shift-open-skip-cash")).toHaveCount(0);
    await page.getByTestId("shift-opening-cash").fill("100");
    await page.getByTestId("shift-open-confirm").click();
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await expect(page.getByTestId("shift-close-skip-cash")).toHaveCount(0);
    await page.getByTestId("shift-closing-cash").fill("100");
    await page.getByTestId("shift-close-confirm").click();
    await expect(page.getByTestId("shift-status-chip")).toBeVisible();
  });

  test("opening optional allows skip; closing optional allows skip close", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPosRegisterShiftApi(page, {
      openShift: false,
      openingCashCountMode: "Optional",
      closingCashCountMode: "Optional",
      denominations: [],
    });
    await signInOwnerOps(page);
    await page.getByTestId("open-shift-open").first().click();
    await expect(page.getByTestId("shift-open-page")).toBeVisible();
    await expect(page.getByTestId("shift-open-skip-cash")).toBeVisible();
    await page.getByTestId("shift-open-skip-cash").click();
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await expect(page.getByTestId("shift-close-skip-cash")).toBeVisible();
    await page.getByTestId("shift-close-skip-cash").click();
    await expect(page.getByTestId("shift-status-chip")).toBeVisible();
  });

  test("admin can configure opening-only and closing-only requirements", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPosRegisterShiftApi(page, {
      openShift: false,
      openingCashCountMode: "Optional",
      closingCashCountMode: "Optional",
    });
    await signInOwnerManage(page);
    await clientNavigate(page, "/org/cash-handling");
    await expect(page.getByTestId("cash-handling-require-opening")).not.toBeChecked();
    await expect(page.getByTestId("cash-handling-require-closing")).not.toBeChecked();

    await page.getByTestId("cash-handling-require-opening").check();
    await page.getByTestId("cash-handling-save-policy").click();
    await expect(page.getByText(/saved|Na-save/i)).toBeVisible();

    await page.getByTestId("cash-handling-require-closing").check();
    await page.getByTestId("cash-handling-require-opening").uncheck();
    await page.getByTestId("cash-handling-save-policy").click();
    await expect(page.getByText(/saved|Na-save/i)).toBeVisible();
    await expect(page.getByTestId("cash-handling-require-opening")).not.toBeChecked();
    await expect(page.getByTestId("cash-handling-require-closing")).toBeChecked();
  });

  test("already-open shift keeps prior closing policy snapshot", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const api = await mockPosRegisterShiftApi(page, {
      openShift: true,
      openingCashCountMode: "Optional",
      closingCashCountMode: "Required",
    });
    await signInOwnerOps(page);
    await page.getByTestId("open-shifts").first().click();
    await expect(page.getByTestId("shifts-hub-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("shift-open-detail").click();
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await expect(page.getByTestId("shift-close-skip-cash")).toHaveCount(0);

    await page.getByRole("button", { name: /Switch workspace/i }).click();
    await page
      .getByTestId("workspace-destination-manage_business")
      .waitFor({ state: "visible", timeout: 15000 });
    await chooseOwnerManageBusiness(page);
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await page.getByTestId("open-cash-handling").click();
    await expect(page.getByTestId("cash-handling-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("cash-handling-require-closing").uncheck();
    await page.getByTestId("cash-handling-save-policy").click();
    api.setState({ closingCashCountMode: "Optional", openingCashCountMode: "Optional" });

    await page.getByRole("button", { name: /Switch workspace/i }).click();
    await page
      .getByTestId("workspace-destination-operations")
      .waitFor({ state: "visible", timeout: 15000 });
    await chooseOwnerOperations(page);
    await page.getByTestId("open-shifts").first().click();
    await page.getByTestId("shift-open-detail").click();
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await expect(page.getByTestId("shift-close-skip-cash")).toHaveCount(0);
  });

  for (const viewport of viewports) {
    test(`responsive ${viewport.width}x${viewport.height} cash handling`, async ({ page }) => {
      await mockBoundOwnerSession(page);
      await mockPosRegisterShiftApi(page, {
        openShift: false,
        openingCashCountMode: "Optional",
        closingCashCountMode: "Optional",
      });
      await signInOwnerManage(page);
      await page.setViewportSize(viewport);
      await clientNavigate(page, "/org/cash-handling");
      await expect(page.getByTestId("cash-handling-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
      const target = page.getByTestId("cash-handling-save-policy");
      const box = await target.boundingBox();
      expect(box?.height ?? 0).toBeGreaterThanOrEqual(40);
    });
  }
});

import { expect, test } from "@playwright/test";
import {
  mockBoundCashierSession,
  mockPersonalSession,
  signInAndBindCashier,
  signInAsPersonal,
  expectSellEntryVisible,
} from "./mock-bound-session";

test.describe("RMAP-02 workspace / role guards", () => {
  test.use({ serviceWorkers: "block" });

  test("Personal session is denied sell floor", async ({ page }) => {
    await mockPersonalSession(page);
    await signInAsPersonal(page);
    await page.goto("/sell");
    await expect(page.getByTestId("account-class-denied")).toBeVisible();
  });

  test("locked Organization staff workspace control is not a switch action", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await expectSellEntryVisible(page);
    const context = page.getByTestId("workspace-context");
    await expect(context).toBeVisible();
    await expect(context).toBeDisabled();
    await expect(context).not.toHaveAttribute("aria-label", /Switch workspace/i);
  });

  test("product access denied fails closed at workspace bind", async ({ page }) => {
    await mockBoundCashierSession(page, {
      productAccessAllowed: false,
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
    });
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
    await expect(page.getByTestId("sell-floor")).toHaveCount(0);
  });
});

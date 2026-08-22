import { expect, test } from "@playwright/test";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundOwnerSession,
} from "./mock-bound-session";

const E2E_BRANCH_2_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

test.describe("workspace after offline PIN enrollment", () => {
  test.use({ serviceWorkers: "block" });

  test("owner sees authorized destinations after PIN setup", async ({ page }) => {
    await mockBoundOwnerSession(page, {
      extraBranches: [
        {
          id: E2E_BRANCH_2_ID,
          organizationId: E2E_ORG_ID,
          code: "K02",
          name: "Kizy Store 02",
          isPrimary: false,
          status: "Active",
        },
      ],
    });

    await page.goto("/sign-in");
    await page.getByLabel("Email or staff login").fill("owner");
    await page.getByLabel("Password").fill("secret");
    await page.getByTestId("sign-in-submit").click();
    await expect(page.getByTestId("offline-pin-setup-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("offline-pin-enroll-input").fill("123456");
    await page.getByTestId("offline-pin-enroll-confirm").fill("123456");
    await page.getByTestId("offline-pin-enroll-submit").click();

    await expect(page).toHaveURL(/\/workspace$/, { timeout: 15000 });
    await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
    await expect(page.getByText("Kizy Store", { exact: true })).toBeVisible();
    await expect(page.getByTestId("workspace-destination-manage_business")).toBeVisible();
    await expect(page.getByTestId(`workspace-branch-${E2E_BRANCH_ID}`)).toBeVisible();
    await expect(page.getByTestId(`workspace-branch-${E2E_BRANCH_2_ID}`)).toBeVisible();
    await expect(page.getByTestId("workspace-destination-operations")).toHaveCount(2);
    await expect(page.getByTestId("workspace-destination-start_selling")).toHaveCount(2);

    for (const viewport of [
      { width: 390, height: 844 },
      { width: 768, height: 1024 },
      { width: 1024, height: 768 },
    ]) {
      await page.setViewportSize(viewport);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth > window.innerWidth,
      );
      expect(overflow).toBe(false);
    }
  });
});

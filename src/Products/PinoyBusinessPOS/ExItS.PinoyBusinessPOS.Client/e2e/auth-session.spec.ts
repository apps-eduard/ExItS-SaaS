import { expect, test } from "@playwright/test";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";

test.describe("auth session", () => {
  test("mocked login happy path keeps sessionToken and Bearer out of storage", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);

    await expect(page.getByRole("heading", { name: "Cashier home" })).toBeVisible();
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

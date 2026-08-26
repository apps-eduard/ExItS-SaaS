import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mockSignedOutSession } from "./helpers/session";

const artifactDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "artifacts");

async function assertNoHorizontalOverflow(page: import("@playwright/test").Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

test.describe("sign in", () => {
  test.beforeEach(async ({ page }) => {
    await mockSignedOutSession(page);
  });

  test("phone 375 has compact Sign in without overflow", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expect(page.getByLabel("Email or username")).toBeVisible();
    await expect(page.getByLabel("Password", { exact: true })).toBeVisible();
    await assertNoHorizontalOverflow(page);
    const submit = page.getByRole("button", { name: "Sign in" });
    const box = await submit.boundingBox();
    expect(box?.height).toBeGreaterThanOrEqual(44);
  });

  test("tablet and desktop Sign in do not overflow", async ({ page }) => {
    for (const viewport of [
      { width: 768, height: 1024 },
      { width: 1280, height: 800 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/sign-in");
      await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }
  });

  test("English Filipino Light and Dark Sign in remain usable", async ({ page }) => {
    const shots: Array<{ file: string; locale?: "fil-PH"; theme?: "dark" }> = [
      { file: "sign-in-375-en-light.png" },
      { file: "sign-in-375-en-dark.png", theme: "dark" },
      { file: "sign-in-375-fil-PH.png", locale: "fil-PH" },
    ];

    for (const shot of shots) {
      await page.addInitScript(
        ({ theme, locale }) => {
          localStorage.setItem(
            "exits.mobile-client.ui-preferences.v1",
            JSON.stringify({ theme: theme ?? "light", locale: locale ?? "en" }),
          );
        },
        { theme: shot.theme, locale: shot.locale },
      );
      await page.setViewportSize({ width: 375, height: 812 });
      await page.goto("/sign-in", { waitUntil: "networkidle" });
      const heading = shot.locale === "fil-PH" ? "Mag-sign in" : "Sign in";
      await expect(page.getByRole("heading", { name: heading })).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await page.screenshot({
        path: path.join(artifactDir, shot.file),
        fullPage: true,
      });
    }
  });

  test("axe has no serious or critical violations on Sign in", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});

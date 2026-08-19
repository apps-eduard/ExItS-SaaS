import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import path from "node:path";
import { fileURLToPath } from "node:url";

const artifactDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "artifacts");

async function assertNoHorizontalOverflow(page: import("@playwright/test").Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

async function assertTouchTargets(page: import("@playwright/test").Page) {
  const targets = page.locator("nav a, button").locator("visible=true");
  const count = await targets.count();
  expect(count).toBeGreaterThan(0);
  for (let i = 0; i < count; i += 1) {
    const box = await targets.nth(i).boundingBox();
    expect(box).not.toBeNull();
    if (box) {
      expect(box.height).toBeGreaterThanOrEqual(44);
    }
  }
}

test.describe("foundation shell", () => {
  test("phone 320 and 375 have no horizontal overflow", async ({ page }) => {
    for (const width of [320, 375] as const) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto("/");
      await expect(page.getByRole("heading", { name: "Client foundation" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await assertTouchTargets(page);
    }
  });

  test("tablet portrait and landscape render the shell", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Client foundation" })).toBeVisible();
    await assertNoHorizontalOverflow(page);

    await page.setViewportSize({ width: 1024, height: 768 });
    await expect(page.getByRole("heading", { name: "Client foundation" })).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test("desktop width uses side navigation", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Client foundation" })).toBeVisible();
    await expect(page.locator("aside")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test("keyboard focus is visible on appearance controls", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/appearance");
    await page.keyboard.press("Tab");
    await page.keyboard.press("Tab");
    const focused = page.locator(":focus-visible");
    await expect(focused).toBeVisible();
  });

  test("axe has no serious or critical violations on the foundation screen", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });

  test("captures human-visual preview screenshots", async ({ page }) => {
    const shots: Array<{
      file: string;
      width: number;
      height: number;
      locale?: "fil-PH";
      theme?: "dark";
    }> = [
      { file: "375x812-en-light.png", width: 375, height: 812 },
      { file: "375x812-en-dark.png", width: 375, height: 812, theme: "dark" },
      { file: "375x812-fil-PH.png", width: 375, height: 812, locale: "fil-PH" },
      { file: "768x1024-en-light.png", width: 768, height: 1024 },
      { file: "1280x800-en-light.png", width: 1280, height: 800 },
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
      await page.setViewportSize({ width: shot.width, height: shot.height });
      await page.goto("/", { waitUntil: "networkidle" });
      await page.screenshot({
        path: path.join(artifactDir, shot.file),
        fullPage: true,
      });
    }
  });
});

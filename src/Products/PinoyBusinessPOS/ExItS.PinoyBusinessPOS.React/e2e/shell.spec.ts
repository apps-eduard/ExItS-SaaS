import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mockAuthenticatedSession } from "./helpers/session";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../../docs/Mobile-React/Reports/impl-02a-ui",
);

const forbidden = [
  "Preview",
  "Foundation",
  "1,250.00",
  "Online",
  "Synced",
  "no workspace selected",
  "this package",
  "not live",
];

async function assertNoHorizontalOverflow(page: import("@playwright/test").Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

async function assertProductCopy(page: import("@playwright/test").Page) {
  const text = (await page.locator("body").innerText()).toLowerCase();
  for (const phrase of forbidden) {
    expect(text, `product UI must not contain “${phrase}”`).not.toContain(phrase.toLowerCase());
  }
}

test.describe("product shell", () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthenticatedSession(page);
  });

  test("phone 375 and 393 have compact chrome and no overflow", async ({ page }) => {
    for (const viewport of [
      { width: 375, height: 812 },
      { width: 393, height: 852 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/");
      await expect(page.getByRole("heading", { name: "ExItS Mobile" })).toBeVisible();
      const topBar = page.getByRole("banner");
      await expect(topBar).toContainText("ExItS Mobile");
      await expect(topBar).not.toContainText("Online");
      await expect(topBar).not.toContainText("Offline");
      await expect(topBar).not.toContainText("workspace");
      await expect(page.getByRole("button", { name: "Settings" })).toBeVisible();
      await expect(page.getByRole("navigation")).toHaveCount(0);
      await expect(page.getByRole("link", { name: "Sell" })).toHaveCount(0);
      await expect(page.getByRole("link", { name: "Appearance" })).toHaveCount(0);
      await assertProductCopy(page);
      await assertNoHorizontalOverflow(page);
      const barBox = await topBar.boundingBox();
      expect(barBox?.height).toBeGreaterThanOrEqual(56);
      expect(barBox?.height).toBeLessThanOrEqual(64);
    }
  });

  test("Appearance opens from Settings and is absent from primary nav", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await page.getByRole("button", { name: "Settings" }).click();
    await expect(page.getByRole("heading", { name: "Appearance" })).toBeVisible();
    await expect(page.getByRole("radio", { name: "English" })).toBeVisible();
    await expect(page.getByRole("radio", { name: "System" })).toHaveAttribute(
      "aria-checked",
      "true",
    );
    await expect(page.getByRole("link", { name: "Back" })).toBeVisible();
    const english = page.getByRole("radio", { name: "English" });
    await english.focus();
    await expect(english).toBeFocused();
  });

  test("tablet and desktop do not invent a fake sidebar", async ({ page }) => {
    for (const viewport of [
      { width: 768, height: 1024 },
      { width: 1280, height: 800 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/");
      await expect(page.getByRole("heading", { name: "ExItS Mobile" })).toBeVisible();
      await expect(page.locator("aside")).toHaveCount(0);
      await expect(page.getByRole("navigation")).toHaveCount(0);
      await assertProductCopy(page);
      await assertNoHorizontalOverflow(page);
    }
  });

  test("axe has no serious or critical violations on home and appearance", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    for (const route of ["/", "/appearance"] as const) {
      await page.goto(route);
      const results = await new AxeBuilder({ page }).analyze();
      const serious = results.violations.filter(
        (violation) => violation.impact === "serious" || violation.impact === "critical",
      );
      expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
    }
  });

  test("captures product-shell screenshots", async ({ page }) => {
    const shots: Array<{
      file: string;
      width: number;
      height: number;
      route?: "/appearance";
      locale?: "fil-PH";
      theme?: "dark";
    }> = [
      { file: "01-home-375x812-en-light.png", width: 375, height: 812 },
      { file: "02-home-375x812-en-dark.png", width: 375, height: 812, theme: "dark" },
      { file: "03-home-375x812-fil-PH.png", width: 375, height: 812, locale: "fil-PH" },
      { file: "04-appearance-375x812-en-light.png", width: 375, height: 812, route: "/appearance" },
      {
        file: "05-appearance-375x812-en-dark.png",
        width: 375,
        height: 812,
        route: "/appearance",
        theme: "dark",
      },
      { file: "06-home-768x1024.png", width: 768, height: 1024 },
      { file: "07-home-1280x800.png", width: 1280, height: 800 },
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
      await page.goto(shot.route ?? "/", { waitUntil: "networkidle" });
      await assertProductCopy(page);
      await page.screenshot({
        path: path.join(screenshotDir, shot.file),
        fullPage: true,
      });
    }
  });
});

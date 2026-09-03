import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const publicPages = [
  "/",
  "/pos",
  "/products",
  "/service-pro",
  "/pricing",
  "/about",
  "/contact",
  "/privacy",
  "/terms",
] as const;

const responsiveViewports = [
  { width: 375, height: 812 },
  { width: 640, height: 900 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1280, height: 800 },
  { width: 1440, height: 900 },
  { width: 1536, height: 960 },
  { width: 1920, height: 1080 },
] as const;

for (const path of publicPages) {
  test(`axe accessibility scan has no serious/critical violations on ${path}`, async ({
    page,
  }) => {
    await page.goto(path);
    const results = await new AxeBuilder({ page })
      .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
      .analyze();

    const blocking = results.violations.filter((violation) =>
      ["serious", "critical"].includes(violation.impact ?? ""),
    );

    expect(
      blocking,
      blocking
        .map((v) => `${v.id} [${v.impact}]: ${v.help} :: ${v.nodes.map((n) => n.html).join(" | ")}`)
        .join("\n\n"),
    ).toEqual([]);
  });
}

test("drawer supports keyboard open, focus trap, Escape, and focus return", async ({
  page,
}) => {
  await page.goto("/");
  const menuButton = page.getByRole("button", { name: "Open menu" });
  await menuButton.focus();
  await page.keyboard.press("Enter");

  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await expect(page.getByRole("heading", { name: "Main menu" })).toBeAttached();

  // Focus should be inside the dialog (Radix moves focus into content).
  await expect
    .poll(async () =>
      page.evaluate(() => {
        const active = document.activeElement;
        const dialogEl = document.querySelector('[role="dialog"]');
        return Boolean(active && dialogEl?.contains(active));
      }),
    )
    .toBe(true);

  await page.keyboard.press("Escape");
  await expect(dialog).toBeHidden();
  await expect(menuButton).toBeFocused();
});

test("skip link moves keyboard focus to main content", async ({ page }) => {
  await page.goto("/");
  await page.keyboard.press("Tab");
  const skip = page.getByRole("link", { name: "Skip to main content" });
  await expect(skip).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.locator("#main-content")).toBeFocused();
});

test("reduced motion keeps content visible without reveal opacity animation", async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/");

  const hero = page.getByRole("heading", { level: 1 });
  await expect(hero).toBeVisible();
  const heroOpacity = await hero.evaluate((el) => {
    const opacity = window.getComputedStyle(el).opacity;
    return opacity === "" ? "1" : opacity;
  });
  expect(Number(heroOpacity)).toBeGreaterThan(0.95);
});

for (const viewport of responsiveViewports) {
  for (const path of ["/", "/pos", "/pricing", "/contact", "/privacy"] as const) {
    test(`${path} has no horizontal overflow at ${viewport.width}px`, async ({
      page,
    }) => {
      await page.setViewportSize(viewport);
      await page.goto(path);
      const overflow = await page.evaluate(
        () =>
          document.documentElement.scrollWidth >
          document.documentElement.clientWidth + 1,
      );
      expect(overflow).toBe(false);
    });
  }
}

test("SEO hardening remains intact after WEB-10", async ({ request, page }) => {
  const sitemap = await (await request.get("/sitemap.xml")).text();
  expect(sitemap).toContain("https://exits.ph/pos");
  expect(sitemap).not.toContain("/loan-manager");

  const robots = await (await request.get("/robots.txt")).text();
  expect(robots).toContain("Sitemap: https://exits.ph/sitemap.xml");

  await page.goto("/pos");
  const ld = await page.locator('script[type="application/ld+json"]').allTextContents();
  expect(ld.some((block) => block.includes("SoftwareApplication"))).toBe(true);
  expect(ld.join("\n")).not.toContain('"price"');
});

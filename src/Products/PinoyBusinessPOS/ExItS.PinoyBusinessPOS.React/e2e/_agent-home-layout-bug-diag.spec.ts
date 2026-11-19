/**
 * Diagnostic: measure Personal Home atmosphere / shell geometry at 1440.
 * One-off agent capture — not permanent suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { clientNavigate, mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown) {
  return route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function installHomeMocks(page: Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/api/v1/personal/dashboard")) {
      return json(route, {
        userIdentityId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        accountProfileId: "11111111-1111-4111-8111-111111111111",
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 12,
        activeRelationshipCount: 5,
        totalLentBalance: 128450.75,
        totalBorrowedBalance: 98765.5,
        pendingConfirmationCount: 1,
        sharedWithMeLentBalance: 15420.25,
        sharedWithMeBorrowedBalance: 8320,
        sharedWithMeActiveCount: 3,
      });
    }
    if (url.includes("/api/v1/personal/")) {
      return json(route, []);
    }
    return route.fallback();
  });
}

test("diagnose Personal Home black-area layout at 1440", async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.evaluate(() => {
    document.documentElement.setAttribute("data-theme", "dark");
    document.documentElement.setAttribute("data-primary", "green");
    document.documentElement.setAttribute("data-accent", "green");
  });
  await expect(page.getByTestId("personal-home-page")).toBeVisible();

  const metrics = await page.evaluate(() => {
    const pick = (sel: string) => {
      const el = document.querySelector(sel) as HTMLElement | null;
      if (!el) return null;
      const r = el.getBoundingClientRect();
      const cs = getComputedStyle(el);
      return {
        sel,
        tag: el.tagName,
        className: el.className?.toString?.().slice(0, 120) ?? "",
        x: r.x,
        y: r.y,
        w: r.width,
        h: r.height,
        position: cs.position,
        top: cs.top,
        height: cs.height,
        overflow: cs.overflow,
        zIndex: cs.zIndex,
        bg: cs.backgroundColor,
        fill: (el as SVGElement).getAttribute?.("fill") ?? cs.fill,
        opacity: cs.opacity,
        maskImage: cs.maskImage || (cs as CSSStyleDeclaration & { webkitMaskImage?: string }).webkitMaskImage,
      };
    };

    const wavePaths = Array.from(
      document.querySelectorAll(".personal-shell-atmosphere__wave"),
    ).map((el, i) => {
      const r = (el as SVGGraphicsElement).getBoundingClientRect();
      const cs = getComputedStyle(el);
      return {
        i,
        className: el.className?.baseVal ?? el.getAttribute("class"),
        x: r.x,
        y: r.y,
        w: r.width,
        h: r.height,
        fill: cs.fill,
        opacity: cs.opacity,
        color: cs.color,
      };
    });

    const hero = document.querySelector('[data-testid="personal-home-hero"]') as HTMLElement | null;
    const pageEl = document.querySelector('[data-testid="personal-home-page"]') as HTMLElement | null;
    const content = document.querySelector(".admin-shell__content, .personal-shell__content") as HTMLElement | null;

    return {
      crystal: pick('[data-testid="personal-crystal-screen"]'),
      atmosphere: pick('[data-testid="personal-shell-atmosphere"]'),
      canvas: pick(".personal-shell-atmosphere__canvas"),
      waves: pick(".personal-shell-atmosphere__waves"),
      shell: pick('[data-testid="personal-shell"]'),
      hero: hero
        ? {
            y: hero.getBoundingClientRect().y,
            h: hero.getBoundingClientRect().height,
          }
        : null,
      page: pageEl
        ? {
            y: pageEl.getBoundingClientRect().y,
            h: pageEl.getBoundingClientRect().height,
          }
        : null,
      content: content
        ? {
            y: content.getBoundingClientRect().y,
            h: content.getBoundingClientRect().height,
            paddingTop: getComputedStyle(content).paddingTop,
          }
        : null,
      wavePaths,
      scrollHeight: document.documentElement.scrollHeight,
      clientHeight: document.documentElement.clientHeight,
    };
  });

  console.log("HOME_LAYOUT_METRICS", JSON.stringify(metrics, null, 2));

  await page.screenshot({
    path: path.join(OUT_DIR, "v1-home-layout-bug-diag-1440-dark.png"),
    fullPage: false,
  });

  await page.evaluate(() => {
    document.documentElement.setAttribute("data-theme", "light");
  });
  await page.screenshot({
    path: path.join(OUT_DIR, "v1-home-layout-bug-diag-1440-light.png"),
    fullPage: false,
  });
});

/**
 * Diagnostic: find the element creating blank space above Personal shell.
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

async function collectMetrics(page: Page) {
  return page.evaluate(() => {
    const pick = (sel: string) => {
      const el = document.querySelector(sel) as HTMLElement | null;
      if (!el) return null;
      const r = el.getBoundingClientRect();
      const cs = getComputedStyle(el);
      return {
        sel,
        className: (el.getAttribute("class") || "").slice(0, 140),
        rect: { x: +r.x.toFixed(1), y: +r.y.toFixed(1), w: +r.width.toFixed(1), h: +r.height.toFixed(1) },
        offsetTop: (el as HTMLElement).offsetTop,
        position: cs.position,
        display: cs.display,
        height: cs.height,
        marginTop: cs.marginTop,
        paddingTop: cs.paddingTop,
        transform: cs.transform,
        flex: cs.flex,
        overflow: cs.overflow,
        bg: cs.backgroundColor,
        borderRadius: cs.borderRadius,
      };
    };

    const crystal = document.querySelector('[data-testid="personal-crystal-screen"]');
    const crystalChildren = crystal
      ? Array.from(crystal.children).map((c, i) => {
          const r = c.getBoundingClientRect();
          const cs = getComputedStyle(c);
          return {
            i,
            testId: c.getAttribute("data-testid"),
            className: (c.getAttribute("class") || "").slice(0, 100),
            y: +r.y.toFixed(1),
            h: +r.height.toFixed(1),
            position: cs.position,
            marginTop: cs.marginTop,
            paddingTop: cs.paddingTop,
            height: cs.height,
            flex: cs.flex,
          };
        })
      : [];

    const probe = document.createElement("div");
    probe.style.paddingTop = "env(safe-area-inset-top, 0px)";
    document.body.appendChild(probe);
    const safeAreaProbe = getComputedStyle(probe).paddingTop;
    probe.remove();

    const menu = document.querySelector('[data-testid="personal-mobile-nav-toggle"]') as HTMLElement | null;
    const homeBtn = document.querySelector('[data-testid="personal-shell-home"]') as HTMLElement | null;

    return {
      viewport: { w: window.innerWidth, h: window.innerHeight },
      safeAreaProbe,
      html: pick("html"),
      body: pick("body"),
      root: pick("#root"),
      crystal: pick('[data-testid="personal-crystal-screen"]'),
      atmosphere: pick('[data-testid="personal-shell-atmosphere"]'),
      waves: pick(".personal-shell-atmosphere__waves"),
      shell: pick('[data-testid="personal-shell"]'),
      topbar: pick('[data-testid="personal-top-bar"]'),
      topbarRow: pick(".app-top-bar__row--personal"),
      sidebarRail: pick('[data-testid="personal-desktop-sidebar"]'),
      sidebar: pick('[data-testid="personal-sidebar"]'),
      menu: menu
        ? {
            y: +menu.getBoundingClientRect().y.toFixed(1),
            h: +menu.getBoundingClientRect().height.toFixed(1),
          }
        : null,
      homeBtn: homeBtn
        ? {
            y: +homeBtn.getBoundingClientRect().y.toFixed(1),
            h: +homeBtn.getBoundingClientRect().height.toFixed(1),
          }
        : null,
      crystalChildren,
      rootChildYs: Array.from(document.getElementById("root")?.children ?? []).map((c, i) => ({
        i,
        testId: c.getAttribute("data-testid"),
        y: +c.getBoundingClientRect().y.toFixed(1),
        h: +c.getBoundingClientRect().height.toFixed(1),
      })),
    };
  });
}

test("diagnose blank space above Personal application (desktop + mobile)", async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });

  await page.evaluate(() => {
    document.documentElement.setAttribute("data-theme", "light");
    document.documentElement.setAttribute("data-primary", "green");
  });

  for (const [w, h, tag] of [
    [1440, 900, "1440"],
    [390, 844, "390"],
  ] as const) {
    await page.setViewportSize({ width: w, height: h });
    await expect(page.getByTestId("personal-home-page")).toBeVisible();
    const metrics = await collectMetrics(page);
    console.log(`TOP_GAP_${tag}`, JSON.stringify(metrics, null, 2));
    await page.screenshot({
      path: path.join(OUT_DIR, `v1-top-gap-diag-${tag}-light.png`),
      fullPage: false,
    });
    expect(metrics.shell?.rect.y ?? 999).toBeLessThan(3);
    expect(metrics.topbar?.rect.y ?? 999).toBeLessThan(3);
  }
});

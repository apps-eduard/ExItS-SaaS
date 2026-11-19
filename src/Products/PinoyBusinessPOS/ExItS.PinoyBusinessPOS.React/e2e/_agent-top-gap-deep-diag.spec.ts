/**
 * Deep diagnostic: ancestor chain + elementFromPoint + pixel proof.
 * One-off agent capture — not permanent suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { PNG } from "pngjs";
import fs from "node:fs";
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

test("deep top-gap ancestor + pixel dump", async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.evaluate(() => {
    document.documentElement.setAttribute("data-theme", "light");
    document.documentElement.setAttribute("data-primary", "green");
  });

  const dump = await page.evaluate(() => {
    const topbar = document.querySelector('[data-testid="personal-top-bar"]') as HTMLElement | null;
    const chain: Array<Record<string, unknown>> = [];
    let el: HTMLElement | null = topbar;
    while (el) {
      const r = el.getBoundingClientRect();
      const cs = getComputedStyle(el);
      chain.push({
        tag: el.tagName.toLowerCase(),
        id: el.id || null,
        testId: el.getAttribute("data-testid"),
        className: (el.getAttribute("class") || "").slice(0, 160),
        rect: {
          top: +r.top.toFixed(2),
          left: +r.left.toFixed(2),
          width: +r.width.toFixed(2),
          height: +r.height.toFixed(2),
        },
        margin: `${cs.marginTop} ${cs.marginRight} ${cs.marginBottom} ${cs.marginLeft}`,
        padding: `${cs.paddingTop} ${cs.paddingRight} ${cs.paddingBottom} ${cs.paddingLeft}`,
        border: `${cs.borderTopWidth} ${cs.borderRadius}`,
        position: cs.position,
        transform: cs.transform,
        overflow: cs.overflow,
        bg: cs.backgroundColor,
        crystalInset: cs.getPropertyValue("--ui-standards-crystal-inset").trim(),
        crystalRadius: cs.getPropertyValue("--ui-standards-crystal-radius").trim(),
        softGlassRadius: cs.getPropertyValue("--soft-glass-radius").trim(),
      });
      el = el.parentElement;
    }

    const points = [
      [1, 0],
      [8, 0],
      [24, 0],
      [72, 0],
      [120, 0],
      [300, 0],
      [720, 0],
      [1, 4],
      [72, 4],
      [72, 12],
      [8, 12],
    ] as const;
    const fromPoint = points.map(([x, y]) => {
      const hit = document.elementFromPoint(x, y) as HTMLElement | null;
      const r = hit?.getBoundingClientRect();
      return {
        x,
        y,
        tag: hit?.tagName.toLowerCase() ?? null,
        testId: hit?.getAttribute("data-testid"),
        className: (hit?.getAttribute("class") || "").slice(0, 120),
        top: r ? +r.top.toFixed(2) : null,
      };
    });

    const sidebar = document.querySelector('[data-testid="personal-sidebar"]') as HTMLElement | null;
    const sidebarCs = sidebar ? getComputedStyle(sidebar) : null;
    const before = sidebar ? getComputedStyle(sidebar, "::before") : null;
    const after = sidebar ? getComputedStyle(sidebar, "::after") : null;

    return {
      dpr: window.devicePixelRatio,
      viewport: { w: window.innerWidth, h: window.innerHeight },
      chain,
      fromPoint,
      sidebar: sidebar
        ? {
            rect: (() => {
              const r = sidebar.getBoundingClientRect();
              return { top: +r.top.toFixed(2), left: +r.left.toFixed(2), w: +r.width.toFixed(2), h: +r.height.toFixed(2) };
            })(),
            borderRadius: sidebarCs?.borderRadius,
            bg: sidebarCs?.backgroundColor,
            overflow: sidebarCs?.overflow,
            before: before
              ? {
                  content: before.content,
                  display: before.display,
                  width: before.width,
                  height: before.height,
                  borderRadius: before.borderRadius,
                  bg: before.backgroundImage || before.backgroundColor,
                  top: before.top,
                  insetBlockStart: before.getPropertyValue("inset-block-start"),
                }
              : null,
            after: after
              ? {
                  content: after.content,
                  display: after.display,
                  width: after.width,
                  borderRadius: after.borderRadius,
                }
              : null,
          }
        : null,
    };
  });

  console.log("DEEP_TOP_GAP", JSON.stringify(dump, null, 2));

  const shotPath = path.join(OUT_DIR, "v1-top-gap-deep-1440-light.png");
  await page.screenshot({ path: shotPath, fullPage: false });

  const buf = fs.readFileSync(shotPath);
  const png = PNG.sync.read(buf);
  const rows: Array<Record<string, unknown>> = [];
  for (let y = 0; y < 20; y++) {
    const sample = (x: number) => {
      const i = (png.width * y + x) * 4;
      return [png.data[i], png.data[i + 1], png.data[i + 2]].join(",");
    };
    rows.push({ y, x0: sample(0), x1: sample(1), x8: sample(8), x72: sample(72), x300: sample(300), x720: sample(720) });
  }
  console.log("PNG_TOP_ROWS", JSON.stringify(rows, null, 2));
  console.log("PNG_SIZE", png.width, png.height);

  expect(dump.chain[0]?.rect).toBeTruthy();
});

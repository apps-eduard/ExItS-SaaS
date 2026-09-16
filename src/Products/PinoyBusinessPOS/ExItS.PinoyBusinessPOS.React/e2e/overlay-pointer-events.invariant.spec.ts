import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

/**
 * VISUALLY HIDDEN OR LOGICALLY CLOSED OVERLAYS MUST NEVER HIT-TEST.
 * Parent `pointer-events:none` alone is insufficient — descendants default to `auto`
 * and still intercept clicks (Case B / CSS UI / MDN).
 */
const cssPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../src/styles/globals.css",
);

test.describe("Overlay pointer-events invariant", () => {
  test("inactive workspace transition must defeat child pointer-events:auto", async ({
    page,
  }) => {
    const css = fs.readFileSync(cssPath, "utf8");
    await page.setContent(`<!DOCTYPE html>
<html><head><style>${css}</style></head>
<body>
  <button id="under" style="position:fixed;inset:0;z-index:1">under</button>
  <div class="exits-workspace-transition" data-active="false" data-testid="ws">
    <!-- Explicit auto simulates UA/button inheritance leaks under a PE:none parent. -->
    <div class="exits-workspace-transition__backdrop" data-testid="ws-backdrop" style="pointer-events:auto"></div>
    <div class="exits-workspace-transition__panel" data-testid="ws-panel" style="pointer-events:auto">panel</div>
  </div>
</body></html>`);

    const backdropPe = await page
      .getByTestId("ws-backdrop")
      .evaluate((el) => getComputedStyle(el).pointerEvents);
    expect(backdropPe, "ACTUAL_CLICK_BLOCKER=workspace-transition__backdrop").toBe("none");

    const hit = await page.evaluate(() => {
      const el = document.elementFromPoint(window.innerWidth / 2, window.innerHeight / 2);
      return el instanceof Element ? el.id || el.getAttribute("data-testid") : null;
    });
    expect(hit).toBe("under");
  });

  test("closed SideDrawer must defeat child pointer-events:auto", async ({ page }) => {
    const css = fs.readFileSync(cssPath, "utf8");
    await page.setContent(`<!DOCTYPE html>
<html><head><style>${css}</style></head>
<body>
  <button id="under" style="position:fixed;inset:0;z-index:1">under</button>
  <div class="exits-side-drawer" data-interactive="false" data-open="false" data-testid="drawer">
    <div class="exits-side-drawer__backdrop" data-interactive="false" data-open="false" data-testid="drawer-backdrop" style="pointer-events:auto;opacity:0"></div>
    <div class="exits-side-drawer__panel" data-interactive="false" data-open="false" data-testid="drawer-panel" style="pointer-events:auto">panel</div>
  </div>
</body></html>`);

    const backdropPe = await page
      .getByTestId("drawer-backdrop")
      .evaluate((el) => getComputedStyle(el).pointerEvents);
    expect(backdropPe, "ACTUAL_CLICK_BLOCKER=side-drawer__backdrop").toBe("none");

    const hit = await page.evaluate(() => {
      const el = document.elementFromPoint(window.innerWidth / 2, window.innerHeight / 2);
      return el instanceof Element ? el.id || el.getAttribute("data-testid") : null;
    });
    expect(hit).toBe("under");
  });
});

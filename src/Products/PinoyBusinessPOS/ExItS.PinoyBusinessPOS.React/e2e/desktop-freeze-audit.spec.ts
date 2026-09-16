import { expect, test, type Page } from "@playwright/test";
import {
  assertProbeClickable,
  captureClickDeadnessSnapshot,
  readRuntimeBuildSha,
} from "./click-deadness-diagnostics";
import { mockBoundManagerSession, signInAndBindManager } from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";

/**
 * POS-CLICK-DEADNESS-ROOT-CAUSE-PROOF
 * Real Playwright clicks only (no pushState, no force:true).
 * On failure, dumps elementsFromPoint + overlay inventory.
 */

const PO_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

async function mockPurchasingMinimal(page: Page) {
  await page.route("**/pos-api/api/v1/pos/purchase-orders**", async (route) => {
    const url = new URL(route.request().url());
    const method = route.request().method();
    const pathname = url.pathname;

    if (pathname.match(/\/purchase-orders\/?$/) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              purchaseOrderId: PO_ID,
              organizationId: "11111111-1111-1111-1111-111111111111",
              supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
              supplierName: "Acme Supply",
              poNumber: "PO-1001",
              status: "Ordered",
              displayStatus: "Ordered",
              orderDate: "2026-08-27",
              orderedAtUtc: "2026-08-27T09:00:00Z",
              orderedBy: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
              createdAtUtc: "2026-08-27T08:00:00Z",
              updatedAtUtc: "2026-08-27T09:00:00Z",
              lines: [],
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
        }),
      });
    }

    if (pathname.endsWith(`/purchase-orders/${PO_ID}`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          purchaseOrderId: PO_ID,
          organizationId: "11111111-1111-1111-1111-111111111111",
          supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          supplierName: "Acme Supply",
          poNumber: "PO-1001",
          status: "Ordered",
          displayStatus: "Ordered",
          paymentTerm: "Cash",
          paymentTermLabel: "Cash",
          orderDate: "2026-08-27",
          orderedAtUtc: "2026-08-27T09:00:00Z",
          orderedBy: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
          createdAtUtc: "2026-08-27T08:00:00Z",
          updatedAtUtc: "2026-08-27T09:00:00Z",
          lines: [
            {
              lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
              productId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
              lineNumber: 1,
              nameSnapshot: "Bath Soap",
              uomSnapshot: "Case",
              orderedQty: 2,
              unitPurchaseCost: 240,
              lineTotal: 480,
              receivedQty: 0,
              outstandingQty: 2,
              needsProductSetup: false,
            },
          ],
        }),
      });
    }

    if (pathname.includes(`/purchase-orders/${PO_ID}/goods-receipts`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([]),
      });
    }

    return route.fallback();
  });
}

async function realClick(page: Page, testId: string) {
  await page.getByTestId(testId).click({ force: false, timeout: 8_000 });
}

async function closeDropdownIfOpen(page: Page) {
  const menu = page.locator('[data-exits-dropdown-portal="true"]');
  if ((await menu.count()) > 0 && (await menu.first().isVisible().catch(() => false))) {
    await page.keyboard.press("Escape");
    await expect(menu).toHaveCount(0, { timeout: 3_000 });
  }
}

test.describe("Click deadness real-interaction stress", () => {
  test.use({ serviceWorkers: "block" });

  test.beforeEach(async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogApi(page);
    await mockPurchasingMinimal(page);
    await signInAndBindManager(page);
  });

  test("50+ real clicks across drawers/menus stay clickable", async ({ page }) => {
    test.setTimeout(180_000);
    await page.setViewportSize({ width: 1440, height: 900 });

    const buildSha = await readRuntimeBuildSha(page);
    expect(buildSha, "runtime must expose build SHA from CURRENT HEAD bundle").toBeTruthy();
    expect(buildSha).toMatch(/^[0-9a-f]{7,40}$|development|unknown/i);

    // SW blocked by test.use; still assert controller absence.
    const swCount = await page.evaluate(() => (navigator.serviceWorker?.controller ? 1 : 0));
    expect(swCount).toBe(0);

    await expect(page.getByTestId("operations-shell")).toBeVisible();
    await expect(page.getByTestId("ops-sidebar-home")).toBeVisible();

    let clicks = 0;
    const bump = async (label: string, fn: () => Promise<void>) => {
      await fn();
      clicks += 1;
      // Probe after every interaction — must be a normal click, never force:true.
      await assertProbeClickable(page, "ops-sidebar-home", clicks);
      void label;
    };

    // Sidebar / nav (desktop)
    for (const id of [
      "ops-sidebar-sell",
      "ops-sidebar-inventory",
      "ops-sidebar-purchasing",
      "ops-sidebar-customers",
      "ops-sidebar-home",
      "ops-sidebar-catalog",
      "ops-sidebar-orders",
      "ops-sidebar-home",
    ]) {
      await bump(`nav:${id}`, async () => {
        await realClick(page, id);
      });
    }

    // Account dropdown open/close cycles
    for (let i = 0; i < 6; i++) {
      await bump(`account-menu-${i}`, async () => {
        await realClick(page, "account-menu-trigger");
        await expect(page.locator('[data-exits-dropdown-portal="true"]')).toBeVisible();
        await page.keyboard.press("Escape");
        await closeDropdownIfOpen(page);
      });
    }

    // Preferences SideDrawer open/close
    for (let i = 0; i < 5; i++) {
      await bump(`preferences-${i}`, async () => {
        await realClick(page, "shell-preferences-button");
        await expect(page.getByTestId("preferences-drawer")).toBeVisible();
        await expect(page.getByTestId("preferences-drawer")).toHaveAttribute(
          "data-interactive",
          "true",
        );
        await realClick(page, "preferences-close");
        await expect(page.getByTestId("preferences-drawer")).toHaveCount(0, { timeout: 5_000 });
      });
    }

    // Purchasing list → PO detail → timeline drawer
    await bump("purchasing-nav", async () => {
      await realClick(page, "ops-sidebar-purchasing");
    });

    // Prefer a row link if present; otherwise go via sidebar home then purchasing again.
    const poLink = page.getByRole("link", { name: /PO-1001/i }).first();
    if (await poLink.isVisible().catch(() => false)) {
      await bump("po-open", async () => {
        await poLink.click({ force: false });
      });
    } else {
      // Fallback: click open-purchasing from home if list UI differs.
      await bump("purchasing-home", async () => {
        await realClick(page, "ops-sidebar-home");
      });
      const openPurchasing = page.getByTestId("open-purchasing");
      if (await openPurchasing.isVisible().catch(() => false)) {
        await bump("open-purchasing", async () => {
          await openPurchasing.click({ force: false });
        });
      }
      if (await poLink.isVisible().catch(() => false)) {
        await bump("po-open-fallback", async () => {
          await poLink.click({ force: false });
        });
      }
    }

    if (await page.getByTestId("po-timeline-open").isVisible().catch(() => false)) {
      for (let i = 0; i < 4; i++) {
        await bump(`timeline-${i}`, async () => {
          await realClick(page, "po-timeline-open");
          await expect(page.getByTestId("po-timeline-drawer")).toBeVisible();
          await realClick(page, "po-timeline-drawer-close");
          await expect(page.getByTestId("po-timeline-drawer")).toHaveCount(0, { timeout: 5_000 });
        });
      }
    }

    // Confirmation dialog via FormDrawer unsaved path is page-specific; use preferences
    // theme toggle + Escape as additional open/close churn, then confirm dialog if any
    // ConfirmationDialog appears from a destructive action on purchasing list filters.
    await bump("prefs-again", async () => {
      await realClick(page, "shell-preferences-button");
      await expect(page.getByTestId("preferences-drawer")).toBeVisible();
      await realClick(page, "preferences-close");
      await expect(page.getByTestId("preferences-drawer")).toHaveCount(0, { timeout: 5_000 });
    });

    // Extra nav churn to exceed 50 interactions
    while (clicks < 55) {
      await bump(`extra-nav-${clicks}`, async () => {
        await realClick(page, clicks % 2 === 0 ? "ops-sidebar-inventory" : "ops-sidebar-home");
      });
    }

    const finalSnap = await captureClickDeadnessSnapshot(page);
    expect(finalSnap.buildSha).toBe(buildSha);
    expect(finalSnap.body.inert).toBe(false);
    expect(finalSnap.html.inert).toBe(false);
    expect(await page.getByTestId("client-error-overlay").count()).toBe(0);
    expect(await page.getByTestId("workspace-transition-overlay").count()).toBe(0);
    expect(await page.locator('.exits-side-drawer[data-interactive="true"]').count()).toBe(0);
    expect(clicks).toBeGreaterThanOrEqual(50);
  });
});

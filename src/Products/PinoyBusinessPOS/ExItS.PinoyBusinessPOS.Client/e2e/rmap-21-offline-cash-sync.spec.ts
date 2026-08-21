import { expect, test, type Page } from "@playwright/test";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundCashierSession,
  signInAndBindCashier,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID } from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import {
  mockPosPriceAuthorityApi,
  type MockPriceAuthorityApi,
} from "./mock-pos-price-authority-route";
import { E2E_SHIFT_ID, mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";

/**
 * RMAP-21H end-to-end: the offline path a cashier actually walks.
 *
 * Covers what unit tests cannot: that dropping the network mid-checkout queues the Cash sale
 * instead of posting it, that Connection & Sync counts that queued sale from the real outbox, and
 * that reconnecting replays it exactly once with the sale idempotency headers.
 *
 * RMAP-21 Review Repair 01 adds the financial-finality half: the queued sale carries a
 * server-signed price lease, so a shelf price that changed while the device was offline cannot
 * move the recorded total in either direction.
 */

const CATALOG_PRICE = 25;

type RecordedPost = { body: Record<string, unknown>; headers: Record<string, string> };

type PostedLine = {
  lineTotal?: number;
  offlinePriceAuthority?: { unitPrice?: number; signature?: string };
};

/**
 * Prices the posted sale the way the server does: a line that carries a lease is billed from the
 * lease, and only a line without one falls back to the live catalog price. Anything else here
 * would let the test pass while the real server silently repriced the sale.
 */
function priceSale(
  body: Record<string, unknown>,
  authorities: MockPriceAuthorityApi,
): { total: number; lines: PostedLine[] } {
  const lines = (body.lines as PostedLine[] | undefined) ?? [];
  let total = 0;
  for (const line of lines) {
    if (line.offlinePriceAuthority && typeof line.lineTotal === "number") {
      total += line.lineTotal;
    } else {
      total += authorities.currentCatalogPrice(MOCK_COKE_PRODUCT_ID);
    }
  }
  return { total: Math.round(total * 100) / 100, lines };
}

async function mockPosSalesApi(page: Page, authorities: MockPriceAuthorityApi) {
  const posts: RecordedPost[] = [];

  await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
    const method = route.request().method();
    const pathname = new URL(route.request().url()).pathname.replace(/\/$/, "");

    if (method === "POST" && pathname.endsWith("/sales/quote")) {
      const live = authorities.currentCatalogPrice(MOCK_COKE_PRODUCT_ID);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          grossSubtotal: live,
          lineDiscountTotal: 0,
          saleDiscountTotal: 0,
          discountTotal: 0,
          subtotal: live,
          taxAmount: 0,
          total: live,
          lines: [
            {
              lineNumber: 1,
              productId: MOCK_COKE_PRODUCT_ID,
              name: "Coca-Cola 330ml",
              unitOfMeasure: "pc",
              sellingMode: "PerItem",
              unitPrice: live,
              quantity: 1,
              grossLineTotal: live,
              lineDiscountAmount: 0,
              saleDiscountAllocatedAmount: 0,
              lineTotal: live,
            },
          ],
          discounts: [],
        }),
      });
    }

    if (method === "POST" && pathname.endsWith("/sales")) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      posts.push({ body, headers: route.request().headers() });
      const tendered = Number(body.amountTendered ?? 0);
      const { total } = priceSale(body, authorities);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: String(body.saleId),
          organizationId: E2E_ORG_ID,
          branchId: E2E_BRANCH_ID,
          saleNumber: "S-9001",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: total,
          total,
          taxAmount: 0,
          amountTendered: tendered,
          changeAmount: Math.max(0, Math.round((tendered - total) * 100) / 100),
          recordedAtUtc: "2026-08-21T02:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T02:00:00Z",
          lines: [],
          shiftId: E2E_SHIFT_ID,
          shiftNumber: "S-1001",
          documentKind: "TransactionSummary",
        }),
      });
    }

    return route.fallback();
  });

  return { posts };
}

async function signInSellReady(page: Page) {
  await seedInstallationId(page);
  await mockBoundCashierSession(page);
  await mockAuthorizedPosDevice(page);
  await mockPosCatalogApi(page);
  await mockPosRegisterShiftApi(page, { openShift: true });
  const authorities = await mockPosPriceAuthorityApi(page);
  const sales = await mockPosSalesApi(page, authorities);
  await signInAndBindCashier(page);
  return { sales, authorities };
}

/** Cart one product and open Cash checkout, from the layout the current viewport gives us. */
async function openCashCheckout(page: Page) {
  await expect(page.getByTestId("sell-floor")).toBeVisible({ timeout: 15000 });
  await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
  await expect(page.getByTestId("sell-pay").first()).toBeEnabled({ timeout: 10000 });

  const width = page.viewportSize()?.width ?? 1280;
  if (width < 1024) {
    await page.getByTestId("sell-cart-bar").click();
    await expect(page.getByTestId("sell-cart-sheet")).toBeVisible();
    await page.getByTestId("sell-cart-sheet").getByTestId("sell-pay").click();
  } else {
    await page.locator('[data-testid="sell-pay"]:visible').first().click();
  }
  await expect(page.getByTestId("checkout-cash-page")).toBeVisible({ timeout: 15000 });
}

const CONNECTION_BUTTON = "org-shell-connection-button";

async function openConnectionAndSync(page: Page) {
  await page.getByTestId(CONNECTION_BUTTON).click();
  await expect(page.getByTestId(`${CONNECTION_BUTTON}-panel`)).toBeVisible();
  return page.getByTestId(`${CONNECTION_BUTTON}-sync-status`);
}

/** Queue one offline Cash sale for the price the sell floor leased, tendering exactly. */
async function queueOfflineCashSale(page: Page, tender: string) {
  await page.context().setOffline(true);
  await expect(page.getByTestId("checkout-offline-cash-notice")).toBeVisible();
  await expect(page.getByTestId("checkout-offline-price-authority-required")).toHaveCount(0);

  await page.getByTestId("checkout-cash-received").fill(tender);
  await page.getByTestId("checkout-confirm").click();
  await expect(page.getByTestId("offline-sale-queued")).toBeVisible({ timeout: 15000 });
}

test.describe("RMAP-21 offline cash sale and reconnect sync", () => {
  test.use({ serviceWorkers: "block" });

  test("queues a Cash sale offline, counts it in Connection & Sync, and replays it once on reconnect", async ({
    page,
  }) => {
    const { sales } = await signInSellReady(page);
    await openCashCheckout(page);

    await queueOfflineCashSale(page, "50");

    // The sale is on the device, not on the server.
    expect(sales.posts).toHaveLength(0);

    const syncStatus = await openConnectionAndSync(page);
    await expect(syncStatus).toHaveText(/Offline · 1 waiting/);
    await expect(page.getByTestId(`${CONNECTION_BUTTON}-offline-icon`)).toBeVisible();
    await page.keyboard.press("Escape");

    // Reconnect: OutboxSyncHost drains after its debounce, with no further cashier action.
    await page.context().setOffline(false);
    await expect.poll(() => sales.posts.length, { timeout: 20000 }).toBe(1);

    const replayed = sales.posts[0];
    expect(replayed.body.paymentMethod).toBe("Cash");
    expect(replayed.body.amountTendered).toBe(50);
    expect(replayed.body.shiftId).toBe(E2E_SHIFT_ID);
    expect(replayed.headers["idempotency-key"]).toMatch(/^[0-9a-f]{32}$/);
    expect(replayed.headers["x-pos-payload-hash"]).toMatch(/^[0-9a-f]{64}$/);
    expect(replayed.headers["x-pos-operation-type"]).toBe("sale.checkout");

    // Financial finality: the queued sale priced itself from the lease, not from ProductId alone.
    const [line] = (replayed.body.lines as PostedLine[]) ?? [];
    expect(line?.offlinePriceAuthority?.signature).toMatch(/^[0-9a-f]{64}$/);
    expect(line?.offlinePriceAuthority?.unitPrice).toBe(CATALOG_PRICE);
    expect(line?.lineTotal).toBe(CATALOG_PRICE);

    const afterSync = await openConnectionAndSync(page);
    await expect(afterSync).toHaveText(/All changes synced/, { timeout: 15000 });
  });

  test("a price rise while offline does not raise the total the customer already paid", async ({
    page,
  }) => {
    const { sales, authorities } = await signInSellReady(page);
    await openCashCheckout(page);

    await queueOfflineCashSale(page, String(CATALOG_PRICE));

    // The owner raises the shelf price while the device is still offline.
    authorities.setCatalogPrice(MOCK_COKE_PRODUCT_ID, 120);

    await page.context().setOffline(false);
    await expect.poll(() => sales.posts.length, { timeout: 20000 }).toBe(1);

    const [line] = (sales.posts[0].body.lines as PostedLine[]) ?? [];
    expect(line?.offlinePriceAuthority?.unitPrice).toBe(CATALOG_PRICE);
    expect(line?.lineTotal).toBe(CATALOG_PRICE);
    expect(sales.posts[0].body.amountTendered).toBe(CATALOG_PRICE);

    // Live repricing would have recorded 120 against a 25-peso tender and left the shop short.
    const syncStatus = await openConnectionAndSync(page);
    await expect(syncStatus).toHaveText(/All changes synced/, { timeout: 15000 });
  });

  test("a price cut while offline does not invent change the cashier never handed back", async ({
    page,
  }) => {
    const { sales, authorities } = await signInSellReady(page);
    await openCashCheckout(page);

    await queueOfflineCashSale(page, String(CATALOG_PRICE));

    authorities.setCatalogPrice(MOCK_COKE_PRODUCT_ID, 10);

    await page.context().setOffline(false);
    await expect.poll(() => sales.posts.length, { timeout: 20000 }).toBe(1);

    const [line] = (sales.posts[0].body.lines as PostedLine[]) ?? [];
    expect(line?.lineTotal).toBe(CATALOG_PRICE);
    // 25 tendered against a 25 sale is zero change; repricing to 10 would have invented 15.
    expect(sales.posts[0].body.amountTendered).toBe(CATALOG_PRICE);

    const syncStatus = await openConnectionAndSync(page);
    await expect(syncStatus).toHaveText(/All changes synced/, { timeout: 15000 });
  });

  test("refuses an offline Cash sale when no price lease reached this device", async ({ page }) => {
    const { sales } = await signInSellReady(page);
    await openCashCheckout(page);

    // Simulate a device that cached the catalog before leases existed, or whose leases expired.
    await page.evaluate(async () => {
      const names = await indexedDB.databases();
      const offline = names.find((entry) => entry.name?.startsWith("exits-offline-Organization-"));
      if (!offline?.name) {
        return;
      }
      const db = await new Promise<IDBDatabase>((resolve, reject) => {
        const request = indexedDB.open(offline.name as string);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
      });
      await new Promise<void>((resolve, reject) => {
        const request = db
          .transaction("priceAuthorities", "readwrite")
          .objectStore("priceAuthorities")
          .clear();
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
      });
      db.close();
    });

    await page.context().setOffline(true);
    await expect(page.getByTestId("checkout-offline-cash-notice")).toBeVisible();
    await expect(page.getByTestId("checkout-offline-price-authority-required")).toBeVisible({
      timeout: 15000,
    });
    await expect(page.getByTestId("checkout-offline-price-authority-required")).toContainText(
      "Connect to refresh prices before selling.",
    );
    await expect(page.getByTestId("checkout-confirm")).toBeDisabled();

    expect(sales.posts).toHaveLength(0);
    await page.context().setOffline(false);
  });

  test("stores the queued sale as ciphertext with no cart or credential plaintext", async ({
    page,
  }) => {
    await signInSellReady(page);
    await openCashCheckout(page);

    await queueOfflineCashSale(page, "50");

    const outboxRows = await page.evaluate(async () => {
      const names = await indexedDB.databases();
      const offline = names.find((entry) => entry.name?.startsWith("exits-offline-Organization-"));
      if (!offline?.name) {
        return null;
      }
      const db = await new Promise<IDBDatabase>((resolve, reject) => {
        const request = indexedDB.open(offline.name as string);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
      });
      const rows = await new Promise<unknown[]>((resolve, reject) => {
        const request = db.transaction("outbox").objectStore("outbox").getAll();
        request.onsuccess = () => resolve(request.result as unknown[]);
        request.onerror = () => reject(request.error);
      });
      db.close();
      return JSON.stringify(rows);
    });

    expect(outboxRows).not.toBeNull();
    expect(outboxRows).toContain("sale.checkout");
    expect(outboxRows).not.toContain(MOCK_COKE_PRODUCT_ID);
    expect(outboxRows).not.toContain("amountTendered");
    expect(outboxRows).not.toContain("offlinePriceAuthority");
    expect(outboxRows).not.toMatch(/accessToken|sessionToken|password/i);

    await page.context().setOffline(false);
  });
});

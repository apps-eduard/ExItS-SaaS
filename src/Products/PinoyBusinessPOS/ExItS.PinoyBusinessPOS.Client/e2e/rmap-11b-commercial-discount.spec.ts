import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
  clientNavigate,
} from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosRegisterShiftApi, E2E_SHIFT_ID } from "./mock-pos-register-shift-route";
import { MOCK_COKE_PRODUCT_ID } from "./mock-pos-catalog";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const INSTALL_KEY = "exits.pos-client.installation-device-id.v1";
const FIXED_INSTALL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const DEVICE_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const SALE_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

async function seedInstallationId(page: import("@playwright/test").Page) {
  await page.addInitScript(
    ([key, id]) => {
      window.localStorage.setItem(key, id);
    },
    [INSTALL_KEY, FIXED_INSTALL_ID] as const,
  );
}

async function mockAuthorizedDevice(page: import("@playwright/test").Page) {
  await page.route("**/platform-api/**/pos-devices/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/pos-devices/authorize") && method === "POST") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          posDeviceId: DEVICE_ID,
          branchId: E2E_BRANCH_ID,
          installationDeviceId: FIXED_INSTALL_ID,
        }),
      });
    }
    return route.fallback();
  });
}

type DiscountSaleMock = {
  rejectDiscount?: boolean;
};

async function mockPosSalesWithQuote(
  page: import("@playwright/test").Page,
  opts: DiscountSaleMock = {},
) {
  const quotes: Array<Record<string, unknown>> = [];
  const posts: Array<Record<string, unknown>> = [];

  await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
    const method = route.request().method();
    const pathname = new URL(route.request().url()).pathname.replace(/\/$/, "");
    const body = (route.request().postDataJSON?.() ?? {}) as Record<string, unknown>;

    if (method === "POST" && pathname.endsWith("/sales/quote")) {
      quotes.push(body);
      const discounts = (body.discounts as Array<Record<string, unknown>> | undefined) ?? [];
      if (opts.rejectDiscount && discounts.length > 0) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "ApplyCommercialDiscount is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }

      const fullSaleDiscount = discounts.some(
        (d) => d.scope === "Sale" && d.method === "Percentage" && Number(d.value) >= 100,
      );
      const percent10 = discounts.some(
        (d) => d.scope === "Sale" && d.method === "Percentage" && Number(d.value) === 10,
      );
      const gross = 25;
      const discountTotal = fullSaleDiscount ? 25 : percent10 ? 2.5 : 0;
      const total = gross - discountTotal;

      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          grossSubtotal: gross,
          lineDiscountTotal: 0,
          saleDiscountTotal: discountTotal,
          discountTotal,
          subtotal: total,
          taxAmount: 0,
          total,
          lines: [
            {
              lineNumber: 1,
              productId: MOCK_COKE_PRODUCT_ID,
              name: "Coca-Cola 330ml",
              unitOfMeasure: "pc",
              sellingMode: "PerItem",
              unitPrice: 25,
              quantity: 1,
              grossLineTotal: 25,
              lineDiscountAmount: 0,
              saleDiscountAllocatedAmount: discountTotal,
              lineTotal: total,
            },
          ],
          discounts: discounts.map((d) => ({
            scope: d.scope,
            method: d.method,
            requestedValue: Number(d.value),
            calculatedAmount: discountTotal,
            reason: String(d.reason ?? ""),
            lineNumber: d.lineNumber ?? null,
          })),
        }),
      });
    }

    if (method === "POST" && pathname.endsWith("/sales")) {
      posts.push(body);
      const discounts = (body.discounts as Array<Record<string, unknown>> | undefined) ?? [];
      if (opts.rejectDiscount && discounts.length > 0) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "ApplyCommercialDiscount is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }

      const tendered = Number(body.amountTendered ?? 0);
      const fullSaleDiscount = discounts.some(
        (d) => d.scope === "Sale" && d.method === "Percentage" && Number(d.value) >= 100,
      );
      const percent10 = discounts.some(
        (d) => d.scope === "Sale" && d.method === "Percentage" && Number(d.value) === 10,
      );
      const gross = 25;
      const discountTotal = fullSaleDiscount ? 25 : percent10 ? 2.5 : 0;
      const total = gross - discountTotal;
      const recordedSaleId = String(body.saleId ?? SALE_ID);

      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: recordedSaleId,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-9100",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: total,
          total,
          taxAmount: 0,
          amountTendered: tendered,
          changeAmount: Math.max(0, tendered - total),
          grossSubtotal: gross,
          discountTotal,
          recordedAtUtc: "2026-08-21T03:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T03:00:00Z",
          lines: [
            {
              saleLineId: "99999999-9999-4999-8999-999999999999",
              productId: MOCK_COKE_PRODUCT_ID,
              lineNumber: 1,
              name: "Coca-Cola 330ml",
              sku: "COKE-330",
              unitOfMeasure: "pc",
              sellingMode: "PerItem",
              unitPrice: 25,
              quantity: 1,
              lineTotal: total,
              grossLineTotal: 25,
            },
          ],
          shiftId: E2E_SHIFT_ID,
          shiftNumber: "S-1001",
          documentKind: "TransactionSummary",
          branchId: E2E_BRANCH_ID,
        }),
      });
    }

    if (method === "GET" && /\/sales\/[0-9a-f-]+$/i.test(pathname)) {
      const recordedSaleId = pathname.split("/").pop() ?? SALE_ID;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: recordedSaleId,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-9100",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: 0,
          total: 0,
          taxAmount: 0,
          amountTendered: 0,
          changeAmount: 0,
          grossSubtotal: 25,
          discountTotal: 25,
          recordedAtUtc: "2026-08-21T03:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T03:00:00Z",
          lines: [
            {
              saleLineId: "99999999-9999-4999-8999-999999999999",
              productId: MOCK_COKE_PRODUCT_ID,
              lineNumber: 1,
              name: "Coca-Cola 330ml",
              sku: "COKE-330",
              unitOfMeasure: "pc",
              sellingMode: "PerItem",
              unitPrice: 25,
              quantity: 1,
              lineTotal: 0,
              grossLineTotal: 25,
            },
          ],
          shiftId: E2E_SHIFT_ID,
          documentKind: "TransactionSummary",
        }),
      });
    }

    return route.fallback();
  });

  return { quotes, posts };
}

async function addCokeAndOpenCheckout(page: import("@playwright/test").Page) {
  await expect(page.getByTestId("sell-floor")).toBeVisible();
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
  await expect(page.getByTestId("checkout-cash-page")).toBeVisible();
}

async function signInOwnerAndStartSelling(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  const startSelling = page.getByTestId("workspace-destination-start_selling");
  await startSelling.waitFor({ state: "visible", timeout: 15000 });
  await startSelling.click();
  await expect(page.getByTestId("sell-floor")).toBeVisible({ timeout: 15000 });
}

test.describe("RMAP-11b commercial discount UX", () => {
  test.use({ serviceWorkers: "block" });

  test("cashier has no discount controls", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithQuote(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await expect(page.getByTestId("checkout-discount-panel")).toHaveCount(0);
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("25");
  });

  test("cashier discount POST is rejected by server", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithQuote(page, { rejectDiscount: true });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);

    // Simulate a forbidden discount payload reaching the API (UI does not expose controls).
    await page.evaluate(async () => {
      const res = await fetch("/pos-api/api/v1/pos/sales", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          lines: [{ productId: "11111111-1111-4111-8111-111111111111", quantity: 1 }],
          paymentMethod: "Cash",
          amountTendered: 25,
          saleId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          shiftId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          discounts: [
            { scope: "Sale", method: "Percentage", value: 10, reason: "Should be denied" },
          ],
        }),
      });
      (window as unknown as { __discStatus?: number }).__discStatus = res.status;
    });

    const status = await page.evaluate(
      () => (window as unknown as { __discStatus?: number }).__discStatus,
    );
    expect(status).toBe(403);
    expect(sales.posts.some((p) => Array.isArray(p.discounts) && p.discounts.length > 0)).toBe(
      true,
    );
  });

  test("owner can apply sale percent discount via quote", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithQuote(page);
    await signInOwnerAndStartSelling(page);
    await addCokeAndOpenCheckout(page);

    await expect(page.getByTestId("checkout-discount-panel")).toBeVisible();
    await page.getByTestId("checkout-discount-value").fill("10");
    await page.getByTestId("checkout-discount-reason").fill("Bulk buyer courtesy");
    await page.getByTestId("checkout-discount-add").click();

    await expect(page.getByTestId("checkout-total-amount")).toContainText("25");
    await expect(page.getByTestId("checkout-discount-total")).toContainText("2.5");
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("22.5");
    await expect(page.getByTestId("checkout-cash-received")).toHaveValue("22.50");

    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(sales.posts).toHaveLength(1);
    expect(sales.posts[0].discounts).toEqual([
      {
        scope: "Sale",
        method: "Percentage",
        value: 10,
        reason: "Bulk buyer courtesy",
      },
    ]);
    expect(sales.posts[0].amountTendered).toBe(22.5);
    const line = (sales.posts[0].lines as Array<Record<string, unknown>>)[0];
    expect(line.unitPrice).toBeUndefined();
    expect(line.unitPriceSnapshot).toBeUndefined();
  });

  test("full discount shows no payment required and posts tendered 0", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithQuote(page);
    await signInOwnerAndStartSelling(page);
    await addCokeAndOpenCheckout(page);

    await page.getByTestId("checkout-discount-value").fill("100");
    await page.getByTestId("checkout-discount-reason").fill("Full courtesy");
    await page.getByTestId("checkout-discount-add").click();

    await expect(page.getByTestId("checkout-no-payment-required")).toBeVisible();
    await expect(page.getByTestId("checkout-cash-received")).toHaveCount(0);
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("0");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(sales.posts[0].amountTendered).toBe(0);
  });

  test("reason is required before adding a discount", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithQuote(page);
    await signInOwnerAndStartSelling(page);
    await addCokeAndOpenCheckout(page);

    await page.getByTestId("checkout-discount-value").fill("10");
    await page.getByTestId("checkout-discount-add").click();
    await expect(page.getByTestId("checkout-discount-form-error")).toContainText("reason");
    await expect(page.getByTestId("checkout-discount-list")).toHaveCount(0);
  });

  for (const viewport of VIEWPORTS) {
    test(`discount checkout responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await seedInstallationId(page);
      await mockBoundOwnerSession(page);
      await mockAuthorizedDevice(page);
      await mockPosCatalogApi(page);
      await mockPosRegisterShiftApi(page, { openShift: true });
      await mockPosSalesWithQuote(page);
      await signInOwnerAndStartSelling(page);
      await addCokeAndOpenCheckout(page);
      await page.getByTestId("checkout-discount-value").fill("10");
      await page.getByTestId("checkout-discount-reason").fill("Viewport courtesy");
      await page.getByTestId("checkout-discount-add").click();
      await assertNoHorizontalOverflow(page);
      await page.getByTestId("checkout-confirm").click();
      await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

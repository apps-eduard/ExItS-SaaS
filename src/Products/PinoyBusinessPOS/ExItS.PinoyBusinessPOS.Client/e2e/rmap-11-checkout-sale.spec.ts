import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundCashierSession,
  signInAndBindCashier,
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

type SaleMockOptions = {
  failCode?: string;
  failStatus?: number;
  failDetail?: string;
};

async function mockPosSalesApi(page: import("@playwright/test").Page, opts: SaleMockOptions = {}) {
  const posts: Array<{ body: Record<string, unknown>; headers: Record<string, string> }> = [];

  await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const headers = route.request().headers();
    const pathname = new URL(url).pathname.replace(/\/$/, "");

    if (method === "POST" && pathname.endsWith("/sales/quote")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          grossSubtotal: 25,
          lineDiscountTotal: 0,
          saleDiscountTotal: 0,
          discountTotal: 0,
          subtotal: 25,
          taxAmount: 0,
          total: 25,
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
              saleDiscountAllocatedAmount: 0,
              lineTotal: 25,
            },
          ],
          discounts: [],
        }),
      });
    }

    if (method === "POST" && pathname.endsWith("/sales")) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      posts.push({ body, headers });

      if (opts.failCode) {
        return route.fulfill({
          status: opts.failStatus ?? 400,
          contentType: "application/json",
          body: JSON.stringify({
            detail: opts.failDetail ?? "Checkout failed",
            errorCode: opts.failCode,
          }),
        });
      }

      const tendered = Number(body.amountTendered ?? 0);
      const total = 25;
      const recordedSaleId = String(body.saleId ?? SALE_ID);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: recordedSaleId,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-9001",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: total,
          total,
          taxAmount: 0,
          amountTendered: tendered,
          changeAmount: Math.max(0, tendered - total),
          recordedAtUtc: "2026-08-21T02:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T02:00:00Z",
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
              lineTotal: 25,
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
          saleNumber: "S-9001",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: 25,
          total: 25,
          taxAmount: 0,
          amountTendered: 50,
          changeAmount: 25,
          recordedAtUtc: "2026-08-21T02:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T02:00:00Z",
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
              lineTotal: 25,
            },
          ],
          shiftId: E2E_SHIFT_ID,
          shiftNumber: "S-1001",
          documentKind: "TransactionSummary",
        }),
      });
    }

    return route.fallback();
  });

  return {
    posts,
  };
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

test.describe("RMAP-11 checkout cash sale", () => {
  test.use({ serviceWorkers: "block" });

  test("unauthorized device blocks Sell before Pay", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await page.route("**/platform-api/**/pos-devices/authorize", async (route) => {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "This POS installation is not registered.",
          errorCode: "application.pos_device.not_authorized",
        }),
      });
    });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("sell-readiness-device")).toBeVisible();
    await expect(page.getByTestId("sell-floor")).toHaveCount(0);
  });

  test("cash sale success clears cart and shows Transaction Summary disclaimer", async ({
    page,
  }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);

    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("25");
    await expect(page.getByTestId("checkout-cash-received")).toHaveValue("25.00");
    await page.getByTestId("checkout-cash-received").fill("50");
    await expect(page.getByTestId("checkout-cash-received")).toHaveValue("50");
    await expect(page.getByTestId("checkout-change")).toContainText("25");
    await page.getByTestId("checkout-confirm").click();

    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    await expect(page.getByTestId("transaction-summary-disclaimer")).toContainText(
      "not a BIR-registered invoice",
    );
    await expect(page.getByRole("heading", { name: "Transaction Summary" })).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Invoice$/i })).toHaveCount(0);
    await expect(page.getByTestId("summary-sale-number")).toHaveText("S-9001");
    expect(sales.posts).toHaveLength(1);
    expect(sales.posts[0].body.paymentMethod).toBe("Cash");
    expect(sales.posts[0].body.amountTendered).toBe(50);
    expect(sales.posts[0].body.shiftId).toBe(E2E_SHIFT_ID);
    expect(sales.posts[0].headers["x-pos-installation-device-id"]).toBe(FIXED_INSTALL_ID);
    expect(sales.posts[0].body.discounts).toBeUndefined();
    const line = (sales.posts[0].body.lines as Array<Record<string, unknown>>)[0];
    expect(line.unitPriceSnapshot).toBeUndefined();
    expect(line.nameSnapshot).toBeUndefined();

    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("sell-cart-subtotal")).toHaveCount(0);
  });

  test("checkout failure keeps cart and maps insufficient tender", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesApi(page, {
      failCode: "pos.sale.amount_tendered.below_total",
      failStatus: 400,
      failDetail: "Amount tendered must be at least the sale total.",
    });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await page.getByTestId("checkout-cash-received").fill("50");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("checkout-error")).toBeVisible();
    await expect(page.getByTestId("checkout-error")).toContainText("at least the sale total");
    await page.getByRole("link", { name: /back to cart/i }).click();
    await expect(page.getByTestId("sell-cart-subtotal").first()).toBeVisible();
  });

  test("idempotent retry reuses the same saleId", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });

    let failOnce = true;
    const saleIds: string[] = [];
    await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
      const pathname = new URL(route.request().url()).pathname.replace(/\/$/, "");
      if (route.request().method() === "GET" && /\/sales\/[0-9a-f-]+$/i.test(pathname)) {
        const id = pathname.split("/").pop();
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            saleId: id,
            organizationId: E2E_ORG_ID,
            saleNumber: "S-9002",
            status: "Completed",
            paymentMethod: "Cash",
            subtotal: 25,
            total: 25,
            taxAmount: 0,
            amountTendered: 25,
            changeAmount: 0,
            recordedAtUtc: "2026-08-21T02:00:00Z",
            recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            updatedAtUtc: "2026-08-21T02:00:00Z",
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
                lineTotal: 25,
              },
            ],
            shiftId: E2E_SHIFT_ID,
            documentKind: "TransactionSummary",
          }),
        });
      }
      if (route.request().method() === "POST" && pathname.endsWith("/sales/quote")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            grossSubtotal: 25,
            lineDiscountTotal: 0,
            saleDiscountTotal: 0,
            discountTotal: 0,
            subtotal: 25,
            taxAmount: 0,
            total: 25,
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
                saleDiscountAllocatedAmount: 0,
                lineTotal: 25,
              },
            ],
            discounts: [],
          }),
        });
      }
      if (route.request().method() !== "POST" || !pathname.endsWith("/sales")) {
        return route.fallback();
      }
      const body = route.request().postDataJSON() as { saleId?: string; amountTendered?: number };
      saleIds.push(String(body.saleId));
      if (failOnce) {
        failOnce = false;
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Not enough stock",
            errorCode: "pos.inventory.insufficient_stock",
          }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: body.saleId,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-9002",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: 25,
          total: 25,
          taxAmount: 0,
          amountTendered: body.amountTendered ?? 25,
          changeAmount: 0,
          recordedAtUtc: "2026-08-21T02:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T02:00:00Z",
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
              lineTotal: 25,
            },
          ],
          shiftId: E2E_SHIFT_ID,
          documentKind: "TransactionSummary",
        }),
      });
    });

    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("checkout-error")).toContainText("stock");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(saleIds).toHaveLength(2);
    expect(saleIds[0]).toBe(saleIds[1]);
  });

  test("blocked checkout gate when moneyPostReady is false", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await page.route("**/platform-api/**/pos-devices/authorize", async (route) => {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "not registered",
          errorCode: "application.pos_device.not_authorized",
        }),
      });
    });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell/checkout");
    await expect(page.getByTestId("checkout-blocked")).toBeVisible();
  });

  for (const viewport of VIEWPORTS) {
    test(`checkout responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await seedInstallationId(page);
      await mockBoundCashierSession(page);
      await mockAuthorizedDevice(page);
      await mockPosCatalogApi(page);
      await mockPosRegisterShiftApi(page, { openShift: true });
      await mockPosSalesApi(page);
      await signInAndBindCashier(page);
      await clientNavigate(page, "/sell");
      await addCokeAndOpenCheckout(page);
      await assertNoHorizontalOverflow(page);
      await page.getByTestId("checkout-confirm").click();
      await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

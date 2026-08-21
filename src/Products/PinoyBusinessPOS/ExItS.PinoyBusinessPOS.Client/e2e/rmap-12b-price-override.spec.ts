import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundCashierSession,
  mockBoundManagerSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindManager,
  signInAndBindOwner,
  clientNavigate,
} from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosRegisterShiftApi, E2E_SHIFT_ID } from "./mock-pos-register-shift-route";
import {
  MOCK_COKE_PRODUCT_ID,
  MOCK_MEAT_PRODUCT_ID,
  MOCK_RICE_PRODUCT_ID,
  MOCK_RICE_KG_UNIT_ID,
} from "./mock-pos-catalog";

const INSTALL_KEY = "exits.pos-client.installation-device-id.v1";
const FIXED_INSTALL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const DEVICE_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const SALE_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const COKE_LINE_KEY = `${MOCK_COKE_PRODUCT_ID}::base`;
const CUSTOMER_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

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

type OverrideSaleMock = {
  rejectOverride?: boolean;
  rejectAboveLimit?: boolean;
};

async function mockPosSalesWithOverride(
  page: import("@playwright/test").Page,
  opts: OverrideSaleMock = {},
) {
  const quotes: Array<Record<string, unknown>> = [];
  const posts: Array<Record<string, unknown>> = [];

  await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
    const method = route.request().method();
    const pathname = new URL(route.request().url()).pathname.replace(/\/$/, "");
    const body = (route.request().postDataJSON?.() ?? {}) as Record<string, unknown>;

    if (method === "POST" && pathname.endsWith("/sales/quote")) {
      quotes.push(body);
      const overrides = (body.priceOverrides as Array<Record<string, unknown>> | undefined) ?? [];
      const discounts = (body.discounts as Array<Record<string, unknown>> | undefined) ?? [];

      if (opts.rejectOverride && overrides.length > 0) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "OverrideSalePrice is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }

      if (opts.rejectAboveLimit && overrides.length > 0) {
        const requested = Number(overrides[0]?.requestedUnitPrice ?? 0);
        if (requested > 50) {
          return route.fulfill({
            status: 400,
            contentType: "application/json",
            body: JSON.stringify({
              detail: "That price is above your allowed limit.",
              errorCode: "pos.sale.price_override.exceeds_manager_limit",
            }),
          });
        }
      }

      const applied = overrides.length > 0 ? Number(overrides[0]!.requestedUnitPrice) : 25;
      const baseline = 25;
      const percent10 = discounts.some(
        (d) => d.scope === "Sale" && d.method === "Percentage" && Number(d.value) === 10,
      );
      const gross = applied;
      const discountTotal = percent10 ? Number((gross * 0.1).toFixed(2)) : 0;
      const total = Number((gross - discountTotal).toFixed(2));

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
              unitPrice: applied,
              quantity: 1,
              grossLineTotal: gross,
              lineDiscountAmount: 0,
              saleDiscountAllocatedAmount: discountTotal,
              lineTotal: total,
              baselineUnitPrice: overrides.length > 0 ? baseline : null,
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
          priceOverrides:
            overrides.length > 0
              ? [
                  {
                    lineNumber: 1,
                    baselineUnitPrice: baseline,
                    appliedUnitPrice: applied,
                    reason: String(overrides[0]!.reason ?? ""),
                  },
                ]
              : [],
        }),
      });
    }

    if (method === "POST" && pathname.endsWith("/sales")) {
      posts.push(body);
      const overrides = (body.priceOverrides as Array<Record<string, unknown>> | undefined) ?? [];
      if (opts.rejectOverride && overrides.length > 0) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "OverrideSalePrice is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }

      const applied = overrides.length > 0 ? Number(overrides[0]!.requestedUnitPrice) : 25;
      const discounts = (body.discounts as Array<Record<string, unknown>> | undefined) ?? [];
      const percent10 = discounts.some(
        (d) => d.scope === "Sale" && d.method === "Percentage" && Number(d.value) === 10,
      );
      const gross = applied;
      const discountTotal = percent10 ? Number((gross * 0.1).toFixed(2)) : 0;
      const total = Number((gross - discountTotal).toFixed(2));
      const tendered = Number(body.amountTendered ?? total);
      const recordedSaleId = String(body.saleId ?? SALE_ID);

      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: recordedSaleId,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-912b",
          status: "Completed",
          paymentMethod: body.paymentMethod ?? "Cash",
          subtotal: total,
          total,
          taxAmount: 0,
          amountTendered: body.paymentMethod === "Utang" ? null : tendered,
          changeAmount: body.paymentMethod === "Cash" ? Math.max(0, tendered - total) : null,
          grossSubtotal: gross,
          discountTotal,
          recordedAtUtc: "2026-08-21T03:00:00Z",
          recordedBy: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          updatedAtUtc: "2026-08-21T03:00:00Z",
          customerDisplayName: body.paymentMethod === "Utang" ? "Utang Customer" : null,
          lines: [
            {
              saleLineId: "99999999-9999-4999-8999-999999999999",
              productId: MOCK_COKE_PRODUCT_ID,
              lineNumber: 1,
              name: "Coca-Cola 330ml",
              sku: "COKE-330",
              unitOfMeasure: "pc",
              sellingMode: "PerItem",
              unitPrice: applied,
              quantity: 1,
              lineTotal: total,
              grossLineTotal: gross,
            },
          ],
          priceOverrides:
            overrides.length > 0
              ? [
                  {
                    lineNumber: 1,
                    baselineUnitPrice: 25,
                    appliedUnitPrice: applied,
                    reason: String(overrides[0]!.reason ?? ""),
                  },
                ]
              : undefined,
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
          saleNumber: "S-912b",
          status: "Completed",
          paymentMethod: "Cash",
          subtotal: 22.5,
          total: 22.5,
          taxAmount: 0,
          amountTendered: 22.5,
          changeAmount: 0,
          grossSubtotal: 22.5,
          discountTotal: 0,
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
              unitPrice: 22.5,
              quantity: 1,
              lineTotal: 22.5,
              grossLineTotal: 22.5,
            },
          ],
          priceOverrides: [
            {
              lineNumber: 1,
              baselineUnitPrice: 25,
              appliedUnitPrice: 22.5,
              reason: "Manager courtesy",
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

async function addCokeVisible(page: import("@playwright/test").Page) {
  await expect(page.getByTestId("sell-floor")).toBeVisible();
  await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
  await expect(page.getByTestId(`sell-cart-line-${COKE_LINE_KEY}`).first()).toBeVisible({
    timeout: 10000,
  });
}

async function openCartSheetIfNeeded(page: import("@playwright/test").Page) {
  const width = page.viewportSize()?.width ?? 1280;
  if (width < 1024) {
    await page.getByTestId("sell-cart-bar").click();
    await expect(page.getByTestId("sell-cart-sheet")).toBeVisible();
  }
}

async function changePriceOnCoke(
  page: import("@playwright/test").Page,
  price: string,
  reason: string,
) {
  await openCartSheetIfNeeded(page);
  await page
    .locator(`[data-testid="sell-cart-change-price-${COKE_LINE_KEY}"]:visible`)
    .first()
    .click();
  await expect(page.getByTestId("sell-price-override-dialog")).toBeVisible();
  await page.getByTestId("sell-price-override-new").fill(price);
  await page.getByTestId("sell-price-override-reason").fill(reason);
  await page.getByTestId("sell-price-override-apply").click();
}

async function goCheckout(page: import("@playwright/test").Page) {
  await openCartSheetIfNeeded(page);
  await page.locator('[data-testid="sell-pay"]:visible').first().click();
  await expect(page.getByTestId("checkout-cash-page")).toBeVisible();
}

async function signInManagerSelling(page: import("@playwright/test").Page) {
  await signInAndBindManager(page);
  await clientNavigate(page, "/sell");
  await expect(page.getByTestId("sell-floor")).toBeVisible({ timeout: 15000 });
}

async function signInOwnerSelling(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  const startSelling = page.getByTestId("workspace-destination-start_selling");
  await startSelling.waitFor({ state: "visible", timeout: 15000 });
  await startSelling.click();
  await expect(page.getByTestId("sell-floor")).toBeVisible({ timeout: 15000 });
}

test.describe("RMAP-12b price override UX", () => {
  test.use({ serviceWorkers: "block" });

  test("A manager 90% of baseline applies (22.50 on ₱25)", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "22.50", "Manager courtesy");
    await expect(
      page.getByTestId(`sell-cart-price-changed-${COKE_LINE_KEY}`).first(),
    ).toBeVisible();
    await expect(
      page.getByTestId(`sell-cart-regular-price-${COKE_LINE_KEY}`).first(),
    ).toContainText("25.00");
    await goCheckout(page);
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("22.5");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(sales.posts[0].priceOverrides).toEqual([
      {
        requestedUnitPrice: 22.5,
        reason: "Manager courtesy",
        lineNumber: 1,
        productId: MOCK_COKE_PRODUCT_ID,
        expectedBaselineUnitPrice: 25,
      },
    ]);
    await expect(page.getByTestId("summary-line-price-changed-1")).toContainText("Price changed");
  });

  test("B manager exact 100% up (50.00) applies", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "50.00", "Exact ceiling");
    await goCheckout(page);
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("50");
  });

  test("C manager above 100% (50.01) shows friendly denial — no silent clamp", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page, { rejectAboveLimit: true });
    await signInManagerSelling(page);
    await addCokeVisible(page);
    await openCartSheetIfNeeded(page);
    await page
      .locator(`[data-testid="sell-cart-change-price-${COKE_LINE_KEY}"]:visible`)
      .first()
      .click();
    await page.getByTestId("sell-price-override-new").fill("50.01");
    await page.getByTestId("sell-price-override-reason").fill("Too high");
    await page.getByTestId("sell-price-override-apply").click();
    await expect(page.getByTestId("sell-price-override-form-error")).toContainText(
      "above your allowed limit",
    );
    await expect(page.getByTestId("sell-price-override-new")).toHaveValue("50.01");
  });

  test("D owner 250 applies unlimited", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithOverride(page);
    await signInOwnerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "250.00", "Owner special");
    await goCheckout(page);
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("250");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(
      (sales.posts[0].priceOverrides as Array<Record<string, unknown>>)[0].requestedUnitPrice,
    ).toBe(250);
  });

  test("E cashier has no Change price action", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page, { rejectOverride: true });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeVisible(page);
    await openCartSheetIfNeeded(page);
    await expect(page.getByTestId(`sell-cart-change-price-${COKE_LINE_KEY}`)).toHaveCount(0);
  });

  test("F zero price directs to discount wording", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await addCokeVisible(page);
    await openCartSheetIfNeeded(page);
    await page
      .locator(`[data-testid="sell-cart-change-price-${COKE_LINE_KEY}"]:visible`)
      .first()
      .click();
    await page.getByTestId("sell-price-override-new").fill("0");
    await page.getByTestId("sell-price-override-reason").fill("Free?");
    await page.getByTestId("sell-price-override-apply").click();
    await expect(page.getByTestId("sell-price-override-form-error")).toContainText(
      "Use a discount if you want to make this item free",
    );
  });

  test("G override stacks with commercial discount", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithOverride(page);
    await signInOwnerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "20.00", "Stack base");
    await goCheckout(page);
    await expect(page.getByTestId("checkout-price-override-note")).toBeVisible();
    await page.getByTestId("checkout-discount-value").fill("10");
    await page.getByTestId("checkout-discount-reason").fill("Stack discount");
    await page.getByTestId("checkout-discount-add").click();
    await expect(page.getByTestId("checkout-amount-to-pay")).toContainText("18");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(sales.posts[0].priceOverrides).toBeTruthy();
    expect(sales.posts[0].discounts).toBeTruthy();
  });

  test("H by-weight line supports Change price", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await page.getByTestId(`sell-product-${MOCK_MEAT_PRODUCT_ID}`).click();
    await expect(page.getByTestId("sell-weight-entry")).toBeVisible();
    await page.getByTestId("sell-weight-input").fill("1");
    await page.getByTestId("sell-weight-confirm").click();
    await openCartSheetIfNeeded(page);
    const changeBtn = page.locator(`[data-testid^="sell-cart-change-price-"]:visible`).first();
    await expect(changeBtn).toBeVisible();
  });

  test("I multi-UOM rice line supports Change price", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await page.getByTestId(`sell-product-${MOCK_RICE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("sell-unit-entry")).toBeVisible();
    await page.getByTestId(`sell-unit-option-${MOCK_RICE_KG_UNIT_ID}`).click();
    await page.getByTestId("sell-unit-add").click();
    await openCartSheetIfNeeded(page);
    const lineKey = `${MOCK_RICE_PRODUCT_ID}::${MOCK_RICE_KG_UNIT_ID}`;
    await expect(page.getByTestId(`sell-cart-change-price-${lineKey}`).first()).toBeVisible();
  });

  test("J Today's Prices remains separate from cart override", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page);
    await signInOwnerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "30.00", "Override only");
    await expect(
      page.getByTestId(`sell-cart-regular-price-${COKE_LINE_KEY}`).first(),
    ).toContainText("25.00");
    // Catalog tile still reflects Today's Price / catalog — override never rewrites it.
    await expect(page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toContainText("25");
    await expect(page.getByText("ManualGCash")).toHaveCount(0);
    await expect(page.getByText("OverrideSalePrice")).toHaveCount(0);
  });

  test("K Utang checkout carries priceOverrides", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithOverride(page);
    await page.route("**/pos-api/api/v1/pos/customers**", async (route) => {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              customerId: CUSTOMER_ID,
              organizationId: E2E_ORG_ID,
              displayName: "Utang Customer",
              mobileNumber: "09171234567",
              status: "Active",
              createdAtUtc: "2026-08-01T00:00:00Z",
              updatedAtUtc: "2026-08-01T00:00:00Z",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    });
    await signInOwnerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "22.50", "Utang override");
    await goCheckout(page);
    await page.getByTestId("checkout-pay-utang").click();
    await page.getByTestId(`checkout-customer-${CUSTOMER_ID}`).click();
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(sales.posts[0].paymentMethod).toBe("Utang");
    expect(sales.posts[0].priceOverrides).toBeTruthy();
  });

  test("L Use regular price clears pending override before checkout", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const sales = await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await addCokeVisible(page);
    await changePriceOnCoke(page, "22.50", "Temporary");
    await openCartSheetIfNeeded(page);
    await page
      .locator(`[data-testid="sell-cart-change-price-${COKE_LINE_KEY}"]:visible`)
      .first()
      .click();
    await page.getByTestId("sell-price-override-use-regular").click();
    await expect(page.getByTestId(`sell-cart-price-changed-${COKE_LINE_KEY}`)).toHaveCount(0);
    await goCheckout(page);
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(sales.posts[0].priceOverrides).toBeUndefined();
  });

  test("never shows ManualGCash on override surfaces", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundManagerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesWithOverride(page);
    await signInManagerSelling(page);
    await addCokeVisible(page);
    await openCartSheetIfNeeded(page);
    await page
      .locator(`[data-testid="sell-cart-change-price-${COKE_LINE_KEY}"]:visible`)
      .first()
      .click();
    await expect(page.getByTestId("sell-price-override-dialog")).toBeVisible();
    await expect(page.getByText("ManualGCash")).toHaveCount(0);
    await assertNoHorizontalOverflow(page);
  });
});

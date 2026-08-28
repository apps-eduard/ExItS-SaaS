import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow, fillCheckoutCashExact, openCheckoutPaymentMethods } from "./helpers";
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

type SaleState = {
  posts: Array<Record<string, unknown>>;
  voids: Array<Record<string, unknown>>;
  lastSale: Record<string, unknown> | null;
};

function baseSale(paymentMethod: string, extra: Record<string, unknown> = {}) {
  return {
    saleId: SALE_ID,
    organizationId: E2E_ORG_ID,
    saleNumber: "S-9012",
    status: "Completed",
    paymentMethod,
    subtotal: 25,
    total: 25,
    taxAmount: 0,
    amountTendered: paymentMethod === "Cash" ? 25 : null,
    changeAmount: paymentMethod === "Cash" ? 0 : null,
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
    ...extra,
  };
}

async function mockCustomersApi(page: import("@playwright/test").Page, allowFullList: boolean) {
  await page.route("**/pos-api/api/v1/pos/customers**", async (route) => {
    const pathname = new URL(route.request().url()).pathname.replace(/\/$/, "");
    const method = route.request().method();

    if (method === "GET" && pathname.endsWith("/customers/checkout-search")) {
      const search = new URL(route.request().url()).searchParams.get("search") ?? "";
      if (!search.trim()) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Checkout customer search requires a non-blank search term.",
            errorCode: "pos.customer.checkout_search.required",
          }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              customerId: CUSTOMER_ID,
              displayName: "Juan Dela Cruz",
              mobileNumber: "09171234567",
              status: "Active",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    }

    if (!allowFullList) {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "ViewCustomersAndHistory is required.",
          errorCode: "application.auth.capability.denied",
        }),
      });
    }
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        items: [
          {
            customerId: CUSTOMER_ID,
            organizationId: E2E_ORG_ID,
            displayName: "Juan Dela Cruz",
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
}

async function mockPosSalesApi(
  page: import("@playwright/test").Page,
  opts: { rejectVoid?: boolean; quoteTotal?: number } = {},
): Promise<SaleState> {
  const state: SaleState = { posts: [], voids: [], lastSale: null };
  const quoteTotal = opts.quoteTotal ?? 25;

  await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname.replace(/\/$/, "");

    if (method === "POST" && pathname.endsWith("/sales/quote")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          grossSubtotal: quoteTotal === 0 ? 25 : quoteTotal,
          lineDiscountTotal: 0,
          saleDiscountTotal: quoteTotal === 0 ? 25 : 0,
          discountTotal: quoteTotal === 0 ? 25 : 0,
          subtotal: quoteTotal,
          taxAmount: 0,
          total: quoteTotal,
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
              saleDiscountAllocatedAmount: quoteTotal === 0 ? 25 : 0,
              lineTotal: quoteTotal,
            },
          ],
          discounts: [],
        }),
      });
    }

    if (method === "POST" && /\/sales\/[^/]+\/void$/i.test(pathname)) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.voids.push(body);
      if (opts.rejectVoid) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "VoidSale is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }
      state.lastSale = {
        ...(state.lastSale ?? baseSale("Cash")),
        status: "Voided",
        voidReason: String(body.reason ?? ""),
        voidedAtUtc: "2026-08-21T03:00:00Z",
      };
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.lastSale),
      });
    }

    if (method === "POST" && pathname.endsWith("/sales")) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.posts.push(body);
      const methodCode = String(body.paymentMethod ?? "Cash");
      state.lastSale = baseSale(methodCode, {
        saleId: String(body.saleId ?? SALE_ID),
        gCashReference: body.gCashReference ?? null,
        customerId: body.customerId ?? null,
        customerDisplayName: body.customerId ? "Juan Dela Cruz" : null,
        linkedCreditEntryId: methodCode === "Utang" ? "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb" : null,
        amountTendered: methodCode === "Cash" ? Number(body.amountTendered ?? 0) : null,
        changeAmount:
          methodCode === "Cash" ? Math.max(0, Number(body.amountTendered ?? 0) - quoteTotal) : null,
        total: quoteTotal,
        subtotal: quoteTotal,
      });
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.lastSale),
      });
    }

    if (method === "GET" && /\/sales\/[0-9a-f-]+$/i.test(pathname)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.lastSale ?? baseSale("Cash")),
      });
    }

    return route.fallback();
  });

  return state;
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

async function signInOwnerSelling(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  const startSelling = page.getByTestId("workspace-destination-start_selling");
  await startSelling.waitFor({ state: "visible", timeout: 15000 });
  await startSelling.click();
  await expect(page.getByTestId("sell-floor")).toBeVisible({ timeout: 15000 });
}

test.describe("RMAP-12 payments + void", () => {
  test.use({ serviceWorkers: "block" });

  test("Cash regression still posts Cash tender", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const state = await mockPosSalesApi(page);
    await mockCustomersApi(page, false);

    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await openCheckoutPaymentMethods(page);
    await expect(page.getByTestId("checkout-pay-cash")).toBeVisible();
    await expect(page.getByTestId("checkout-no-card")).toBeAttached();
    await expect(page.getByTestId("checkout-no-provider-gcash")).toBeAttached();
    await fillCheckoutCashExact(page);
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(state.posts[0]?.paymentMethod).toBe("Cash");
    expect(Number(state.posts[0]?.amountTendered)).toBeGreaterThanOrEqual(25);
  });

  test("GCash maps to ManualGCash with reference; never shows ManualGCash label", async ({
    page,
  }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const state = await mockPosSalesApi(page);
    await mockCustomersApi(page, false);

    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await openCheckoutPaymentMethods(page);
    await page.getByTestId("checkout-pay-gcash").click();
    await expect(page.getByTestId("checkout-gcash-panel")).toBeVisible();
    await expect(page.getByText("ManualGCash")).toHaveCount(0);
    await page.getByTestId("checkout-gcash-reference").fill("REF-7788");
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    await expect(page.getByTestId("summary-payment-method")).toHaveText("GCash");
    expect(state.posts[0]?.paymentMethod).toBe("ManualGCash");
    expect(state.posts[0]?.gCashReference).toBe("REF-7788");
    expect(state.posts[0]?.amountTendered).toBeUndefined();
  });

  test("Owner Utang selects customer and posts without tender", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const state = await mockPosSalesApi(page);
    await mockCustomersApi(page, true);

    await signInOwnerSelling(page);
    await addCokeAndOpenCheckout(page);
    await openCheckoutPaymentMethods(page);
    await page.getByTestId("checkout-pay-utang").click();
    await expect(page.getByTestId("checkout-utang-panel")).toBeVisible();
    await page.getByTestId(`checkout-customer-${CUSTOMER_ID}`).click();
    await expect(page.getByTestId("checkout-customer-selected")).toBeVisible();
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(state.posts[0]?.paymentMethod).toBe("Utang");
    expect(state.posts[0]?.customerId).toBe(CUSTOMER_ID);
    expect(state.posts[0]?.amountTendered).toBeUndefined();
  });

  test("Zero Amount to Pay blocks Utang", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const state = await mockPosSalesApi(page, { quoteTotal: 0 });
    await mockCustomersApi(page, true);

    await signInOwnerSelling(page);
    await addCokeAndOpenCheckout(page);
    await openCheckoutPaymentMethods(page);
    await page.getByTestId("checkout-pay-utang").click();
    await expect(page.getByTestId("checkout-utang-zero-blocked")).toBeVisible();
    await expect(page.getByTestId("checkout-confirm")).toBeDisabled();
    expect(state.posts).toHaveLength(0);
  });

  test("Cashier Utang uses checkout-search without ViewCustomers", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const state = await mockPosSalesApi(page);
    await mockCustomersApi(page, false);

    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await openCheckoutPaymentMethods(page);
    await page.getByTestId("checkout-pay-utang").click();
    await expect(page.getByTestId("checkout-utang-customer-denied")).toHaveCount(0);
    await expect(page.getByTestId("checkout-customer-search")).toBeVisible();
    await page.getByTestId("checkout-customer-search").fill("Juan");
    await expect(page.getByTestId(`checkout-customer-${CUSTOMER_ID}`)).toBeVisible();
    await page.getByTestId(`checkout-customer-${CUSTOMER_ID}`).click();
    await expect(page.getByTestId("checkout-customer-selected")).toBeVisible();
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    expect(state.posts[0]?.paymentMethod).toBe("Utang");
    expect(state.posts[0]?.customerId).toBe(CUSTOMER_ID);

    await clientNavigate(page, "/customers");
    await expect(page.getByTestId("customers-view-denied")).toBeVisible();
  });

  for (const viewport of [
    { width: 375, height: 812 },
    { width: 768, height: 1024 },
    { width: 1024, height: 768 },
    { width: 1440, height: 900 },
  ] as const) {
    test(`Cashier Utang checkout customer UI usable at ${viewport.width}x${viewport.height}`, async ({
      page,
    }) => {
      await page.setViewportSize(viewport);
      await seedInstallationId(page);
      await mockBoundCashierSession(page);
      await mockAuthorizedDevice(page);
      await mockPosCatalogApi(page);
      await mockPosRegisterShiftApi(page, { openShift: true });
      await mockPosSalesApi(page);
      await mockCustomersApi(page, false);

      await signInAndBindCashier(page);
      await clientNavigate(page, "/sell");
      await addCokeAndOpenCheckout(page);
      await openCheckoutPaymentMethods(page);
      await page.getByTestId("checkout-pay-utang").click();
      await expect(page.getByTestId("checkout-customer-search")).toBeVisible();
      await page.getByTestId("checkout-customer-search").fill("Juan");
      await expect(page.getByTestId("checkout-customer-list")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }

  test("Owner can void; Cashier cannot", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    const state = await mockPosSalesApi(page);
    await mockCustomersApi(page, true);

    await signInOwnerSelling(page);
    await addCokeAndOpenCheckout(page);
    await fillCheckoutCashExact(page);
    await page.getByTestId("checkout-confirm").click();
    await page.getByTestId("summary-void-trigger").click();
    await expect(page.getByTestId("summary-void-panel")).toBeVisible();
    await page.getByTestId("summary-void-reason").fill("Wrong tender");
    await page.getByTestId("summary-void-confirm").click();
    await expect(page.getByTestId("summary-voided-banner")).toBeVisible();
    expect(state.voids[0]?.reason).toBe("Wrong tender");
  });

  test("Cashier summary hides void controls", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedDevice(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await mockPosSalesApi(page);
    await mockCustomersApi(page, false);

    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await addCokeAndOpenCheckout(page);
    await fillCheckoutCashExact(page);
    await page.getByTestId("checkout-confirm").click();
    await expect(page.getByTestId("transaction-summary-page")).toBeVisible();
    await expect(page.getByTestId("summary-void-panel")).toHaveCount(0);
    await expect(page.getByTestId("summary-void-denied")).toBeVisible();
  });

  for (const viewport of VIEWPORTS) {
    test(`checkout payment selector usable at ${viewport.width}x${viewport.height}`, async ({
      page,
    }) => {
      await page.setViewportSize(viewport);
      await seedInstallationId(page);
      await mockBoundCashierSession(page);
      await mockAuthorizedDevice(page);
      await mockPosCatalogApi(page);
      await mockPosRegisterShiftApi(page, { openShift: true });
      await mockPosSalesApi(page);
      await mockCustomersApi(page, false);

      await signInAndBindCashier(page);
      await clientNavigate(page, "/sell");
      await addCokeAndOpenCheckout(page);
      await openCheckoutPaymentMethods(page);
      await expect(page.getByTestId("checkout-pay-gcash")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

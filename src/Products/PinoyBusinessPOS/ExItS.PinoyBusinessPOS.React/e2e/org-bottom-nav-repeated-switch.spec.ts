import { expect, test } from "@playwright/test";
import { mockBoundManagerSession, signInAndBindManager } from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosPriceAuthorityApi } from "./mock-pos-price-authority-route";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";

type ApiCounts = {
  catalogCategories: number;
  catalogProducts: number;
  currentShift: number;
  priceAuthorities: number;
  deviceAuthorize: number;
};

const NAV_SEQUENCE = [
  {
    id: "home",
    testId: "org-nav-home",
    url: /\/role\/manager$/,
    assertVisible: (page: import("@playwright/test").Page) =>
      page.getByTestId("manager-home").waitFor({ state: "visible", timeout: 15000 }),
  },
  {
    id: "catalog",
    testId: "org-nav-catalog",
    url: /\/catalog(\/|$)/,
    assertVisible: (page: import("@playwright/test").Page) =>
      page.getByTestId("catalog-products-page").waitFor({ state: "visible", timeout: 15000 }),
  },
  {
    id: "sell",
    testId: "org-nav-sell",
    url: /\/sell(\/|$)/,
    assertVisible: (page: import("@playwright/test").Page) =>
      page
        .getByTestId("sell-floor")
        .or(page.getByTestId("sell-readiness-device"))
        .or(page.getByTestId("sell-readiness-shift"))
        .waitFor({ state: "visible", timeout: 15000 }),
  },
  {
    id: "orders",
    testId: "org-nav-orders",
    url: /\/orders(\/|$)/,
    assertVisible: (page: import("@playwright/test").Page) =>
      page.getByTestId("seller-orders-page").waitFor({ state: "visible", timeout: 15000 }),
  },
  {
    id: "more",
    testId: "org-nav-more",
    url: /\/more$/,
    assertVisible: (page: import("@playwright/test").Page) =>
      page.getByTestId("org-more-page").waitFor({ state: "visible", timeout: 15000 }),
  },
] as const;

async function installInstrumentedPosApi(page: import("@playwright/test").Page, counts: ApiCounts) {
  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/catalog/categories") && method === "GET") {
      counts.catalogCategories += 1;
    }
    if (url.includes("/catalog/products") && method === "GET") {
      counts.catalogProducts += 1;
    }
    if (url.includes("/cashier-shifts/current") && method === "GET") {
      counts.currentShift += 1;
    }
    if (url.includes("/offline-price-authorities") && method === "POST") {
      counts.priceAuthorities += 1;
    }

    await route.fallback();
  });

  await page.route("**/platform-api/**/pos-devices/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/pos-devices/authorize") && method === "POST") {
      counts.deviceAuthorize += 1;
    }
    await route.fallback();
  });
}

async function mockSellerOrdersApi(page: import("@playwright/test").Page) {
  await page.route("**/pos-api/api/v1/pos/organizations/*/customer-orders**", async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 40 }),
    });
  });
}

async function signInBoundManagerReady(page: import("@playwright/test").Page) {
  await seedInstallationId(page);
  await mockBoundManagerSession(page);
  await mockAuthorizedPosDevice(page);
  await mockPosCatalogApi(page);
  await mockPosRegisterShiftApi(page, { openShift: true });
  await mockPosPriceAuthorityApi(page);
  await mockSellerOrdersApi(page);
  await signInAndBindManager(page);
  await expect(page.getByTestId("manager-home")).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
}

test.describe("Repeated org bottom navigation stress", () => {
  test.use({ serviceWorkers: "block" });

  test("manager survives 20 Home → Catalog → Sell → Orders → More cycles", async ({ page }) => {
    test.setTimeout(300_000);

    const counts: ApiCounts = {
      catalogCategories: 0,
      catalogProducts: 0,
      currentShift: 0,
      priceAuthorities: 0,
      deviceAuthorize: 0,
    };
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    const requestFailures: string[] = [];

    page.on("console", (message) => {
      if (message.type() === "error") {
        consoleErrors.push(message.text());
      }
    });
    page.on("pageerror", (error) => {
      pageErrors.push(error.message);
    });
    page.on("requestfailed", (request) => {
      requestFailures.push(`${request.method()} ${request.url()} :: ${request.failure()?.errorText ?? "failed"}`);
    });

    await page.setViewportSize({ width: 390, height: 844 });
    await signInBoundManagerReady(page);
    await installInstrumentedPosApi(page, counts);

    const cycles = 20;

    for (let cycle = 0; cycle < cycles; cycle += 1) {
      for (const destination of NAV_SEQUENCE) {
        const navButton = page.getByTestId(destination.testId);
        await expect(navButton).toBeEnabled({ timeout: 15000 });
        await navButton.click();
        await expect(page).toHaveURL(destination.url, { timeout: 15000 });
        await destination.assertVisible(page);
        await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
        await expect(page.getByTestId("client-error-overlay")).toHaveCount(0);
        await expect(page.getByTestId("session-checking")).toHaveCount(0);
        await expect(page.getByText("Checking session…")).toHaveCount(0);
      }
    }

    expect(pageErrors, `uncaught page errors: ${pageErrors.join("; ")}`).toEqual([]);
    const significantConsoleErrors = consoleErrors.filter(
      (line) =>
        !line.includes("favicon") &&
        !line.includes("Failed to load resource") &&
        !line.includes("net::ERR"),
    );
    expect(significantConsoleErrors, "console errors").toEqual([]);
    const significantRequestFailures = requestFailures.filter(
      (line) => !line.includes("ERR_ABORTED"),
    );
    expect(significantRequestFailures, `failed requests: ${significantRequestFailures.join("; ")}`).toEqual([]);

    // Cached catalog browse must not refetch on every Sell remount during rapid tab switching.
    expect(counts.catalogCategories).toBeLessThanOrEqual(cycles + 2);
    expect(counts.catalogProducts).toBeLessThanOrEqual(cycles + 4);
    expect(counts.priceAuthorities).toBeLessThanOrEqual(2);
    expect(counts.deviceAuthorize).toBeLessThanOrEqual(2);
  });
});

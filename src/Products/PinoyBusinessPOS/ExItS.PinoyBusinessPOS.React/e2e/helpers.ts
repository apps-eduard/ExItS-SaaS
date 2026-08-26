import { expect, type Page } from "@playwright/test";

export async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

export async function openCheckoutPaymentMethods(page: Page) {
  const cash = page.getByTestId("checkout-pay-cash");
  if (await cash.isVisible()) {
    return;
  }
  await page.getByTestId("checkout-payment-collapse-toggle").click();
  await expect(cash).toBeVisible();
}

export async function openCheckoutDiscountForm(page: Page) {
  const value = page.getByTestId("checkout-discount-value");
  if (await value.isVisible()) {
    return;
  }
  await page.getByTestId("checkout-discount-collapse-toggle").click();
  await expect(value).toBeVisible();
}

export async function chooseWalkInCustomerCreate(page: Page) {
  const walkIn = page.getByTestId("customer-create-kind-walkin");
  if (await walkIn.isVisible()) {
    await walkIn.click();
  }
  await expect(page.getByTestId("customer-display-name")).toBeVisible();
}

export async function chooseExitsCustomerCreate(page: Page) {
  const exits = page.getByTestId("customer-create-kind-exits");
  if (await exits.isVisible()) {
    await exits.click();
  }
  await expect(page.getByTestId("customer-personal-link-panel")).toBeVisible();
}

export async function fillCheckoutCashExact(page: Page) {
  const exact = page.getByTestId("checkout-cash-exact");
  if (await exact.isVisible()) {
    await exact.click();
  }
}

export async function assertMinTouchTarget(locator: import("@playwright/test").Locator) {
  const box = await locator.boundingBox();
  expect(box, "control should be visible for touch-target check").toBeTruthy();
  expect(box!.height).toBeGreaterThanOrEqual(40);
}

export async function waitForServiceWorker(page: Page) {
  await page.waitForFunction(async () => {
    const registration = await navigator.serviceWorker.ready;
    return Boolean(registration.active);
  });
}

export async function inspectServiceWorkerCaches(page: Page) {
  await page.waitForFunction(async () => {
    const registration = await navigator.serviceWorker.ready;
    if (!registration.active) {
      return false;
    }
    const cacheNames = await caches.keys();
    let count = 0;
    for (const name of cacheNames) {
      const cache = await caches.open(name);
      count += (await cache.keys()).length;
    }
    return count > 0;
  });
  return page.evaluate(async () => {
    const cacheNames = await caches.keys();
    const urls: string[] = [];
    for (const name of cacheNames) {
      const cache = await caches.open(name);
      const requests = await cache.keys();
      for (const request of requests) {
        urls.push(request.url);
      }
    }
    const indexedDbNames = indexedDB.databases
      ? (await indexedDB.databases()).map((database) => database.name ?? "")
      : [];
    return { cacheNames, urls, indexedDbNames };
  });
}

export function assertNoApiOrAuthTrafficInCaches(urls: string[]) {
  for (const url of urls) {
    expect(url, url).not.toMatch(/\/api\//i);
    expect(url, url).not.toMatch(/\/platform-api\//i);
    expect(url, url).not.toMatch(/sessionToken|refreshToken|Bearer /i);
  }
}

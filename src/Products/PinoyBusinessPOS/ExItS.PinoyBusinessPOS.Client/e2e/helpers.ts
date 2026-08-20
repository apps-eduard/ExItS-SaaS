import { expect, type Page } from "@playwright/test";

export async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
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

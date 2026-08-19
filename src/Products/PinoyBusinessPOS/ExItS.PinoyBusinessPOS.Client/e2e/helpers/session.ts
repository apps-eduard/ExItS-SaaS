import type { Page } from "@playwright/test";

export const PREVIEW_SESSION = {
  sessionId: "11111111-1111-4111-8111-111111111111",
  userId: "22222222-2222-4222-8222-222222222222",
  username: "preview.user",
  displayName: "Preview User",
  email: "preview.user@example.test",
  expiresAtUtc: "2026-12-31T00:00:00.000Z",
  absoluteExpiresAtUtc: "2026-12-31T00:00:00.000Z",
  lastActivityAtUtc: "2026-08-19T00:00:00.000Z",
  organizationSelectionState: "None",
  activeOrganizationCount: 0,
};

type MeStub = { status: number; body: unknown };

async function stubAuthMe(page: Page, stub: MeStub) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      status: stub.status,
      contentType: "application/json",
      body: JSON.stringify(stub.body),
    });
  });
  await page.addInitScript(({ status, body }) => {
    const original = window.fetch.bind(window);
    window.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      const method = init?.method ?? (input instanceof Request ? input.method : "GET");
      if (url.includes("/api/v1/platform/auth/me") && method.toUpperCase() === "GET") {
        return new Response(JSON.stringify(body), {
          status,
          headers: { "Content-Type": "application/json" },
        });
      }
      return original(input, init);
    };
  }, stub);
}

export async function mockAuthenticatedSession(page: Page) {
  await stubAuthMe(page, { status: 200, body: PREVIEW_SESSION });
}

export async function mockSignedOutSession(page: Page) {
  await stubAuthMe(page, {
    status: 401,
    body: {
      status: 401,
      title: "Unauthorized",
      errorCode: "auth.session_invalid",
    },
  });
}

export async function collectBrowserAuthPersistence(page: Page) {
  return page.evaluate(async () => {
    const sentinels = ["sessionToken", "SessionToken", "access_token", "refresh_token"];
    const hits: string[] = [];
    const scan = (storage: Storage, label: string) => {
      for (let index = 0; index < storage.length; index += 1) {
        const key = storage.key(index) ?? "";
        const value = storage.getItem(key) ?? "";
        if (sentinels.some((sentinel) => key.includes(sentinel) || value.includes(sentinel))) {
          hits.push(`${label}:${key}`);
        }
      }
    };
    scan(window.localStorage, "localStorage");
    scan(window.sessionStorage, "sessionStorage");
    if (typeof indexedDB !== "undefined" && typeof indexedDB.databases === "function") {
      const databases = await indexedDB.databases();
      for (const database of databases) {
        const name = database.name ?? "";
        if (sentinels.some((sentinel) => name.includes(sentinel))) {
          hits.push(`indexedDB:${name}`);
        }
      }
    }
    if (typeof caches !== "undefined") {
      const cacheNames = await caches.keys();
      for (const cacheName of cacheNames) {
        const cache = await caches.open(cacheName);
        const requests = await cache.keys();
        for (const request of requests) {
          if (/auth\/(login|me|logout)|sessionToken|\/platform-api\//i.test(request.url)) {
            hits.push(`cache:${new URL(request.url).pathname}`);
          }
        }
      }
    }
    return hits;
  });
}

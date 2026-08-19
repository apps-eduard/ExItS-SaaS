import { expect, test } from "@playwright/test";

const loginPath = "/platform-api/api/v1/platform/auth/login";
const mePath = "/platform-api/api/v1/platform/auth/me";
const logoutPath = "/platform-api/api/v1/platform/auth/logout";
const identity = process.env.PLM_CLIENT_GATE_D0_LOGIN ?? "olivia.mendoza@exits.local";
const password = process.env.LOCAL_VALIDATION_SHARED_PASSWORD ?? "";

async function assertNoSessionTokenPersistence(page: import("@playwright/test").Page) {
  const persisted = await page.evaluate(async () => {
    const storage = {
      local: { ...window.localStorage },
      session: { ...window.sessionStorage },
    };
    const cacheKeys = (await window.caches?.keys?.()) ?? [];
    return JSON.stringify({ storage, cacheKeys, href: window.location.href });
  });
  expect(persisted).not.toMatch(/sessionToken/i);
}

test.describe("browser auth transport", () => {
  test("preview /platform-api proxy is same-origin and relative", async ({ request }) => {
    const health = await request.get("/platform-api/health");
    expect(health.url()).toContain("/platform-api/health");
    expect(health.url()).not.toContain(":8091");
    expect(health.ok(), `preview proxy health ${health.status()}`).toBeTruthy();
  });

  test("dev /platform-api proxy is same-origin", async ({ playwright }) => {
    const request = await playwright.request.newContext({
      baseURL: "http://127.0.0.1:5176",
    });
    const health = await request.get("/platform-api/health");
    expect(health.url()).toContain("127.0.0.1:5176/platform-api/health");
    expect(health.ok(), `dev proxy health ${health.status()}`).toBeTruthy();
    await request.dispose();
  });

  test("cookie login, refresh, and logout use HttpOnly session only", async ({ page }) => {
    test.skip(
      !password,
      "LOCAL_VALIDATION_SHARED_PASSWORD is required for real Local Validation transport proof.",
    );

    await page.goto("/");
    const login = await page.request.post(loginPath, {
      data: { usernameOrEmail: identity, password },
      headers: { "Content-Type": "application/json" },
    });
    expect(login.ok(), `login ${login.status()}`).toBeTruthy();
    const setCookie = login.headers()["set-cookie"] ?? "";
    expect(setCookie).toMatch(/\.ExItS\.Platform\.Auth=/i);
    expect(setCookie).toMatch(/httponly/i);
    expect(setCookie).toMatch(/samesite=lax/i);
    const body = (await login.json()) as { sessionToken?: string };
    expect(body.sessionToken).toBeTruthy();

    await assertNoSessionTokenPersistence(page);

    const me = await page.request.get(mePath);
    expect(me.ok(), `auth/me ${me.status()}`).toBeTruthy();
    const meBody = (await me.json()) as { username?: string; email?: string };
    expect(`${meBody.username ?? ""} ${meBody.email ?? ""}`.toLowerCase()).toMatch(/olivia/i);

    await page.reload();
    const meAfterReload = await page.request.get(mePath);
    expect(meAfterReload.ok()).toBeTruthy();
    await assertNoSessionTokenPersistence(page);

    const logout = await page.request.post(logoutPath);
    expect(logout.status()).toBeGreaterThanOrEqual(200);
    expect(logout.status()).toBeLessThan(300);

    const unauthenticated = await page.request.get(mePath);
    expect(unauthenticated.status()).toBe(401);
  });
});

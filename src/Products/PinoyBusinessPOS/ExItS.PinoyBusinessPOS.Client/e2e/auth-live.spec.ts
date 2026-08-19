import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { collectBrowserAuthPersistence } from "./helpers/session";
import { readLocalValidationSharedPassword } from "./helpers/local-validation-password";

function redactSetCookie(value: string | undefined): string {
  if (!value) {
    return "";
  }
  return value.replace(/(\.ExItS\.Platform\.Auth)=[^;]*/i, "$1=redacted");
}

async function requireLocalValidationProxy(request: APIRequestContext) {
  const health = await request.get("/platform-api/health");
  expect(
    health.ok(),
    "Local Validation Platform API must be reachable through the same-origin /platform-api proxy.",
  ).toBeTruthy();
  const enabled = await request.get("/platform-api/api/v1/platform/local-validation/enabled");
  expect(enabled.ok()).toBeTruthy();
  expect(await enabled.json()).toBe(true);
}

async function loginInBrowser(page: Page, username: string, password: string, displayName: string) {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/sign-in");
  await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
  await page.getByLabel("Email or username").fill(username);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Client foundation" })).toBeVisible();
  await expect(page.getByRole("banner")).toContainText(displayName);
}

test.describe("real Local Validation cookie auth", () => {
  test("Maria and Personal login use HttpOnly cookie and survive refresh", async ({
    page,
    request,
  }) => {
    const password = readLocalValidationSharedPassword();
    await requireLocalValidationProxy(request);

    const login = await request.post("/platform-api/api/v1/platform/auth/login", {
      data: { usernameOrEmail: "maria.santos", password },
      failOnStatusCode: false,
    });
    expect(
      login.ok(),
      "Maria password login through the same-origin proxy must succeed.",
    ).toBeTruthy();
    const setCookie = redactSetCookie(
      login.headers()["set-cookie"] ?? login.headers()["Set-Cookie"],
    );
    expect(setCookie).toContain(".ExItS.Platform.Auth=");
    expect(setCookie).toMatch(/httponly/i);
    expect(setCookie).toMatch(/samesite=lax/i);
    expect(setCookie).not.toMatch(/(?:^|;)\s*secure(?:;|$)/i);
    const loginBody = await login.text();
    expect(loginBody).not.toContain("password");

    await loginInBrowser(page, "maria.santos", password, "Maria Santos");
    expect(await collectBrowserAuthPersistence(page)).toEqual([]);

    await page.reload();
    await expect(page.getByRole("heading", { name: "Client foundation" })).toBeVisible();
    await expect(page.getByRole("banner")).toContainText("Maria Santos");
    expect(await collectBrowserAuthPersistence(page)).toEqual([]);

    const me = await page.request.get("/platform-api/api/v1/platform/auth/me");
    expect(me.ok(), "auth/me must succeed after refresh using the session cookie.").toBeTruthy();
    const meBody = await me.json();
    expect(meBody.username).toBe("maria.santos");
    expect(meBody.sessionToken).toBeUndefined();

    await page.goto("/appearance");
    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();

    const wrong = await page.request.post("/platform-api/api/v1/platform/auth/login", {
      data: { usernameOrEmail: "maria.santos", password: "Wrong-Password-9!" },
      failOnStatusCode: false,
    });
    expect(wrong.status()).toBe(401);

    await loginInBrowser(page, "luis.navarro", password, "Luis Navarro");
    await page.reload();
    await expect(page.getByRole("banner")).toContainText("Luis Navarro");
    expect(await collectBrowserAuthPersistence(page)).toEqual([]);
  });
});

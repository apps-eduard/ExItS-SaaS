import { expect, type Page } from "@playwright/test";

export async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

export async function mockAnonymousSession(page: Page) {
  await page.route("**/platform-api/api/v1/platform/auth/me", (route) =>
    route.fulfill({
      status: 401,
      contentType: "application/json",
      body: JSON.stringify({ errorCode: "application.auth.session_invalid" }),
    }),
  );
  await page.route("**/platform-api/api/v1/platform/local-validation/enabled", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: "false",
    }),
  );
}

export async function mockAuthenticatedSession(page: Page) {
  let signedIn = false;
  await page.route("**/platform-api/api/v1/platform/auth/me", (route) => {
    if (!signedIn) {
      return route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ errorCode: "application.auth.session_invalid" }),
      });
    }
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ username: "olivia", displayName: "Olivia Mendoza" }),
    });
  });
  await page.route("**/platform-api/api/v1/platform/auth/login", async (route) => {
    const posted = route.request().postDataJSON() as {
      usernameOrEmail?: string;
      password?: string;
    };
    if (!posted.password || posted.password === "wrong") {
      return route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ errorCode: "application.auth.login_failed" }),
      });
    }
    signedIn = true;
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        username: "olivia",
        displayName: "Olivia Mendoza",
        sessionToken: "must-not-be-used",
      }),
    });
  });
  await page.route("**/platform-api/api/v1/platform/auth/logout", (route) => {
    signedIn = false;
    return route.fulfill({ status: 204, body: "" });
  });
  await page.route("**/platform-api/api/v1/platform/local-validation/enabled", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: "false",
    }),
  );
}

export function passwordField(page: Page) {
  return page.getByRole("textbox", { name: "Password" });
}

export async function assertNoSessionTokenPersistence(page: Page) {
  const persisted = await page.evaluate(async () => {
    const cacheKeys = (await window.caches?.keys?.()) ?? [];
    return JSON.stringify({
      local: { ...window.localStorage },
      session: { ...window.sessionStorage },
      cacheKeys,
    });
  });
  expect(persisted).not.toMatch(/sessionToken/i);
}

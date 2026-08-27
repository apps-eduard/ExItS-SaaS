/**
 * EXITS-PERSONAL-GUIDE-01 — Explore ExItS learning progress (not feature flags).
 */
import { expect, test, type Page } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { clientNavigate } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const USER_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const USER_B = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const PROFILE_A = "11111111-1111-4111-8111-111111111111";
const PROFILE_B = "22222222-2222-4222-8222-222222222222";

type UserRecord = {
  userId: string;
  profileId: string;
  email: string;
  displayName: string;
};

const USERS: Record<string, UserRecord> = {
  "ana@example.com": {
    userId: USER_A,
    profileId: PROFILE_A,
    email: "ana@example.com",
    displayName: "Ana Personal",
  },
  "ben@example.com": {
    userId: USER_B,
    profileId: PROFILE_B,
    email: "ben@example.com",
    displayName: "Ben Personal",
  },
};

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function dashboard(user: UserRecord) {
  return {
    userIdentityId: user.userId,
    accountProfileId: user.profileId,
    accountClass: "Personal",
    utangAvailable: true,
    contactCount: 0,
    activeRelationshipCount: 0,
    totalLentBalance: 0,
    totalBorrowedBalance: 0,
    pendingConfirmationCount: 0,
  };
}

function sessionBody(user: UserRecord) {
  return {
    sessionId: "55555555-5555-4555-8555-555555555555",
    userId: user.userId,
    accountClass: "Personal",
    username: user.email,
    displayName: user.displayName,
    email: user.email,
    homeOrganizationId: null,
    organizationContextLocked: false,
    roles: [] as string[],
  };
}

async function installGuideMocks(page: Page) {
  let current: UserRecord | null = null;

  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }

    if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
      if (!current) {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }
      return json(route, sessionBody(current));
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      const raw = route.request().postData() ?? "";
      let email = "ana@example.com";
      try {
        const parsed = JSON.parse(raw) as {
          usernameOrEmail?: string;
          username?: string;
          email?: string;
        };
        email = parsed.usernameOrEmail ?? parsed.username ?? parsed.email ?? email;
      } catch {
        const match = /ana@example.com|ben@example.com/.exec(raw);
        if (match) {
          email = match[0];
        }
      }
      current = USERS[email] ?? USERS["ana@example.com"]!;
      return json(route, { ...sessionBody(current), sessionToken: "must-not-persist" });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      current = null;
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return json(route, []);
    }

    if (!current) {
      return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
    }

    if (url.includes("/api/v1/personal/dashboard")) {
      return json(route, dashboard(current));
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      return json(route, { items: [], totalCount: 0, page: 1, pageSize: 50 });
    }

    if (
      url.includes("/api/v1/personal/") ||
      url.includes("/ownership-transfers") ||
      url.includes("/account-profiles")
    ) {
      return json(route, []);
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

async function signInPersonal(page: Page, email: string) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill(email);
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
}

async function signOutPersonal(page: Page) {
  await page.getByTestId("account-menu-trigger").click();
  await page.getByRole("menuitem", { name: "Sign out" }).click();
  await expect(page).toHaveURL(/\/sign-in/);
}

test.describe("EXITS-PERSONAL-GUIDE-01", () => {
  test("A) Home discovery card opens Explore ExItS with categories", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await installGuideMocks(page);
    await signInPersonal(page, "ana@example.com");
    await expect(page.getByTestId("personal-home-page")).toBeVisible();
    await expect(page.getByTestId("personal-guide-home-card")).toBeVisible();
    await page.getByTestId("personal-guide-home-continue").click();
    await expect(page.getByTestId("personal-guide-page")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Explore ExItS" })).toBeVisible();
    await expect(page.getByTestId("guide-category-account")).toBeVisible();
    await expect(page.getByTestId("guide-category-people")).toBeVisible();
    await expect(page.getByTestId("guide-category-money")).toBeVisible();
    await expect(page.getByTestId("guide-category-shopping")).toBeVisible();
    await expect(page.getByTestId("guide-category-business")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test("B) Mark Stores learned, progress updates, refresh keeps it", async ({ page }) => {
    await installGuideMocks(page);
    await signInPersonal(page, "ana@example.com");
    await clientNavigate(page, "/personal/guide");
    await expect(page.getByTestId("personal-guide-page")).toBeVisible();
    await expect(page.getByTestId("guide-progress-text")).toContainText("0 of 20");
    await page.getByTestId("guide-card-toggle-stores").click();
    await page.getByTestId("guide-learned-stores").check();
    await expect(page.getByTestId("guide-progress-text")).toContainText("1 of 20");
    await expect(page.getByTestId("guide-card-state-stores")).toHaveText("Completed");
    await page.reload();
    await expect(page.getByTestId("personal-guide-page")).toBeVisible();
    await expect(page.getByTestId("guide-card-state-stores")).toHaveText("Completed");
    await expect(page.getByTestId("guide-progress-text")).toContainText("1 of 20");
  });

  test("C) User A progress is not visible to User B", async ({ page }) => {
    await installGuideMocks(page);
    await signInPersonal(page, "ana@example.com");
    await clientNavigate(page, "/personal/guide");
    await page.getByTestId("guide-card-toggle-stores").click();
    await page.getByTestId("guide-learned-stores").check();
    await expect(page.getByTestId("guide-card-state-stores")).toHaveText("Completed");
    await signOutPersonal(page);

    await signInPersonal(page, "ben@example.com");
    await clientNavigate(page, "/personal/guide");
    await expect(page.getByTestId("guide-card-state-stores")).toHaveText("Not explored");
    await expect(page.getByTestId("guide-progress-text")).toContainText("0 of 20");
  });

  test("D) Try It on Stores opens the real Stores route", async ({ page }) => {
    await installGuideMocks(page);
    await signInPersonal(page, "ana@example.com");
    await clientNavigate(page, "/personal/guide");
    await page.getByTestId("guide-card-toggle-stores").click();
    await page.getByTestId("guide-try-stores").click();
    await expect(page).toHaveURL(/\/personal\/linked-merchants$/);
    await expect(page.getByTestId("linked-merchants-page")).toBeVisible();
  });

  test("E) Dismiss Home card, refresh keeps it hidden, guide stays in More", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await installGuideMocks(page);
    await signInPersonal(page, "ana@example.com");
    await expect(page.getByTestId("personal-guide-home-card")).toBeVisible();
    await page.getByTestId("personal-guide-home-dismiss").click();
    await expect(page.getByTestId("personal-guide-home-card")).toHaveCount(0);
    await page.reload();
    await expect(page.getByTestId("personal-home-page")).toBeVisible();
    await expect(page.getByTestId("personal-guide-home-card")).toHaveCount(0);
    await page.getByTestId("personal-nav-more").click();
    await expect(page.getByTestId("more-open-guide")).toBeVisible();
    await page.getByTestId("more-open-guide").click();
    await expect(page.getByTestId("personal-guide-page")).toBeVisible();
    await page.setViewportSize({ width: 1440, height: 900 });
    await assertNoHorizontalOverflow(page);
  });
});

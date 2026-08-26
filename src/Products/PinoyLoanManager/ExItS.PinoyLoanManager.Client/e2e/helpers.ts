import { expect, type Page } from "@playwright/test";

export const E2E_ORG_ALLOWED = {
  organizationId: "11111111-1111-4111-8111-111111111111",
  displayName: "ABC Sari-Sari Store",
  slug: "abc-sari-sari",
  membershipRole: "OrganizationOwner",
};

export const E2E_ORG_DENIED = {
  organizationId: "33333333-3333-4333-8333-333333333333",
  displayName: "XYZ Mini Grocery",
  slug: "xyz-mini-grocery",
  membershipRole: "OrganizationOwner",
};

export type OrganizationAccessMockOptions = {
  accountClass?: string;
  selectedOrganizationId?: string | null;
  organizations?: (typeof E2E_ORG_ALLOWED)[];
  productAccess?: { allowed: boolean; reasonCode?: string; subscriptionStatus?: string | null };
  startSignedIn?: boolean;
};

type AccessState = {
  signedIn: boolean;
  accountClass: string;
  selectedOrganizationId: string | null;
  organizations: (typeof E2E_ORG_ALLOWED)[];
  productAccess: {
    allowed: boolean;
    reasonCode?: string;
    subscriptionStatus?: string | null;
  };
};

const PLATFORM_ROUTE = "**/platform-api/api/v1/platform/**";

export async function mockOrganizationProductAccess(
  page: Page,
  options: OrganizationAccessMockOptions = {},
) {
  const state: AccessState = {
    signedIn: options.startSignedIn ?? false,
    accountClass: options.accountClass ?? "Organization",
    selectedOrganizationId:
      options.selectedOrganizationId === undefined
        ? ((options.organizations ?? [E2E_ORG_ALLOWED])[0]?.organizationId ?? null)
        : options.selectedOrganizationId,
    organizations: options.organizations ?? [E2E_ORG_ALLOWED],
    productAccess: options.productAccess ?? { allowed: true, reasonCode: "allowed" },
  };

  await page.unroute(PLATFORM_ROUTE).catch(() => undefined);
  await page
    .unroute("**/platform-api/api/v1/platform/local-validation/enabled")
    .catch(() => undefined);

  await page.route("**/platform-api/api/v1/platform/local-validation/enabled", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: "false",
    }),
  );

  await page.route(PLATFORM_ROUTE, async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/antiforgery/token") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          headerName: "X-XSRF-TOKEN",
          token: "e2e-csrf-token",
        }),
      });
    }

    if (url.includes("/auth/me")) {
      if (!state.signedIn) {
        return route.fulfill({
          status: 401,
          contentType: "application/json",
          body: JSON.stringify({ errorCode: "application.auth.session_invalid" }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          username: "olivia",
          displayName: "Olivia Mendoza",
          accountClass: state.accountClass,
          selectedOrganizationId: state.selectedOrganizationId,
          organizationSelectionState: state.selectedOrganizationId
            ? "Selected"
            : "SelectionRequired",
        }),
      });
    }

    if (url.includes("/auth/login") && method === "POST") {
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
      state.signedIn = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          username: "olivia",
          displayName: "Olivia Mendoza",
          accountClass: state.accountClass,
          selectedOrganizationId: state.selectedOrganizationId,
          sessionToken: "must-not-be-used",
        }),
      });
    }

    if (url.includes("/auth/logout") && method === "POST") {
      expect(route.request().headers()["x-xsrf-token"]).toBe("e2e-csrf-token");
      state.signedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/auth/organizations")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.organizations),
      });
    }

    if (url.includes("/auth/organization-context") && method === "PUT") {
      expect(route.request().headers()["x-xsrf-token"]).toBe("e2e-csrf-token");
      const body = route.request().postDataJSON() as { organizationId?: string };
      state.selectedOrganizationId = body.organizationId ?? null;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          selectedOrganizationId: state.selectedOrganizationId,
          organizationSelectionState: "Selected",
        }),
      });
    }

    if (url.includes("/auth/product-access/effective")) {
      expect(url).not.toMatch(/[?&]userId=/i);
      expect(url).not.toMatch(/[?&]organizationId=/i);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          allowed: state.productAccess.allowed,
          reasonCode:
            state.productAccess.reasonCode ??
            (state.productAccess.allowed ? "allowed" : "product_assignment_missing"),
          organizationId: state.selectedOrganizationId ?? E2E_ORG_ALLOWED.organizationId,
          productCode: "pinoy-loan-manager",
          subscriptionStatus: state.productAccess.subscriptionStatus ?? null,
        }),
      });
    }

    if (url.includes("/auth/account-profiles")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([]),
      });
    }

    if (url.includes("/auth/access/evaluate")) {
      return route.fulfill({ status: 500, body: "must-not-call" });
    }

    return route.continue();
  });

  return state;
}

export async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

export async function mockAntiforgeryToken(page: Page) {
  await page.unroute("**/platform-api/api/v1/platform/antiforgery/token").catch(() => undefined);
  await page.route("**/platform-api/api/v1/platform/antiforgery/token", (route) => {
    if (route.request().method() !== "GET") {
      return route.continue();
    }
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        headerName: "X-XSRF-TOKEN",
        token: "e2e-csrf-token",
      }),
    });
  });
}

export async function mockAnonymousSession(page: Page) {
  await page.unroute(PLATFORM_ROUTE).catch(() => undefined);
  await mockAntiforgeryToken(page);
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

export async function mockAuthenticatedSession(
  page: Page,
  accessOptions: OrganizationAccessMockOptions = {},
) {
  return mockOrganizationProductAccess(page, accessOptions);
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

const SENSITIVE_STORAGE = /sessionToken|authorization:\s*bearer|password|refreshToken/i;

export function assertNoSensitiveAuthMaterial(serialized: string) {
  expect(serialized).not.toMatch(SENSITIVE_STORAGE);
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
    expect(url, url).not.toMatch(/\/platform-api\//i);
    expect(url, url).not.toMatch(/\/api\//i);
    expect(url, url).not.toMatch(/sessionToken/i);
  }
}

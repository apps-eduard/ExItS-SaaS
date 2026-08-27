import { expect, test } from "@playwright/test";
import {
  chooseOwnerManageBusiness,
  clientNavigate,
  completeOfflinePinSetupIfNeeded,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosPriceAuthorityApi } from "./mock-pos-price-authority-route";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";

type ApiCounts = {
  organizations: number;
  branches: number;
  token: number;
  currentShift: number;
};

async function mockManageBusinessApis(page: import("@playwright/test").Page) {
  await page.route("**/pos-api/**/management/overview**", async (route) => {
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        businessDate: "2026-08-27",
        todaySalesTotal: 0,
        todaySaleCount: 0,
        todayCashSalesTotal: 0,
        todayUtangSalesTotal: 0,
        todayPaymentsReceived: 0,
        openUtangOutstanding: 0,
        lowStockProductCount: 0,
        expiredLotCount: 0,
        nearExpiryLotCount: 0,
        pendingTransferCount: 0,
        openShiftCount: 0,
        activeRegisterCount: 0,
      }),
    });
  });
}

async function signInManageBusiness(page: import("@playwright/test").Page) {
  await seedInstallationId(page);
  await mockBoundOwnerSession(page);
  await mockAuthorizedPosDevice(page);
  await mockPosCatalogApi(page);
  await mockPosRegisterShiftApi(page, { openShift: false });
  await mockPosPriceAuthorityApi(page);
  await mockManageBusinessApis(page);
  await signInAndBindOwner(page);
  await Promise.race([
    page.getByTestId("workspace-destination-manage_business").waitFor({ state: "visible", timeout: 15000 }),
    page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 15000 }),
    page.getByRole("heading", { name: "Choose workspace" }).waitFor({ state: "visible", timeout: 15000 }),
  ]);
  await completeOfflinePinSetupIfNeeded(page);
  await chooseOwnerManageBusiness(page);
  await expect(page.getByTestId("org-essentials-page")).toBeVisible({ timeout: 15000 });
}

async function installWorkspaceCounters(page: import("@playwright/test").Page, counts: ApiCounts) {
  await page.route("**/api/v1/platform/auth/organizations**", async (route) => {
    counts.organizations += 1;
    return route.fallback();
  });
  await page.route("**/api/v1/platform/organizations/*/branches**", async (route) => {
    counts.branches += 1;
    return route.fallback();
  });
  await page.route("**/api/v1/platform/auth/token**", async (route) => {
    counts.token += 1;
    return route.fallback();
  });
  await page.route("**/pos-api/**/cashier-shifts/current**", async (route) => {
    counts.currentShift += 1;
    return route.fallback();
  });
}

async function assertSettledDestination(
  page: import("@playwright/test").Page,
  opts: {
    url: RegExp;
    ready: () => Promise<void>;
  },
) {
  await expect(page).toHaveURL(opts.url, { timeout: 15000 });
  await opts.ready();
  await expect(page.getByText("Checking session…")).toHaveCount(0);
  await expect(page.getByTestId("session-checking")).toHaveCount(0);
  await expect(page.getByTestId("client-error-overlay")).toHaveCount(0);
  // Plain Loading must not be the only remaining content for these destinations.
  const branchRequired = page.getByTestId("branch-required-panel");
  const pageReady = page
    .getByTestId("shifts-hub-page")
    .or(page.getByTestId("inventory-list-page"))
    .or(page.getByTestId("inventory-expiration-page"))
    .or(page.getByTestId("suppliers-list-page"))
    .or(page.getByTestId("returns-hub-page"))
    .or(page.getByTestId("org-essentials-page"))
    .or(page.getByTestId("org-more-page"))
    .or(page.getByTestId("catalog-products-page"))
    .or(page.getByTestId("manager-home"))
    .or(branchRequired);
  await expect(pageReady).toBeVisible({ timeout: 15000 });
}

test.describe("Manage Business route hang regression", () => {
  test.use({ serviceWorkers: "block" });

  test("org-only Manage Business settles branch-required for Shifts/Inventory/Suppliers/Returns", async ({
    page,
  }) => {
    test.setTimeout(300_000);

    const counts: ApiCounts = {
      organizations: 0,
      branches: 0,
      token: 0,
      currentShift: 0,
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
      requestFailures.push(
        `${request.method()} ${request.url()} :: ${request.failure()?.errorText ?? "failed"}`,
      );
    });

    await page.setViewportSize({ width: 390, height: 844 });
    await signInManageBusiness(page);
    await installWorkspaceCounters(page, counts);

    const sequence = [
      {
        path: "/shifts",
        url: /\/shifts$/,
        ready: async () => {
          await expect(page.getByTestId("branch-required-panel")).toBeVisible();
        },
      },
      {
        path: "/org",
        url: /\/org$/,
        ready: async () => {
          await expect(page.getByTestId("org-essentials-page")).toBeVisible();
        },
      },
      {
        path: "/inventory",
        url: /\/inventory$/,
        ready: async () => {
          await expect(page.getByTestId("branch-required-panel")).toBeVisible();
        },
      },
      {
        path: "/inventory/expiration",
        url: /\/inventory\/expiration$/,
        ready: async () => {
          await expect(page.getByTestId("branch-required-panel")).toBeVisible();
        },
      },
      {
        path: "/org",
        url: /\/org$/,
        ready: async () => {
          await expect(page.getByTestId("org-essentials-page")).toBeVisible();
        },
      },
      {
        path: "/suppliers",
        url: /\/suppliers$/,
        ready: async () => {
          await expect(page.getByTestId("branch-required-panel")).toBeVisible();
        },
      },
      {
        path: "/org",
        url: /\/org$/,
        ready: async () => {
          await expect(page.getByTestId("org-essentials-page")).toBeVisible();
        },
      },
      {
        path: "/returns",
        url: /\/returns$/,
        ready: async () => {
          await expect(page.getByTestId("branch-required-panel")).toBeVisible();
        },
      },
      {
        path: "/org",
        url: /\/org$/,
        ready: async () => {
          await expect(page.getByTestId("org-essentials-page")).toBeVisible();
        },
      },
    ] as const;

    const cycles = 5;
    for (let cycle = 0; cycle < cycles; cycle += 1) {
      for (const step of sequence) {
        await clientNavigate(page, step.path);
        await assertSettledDestination(page, step);
      }
    }

    // Regression protection for bottom-nav surfaces from Manage Business.
    await clientNavigate(page, "/catalog");
    await assertSettledDestination(page, {
      url: /\/catalog/,
      ready: async () => {
        await expect(
          page.getByTestId("catalog-products-page").or(page.getByTestId("branch-required-panel")),
        ).toBeVisible();
      },
    });
    await clientNavigate(page, "/sell");
    await assertSettledDestination(page, {
      url: /\/sell/,
      ready: async () => {
        await expect(page.getByTestId("branch-required-panel")).toBeVisible();
      },
    });
    await clientNavigate(page, "/more");
    await assertSettledDestination(page, {
      url: /\/more/,
      ready: async () => {
        await expect(page.getByTestId("org-more-page").or(page.getByText(/More/i)).first()).toBeVisible();
      },
    });

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
    expect(significantRequestFailures, `failed requests: ${significantRequestFailures.join("; ")}`).toEqual(
      [],
    );

    // Soft refresh must stay bounded across repeated Manage Business route hops.
    expect(counts.organizations).toBeLessThanOrEqual(8);
    expect(counts.branches).toBeLessThanOrEqual(12);
    expect(counts.token).toBeLessThanOrEqual(6);
    // Org-only bind must not spam current-shift lookups.
    expect(counts.currentShift).toBeLessThanOrEqual(2);
  });
});

/**
 * EXITS-V1-CLOSURE-01 — External Business QR / public store acquisition (mock).
 */
import { expect, test } from "@playwright/test";
import {
  chooseOwnerManageBusiness,
  clientNavigate,
  E2E_ORG_ID,
  mockBoundOwnerSession,
  mockPersonalSession,
  signInAndBindOwner,
  signInAsPersonal,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const ORG_ID = E2E_ORG_ID;
const PUBLIC_ORG = "ORG123456";

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function mockPublicStore(
  page: import("@playwright/test").Page,
  opts?: { linked?: boolean },
) {
  await page.route(`**/api/v1/public/stores/${PUBLIC_ORG}`, async (route) =>
    json(route, {
      publicOrganizationId: PUBLIC_ORG,
      displayName: "Kizy Store",
      orderingAvailable: true,
    }),
  );
  await page.route("**/api/v1/organizations/resolve-public-id", async (route) =>
    json(route, {
      publicOrganizationId: PUBLIC_ORG,
      organizationId: ORG_ID,
      displayName: "Kizy Store",
      status: "Active",
    }),
  );
  await page.route("**/api/v1/personal/linked-merchants**", async (route) => {
    if (opts?.linked) {
      return json(route, {
        items: [
          {
            organizationId: ORG_ID,
            organizationDisplayName: "Kizy Store",
            businessCustomerId: "55555555-5555-4555-8555-555555555555",
            publicOrganizationId: PUBLIC_ORG,
            linkedCustomerId: "66666666-6666-4666-8666-666666666666",
            customerDisplayName: "Buyer",
            linkStatus: "Active",
            linkedAtUtc: "2026-08-01T00:00:00Z",
            canCustomerOrder: true,
            canCustomerDelivery: false,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 50,
      });
    }
    return json(route, { items: [], totalCount: 0, page: 1, pageSize: 50 });
  });
}

test.describe("EXITS-V1 business QR acquisition", () => {
  test("anonymous landing shows store and sign-in continue", async ({ page }) => {
    await mockPublicStore(page);
    await page.goto(`/store/${PUBLIC_ORG}`);
    await expect(page.getByTestId("public-store-landing-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("public-store-name")).toHaveText("Kizy Store");
    await expect(page.getByTestId("public-store-org-id")).toHaveText(PUBLIC_ORG);
    const signIn = page.getByTestId("public-store-sign-in");
    await expect(signIn).toBeVisible();
    await signIn.click();
    await expect(page).toHaveURL(new RegExp(`/sign-in\\?.*continue=.*${PUBLIC_ORG}`));
  });

  test("authenticated Personal continues into linked shop", async ({ page }) => {
    await mockPersonalSession(page);
    await mockPublicStore(page, { linked: true });
    await signInAsPersonal(page);
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await page.goto(`/store/${PUBLIC_ORG}`);
    await expect(page.getByTestId("public-store-continue")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("public-store-continue").click();
    await expect(page).toHaveURL(new RegExp(`/personal/linked-merchants/${ORG_ID}/shop`), {
      timeout: 15000,
    });
  });

  test("unknown store fails safe", async ({ page }) => {
    await page.route("**/api/v1/public/stores/ORG999999", async (route) =>
      json(
        route,
        {
          title: "Not found",
          status: 404,
          detail: "This store is unavailable.",
          errorCode: "application.organization.not_found",
        },
        404,
      ),
    );
    await page.goto("/store/ORG999999");
    await expect(page.getByText(/unavailable/i).first()).toBeVisible({ timeout: 15000 });
  });

  test("install dismiss does not block store CTAs", async ({ page }) => {
    await mockPublicStore(page);
    await page.addInitScript(() => {
      window.addEventListener(
        "DOMContentLoaded",
        () => {
          const event = new Event("beforeinstallprompt", { cancelable: true }) as Event & {
            prompt: () => Promise<void>;
            userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
          };
          Object.defineProperty(event, "prompt", { value: () => Promise.resolve() });
          Object.defineProperty(event, "userChoice", {
            value: Promise.resolve({ outcome: "dismissed" as const }),
          });
          window.dispatchEvent(event);
        },
        { once: true },
      );
    });
    await page.goto(`/store/${PUBLIC_ORG}`);
    await expect(page.getByTestId("public-store-landing-page")).toBeVisible({ timeout: 15000 });
    const offer = page.getByTestId("install-exits-offer");
    if (await offer.isVisible().catch(() => false)) {
      await page.getByTestId("install-exits-dismiss").click();
      await expect(offer).toHaveCount(0);
    }
    await expect(page.getByTestId("public-store-sign-in")).toBeVisible();
  });

  test("Business QR page encodes HTTPS store URL", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await page.route("**/pos-api/**/management/overview**", async (route) =>
      json(route, {
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
    );
    await page.route(`**/api/v1/organizations/${ORG_ID}/public-identity`, async (route) =>
      json(route, {
        publicOrganizationId: PUBLIC_ORG,
        qrPayload: `exits://qr/v1/organization/${PUBLIC_ORG}`,
        displayName: "Kizy Store",
      }),
    );
    await signInAndBindOwner(page);
    await page
      .getByTestId("workspace-destination-manage_business")
      .waitFor({ state: "visible", timeout: 15000 });
    await chooseOwnerManageBusiness(page);
    const overlayDismiss = page.getByRole("button", { name: "Dismiss" });
    if (await overlayDismiss.isVisible().catch(() => false)) {
      await overlayDismiss.click();
    }
    await expect(page.getByTestId("org-essentials-page")).toBeVisible({ timeout: 15000 });
    await clientNavigate(page, "/org/business-qr");
    await expect(page.getByTestId("org-business-store-url")).toContainText(`/store/${PUBLIC_ORG}`);
    const urlText = await page.getByTestId("org-business-store-url").innerText();
    expect(urlText).toMatch(/^https?:\/\//);
    expect(urlText).not.toMatch(/exits:\/\//);
    expect(urlText).not.toMatch(/token/i);
    expect(urlText).not.toContain("@");
  });
});

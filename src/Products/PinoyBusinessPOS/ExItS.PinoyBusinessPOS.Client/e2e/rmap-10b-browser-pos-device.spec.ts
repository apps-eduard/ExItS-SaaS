import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
  chooseOwnerManageBusiness,
  clientNavigate,
} from "./mock-bound-session";

// Prefer manage-business bind for /org/devices; register uses explicit branch select.

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const INSTALL_KEY = "exits.pos-client.installation-device-id.v1";
const FIXED_INSTALL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const DEVICE_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";

type DeviceApiState = {
  authorized: boolean;
  devices: Array<{
    id: string;
    organizationId: string;
    branchId: string;
    installationDeviceId: string;
    friendlyName: string;
    status: string;
    registeredAtUtc: string;
    lastSeenAtUtc: string;
  }>;
  lastRegistrationToken: string | null;
};

async function mockPosDevicesApi(
  page: import("@playwright/test").Page,
  initial?: Partial<DeviceApiState>,
) {
  const state: DeviceApiState = {
    authorized: initial?.authorized ?? false,
    devices: initial?.devices ?? [],
    lastRegistrationToken: initial?.lastRegistrationToken ?? null,
  };

  await page.route("**/platform-api/**/pos-devices**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/pos-devices/authorize") && method === "POST") {
      if (!state.authorized) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "This POS installation is not registered.",
            errorCode: "application.pos_device.not_authorized",
          }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          posDeviceId: DEVICE_ID,
          branchId: E2E_BRANCH_ID,
          installationDeviceId: FIXED_INSTALL_ID,
        }),
      });
    }

    if (url.includes("/pos-devices/registration-tokens/redeem") && method === "POST") {
      const body = route.request().postDataJSON() as {
        token?: string;
        branchId?: string;
        installationDeviceId?: string;
        friendlyName?: string;
      };
      if (!state.lastRegistrationToken || body.token !== state.lastRegistrationToken) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Registration token was not found.",
            errorCode: "application.pos_device.registration_token.not_found",
          }),
        });
      }
      const device = {
        id: DEVICE_ID,
        organizationId: E2E_ORG_ID,
        branchId: body.branchId ?? E2E_BRANCH_ID,
        installationDeviceId: body.installationDeviceId ?? FIXED_INSTALL_ID,
        friendlyName: body.friendlyName ?? "Redeemed browser",
        status: "Active",
        registeredAtUtc: "2026-08-21T01:00:00Z",
        lastSeenAtUtc: "2026-08-21T01:00:00Z",
      };
      state.devices = [device];
      state.authorized = true;
      state.lastRegistrationToken = null;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(device),
      });
    }

    if (url.includes("/pos-devices/registration-tokens") && method === "POST") {
      state.lastRegistrationToken = "e2e-registration-token-one-time";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          id: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          organizationId: E2E_ORG_ID,
          token: state.lastRegistrationToken,
          qrPayload: `exits://qr/v1/pos-device-registration/${state.lastRegistrationToken}`,
          createdAtUtc: "2026-08-21T01:00:00Z",
          expiresAtUtc: "2026-08-21T01:15:00Z",
          status: "Active",
          expiresInMinutes: 15,
        }),
      });
    }

    if (url.includes("/pos-devices/register") && method === "POST") {
      const body = route.request().postDataJSON() as {
        branchId?: string;
        installationDeviceId?: string;
        friendlyName?: string;
      };
      const device = {
        id: DEVICE_ID,
        organizationId: E2E_ORG_ID,
        branchId: body.branchId ?? E2E_BRANCH_ID,
        installationDeviceId: body.installationDeviceId ?? FIXED_INSTALL_ID,
        friendlyName: body.friendlyName ?? "This browser",
        status: "Active",
        registeredAtUtc: "2026-08-21T01:00:00Z",
        lastSeenAtUtc: "2026-08-21T01:00:00Z",
      };
      state.devices = [device];
      state.authorized = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(device),
      });
    }

    if (url.includes("/pos-devices/") && url.includes("/revoke") && method === "POST") {
      state.devices = state.devices.map((d) =>
        url.includes(d.id) ? { ...d, status: "Revoked" } : d,
      );
      state.authorized = false;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.devices[0] ?? {}),
      });
    }

    if (url.includes("/pos-devices/capacity") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ used: state.devices.length, allowed: 5 }),
      });
    }

    if (url.includes("/pos-devices") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.devices),
      });
    }

    return route.fallback();
  });

  return {
    setAuthorized(value: boolean) {
      state.authorized = value;
    },
    getToken() {
      return state.lastRegistrationToken;
    },
  };
}

async function seedInstallationId(page: import("@playwright/test").Page) {
  await page.addInitScript(
    ([key, id]) => {
      window.localStorage.setItem(key, id);
    },
    [INSTALL_KEY, FIXED_INSTALL_ID] as const,
  );
}

test.describe("RMAP-10b browser POS device", () => {
  test.use({ serviceWorkers: "block" });

  test("owner can register this browser and see authorized status", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await mockPosDevicesApi(page);
    await signInAndBindOwner(page);
    await chooseOwnerManageBusiness(page);
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await page.getByTestId("open-org-devices").click();
    await expect(page.getByTestId("org-devices-page")).toBeVisible();

    await page.getByTestId("devices-branch-select").selectOption(E2E_BRANCH_ID);
    await page.getByTestId("devices-register-browser").click();
    await expect(page.getByTestId("devices-status")).toContainText("authorized", {
      timeout: 10000,
    });
    await expect(page.getByTestId(`device-row-${DEVICE_ID}`)).toBeVisible();
  });

  test("owner can create registration code and staff can redeem without camera", async ({
    page,
  }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    const devicesApi = await mockPosDevicesApi(page);
    await signInAndBindOwner(page);
    await chooseOwnerManageBusiness(page);
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await page.getByTestId("open-org-devices").click();
    await expect(page.getByTestId("org-devices-page")).toBeVisible();
    await page.getByTestId("devices-create-code").click();
    await expect(page.getByTestId("devices-created-code")).toContainText(
      "e2e-registration-token-one-time",
    );
    expect(devicesApi.getToken()).toBe("e2e-registration-token-one-time");

    await mockBoundCashierSession(page);
    await mockPosDevicesApi(page, {
      lastRegistrationToken: "e2e-registration-token-one-time",
    });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/devices/register");
    await expect(page.getByTestId("device-register-page")).toBeVisible();
    await page.getByTestId("device-redeem-code").fill("e2e-registration-token-one-time");
    await page.getByTestId("device-redeem-branch").selectOption(E2E_BRANCH_ID);
    await page.getByTestId("device-redeem-submit").click();
    await expect(page.getByTestId("device-redeem-success")).toBeVisible();
  });

  test("installation id survives logout simulation", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockPosDevicesApi(page);
    await signInAndBindCashier(page);

    const before = await page.evaluate((key) => localStorage.getItem(key), INSTALL_KEY);
    expect(before).toBe(FIXED_INSTALL_ID);

    await page.evaluate(() => {
      sessionStorage.clear();
    });
    const after = await page.evaluate((key) => localStorage.getItem(key), INSTALL_KEY);
    expect(after).toBe(FIXED_INSTALL_ID);
  });

  test("unauthorized device keeps sell pay disabled", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockPosDevicesApi(page, { authorized: false });
    await page.route("**/pos-api/**", async (route) => {
      const url = route.request().url();
      if (url.includes("/cashier-shifts/current")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            shiftId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
            organizationId: E2E_ORG_ID,
            shiftNumber: "S-1",
            status: "Open",
            actorId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
            registerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            registerCode: "REG-1",
            registerName: "Front",
            businessDate: "2026-08-21",
            openingCashAmount: 100,
            openingCashCounted: true,
            effectiveCashCountMode: "Required",
            openedAtUtc: "2026-08-21T01:00:00Z",
            openedBy: "dddddddd-dddd-dddd-dddd-dddddddddddd",
            createdAtUtc: "2026-08-21T01:00:00Z",
            updatedAtUtc: "2026-08-21T01:00:00Z",
          }),
        });
      }
      if (url.includes("/products")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ items: [], totalCount: 0 }),
        });
      }
      return route.fallback();
    });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("sell-pay").first()).toBeDisabled();
  });

  for (const viewport of VIEWPORTS) {
    test(`org devices responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await seedInstallationId(page);
      await mockBoundOwnerSession(page);
      await mockPosDevicesApi(page, {
        authorized: true,
        devices: [
          {
            id: DEVICE_ID,
            organizationId: E2E_ORG_ID,
            branchId: E2E_BRANCH_ID,
            installationDeviceId: FIXED_INSTALL_ID,
            friendlyName: "Front browser",
            status: "Active",
            registeredAtUtc: "2026-08-21T01:00:00Z",
            lastSeenAtUtc: "2026-08-21T01:00:00Z",
          },
        ],
      });
      await signInAndBindOwner(page);
      await chooseOwnerManageBusiness(page);
      await expect(page.getByTestId("org-essentials-page")).toBeVisible();
      await page.getByTestId("open-org-devices").click();
      await expect(page.getByTestId("org-devices-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

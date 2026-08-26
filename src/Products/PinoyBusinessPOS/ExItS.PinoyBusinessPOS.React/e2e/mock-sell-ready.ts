import type { Page } from "@playwright/test";
import { E2E_BRANCH_ID } from "./mock-bound-session";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";

export const INSTALL_KEY = "exits.pos-client.installation-device-id.v1";
export const FIXED_INSTALL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
export const DEVICE_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";

/** Seed durable installation id before first navigation. */
export async function seedInstallationId(page: Page, installId = FIXED_INSTALL_ID) {
  await page.addInitScript(
    ([key, id]) => {
      window.localStorage.setItem(key, id);
    },
    [INSTALL_KEY, installId] as const,
  );
}

/** Mock Platform authorize so SellReadinessGate can pass the device step. */
export async function mockAuthorizedPosDevice(page: Page, installId = FIXED_INSTALL_ID) {
  await page.route("**/platform-api/**/pos-devices/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/pos-devices/authorize") && method === "POST") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          posDeviceId: DEVICE_ID,
          branchId: E2E_BRANCH_ID,
          installationDeviceId: installId,
        }),
      });
    }
    return route.fallback();
  });
}

export type PrepareSellReadyOptions = {
  openShift?: boolean;
};

/**
 * Device + open shift fixtures required to enter SellFloor after UX REPAIR 01.
 * Call before signInAndBind*.
 */
export async function prepareSellReady(page: Page, opts: PrepareSellReadyOptions = {}) {
  const openShift = opts.openShift ?? true;
  await seedInstallationId(page);
  await mockAuthorizedPosDevice(page);
  const shiftApi = await mockPosRegisterShiftApi(page, { openShift });
  return shiftApi;
}

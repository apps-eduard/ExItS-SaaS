import { expect, test } from "@playwright/test";
import { webcrypto } from "node:crypto";
import {
  DEVICE_ID,
  FIXED_INSTALL_ID,
  mockAuthorizedPosDevice,
  seedInstallationId,
} from "./mock-sell-ready";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  E2E_USER_ID,
  expectSellEntryVisible,
  mockBoundCashierSession,
  signInAndBindCashier,
} from "./mock-bound-session";

const DEV_PRIVATE_KEY_PEM = `-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgJuN+Pa6hk6BZUISu
lodghNrUkSR+VQsjrIW49hJ21dihRANCAASSV3pYY5NEuiiPYCs/ZRXZL6dNW0DJ
8VhI3X4k2jMfgEoBV/n9zUzAIZMsJ6XfzAHR+cz3/VxgoQYquH3GV0Lt
-----END PRIVATE KEY-----`;

const USER = E2E_USER_ID;
const INSTALLATION = FIXED_INSTALL_ID;

async function signGrantCanonical(canonical: string): Promise<string> {
  const pemBody = DEV_PRIVATE_KEY_PEM.replace(/-----[^-]+-----/g, "").replace(/\s/g, "");
  const der = Buffer.from(pemBody, "base64");
  const key = await webcrypto.subtle.importKey(
    "pkcs8",
    der,
    { name: "ECDSA", namedCurve: "P-256" },
    false,
    ["sign"],
  );
  const signature = await webcrypto.subtle.sign(
    { name: "ECDSA", hash: "SHA-256" },
    key,
    new TextEncoder().encode(canonical),
  );
  return Buffer.from(signature).toString("hex");
}

async function buildSignedGrantDto() {
  const grantId = "33333333-3333-4333-8333-333333333333";
  const issuedAtUtc = "2026-01-01T12:00:00.000Z";
  const expiresAtUtc = "2030-01-01T12:00:00.000Z";
  const canonical = [
    "v1",
    grantId,
    "4",
    USER,
    "0",
    E2E_ORG_ID,
    "Kizy Store",
    E2E_BRANCH_ID,
    "Main Branch",
    INSTALLATION,
    DEVICE_ID,
    "Cashier",
    "Kizy Uy",
    "kizy",
    String(Math.floor(Date.parse(issuedAtUtc) / 1000)),
    String(Math.floor(Date.parse(issuedAtUtc) / 1000)),
    String(Math.floor(Date.parse(expiresAtUtc) / 1000)),
  ].join("|");
  const signature = await signGrantCanonical(canonical);
  return {
    grantId,
    schemaVersion: 4,
    userId: USER,
    scopeKind: "Organization",
    organizationId: E2E_ORG_ID,
    organizationDisplayName: "Kizy Store",
    branchId: E2E_BRANCH_ID,
    branchName: "Main Branch",
    installationDeviceId: INSTALLATION,
    posDeviceId: DEVICE_ID,
    roleCode: "Cashier",
    displayName: "Kizy Uy",
    username: "kizy",
    issuedAtUtc,
    lastOnlineValidatedAtUtc: issuedAtUtc,
    expiresAtUtc,
    signature,
  };
}

test.describe("offline PIN security", () => {
  test("prepare offline, cold restart, wrong PIN, correct PIN, logout unlock prompt", async ({
    page,
    context,
  }) => {
    await seedInstallationId(page);
    const signedGrant = await buildSignedGrantDto();
    await mockBoundCashierSession(page);
    await mockAuthorizedPosDevice(page);

    await page.route("**/pos-api/api/v1/pos/offline-operating-grants", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ grant: signedGrant }),
      });
    });

    await signInAndBindCashier(page);
    await expectSellEntryVisible(page);

    await context.setOffline(true);
    await page.route("**/platform-api/api/v1/platform/auth/me", async (route) => {
      await route.abort("failed");
    });
    await page.reload();

    await expect(page).toHaveURL(/\/offline-pin/, { timeout: 15000 });
    await expect(page.getByTestId("offline-pin-unlock-page")).toBeVisible();

    await page.getByTestId("offline-pin-unlock-input").fill("000000");
    await page.getByTestId("offline-pin-unlock-submit").click();
    await expect(page.getByTestId("offline-pin-unlock-page")).toBeVisible();

    await page.getByTestId("offline-pin-unlock-input").fill("123456");
    await page.getByTestId("offline-pin-unlock-submit").click();
    await expect(page).not.toHaveURL(/\/offline-pin$/);

    await context.setOffline(false);
    await page.unroute("**/platform-api/api/v1/platform/auth/me");
    await mockBoundCashierSession(page);

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    await expect(page.getByTestId("offline-pin-unlock-page")).toBeVisible();

    await context.setOffline(true);
    await page.reload();
    await expect(page.getByTestId("offline-pin-unlock-page")).toBeVisible();
  });
});

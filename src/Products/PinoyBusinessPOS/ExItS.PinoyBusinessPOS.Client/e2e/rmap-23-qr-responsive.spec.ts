/**
 * RMAP-23 — Dedicated QR responsive coverage (still-image decode + manual ID fallback).
 * Live browser camera (getUserMedia) is NOT implemented; do not claim LIVE_CAMERA_VERIFIED=YES.
 */
import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_ORG_ID,
  mockBoundOwnerSession,
  mockPersonalSession,
  signInAndBindOwner,
  signInAsPersonal,
  chooseOwnerManageBusiness,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 390, height: 844 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const PERSONAL_PUBLIC_ID = "USR000001";
const PERSONAL_QR_PAYLOAD = `exits://qr/v1/personal/${PERSONAL_PUBLIC_ID}`;
const ORG_PUBLIC_ID = "ORG000001";
const ORG_QR_PAYLOAD = `exits://qr/v1/organization/${ORG_PUBLIC_ID}`;

async function mockPersonalPublicIdentity(page: import("@playwright/test").Page) {
  await page.route("**/platform-api/**/me/public-identity**", async (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        publicUserId: PERSONAL_PUBLIC_ID,
        displayName: "Paul Personal",
        qrPayload: PERSONAL_QR_PAYLOAD,
      }),
    }),
  );
}

async function mockOrganizationPublicIdentity(page: import("@playwright/test").Page) {
  await page.route(`**/platform-api/**/organizations/${E2E_ORG_ID}/public-identity**`, async (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        organizationId: E2E_ORG_ID,
        publicOrganizationId: ORG_PUBLIC_ID,
        displayName: "Demo Store",
        qrPayload: ORG_QR_PAYLOAD,
      }),
    }),
  );
}

test.describe("RMAP-23 QR responsive surfaces", () => {
  for (const viewport of VIEWPORTS) {
    test(`Personal My QR ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await mockPersonalSession(page);
      await mockPersonalPublicIdentity(page);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await signInAsPersonal(page);
      await page.goto("/personal/my-qr");
      await expect(page.getByTestId("personal-my-qr-page")).toBeVisible();
      await expect(page.getByTestId("personal-my-qr-image")).toBeVisible();
      await expect(page.getByTestId("personal-public-id")).toHaveText(PERSONAL_PUBLIC_ID);
      await assertNoHorizontalOverflow(page);
    });
  }

  for (const viewport of VIEWPORTS) {
    test(`Business QR ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await mockBoundOwnerSession(page);
      await mockOrganizationPublicIdentity(page);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await signInAndBindOwner(page);
      await page
        .getByTestId("workspace-destination-manage_business")
        .waitFor({ state: "visible", timeout: 15000 });
      await chooseOwnerManageBusiness(page);
      await page.getByTestId("org-essentials-page").waitFor({ state: "visible", timeout: 15000 });
      await page.getByTestId("open-business-qr").click();
      await expect(page.getByTestId("org-business-qr-page")).toBeVisible();
      await expect(page.getByTestId("org-business-qr-image")).toBeVisible();
      await expect(page.getByTestId("org-business-qr-name")).toContainText("Demo Store");
      await assertNoHorizontalOverflow(page);
    });
  }
});

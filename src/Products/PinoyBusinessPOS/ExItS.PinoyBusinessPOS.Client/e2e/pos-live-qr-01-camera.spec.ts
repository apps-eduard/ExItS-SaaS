/**
 * POS-LIVE-QR-01 — Live browser camera QR scanning (mocked MediaDevices + decode harness).
 * Does NOT claim physical camera verification.
 */
import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow, chooseExitsCustomerCreate } from "./helpers";
import { mockBoundOwnerSession, signInAndBindOwner, clientNavigate } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const PERSONAL_PUBLIC_ID = "EX-4827-1936";
const PERSONAL_QR_PAYLOAD = `exits://qr/v1/personal/${PERSONAL_PUBLIC_ID}`;
const ORG_QR_PAYLOAD = "exits://qr/v1/organization/ORG000123";

async function installMockCamera(
  page: import("@playwright/test").Page,
  options: { deny?: boolean; decodePayload?: string | null } = {},
) {
  await page.addInitScript(({ deny, decodePayload }) => {
    HTMLVideoElement.prototype.play = function play() {
      return Promise.resolve();
    };

    if (!navigator.mediaDevices) {
      Object.defineProperty(navigator, "mediaDevices", {
        configurable: true,
        value: {},
      });
    }

    navigator.mediaDevices.getUserMedia = async () => {
      if (deny) {
        throw new DOMException("denied", "NotAllowedError");
      }
      (window as unknown as { __EXITS_E2E_CAMERA_STOPPED__?: boolean }).__EXITS_E2E_CAMERA_STOPPED__ =
        false;

      const canvas = document.createElement("canvas");
      canvas.width = 2;
      canvas.height = 2;
      const ctx = canvas.getContext("2d");
      ctx?.fillRect(0, 0, 2, 2);
      const stream = canvas.captureStream(1);
      const track = stream.getVideoTracks()[0];
      if (track) {
        const nativeStop = track.stop.bind(track);
        track.stop = () => {
          nativeStop();
          (window as unknown as { __EXITS_E2E_CAMERA_STOPPED__?: boolean }).__EXITS_E2E_CAMERA_STOPPED__ =
            true;
        };
      }
      return stream;
    };

    if (decodePayload) {
      (
        window as unknown as {
          __EXITS_LIVE_QR_DECODE__?: () => Promise<string>;
        }
      ).__EXITS_LIVE_QR_DECODE__ = async () => decodePayload;
    }
  }, options);
}

async function signInOwnerOperations(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-operations")
    .waitFor({ state: "visible", timeout: 15000 });
  await page.getByTestId("workspace-destination-operations").click();
  await expect(page.getByTestId("open-customers")).toBeVisible({ timeout: 15000 });
}

async function openCustomerLinkScanner(
  page: import("@playwright/test").Page,
  options: { mockResolve?: boolean } = {},
) {
  await mockBoundOwnerSession(page);
  if (options.mockResolve) {
    await mockPersonalResolve(page);
  }
  await signInOwnerOperations(page);
  await clientNavigate(page, "/customers/new");
  await expect(page.getByTestId("customer-form-page")).toBeVisible();
  await chooseExitsCustomerCreate(page);
  await expect(page.getByTestId("customer-personal-link-panel")).toBeVisible();
}

async function mockPersonalResolve(page: import("@playwright/test").Page) {
  await page.route("**/platform-api/**/resolve-public-id", async (route) => {
    if (route.request().method() !== "POST") {
      return route.fallback();
    }
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        publicUserId: PERSONAL_PUBLIC_ID,
        displayName: "Paul Personal",
        userIdentityId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        maskedEmail: null,
        status: "Active",
        isSelf: false,
      }),
    });
  });
}

test.describe("POS-LIVE-QR-01 mocked live camera", () => {
  test("opens camera and shows live preview", async ({ page }) => {
    await installMockCamera(page);
    await openCustomerLinkScanner(page);

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();

    await expect(page.getByTestId("live-qr-preview")).toBeVisible();
    await expect(page.getByTestId("live-qr-requesting")).toHaveCount(0);
  });

  test("decodes Personal QR and requires explicit link confirmation", async ({ page }) => {
    await installMockCamera(page, { decodePayload: PERSONAL_QR_PAYLOAD });
    await openCustomerLinkScanner(page, { mockResolve: true });

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();
    await expect(page.getByTestId("live-qr-preview")).toBeVisible();

    await expect(page.getByTestId("customer-personal-link-confirm")).toBeVisible({
      timeout: 5000,
    });
    await expect(page.getByText(PERSONAL_PUBLIC_ID)).toBeVisible();
    await expect(page.getByTestId("customer-personal-link-confirm-btn")).toBeVisible();
    await expect(page.getByTestId("customer-personal-link-sent")).toHaveCount(0);
  });

  test("shows wrong-purpose message for Organization QR", async ({ page }) => {
    await installMockCamera(page, { decodePayload: ORG_QR_PAYLOAD });
    await openCustomerLinkScanner(page);

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();

    await expect(page.getByTestId("live-qr-inline-error")).toContainText("can't be used here");
    await expect(page.getByTestId("live-qr-preview")).toBeVisible();
  });

  test("shows permission denied with fallback controls", async ({ page }) => {
    await installMockCamera(page, { deny: true });
    await openCustomerLinkScanner(page);

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();

    await expect(page.getByText("Camera access is blocked.")).toBeVisible();
    await expect(page.getByTestId("live-qr-upload-fallback")).toBeVisible();
    await expect(page.getByTestId("live-qr-manual-fallback")).toBeVisible();
  });

  test("closing scanner stops mocked camera tracks", async ({ page }) => {
    await installMockCamera(page);
    await openCustomerLinkScanner(page);

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();
    await expect(page.getByTestId("live-qr-preview")).toBeVisible();
    await page.getByRole("button", { name: "Close" }).click();

    await expect.poll(async () =>
      page.evaluate(() => (window as unknown as { __EXITS_E2E_CAMERA_STOPPED__?: boolean }).__EXITS_E2E_CAMERA_STOPPED__),
    ).toBe(true);
  });

  test("responsive phone scanner layout 375x812", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await installMockCamera(page);
    await openCustomerLinkScanner(page);

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();
    await expect(page.getByTestId("live-qr-preview")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test("responsive tablet scanner layout 768x1024", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await installMockCamera(page);
    await openCustomerLinkScanner(page);

    await page.getByTestId("qr-live-camera-button").click();
    await page.getByTestId("live-qr-open-camera").click();
    await expect(page.getByTestId("live-qr-preview")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });
});

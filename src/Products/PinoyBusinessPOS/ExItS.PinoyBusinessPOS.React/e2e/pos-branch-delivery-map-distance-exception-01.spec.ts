/**
 * POS-BRANCH-DELIVERY-MAP-AND-CUSTOMER-DISTANCE-EXCEPTION-01 — live MAP/DIST flows.
 * Skips when LocalValidation Platform :8091 / Vite :5177 / pilot email are unavailable.
 */
import { expect, test, type Page } from "@playwright/test";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { completeOfflinePinSetupIfNeeded, clientNavigate } from "./mock-bound-session";
import { readLocalValidationSharedPassword } from "./helpers/local-validation-password";

const BRANCH_ID = "70fddbbb-0208-4be9-a543-426f1b217bfc";
const APP = "http://127.0.0.1:5177";

function readPilotOwnerEmail(): string | null {
  let dir = path.dirname(fileURLToPath(import.meta.url));
  for (let i = 0; i < 12; i += 1) {
    const candidate = path.join(dir, ".tmp-pilot-email.txt");
    if (existsSync(candidate)) {
      const email = readFileSync(candidate, "utf8").trim();
      return email.length > 0 ? email : null;
    }
    if (existsSync(path.join(dir, "ExItS.slnx"))) {
      return null;
    }
    dir = path.dirname(dir);
  }
  return null;
}

async function requireLiveApis(request: import("@playwright/test").APIRequestContext) {
  const platform = await request.get("http://127.0.0.1:8091/health").catch(() => null);
  test.skip(!platform || !platform.ok(), "Platform API :8091 not reachable");
  const vite = await request.get(`${APP}/`).catch(() => null);
  test.skip(!vite || !vite.ok(), "Vite :5177 required");
  const email = readPilotOwnerEmail();
  test.skip(!email, "No .tmp-pilot-email.txt");
  return { email: email!, password: readLocalValidationSharedPassword() };
}

async function bindOwnerManageBusiness(page: Page) {
  const manage = page.getByTestId("workspace-destination-manage_business");
  const orgPage = page.getByTestId("org-essentials-page");
  if (await orgPage.isVisible().catch(() => false)) {
    return;
  }
  await expect(manage).toBeVisible({ timeout: 45000 });
  await manage.click();
  await expect(orgPage).toBeVisible({ timeout: 45000 });
}

async function signInOwner(page: Page, email: string, password: string) {
  await page.goto(`${APP}/sign-in`);
  await expect(page.getByTestId("sign-in-page")).toBeVisible({ timeout: 15000 });
  await page.getByLabel(/Email or staff login|Email or username/i).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByTestId("sign-in-submit").click();
  await expect.poll(async () => page.url(), { timeout: 90000 }).not.toMatch(/\/sign-in/);
  await completeOfflinePinSetupIfNeeded(page);
  await bindOwnerManageBusiness(page);
}

test.describe("POS-BRANCH-DELIVERY-MAP-AND-CUSTOMER-DISTANCE-EXCEPTION-01", () => {
  test("MAP-01/02/04 location picker and isolated Save location", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInOwner(page, email, password);
    await clientNavigate(page, `/org/branches/${BRANCH_ID}`);
    await expect(page.getByTestId("branch-setup-tabs")).toBeVisible({ timeout: 30000 });
    await page.getByTestId("branch-tab-location").click();
    await expect(page.getByTestId("branch-map-section")).toBeVisible();
    await expect(page.getByTestId("branch-choose-on-map")).toBeVisible();

    const choose = page.getByTestId("branch-choose-on-map");
    if (await choose.isEnabled()) {
      await choose.click();
      await expect(page.getByTestId("branch-map-picker")).toBeVisible({ timeout: 15000 });
      await page.getByTestId("branch-map-picker-cancel").click();
    }

    await page.getByTestId("branch-latitude").fill("10.676500");
    await page.getByTestId("branch-longitude").fill("122.950900");
    await page.getByTestId("branch-save").click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
    await expect(page.getByTestId("branch-fulfillment-error")).toHaveCount(0);
  });

  test("DIST-01/02 customer delivery exception toggle", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInOwner(page, email, password);
    await clientNavigate(page, "/customers");
    await expect(page.getByTestId("customers-list-page")).toBeVisible({ timeout: 30000 });
    const first = page.locator("[data-testid^=customer-row-]").first();
    test.skip((await first.count()) === 0, "No people customers in pilot");
    await first.click();
    await expect(page.getByTestId("customer-detail-page")).toBeVisible({ timeout: 20000 });
    const section = page.getByTestId("customer-delivery-section");
    if ((await section.count()) === 0) {
      test.skip(true, "Customer has no Platform BusinessCustomer correlation");
      return;
    }
    await expect(section).toBeVisible();
    const toggle = page.getByTestId("customer-delivery-distance-exception");
    await expect(toggle).toBeVisible();
  });
});

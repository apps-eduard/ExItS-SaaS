/**
 * POS-PH-PSGC-DELIVERY-AREAS-01 — live authenticated PSGC delivery-area UI evidence.
 * Requires LocalValidation Platform :8091, POS :8092, Vite :5177, Joe store pilot.
 */
import { expect, test, type Page } from "@playwright/test";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { completeOfflinePinSetupIfNeeded, clientNavigate } from "./mock-bound-session";
import { readLocalValidationSharedPassword } from "./helpers/local-validation-password";

const ORG_ID = "37acc96a-d5f3-4e0c-8233-d104790caf30";
const BRANCH_ID = "70fddbbb-0208-4be9-a543-426f1b217bfc";
const BUYER_EMAIL = "pilot.buyer.767012@exits.local";
const APP = "http://127.0.0.1:5177";
const BACOLOD_PSGC = "1830200000";
const MURCIA_PSGC = "1804520000";

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
  test.skip(!vite || !vite.ok(), "Vite :5177 required for live PSGC UI");
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

async function signInLive(
  page: Page,
  email: string,
  password: string,
  mode: "owner" | "personal",
) {
  await page.goto(`${APP}/sign-in`);
  await expect(page.getByTestId("sign-in-page")).toBeVisible({ timeout: 15000 });
  await page.getByLabel(/Email or staff login|Email or username/i).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByTestId("sign-in-submit").click();
  await expect.poll(async () => page.url(), { timeout: 90000 }).not.toMatch(/\/sign-in/);
  await completeOfflinePinSetupIfNeeded(page);
  if (mode === "owner") {
    await bindOwnerManageBusiness(page);
  }
}

async function openBranchAreas(page: Page) {
  await clientNavigate(page, `/org/branches/${BRANCH_ID}`);
  await expect(page.getByTestId("branch-fulfillment-edit")).toBeVisible({ timeout: 45000 });
  await page.getByTestId("branch-tab-areas").click();
  await expect(page.getByTestId("branch-delivery-areas")).toBeVisible();
}

test.describe("POS-PH-PSGC-DELIVERY-AREAS-01", () => {
  test("PSGC-UI-01 country Philippines read-only", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await openBranchAreas(page);
    await expect(page.getByTestId("delivery-area-country-readonly")).toContainText("Philippines (PH)");
    await expect(page.getByTestId("delivery-area-city")).toHaveCount(0);
    await expect(page.getByTestId("delivery-area-region")).toHaveCount(0);
    await expect(page.getByTestId("delivery-area-country")).toHaveCount(0);
    await expect(page.getByTestId("delivery-area-search")).toBeVisible();
  });

  test("PSGC-UI-02..06 search add multi-chip duplicate remove", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await openBranchAreas(page);

    const list = page.getByTestId("delivery-areas-list");
    const text = (await list.textContent()) ?? "";
    if (!/Bacolod/i.test(text)) {
      await page.getByTestId("delivery-area-search").fill("Bacolod");
      await expect(page.getByTestId(`delivery-area-result-${BACOLOD_PSGC}`)).toBeVisible({
        timeout: 15000,
      });
      await page.getByTestId(`delivery-area-result-${BACOLOD_PSGC}`).click();
      await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
    }
    await expect(list).toContainText(/Bacolod/i);

    if (!/Murcia/i.test((await list.textContent()) ?? "")) {
      await page.getByTestId("delivery-area-search").fill("Murcia");
      await expect(page.getByTestId(`delivery-area-result-${MURCIA_PSGC}`)).toBeVisible({
        timeout: 15000,
      });
      await page.getByTestId(`delivery-area-result-${MURCIA_PSGC}`).click();
      await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
    }
    await expect(list).toContainText(/Murcia/i);
    await expect(page.locator(".branch-area-chip")).toHaveCount(2, { timeout: 5000 }).catch(async () => {
      await expect(page.locator(".branch-area-chip").filter({ hasText: /Bacolod|Murcia/i })).toHaveCount(2);
    });

    await page.getByTestId("delivery-area-search").fill("Bacolod");
    await expect(page.getByTestId(`delivery-area-result-${BACOLOD_PSGC}`)).toBeDisabled({
      timeout: 15000,
    });

    await page.locator(".branch-area-chip").filter({ hasText: /Murcia/i }).getByTestId(/remove-delivery-area-/).click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
    await expect(list).not.toContainText(/Murcia/i);
    await expect(list).toContainText(/Bacolod/i);
  });

  test("PSGC-UI-07 360px search/chips usable", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await page.setViewportSize({ width: 360, height: 800 });
    await openBranchAreas(page);
    await expect(page.getByTestId("delivery-area-search")).toBeVisible();
    await expect(page.getByTestId("delivery-areas-list")).toBeVisible();
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBeFalsy();
  });

  test("PSGC-UI-08 checkout configured Bacolod only", async ({ page, request }) => {
    const { password } = await requireLiveApis(request);
    await signInLive(page, BUYER_EMAIL, password, "personal");
    await clientNavigate(page, `/personal/linked-merchants/${ORG_ID}/shop`);
    await expect(page.getByTestId("merchant-shop-page")).toBeVisible({ timeout: 30000 });
    await page.getByTestId("cart-increment").first().click();
    await page.getByTestId("shop-review").click();
    await expect(page.getByTestId("merchant-checkout-page")).toBeVisible({ timeout: 20000 });
    await page.getByTestId("fulfillment-delivery").click();
    await expect(page.getByTestId("checkout-delivery-area-select")).toContainText(/Bacolod/i);
  });
});

/**
 * POS-BRANCH-FULFILLMENT-UI-CLOSURE-01 — live authenticated UI evidence.
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
  test.skip(!vite || !vite.ok(), "Vite :5177 required for live UI closure");
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

async function openBranchList(page: Page) {
  await clientNavigate(page, "/org/branches");
  await expect(page.getByTestId("branch-fulfillment-list")).toBeVisible({ timeout: 30000 });
  await expect(page.getByTestId(`branch-fulfillment-card-${BRANCH_ID}`)).toBeVisible();
}

async function openBranchEdit(page: Page) {
  const target = `/org/branches/${BRANCH_ID}`;
  const openLink = page.getByTestId(`open-branch-fulfillment-${BRANCH_ID}`);
  if (await openLink.count()) {
    await Promise.all([
      page.waitForURL(new RegExp(`${BRANCH_ID}$`), { timeout: 30000 }),
      openLink.first().click({ force: true }),
    ]);
  } else {
    await clientNavigate(page, target);
  }

  const edit = page.getByTestId("branch-fulfillment-edit");
  try {
    await expect(edit).toBeVisible({ timeout: 20000 });
  } catch {
    // Soft recovery: re-bind Manage Business then navigate again (viewport/session edge).
    await page.goto(`${APP}/workspace`);
    await bindOwnerManageBusiness(page);
    await clientNavigate(page, "/org/branches");
    await expect(page.getByTestId("branch-fulfillment-list")).toBeVisible({ timeout: 30000 });
    await page.getByTestId(`open-branch-fulfillment-${BRANCH_ID}`).click({ force: true });
    await expect(edit).toBeVisible({ timeout: 45000 });
  }
  await expect(page.getByTestId("branch-setup-tabs")).toBeVisible({ timeout: 30000 });
}

/** Full reload clears in-memory workspace bind — re-enter Manage business then resume. */
async function reloadAndRebindOwner(page: Page, email: string, password: string) {
  await page.reload();
  await expect.poll(async () => page.url(), { timeout: 60000 }).not.toMatch(/\/sign-in/);
  if (page.url().includes("/sign-in")) {
    await signInLive(page, email, password, "owner");
    return;
  }
  await completeOfflinePinSetupIfNeeded(page);
  if (await page.getByTestId("workspace-destination-manage_business").isVisible().catch(() => false)) {
    await bindOwnerManageBusiness(page);
  } else if (!(await page.getByTestId("org-essentials-page").isVisible().catch(() => false))) {
    await page.goto(`${APP}/workspace`);
    await bindOwnerManageBusiness(page);
  }
}

async function assertNoBodyHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const doc = document.documentElement;
    const body = document.body;
    return Math.max(doc.scrollWidth, body.scrollWidth) - Math.max(doc.clientWidth, body.clientWidth);
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

async function waitSwitchIdle(switchLocator: import("@playwright/test").Locator) {
  await expect(switchLocator).not.toHaveAttribute("aria-busy", "true", { timeout: 20000 });
  await expect(switchLocator).toBeEnabled({ timeout: 20000 });
}

test.describe("POS-BRANCH-FULFILLMENT-UI-CLOSURE-01 live UI", () => {
  test.use({ serviceWorkers: "block" });
  test.describe.configure({ mode: "serial" });

  test("FUL-UI-01 branch list switches independent from navigation", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await openBranchList(page);

    const card = page.getByTestId(`branch-fulfillment-card-${BRANCH_ID}`);
    await expect(card.getByText("Main Branch")).toBeVisible();
    await expect(card.getByText("Active")).toBeVisible();

    const pickup = page.getByTestId(`pickup-switch-${BRANCH_ID}`);
    const delivery = page.getByTestId(`delivery-switch-${BRANCH_ID}`);
    await expect(pickup).toBeVisible();
    await expect(delivery).toBeVisible();
    await expect(card.getByText("Active")).toBeVisible();
    const toggles = page.getByTestId(`branch-toggles-${BRANCH_ID}`);
    await expect(toggles.getByText("Pickup", { exact: true })).toBeVisible();
    await expect(toggles.getByText("Delivery", { exact: true })).toBeVisible();

    // Ensure known starting state ON/ON via API if needed
    if ((await pickup.getAttribute("aria-checked")) !== "true") {
      await pickup.click();
      await expect(pickup).toHaveAttribute("aria-checked", "true", { timeout: 15000 });
      await waitSwitchIdle(pickup);
    }
    if ((await delivery.getAttribute("aria-checked")) !== "true") {
      await delivery.click();
      await expect(delivery).toHaveAttribute("aria-checked", "true", { timeout: 15000 });
      await waitSwitchIdle(delivery);
    }

    // A/B — toggle pickup only
    await pickup.click();
    await expect(pickup).toHaveAttribute("aria-checked", "false", { timeout: 15000 });
    await waitSwitchIdle(pickup);
    await expect(delivery).toHaveAttribute("aria-checked", "true");
    expect(page.url()).toContain("/org/branches");
    expect(page.url()).not.toContain(BRANCH_ID);

    // Restore pickup ON
    await pickup.click();
    await expect(pickup).toHaveAttribute("aria-checked", "true", { timeout: 15000 });
    await waitSwitchIdle(pickup);

    // B — toggle delivery only
    await delivery.click();
    await expect(delivery).toHaveAttribute("aria-checked", "false", { timeout: 15000 });
    await waitSwitchIdle(delivery);
    await expect(pickup).toHaveAttribute("aria-checked", "true");
    expect(page.url()).not.toContain(BRANCH_ID);

    // H — OFF always possible (already OFF); restore ON
    await delivery.click();
    await expect(delivery).toHaveAttribute("aria-checked", "true", { timeout: 15000 });
    await waitSwitchIdle(delivery);

    // E — keyboard focus reaches switches separately from nav link
    await pickup.focus();
    await expect(pickup).toBeFocused();
    await delivery.focus();
    await expect(delivery).toBeFocused();
    const nav = page.getByTestId(`open-branch-fulfillment-${BRANCH_ID}`);
    await nav.focus();
    await expect(nav).toBeFocused();

    // F — pending state prevents duplicate mutations
    let putCount = 0;
    await page.route("**/fulfillment-settings", async (route) => {
      if (route.request().method() === "PUT") {
        putCount += 1;
        await new Promise((r) => setTimeout(r, 1200));
      }
      await route.continue();
    });
    await pickup.click();
    await expect(pickup).toHaveAttribute("aria-busy", "true");
    await expect(pickup).toBeDisabled();
    await pickup.click({ force: true });
    await pickup.click({ force: true });
    await expect(pickup).toHaveAttribute("aria-checked", "false", { timeout: 15000 });
    await waitSwitchIdle(pickup);
    expect(putCount).toBe(1);
    await page.unroute("**/fulfillment-settings");
    await pickup.click();
    await expect(pickup).toHaveAttribute("aria-checked", "true", { timeout: 15000 });
    await waitSwitchIdle(pickup);

    // G — rejected mutation does not leave false ON
    await page.route("**/fulfillment-settings", async (route) => {
      if (route.request().method() === "PUT") {
        return route.fulfill({
          status: 409,
          contentType: "application/problem+json",
          body: JSON.stringify({
            title: "Conflict",
            detail: "Simulated rejection for UI closure",
            status: 409,
          }),
        });
      }
      return route.continue();
    });
    const deliveryCheckedBeforeReject = await delivery.getAttribute("aria-checked");
    await delivery.click();
    await expect(page.getByTestId("branch-list-toggle-error")).toBeVisible({ timeout: 15000 });
    await expect(delivery).toHaveAttribute("aria-checked", deliveryCheckedBeforeReject!);
    await page.unroute("**/fulfillment-settings");

    // D — navigation works; C — switches did not navigate
    await nav.click();
    await expect(page.getByTestId("branch-fulfillment-edit")).toBeVisible({ timeout: 15000 });
    expect(page.url()).toContain(`/org/branches/${BRANCH_ID}`);
  });

  test("FUL-UI-02 setup tabs and checkmarks", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await openBranchEdit(page);

    await expect(page.getByTestId("branch-setup-tabs")).toBeVisible();
    const tabs = [
      ["branch-tab-overview", "branch-readiness-panel"],
      ["branch-tab-details", "branch-details-tab"],
      ["branch-tab-hours", "branch-hours-section"],
      ["branch-tab-location", "branch-map-section"],
      ["branch-tab-policy", "branch-delivery-policy"],
      ["branch-tab-areas", "branch-delivery-areas"],
    ] as const;

    for (const [tabId, panelId] of tabs) {
      await page.getByTestId(tabId).click();
      await expect(page.getByTestId(tabId)).toHaveAttribute("aria-selected", "true");
      await expect(page.getByTestId(panelId)).toBeVisible();
    }

    await page.getByTestId("branch-tab-overview").click();
    await expect(page.getByTestId("branch-setup-progress")).toBeVisible();
    await expect(page.getByTestId("pickup-progress")).toContainText(/2\s*(of|\/)\s*2/i);
    await expect(page.getByTestId("delivery-progress")).toContainText(/5\s*(of|\/)\s*5/i);
    await expect(page.getByTestId("pickup-status")).toBeVisible();
    await expect(page.getByTestId("delivery-status")).toBeVisible();

    // Completed section checkmarks present on configured tabs
    for (const tabId of [
      "branch-tab-details",
      "branch-tab-hours",
      "branch-tab-location",
      "branch-tab-policy",
      "branch-tab-areas",
    ]) {
      const icon = page.getByTestId(tabId).locator("svg").first();
      await expect(icon).toBeVisible();
    }
  });

  test("FUL-UI-03 coordinate-only save preserves address", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await openBranchEdit(page);

    await page.getByTestId("branch-tab-details").click();
    const address1 = await page.getByTestId("branch-address1").inputValue();
    const city = await page.getByTestId("branch-city").inputValue();
    const region = await page.getByTestId("branch-region").inputValue();
    const postal = await page.getByTestId("branch-postal").inputValue();
    const country = await page.getByTestId("branch-country").inputValue();
    expect(address1.length).toBeGreaterThan(0);
    expect(city.length).toBeGreaterThan(0);

    await page.getByTestId("branch-tab-location").click();
    const lat = await page.getByTestId("branch-latitude").inputValue();
    const lng = await page.getByTestId("branch-longitude").inputValue();
    const nextLat = (Number(lat) + 0.0001).toFixed(4);
    const nextLng = (Number(lng) + 0.0001).toFixed(4);
    await page.getByTestId("branch-latitude").fill(nextLat);
    await page.getByTestId("branch-longitude").fill(nextLng);
    await page.getByTestId("branch-save").click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });

    await reloadAndRebindOwner(page, email, password);
    await openBranchEdit(page);
    await page.getByTestId("branch-tab-details").click();
    await expect(page.getByTestId("branch-address1")).toHaveValue(address1);
    await expect(page.getByTestId("branch-city")).toHaveValue(city);
    await expect(page.getByTestId("branch-region")).toHaveValue(region);
    await expect(page.getByTestId("branch-postal")).toHaveValue(postal);
    await expect(page.getByTestId("branch-country")).toHaveValue(country);

    await page.getByTestId("branch-tab-overview").click();
    await expect(page.getByTestId("delivery-progress")).toContainText(/5\s*(of|\/)\s*5/i);

    // Restore prior coordinates for stable pilot state
    await page.getByTestId("branch-tab-location").click();
    await page.getByTestId("branch-latitude").fill(lat);
    await page.getByTestId("branch-longitude").fill(lng);
    await page.getByTestId("branch-save").click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
  });

  test("FUL-UI-04 delivery area add / duplicate / remove via PSGC", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");
    await openBranchEdit(page);
    await page.getByTestId("branch-tab-areas").click();
    await expect(page.getByTestId("branch-delivery-areas")).toBeVisible();
    await expect(page.getByTestId("delivery-area-country-readonly")).toContainText("Philippines (PH)");
    await expect(page.getByTestId("delivery-areas-list")).toContainText(/Bacolod/i);

    // Add Murcia municipality (second official locality)
    await page.getByTestId("delivery-area-search").fill("Murcia");
    await expect(page.getByTestId("delivery-area-result-1804520000")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("delivery-area-result-1804520000").click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
    await expect(page.getByTestId("delivery-areas-list")).toContainText(/Murcia/i);

    await reloadAndRebindOwner(page, email, password);
    await openBranchEdit(page);
    await page.getByTestId("branch-tab-areas").click();
    await expect(page.getByTestId("delivery-areas-list")).toContainText(/Murcia/i);
    await expect(page.getByTestId("delivery-areas-list")).toContainText(/Bacolod/i);

    // Duplicate Bacolod should be disabled / already added
    await page.getByTestId("delivery-area-search").fill("Bacolod");
    await expect(page.getByTestId("delivery-area-result-1830200000")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("delivery-area-result-1830200000")).toBeDisabled();

    const murciaChip = page.locator(".branch-area-chip").filter({ hasText: /Murcia/i });
    await murciaChip.getByTestId(/remove-delivery-area-/).click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible({ timeout: 20000 });
    await expect(page.getByTestId("delivery-areas-list")).not.toContainText(/Murcia/i);
    await expect(page.getByTestId("delivery-areas-list")).toContainText(/Bacolod/i);
  });

  test("FUL-UI-05 personal checkout delivery area selector", async ({ page, request }) => {
    const { password } = await requireLiveApis(request);
    await signInLive(page, BUYER_EMAIL, password, "personal");

    await page.goto(`${APP}/personal/linked-merchants`);
    await expect(page.getByTestId("linked-merchants-page")).toBeVisible({ timeout: 30000 });
    const shop = page.getByTestId("open-merchant-shop").first();
    if (await shop.isVisible().catch(() => false)) {
      await shop.click();
    } else {
      await clientNavigate(page, `/personal/linked-merchants/${ORG_ID}/shop`);
    }
    await expect(page.getByTestId("merchant-shop-page")).toBeVisible({ timeout: 30000 });

    const increment = page.getByTestId("cart-increment").first();
    await expect(increment).toBeVisible({ timeout: 20000 });
    await increment.click();
    await page.getByTestId("shop-review").click();
    await expect(page.getByTestId("merchant-checkout-page")).toBeVisible({ timeout: 20000 });

    await expect(page.getByTestId("fulfillment-pickup")).toBeVisible();
    await page.getByTestId("fulfillment-pickup").click();
    await expect(page.getByTestId("checkout-delivery-area-select")).toHaveCount(0);

    await page.getByTestId("fulfillment-delivery").click();
    await expect(page.getByTestId("delivery-fields")).toBeVisible();
    await expect(page.getByTestId("checkout-delivery-area-select")).toBeVisible();
    await expect(page.getByTestId("checkout-delivery-area-select")).toContainText("Bacolod City");
    await expect(page.getByTestId("delivery-recipient")).toBeVisible();
    await expect(page.getByTestId("delivery-address")).toBeVisible();
    await expect(page.getByTestId("delivery-lat")).toBeVisible();
    await expect(page.getByTestId("delivery-lng")).toBeVisible();

    await page.getByTestId("delivery-recipient").fill("UI Closure Buyer");
    await page.getByTestId("delivery-address").fill("Near Plaza");
    // Branch coords are inside Bacolod service radius
    await page.getByTestId("delivery-lat").fill("10.6765");
    await page.getByTestId("delivery-lng").fill("122.9509");
    await expect(page.getByTestId("delivery-fee-quote")).toBeVisible({ timeout: 20000 });
    await expect(page.getByTestId("place-order")).toBeVisible();
  });

  test("FUL-UI-06/07/08 responsive fulfillment pages", async ({ page, request }) => {
    const { email, password } = await requireLiveApis(request);
    await signInLive(page, email, password, "owner");

    const widths = [360, 768, 1440] as const;
    const editTabs = [
      "branch-tab-overview",
      "branch-tab-details",
      "branch-tab-hours",
      "branch-tab-location",
      "branch-tab-policy",
      "branch-tab-areas",
    ] as const;

    for (const width of widths) {
      await page.setViewportSize({ width, height: 900 });

      await openBranchList(page);
      await assertNoBodyHorizontalOverflow(page);
      await expect(page.getByTestId(`pickup-switch-${BRANCH_ID}`)).toBeVisible();
      await expect(page.getByTestId(`delivery-switch-${BRANCH_ID}`)).toBeVisible();
      await expect(page.getByTestId(`open-branch-fulfillment-${BRANCH_ID}`)).toBeVisible();

      await openBranchEdit(page);
      await expect(page.getByTestId("branch-setup-tabs")).toBeVisible();
      for (const tabId of editTabs) {
        await page.getByTestId(tabId).click();
        await assertNoBodyHorizontalOverflow(page);
        const label = page.getByTestId(tabId);
        await expect(label).toBeVisible();
        const box = await label.boundingBox();
        expect(box?.height ?? 0).toBeGreaterThan(0);
      }

      await page.getByTestId("branch-tab-areas").click();
      await expect(page.getByTestId("delivery-areas-list")).toBeVisible();
      await expect(page.getByTestId("delivery-area-search")).toBeVisible();
      await expect(page.getByTestId("delivery-area-country-readonly")).toBeVisible();
      await expect(page.getByTestId("delivery-area-city")).toHaveCount(0);

      await page.getByTestId("branch-tab-location").click();
      await expect(page.getByTestId("branch-latitude")).toBeVisible();
      await expect(page.getByTestId("branch-save")).toBeVisible();
    }

    // Checkout at each width with Personal session
    await page.context().clearCookies();
    await signInLive(page, BUYER_EMAIL, password, "personal");
    for (const width of widths) {
      await page.setViewportSize({ width, height: 900 });
      await clientNavigate(page, `/personal/linked-merchants/${ORG_ID}/shop`);
      await expect(page.getByTestId("merchant-shop-page")).toBeVisible({ timeout: 30000 });
      const increment = page.getByTestId("cart-increment").first();
      if (await increment.isVisible().catch(() => false)) {
        await increment.click();
        await page.getByTestId("shop-review").click();
        await expect(page.getByTestId("merchant-checkout-page")).toBeVisible({ timeout: 20000 });
        await page.getByTestId("fulfillment-delivery").click();
        await expect(page.getByTestId("checkout-delivery-area-select")).toBeVisible();
        await assertNoBodyHorizontalOverflow(page);
        await expect(page.getByTestId("place-order")).toBeVisible();
      }
    }
  });
});

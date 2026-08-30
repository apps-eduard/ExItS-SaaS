import { expect, test, type Page } from "@playwright/test";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { readLocalValidationSharedPassword } from "./helpers/local-validation-password";
import { clientNavigate, mockBoundManagerSession } from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { prepareSellReady } from "./mock-sell-ready";

/**
 * POS-REAL-OPERATOR-SINGLE-STORE-PILOT-01 — React UI operator acceptance (stable subset).
 * Cashier cash + ROP_15: EVIDENCE_REUSED from e2e/rmap-11-checkout-sale.spec.ts and
 * e2e/rmap-02r-role-experience.spec.ts (unchanged production paths).
 */

const ownerPages: Array<{ path: string; label: string }> = [
  { path: "/dashboard", label: "Dashboard" },
  { path: "/catalog", label: "Products" },
  { path: "/inventory", label: "Inventory" },
  { path: "/purchasing/direct-purchases", label: "DirectPurchase" },
  { path: "/purchasing/payables", label: "SupplierCredit" },
  { path: "/reports", label: "Reports" },
  { path: "/customers", label: "Utang" },
  { path: "/inventory/stock-use", label: "StockUse" },
  { path: "/inventory/waste-loss", label: "Waste" },
  { path: "/inventory/stock-counts", label: "StockCount" },
  { path: "/shifts", label: "Shifts" },
];

async function mockEmptyPosGets(page: Page) {
  await page.route("**/pos-api/**", async (route) => {
    if (route.request().method() === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
      });
    }
    return route.fallback();
  });
}

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

async function signInManagerForOperator(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("manager");
  await page.getByLabel("Password", { exact: true }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
  const ops = page.getByTestId("workspace-destination-operations");
  await Promise.race([
    ops.waitFor({ state: "visible", timeout: 20000 }),
    page.getByTestId("open-inventory").waitFor({ state: "visible", timeout: 20000 }),
    page.getByTestId("manager-home").waitFor({ state: "visible", timeout: 20000 }),
    page.getByRole("heading", { name: "Choose workspace" }).waitFor({ state: "visible", timeout: 20000 }),
  ]);
  if (await ops.isVisible().catch(() => false)) {
    await ops.click();
  }
  await page.waitForTimeout(400);
}

test.describe("Real operator single-store UI acceptance", () => {
  test.use({ serviceWorkers: "block" });

  test("ROP_01 live LocalValidation owner sign-in when APIs up", async ({ page, request }) => {
    const email = readPilotOwnerEmail();
    test.skip(!email, "No .tmp-pilot-email.txt — skip live owner login");
    const health = await request.get("http://127.0.0.1:8091/health").catch(() => null);
    test.skip(!health || !health.ok(), "Platform API not reachable");
    const vite = await request.get("http://127.0.0.1:5177/").catch(() => null);
    test.skip(!vite || !vite.ok(), "Vite :5177 required for live proxy login");

    const password = readLocalValidationSharedPassword();
    await page.goto("http://127.0.0.1:5177/sign-in");
    await expect(page.getByTestId("sign-in-page")).toBeVisible({ timeout: 15000 });
    await page.getByLabel(/Email or staff login|Email or username/i).fill(email!);
    await page.getByLabel("Password", { exact: true }).fill(password);
    await page.getByTestId("sign-in-submit").click();

    await expect
      .poll(async () => page.url(), { timeout: 90000 })
      .not.toMatch(/\/sign-in/);
    const url = page.url();
    const pin = await page.getByTestId("offline-pin-setup-page").count();
    const workspace = await page.getByTestId("workspace-destination-operations").count();
    const body = await page.locator("body").innerText();
    expect(
      pin > 0 ||
        workspace > 0 ||
        /workspace|offline-pin|dashboard|sell|home|manager|choose workspace/i.test(`${url}\n${body}`),
    ).toBeTruthy();
  });

  test("Owner operational pages discoverable", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockEmptyPosGets(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await signInManagerForOperator(page);

    const missing: string[] = [];
    for (const entry of ownerPages) {
      await clientNavigate(page, entry.path);
      await page.waitForTimeout(120);
      const denied =
        (await page.getByTestId("permission-denied").count()) +
        (await page.getByTestId("account-class-denied").count()) +
        (await page.getByTestId("admin-experience-denied").count()) +
        (await page.getByTestId("manager-role-denied").count());
      if (denied > 0) {
        missing.push(`${entry.label}:denied`);
      }
    }
    expect(missing, missing.join("; ")).toEqual([]);
  });

  test("Manager inventory purchasing reports surfaces", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockEmptyPosGets(page);
    await prepareSellReady(page);
    await signInManagerForOperator(page);
    for (const p of [
      "/inventory/stock-use",
      "/inventory/waste-loss",
      "/inventory/stock-counts",
      "/purchasing/direct-purchases",
      "/reports",
    ]) {
      await clientNavigate(page, p);
      await expect(page.getByTestId("permission-denied")).toHaveCount(0);
      await expect(page.getByTestId("manager-role-denied")).toHaveCount(0);
    }
  });
});

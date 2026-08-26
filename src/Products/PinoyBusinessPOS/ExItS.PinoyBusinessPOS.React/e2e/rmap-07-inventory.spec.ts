import { expect, test, type Page } from "@playwright/test";
import { assertMinTouchTarget, assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_ORG_ID,
  clientNavigate,
  mockBoundCashierSession,
  mockBoundManagerSession,
  mockBoundOrgAdminSession,
  signInAndBindCashier,
  signInAndBindManager,
  signInAndBindOrgAdmin,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID, mockPosCatalogAdminApi } from "./mock-pos-catalog-admin-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const LONG_NAME =
  "Very Long Inventory Product Name That Must Truncate Safely Without Horizontal Overflow";

type InventoryHarness = {
  tracked: boolean;
  onHand: number;
  movements: Array<Record<string, unknown>>;
};

async function mockInventoryApi(page: Page, harness: InventoryHarness) {
  await page.route("**/pos-api/**/inventory**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const orgHeader = route.request().headers()["x-pos-organization-id"];
    const branchHeader = route.request().headers()["x-pos-branch-id"];

    if (orgHeader && orgHeader !== E2E_ORG_ID) {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({ detail: "Organization scope denied." }),
      });
    }
    if (branchHeader && !branchHeader) {
      return route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ detail: "Branch required." }),
      });
    }

    const accountBody = () => ({
      productId: MOCK_COKE_PRODUCT_ID,
      organizationId: E2E_ORG_ID,
      name: LONG_NAME,
      unitOfMeasure: "Piece",
      productStatus: "Active",
      isTracked: harness.tracked,
      onHandQuantity: harness.onHand,
      stockStatus: harness.tracked ? "InStock" : "Unknown",
      isLowStock: false,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    });

    if (url.includes("/movements") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: harness.movements,
          totalCount: harness.movements.length,
          page: 1,
          pageSize: 50,
        }),
      });
    }

    if (url.match(/\/inventory\/[0-9a-f-]{36}$/i) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(accountBody()),
      });
    }

    if (url.includes("/enable") && method === "POST") {
      const body = route.request().postDataJSON() as { openingQuantity?: number | null };
      harness.tracked = true;
      harness.onHand =
        body.openingQuantity && body.openingQuantity > 0 ? Number(body.openingQuantity) : 0;
      if (harness.onHand > 0) {
        harness.movements.unshift({
          movementId: crypto.randomUUID(),
          productId: MOCK_COKE_PRODUCT_ID,
          inventoryAccountId: crypto.randomUUID(),
          movementType: "OpeningStock",
          quantityEffect: harness.onHand,
          reason: "Opening",
          sourceType: "Manual",
          recordedAtUtc: new Date().toISOString(),
          recordedBy: "00000000-0000-0000-0000-000000000001",
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(accountBody()),
      });
    }

    if (url.includes("/disable") && method === "POST") {
      if (harness.onHand !== 0) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Disable requires zero on-hand quantity.",
            errorCode: "pos.inventory.disable_requires_zero",
          }),
        });
      }
      harness.tracked = false;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(accountBody()),
      });
    }

    if (url.includes("/adjustments") && method === "POST") {
      const body = route.request().postDataJSON() as {
        direction: string;
        quantity: number;
        reason: string;
      };
      if (!body.reason?.trim()) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "A reason is required for manual stock adjustments." }),
        });
      }
      if (!harness.tracked) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Inventory is not tracked for this product." }),
        });
      }
      const effect = body.direction === "Out" ? -Number(body.quantity) : Number(body.quantity);
      if (harness.onHand + effect < 0) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Insufficient stock for adjustment.",
            errorCode: "pos.inventory.insufficient_stock",
          }),
        });
      }
      harness.onHand += effect;
      harness.movements.unshift({
        movementId: crypto.randomUUID(),
        productId: MOCK_COKE_PRODUCT_ID,
        inventoryAccountId: crypto.randomUUID(),
        movementType: "Adjustment",
        quantityEffect: effect,
        reason: body.reason,
        sourceType: "Manual",
        recordedAtUtc: new Date().toISOString(),
        recordedBy: "00000000-0000-0000-0000-000000000001",
      });
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(accountBody()),
      });
    }

    if (method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [accountBody()],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        }),
      });
    }

    return route.fulfill({ status: 404, body: "{}" });
  });
}

test.describe("RMAP-07 inventory tracking", () => {
  test.use({ serviceWorkers: "block" });

  test("untracked default, enable with opening, adjust in/out, disable rules", async ({ page }) => {
    const harness: InventoryHarness = { tracked: false, onHand: 0, movements: [] };
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await mockInventoryApi(page, harness);

    await signInAndBindManager(page);
    await page.getByTestId("open-inventory").click();
    await expect(page.getByTestId("inventory-list-page")).toBeVisible();
    await expect(page.getByTestId(`inventory-row-${MOCK_COKE_PRODUCT_ID}`)).toContainText(
      "Not tracked",
    );

    await page.getByTestId(`inventory-row-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
    await expect(page.getByTestId("inventory-status")).toContainText("Not tracked");
    await expect(page.getByText(/Not tracked means|naka-off ang enforcement/i)).toBeVisible();

    await page.getByRole("textbox", { name: "Opening quantity (base units)" }).fill("0");
    await page.getByTestId("inventory-enable").click();
    await expect(
      page.getByTestId("inventory-status").getByText("Tracked", { exact: true }),
    ).toBeVisible();
    await expect(page.getByText(/On hand:\s*0/)).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).not.toContainText("OpeningStock");

    await page.getByTestId("inventory-disable").click();
    await expect(
      page.getByTestId("inventory-status").getByText("Not tracked", { exact: true }),
    ).toBeVisible();

    await page.getByRole("textbox", { name: "Opening quantity (base units)" }).fill("10");
    await page.getByTestId("inventory-enable").click();
    await expect(page.getByText(/On hand:\s*10/)).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).toContainText("OpeningStock");
    await expect(page.getByTestId("inventory-movements")).toContainText("10");

    await page.getByRole("textbox", { name: "Quantity" }).fill("2");
    await page.getByRole("textbox", { name: "Reason" }).fill("Delivery");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByText(/On hand:\s*12/)).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).toContainText("2");

    await page.locator("select").selectOption("Out");
    await page.getByRole("textbox", { name: "Quantity" }).fill("3");
    await page.getByRole("textbox", { name: "Reason" }).fill("Damage");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByText(/On hand:\s*9/)).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).toContainText("-3");

    await page.getByTestId("inventory-disable").click();
    await expect(page.getByText(/zero on-hand|Disable requires/i)).toBeVisible();
    await expect(
      page.getByTestId("inventory-status").getByText("Tracked", { exact: true }),
    ).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).toContainText("OpeningStock");

    await page.locator("select").selectOption("Out");
    await page.getByRole("textbox", { name: "Quantity" }).fill("9");
    await page.getByRole("textbox", { name: "Reason" }).fill("Clear for disable");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByText(/On hand:\s*0/)).toBeVisible();
    await page.getByTestId("inventory-disable").click();
    await expect(
      page.getByTestId("inventory-status").getByText("Not tracked", { exact: true }),
    ).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).toContainText("OpeningStock");
  });

  test("adjustment rejects empty reason", async ({ page }) => {
    const harness: InventoryHarness = {
      tracked: true,
      onHand: 5,
      movements: [],
    };
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await mockInventoryApi(page, harness);
    await signInAndBindManager(page);
    await clientNavigate(page, `/inventory/${MOCK_COKE_PRODUCT_ID}`);
    await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
    await page.getByRole("textbox", { name: "Quantity" }).fill("1");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByText(/reason is required|Kailangan ang reason/i)).toBeVisible();
  });

  test("cashier denied inventory", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/inventory");
    await expect(page.getByTestId("inventory-view-denied")).toBeVisible();
  });

  test("OrganizationAdministrator alone denied inventory", async ({ page }) => {
    await mockBoundOrgAdminSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindOrgAdmin(page);
    await clientNavigate(page, "/inventory");
    await expect(page.getByTestId("inventory-view-denied")).toBeVisible();
  });

  for (const viewport of VIEWPORTS) {
    test(`inventory list+detail responsive ${viewport.width}x${viewport.height}`, async ({
      page,
    }) => {
      await page.setViewportSize(viewport);
      const harness: InventoryHarness = {
        tracked: true,
        onHand: 1234567.89,
        movements: [
          {
            movementId: crypto.randomUUID(),
            productId: MOCK_COKE_PRODUCT_ID,
            inventoryAccountId: crypto.randomUUID(),
            movementType: "OpeningStock",
            quantityEffect: 1234567.89,
            reason: "Very long opening reason that must remain readable without layout breakage",
            sourceType: "Manual",
            recordedAtUtc: new Date().toISOString(),
            recordedBy: "00000000-0000-0000-0000-000000000001",
          },
        ],
      };
      await mockBoundManagerSession(page);
      await mockPosCatalogAdminApi(page);
      await mockInventoryApi(page, harness);
      await signInAndBindManager(page);

      await page.getByTestId("open-inventory").click();
      await expect(page.getByTestId("inventory-list-page")).toBeVisible();
      await expect(page.getByText(/1234567/)).toBeVisible();
      await assertNoHorizontalOverflow(page);

      await page.getByTestId(`inventory-row-${MOCK_COKE_PRODUCT_ID}`).click();
      await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
      await expect(page.getByText(LONG_NAME)).toBeVisible();
      await assertMinTouchTarget(page.getByTestId("inventory-adjust"));
      await assertMinTouchTarget(page.getByTestId("inventory-disable"));
      await expect(page.getByTestId("inventory-movements")).toBeVisible();
      await page.getByTestId("inventory-disable").scrollIntoViewIfNeeded();
      await assertNoHorizontalOverflow(page);
    });
  }
});

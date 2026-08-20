import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_ORG_ID,
  mockBoundCashierSession,
  mockBoundManagerSession,
  signInAndBindCashier,
  signInAndBindManager,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID, mockPosCatalogAdminApi } from "./mock-pos-catalog-admin-route";

test.describe("RMAP-07 inventory tracking", () => {
  test.use({ serviceWorkers: "block" });

  test("untracked product shows Not tracked and can enable with opening", async ({ page }) => {
    let tracked = false;
    let onHand = 0;
    const movements: Array<Record<string, unknown>> = [];

    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await page.route("**/pos-api/**/inventory**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();
      if (url.includes("/movements") && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            items: movements,
            totalCount: movements.length,
            page: 1,
            pageSize: 50,
          }),
        });
      }
      if (url.match(/\/inventory\/[0-9a-f-]{36}$/i) && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            productId: MOCK_COKE_PRODUCT_ID,
            organizationId: E2E_ORG_ID,
            name: "Coke 330ml",
            unitOfMeasure: "Piece",
            productStatus: "Active",
            isTracked: tracked,
            onHandQuantity: onHand,
            stockStatus: tracked ? "InStock" : "Unknown",
            isLowStock: false,
            createdAtUtc: "2026-01-01T00:00:00Z",
            updatedAtUtc: "2026-01-01T00:00:00Z",
          }),
        });
      }
      if (url.includes("/enable") && method === "POST") {
        const body = route.request().postDataJSON() as { openingQuantity?: number | null };
        tracked = true;
        onHand = body.openingQuantity && body.openingQuantity > 0 ? body.openingQuantity : 0;
        if (onHand > 0) {
          movements.push({
            movementId: crypto.randomUUID(),
            productId: MOCK_COKE_PRODUCT_ID,
            inventoryAccountId: crypto.randomUUID(),
            movementType: "OpeningStock",
            quantityEffect: onHand,
            reason: "Opening",
            sourceType: "Manual",
            recordedAtUtc: new Date().toISOString(),
            recordedBy: "00000000-0000-0000-0000-000000000001",
          });
        }
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            productId: MOCK_COKE_PRODUCT_ID,
            organizationId: E2E_ORG_ID,
            name: "Coke 330ml",
            unitOfMeasure: "Piece",
            productStatus: "Active",
            isTracked: true,
            onHandQuantity: onHand,
            stockStatus: "InStock",
            isLowStock: false,
            createdAtUtc: "2026-01-01T00:00:00Z",
            updatedAtUtc: new Date().toISOString(),
          }),
        });
      }
      if (url.includes("/adjustments") && method === "POST") {
        const body = route.request().postDataJSON() as {
          direction: string;
          quantity: number;
          reason: string;
        };
        const effect = body.direction === "Out" ? -body.quantity : body.quantity;
        onHand += effect;
        movements.unshift({
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
          body: JSON.stringify({
            productId: MOCK_COKE_PRODUCT_ID,
            organizationId: E2E_ORG_ID,
            name: "Coke 330ml",
            unitOfMeasure: "Piece",
            productStatus: "Active",
            isTracked: true,
            onHandQuantity: onHand,
            stockStatus: "InStock",
            isLowStock: false,
            createdAtUtc: "2026-01-01T00:00:00Z",
            updatedAtUtc: new Date().toISOString(),
          }),
        });
      }
      if (method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            items: [
              {
                productId: MOCK_COKE_PRODUCT_ID,
                organizationId: E2E_ORG_ID,
                name: "Coke 330ml",
                unitOfMeasure: "Piece",
                productStatus: "Active",
                isTracked: tracked,
                onHandQuantity: onHand,
                stockStatus: tracked ? "InStock" : "Unknown",
                isLowStock: false,
                createdAtUtc: "2026-01-01T00:00:00Z",
                updatedAtUtc: "2026-01-01T00:00:00Z",
              },
            ],
            totalCount: 1,
            page: 1,
            pageSize: 50,
          }),
        });
      }
      return route.fulfill({ status: 404, body: "{}" });
    });

    await signInAndBindManager(page);
    await page.getByTestId("open-inventory").click();
    await expect(page.getByTestId("inventory-list-page")).toBeVisible();
    await expect(page.getByText("Not tracked")).toBeVisible();
    await page.getByTestId(`inventory-row-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
    await page.getByRole("textbox", { name: "Opening quantity (base units)" }).fill("10");
    await page.getByTestId("inventory-enable").click();
    await expect(page.getByText("Tracked", { exact: true })).toBeVisible();
    await expect(page.getByText(/On hand:\s*10/)).toBeVisible();
    await page.getByRole("textbox", { name: "Quantity" }).fill("2");
    await page.getByRole("textbox", { name: "Reason" }).fill("Delivery");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByText(/On hand:\s*12/)).toBeVisible();
    await expect(page.getByTestId("inventory-movements")).toContainText("Adjustment");
  });

  test("cashier denied inventory", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindCashier(page);
    await page.goto("/inventory");
    await expect(page.getByTestId("inventory-view-denied")).toBeVisible();
  });

  test("inventory list usable at 375x812", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await page.route("**/pos-api/**/inventory**", async (route) => {
      if (route.request().method() === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
        });
      }
      return route.fallback();
    });
    await signInAndBindManager(page);
    await page.getByTestId("open-inventory").click();
    await assertNoHorizontalOverflow(page);
  });
});

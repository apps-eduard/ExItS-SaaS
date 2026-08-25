import { expect, test } from "@playwright/test";
import { assertMinTouchTarget, assertNoHorizontalOverflow } from "./helpers";
import {
  clientNavigate,
  mockBoundCashierSession,
  mockBoundManagerSession,
  mockBoundOrgAdminSession,
  signInAndBindCashier,
  signInAndBindManager,
  signInAndBindOrgAdmin,
} from "./mock-bound-session";
import {
  MOCK_CHIPS_PRODUCT_ID,
  MOCK_COKE_PRODUCT_ID,
  mockPosCatalogAdminApi,
} from "./mock-pos-catalog-admin-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const COKE_TOKEN = "2026-01-01T00:00:00Z";

test.describe("RMAP-06 today's prices", () => {
  test.use({ serviceWorkers: "block" });

  test("manager updates dirty row and sends concurrency token", async ({ page }) => {
    let capturedBody: {
      items: Array<{ productId: string; sellingPrice: number; expectedUpdatedAtUtc: string }>;
    } | null = null;

    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await page.route("**/pos-api/**/catalog/products/prices", async (route) => {
      if (route.request().method() !== "POST") {
        return route.fallback();
      }
      capturedBody = route.request().postDataJSON() as typeof capturedBody;
      const body = capturedBody!;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          results: body.items.map((item) => ({
            productId: item.productId,
            succeeded: true,
            changed: true,
            product: {
              productId: item.productId,
              organizationId: "11111111-1111-1111-1111-111111111111",
              name: item.productId === MOCK_COKE_PRODUCT_ID ? "Coke 330ml" : "Potato Chips",
              unitOfMeasure: "Piece",
              sellingMode: "PerItem",
              sellingPrice: item.sellingPrice,
              status: "Active",
              createdAtUtc: "2026-01-01T00:00:00Z",
              updatedAtUtc: new Date().toISOString(),
            },
          })),
          succeededCount: body.items.length,
          failedCount: 0,
          changedCount: body.items.length,
        }),
      });
    });

    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "Today's Prices" }).click();
    await expect(page.getByTestId("todays-prices-page")).toBeVisible();
    await expect(page.getByText("Coke 330ml")).toBeVisible();

    const cokeRow = page.getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`);
    await cokeRow.getByRole("textbox", { name: "New price" }).fill("30");
    await expect(cokeRow.getByText(/Changed|Binago/i)).toBeVisible();
    await page.getByTestId("prices-save").click();
    await expect(page.getByText(/Saved|Na-save/i)).toBeVisible();

    expect(capturedBody?.items).toHaveLength(1);
    expect(capturedBody?.items[0]?.productId).toBe(MOCK_COKE_PRODUCT_ID);
    expect(capturedBody?.items[0]?.sellingPrice).toBe(30);
    expect(capturedBody?.items[0]?.expectedUpdatedAtUtc).toBe(COKE_TOKEN);
    await expect(cokeRow.getByText(/Changed|Binago/i)).toHaveCount(0);
  });

  test("partial failure keeps failed dirty row and does not claim full success", async ({
    page,
  }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await page.route("**/pos-api/**/catalog/products/prices", async (route) => {
      if (route.request().method() !== "POST") {
        return route.fallback();
      }
      const body = route.request().postDataJSON() as {
        items: Array<{ productId: string; sellingPrice: number; expectedUpdatedAtUtc: string }>;
      };
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          results: body.items.map((item) => {
            if (item.productId === MOCK_COKE_PRODUCT_ID) {
              return {
                productId: item.productId,
                succeeded: false,
                changed: false,
                errorCode: "pos.catalog.concurrency_conflict",
                errorMessage: "Product was modified by another user.",
              };
            }
            return {
              productId: item.productId,
              succeeded: true,
              changed: true,
              product: {
                productId: item.productId,
                organizationId: "11111111-1111-1111-1111-111111111111",
                name: "Potato Chips",
                unitOfMeasure: "Piece",
                sellingMode: "PerItem",
                sellingPrice: item.sellingPrice,
                status: "Active",
                createdAtUtc: "2026-01-01T00:00:00Z",
                updatedAtUtc: new Date().toISOString(),
              },
            };
          }),
          succeededCount: body.items.filter((i) => i.productId !== MOCK_COKE_PRODUCT_ID).length,
          failedCount: 1,
          changedCount: body.items.filter((i) => i.productId !== MOCK_COKE_PRODUCT_ID).length,
        }),
      });
    });

    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "Today's Prices" }).click();
    await page
      .getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`)
      .getByRole("textbox", { name: "New price" })
      .fill("31");
    await page
      .getByTestId(`price-row-${MOCK_CHIPS_PRODUCT_ID}`)
      .getByRole("textbox", { name: "New price" })
      .fill("18");
    await page.getByTestId("prices-save").click();

    await expect(page.getByText(/failed/i)).toBeVisible();
    await expect(page.getByText(/Saved\./i)).toHaveCount(0);
    await expect(
      page.getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`).getByText(/modified by another/i),
    ).toBeVisible();
    await expect(
      page.getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`).getByText(/Changed|Binago/i),
    ).toBeVisible();
  });

  test("stale concurrency conflict surfaces understandable feedback", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await page.route("**/pos-api/**/catalog/products/prices", async (route) => {
      if (route.request().method() !== "POST") {
        return route.fallback();
      }
      const body = route.request().postDataJSON() as {
        items: Array<{ productId: string }>;
      };
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          results: body.items.map((item) => ({
            productId: item.productId,
            succeeded: false,
            changed: false,
            errorCode: "pos.catalog.concurrency_conflict",
            errorMessage: "Product was modified by another user.",
          })),
          succeededCount: 0,
          failedCount: body.items.length,
          changedCount: 0,
        }),
      });
    });

    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "Today's Prices" }).click();
    await page
      .getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`)
      .getByRole("textbox", { name: "New price" })
      .fill("40");
    await page.getByTestId("prices-save").click();
    await expect(page.getByText(/modified by another/i)).toBeVisible();
  });

  test("cashier denied today's prices", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/catalog/todays-prices");
    await expect(page.getByTestId("catalog-manage-denied")).toBeVisible();
  });

  test("OrganizationAdministrator alone denied today's prices", async ({ page }) => {
    await mockBoundOrgAdminSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindOrgAdmin(page);
    await clientNavigate(page, "/catalog/todays-prices");
    await expect(page.getByTestId("catalog-manage-denied")).toBeVisible();
  });

  for (const viewport of VIEWPORTS) {
    test(`todays prices responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundManagerSession(page);
      await mockPosCatalogAdminApi(page);
      await signInAndBindManager(page);
      await page.getByTestId("open-catalog").click();
      await page.getByRole("link", { name: "Today's Prices" }).click();
      await expect(page.getByTestId("todays-prices-page")).toBeVisible();
      await expect(page.getByText("Coke 330ml")).toBeVisible();

      const cokeRow = page.getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`);
      const priceInput = cokeRow.getByRole("textbox", { name: "New price" });
      await priceInput.fill("999999.99");
      await expect(cokeRow.getByText(/Changed|Binago/i)).toBeVisible();
      await assertMinTouchTarget(priceInput);
      await assertMinTouchTarget(page.getByTestId("prices-save"));
      await expect(page.getByTestId("prices-save")).toBeVisible();
      await page.getByTestId("prices-save").scrollIntoViewIfNeeded();
      await assertNoHorizontalOverflow(page);
    });
  }
});

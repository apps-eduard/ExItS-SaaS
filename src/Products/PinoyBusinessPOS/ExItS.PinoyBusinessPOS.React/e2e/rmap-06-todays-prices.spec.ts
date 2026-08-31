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
  { width: 360, height: 800 },
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const COKE_TOKEN = "2026-01-01T00:00:00Z";

test.describe("RMAP-06 today's prices", () => {
  test.use({ serviceWorkers: "block" });

  test("manager saves one dirty product with concurrency token and toast", async ({ page }) => {
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
    const chipsRow = page.getByTestId(`price-row-${MOCK_CHIPS_PRODUCT_ID}`);
    await cokeRow.getByRole("textbox", { name: "New price" }).fill("30");
    await expect(cokeRow.getByTestId(`price-save-${MOCK_COKE_PRODUCT_ID}`)).toBeVisible();
    await expect(chipsRow.getByTestId(`price-save-${MOCK_CHIPS_PRODUCT_ID}`)).toHaveCount(0);
    await expect(page.getByTestId("prices-save")).toHaveCount(0);

    await cokeRow.getByTestId(`price-save-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("exits-toast")).toContainText(/Coke 330ml/i);
    await expect(page.getByTestId("exits-toast")).toContainText(/₱30\.00/);

    expect(capturedBody?.items).toHaveLength(1);
    expect(capturedBody?.items[0]?.productId).toBe(MOCK_COKE_PRODUCT_ID);
    expect(capturedBody?.items[0]?.sellingPrice).toBe(30);
    expect(capturedBody?.items[0]?.expectedUpdatedAtUtc).toBe(COKE_TOKEN);
    await expect(cokeRow.getByTestId(`price-save-${MOCK_COKE_PRODUCT_ID}`)).toHaveCount(0);
    await expect(cokeRow.getByText(/Current:.*₱30\.00/i)).toBeVisible();
  });

  test("row failure preserves draft without global success banner", async ({ page }) => {
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
    const cokeRow = page.getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`);
    await cokeRow.getByRole("textbox", { name: "New price" }).fill("40");
    await cokeRow.getByTestId(`price-save-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(cokeRow.getByText(/changed elsewhere|modified by another/i)).toBeVisible();
    await expect(cokeRow.getByRole("textbox", { name: "New price" })).toHaveValue("40");
    await expect(page.getByText(/Saved\./i)).toHaveCount(0);
  });

  test("Enter saves only the focused product", async ({ page }) => {
    let capturedBody: {
      items: Array<{ productId: string; sellingPrice: number }>;
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
              name: "Coke 330ml",
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
    const cokeRow = page.getByTestId(`price-row-${MOCK_COKE_PRODUCT_ID}`);
    const chipsRow = page.getByTestId(`price-row-${MOCK_CHIPS_PRODUCT_ID}`);
    await chipsRow.getByRole("textbox", { name: "New price" }).fill("18");
    const cokeInput = cokeRow.getByRole("textbox", { name: "New price" });
    await cokeInput.fill("31");
    await cokeInput.press("Enter");

    await expect.poll(() => capturedBody?.items.length ?? 0).toBe(1);
    expect(capturedBody?.items[0]?.productId).toBe(MOCK_COKE_PRODUCT_ID);
    expect(capturedBody?.items[0]?.sellingPrice).toBe(31);
    await expect(chipsRow.getByRole("textbox", { name: "New price" })).toHaveValue("18");
    await expect(chipsRow.getByTestId(`price-save-${MOCK_CHIPS_PRODUCT_ID}`)).toBeVisible();
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
      const save = cokeRow.getByTestId(`price-save-${MOCK_COKE_PRODUCT_ID}`);
      await expect(save).toBeVisible();
      await assertMinTouchTarget(priceInput);
      await assertMinTouchTarget(save);
      await expect(page.getByTestId("prices-save")).toHaveCount(0);
      await assertNoHorizontalOverflow(page);
    });
  }
});

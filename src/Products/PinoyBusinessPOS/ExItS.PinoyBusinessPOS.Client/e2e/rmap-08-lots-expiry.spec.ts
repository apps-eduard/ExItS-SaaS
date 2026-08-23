import { expect, test, type Page } from "@playwright/test";
import { assertMinTouchTarget, assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_ORG_ID,
  clientNavigate,
  mockBoundCashierSession,
  mockBoundManagerSession,
  signInAndBindCashier,
  signInAndBindManager,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID, mockPosCatalogAdminApi } from "./mock-pos-catalog-admin-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const LONG_NAME =
  "Very Long Expiring Milk Product Name That Must Truncate Safely Without Horizontal Overflow";

const LOT_NEAR_ID = "aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa";
const LOT_EXPIRED_ID = "bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb";

type LotHarness = {
  tracked: boolean;
  tracksExpiration: boolean;
  onHand: number;
  sellable: number;
  expired: number;
  nearExpiry: number;
  lots: Array<Record<string, unknown>>;
  expiring: Array<Record<string, unknown>>;
  movements: Array<Record<string, unknown>>;
};

function defaultLots(): LotHarness["lots"] {
  return [
    {
      lotId: LOT_NEAR_ID,
      productId: MOCK_COKE_PRODUCT_ID,
      lotNumber: "NEAR-1",
      expirationDate: "2026-08-28",
      quantityOnHand: 20,
      expiryStatus: "NearExpiry",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    },
    {
      lotId: LOT_EXPIRED_ID,
      productId: MOCK_COKE_PRODUCT_ID,
      lotNumber: "EXP-1",
      expirationDate: "2026-08-01",
      quantityOnHand: 5,
      expiryStatus: "Expired",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    },
  ];
}

function defaultExpiring(): LotHarness["expiring"] {
  return [
    {
      lotId: LOT_EXPIRED_ID,
      productId: MOCK_COKE_PRODUCT_ID,
      productName: LONG_NAME,
      sku: "MILK-1L",
      lotNumber: "EXP-1",
      expirationDate: "2026-08-01",
      quantityOnHand: 5,
      expiryStatus: "Expired",
      warningDays: 7,
    },
    {
      lotId: LOT_NEAR_ID,
      productId: MOCK_COKE_PRODUCT_ID,
      productName: LONG_NAME,
      sku: "MILK-1L",
      lotNumber: "NEAR-1",
      expirationDate: "2026-08-28",
      quantityOnHand: 20,
      expiryStatus: "NearExpiry",
      warningDays: 7,
    },
  ];
}

async function mockInventoryLotsApi(page: Page, harness: LotHarness) {
  await page.route("**/cashier-shifts/current**", async (route) => {
    await route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
  await page.route("**/pos-api/**/inventory**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const orgHeader = route.request().headers()["x-pos-organization-id"];

    if (orgHeader && orgHeader !== E2E_ORG_ID) {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({ detail: "Organization scope denied." }),
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
      tracksExpiration: harness.tracksExpiration,
      expirationWarningDays: harness.tracksExpiration ? 7 : null,
      sellableQuantity: harness.sellable,
      expiredQuantity: harness.expired,
      nearExpiryQuantity: harness.nearExpiry,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    });

    if (
      url.includes("/inventory/lots") &&
      !url.match(/\/inventory\/[0-9a-f-]{36}\/lots/i) &&
      method === "GET"
    ) {
      const parsed = new URL(url);
      const windowParam = parsed.searchParams.get("window") ?? "Days30";
      const pageNum = Math.max(1, Number(parsed.searchParams.get("page") ?? "1") || 1);
      const pageSize = Math.max(1, Number(parsed.searchParams.get("pageSize") ?? "50") || 50);
      let items = harness.expiring;
      if (windowParam === "Expired") {
        items = harness.expiring.filter((lot) => lot.expiryStatus === "Expired");
      }
      const start = (pageNum - 1) * pageSize;
      const slice = items.slice(start, start + pageSize);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: slice,
          totalCount: items.length,
          page: pageNum,
          pageSize,
          expiredCount: harness.expiring.filter((l) => l.expiryStatus === "Expired").length,
          nearExpiryCount: harness.expiring.filter((l) => l.expiryStatus === "NearExpiry").length,
        }),
      });
    }

    if (url.includes("/lots") && method === "GET") {
      const parsed = new URL(url);
      const pageNum = Math.max(1, Number(parsed.searchParams.get("page") ?? "1") || 1);
      const pageSize = Math.max(1, Number(parsed.searchParams.get("pageSize") ?? "50") || 50);
      const start = (pageNum - 1) * pageSize;
      const slice = harness.lots.slice(start, start + pageSize);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: slice,
          totalCount: harness.lots.length,
          page: pageNum,
          pageSize,
        }),
      });
    }

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
      const body = route.request().postDataJSON() as {
        openingQuantity?: number | null;
        expirationDate?: string | null;
      };
      if (
        harness.tracksExpiration &&
        body.openingQuantity &&
        body.openingQuantity > 0 &&
        !body.expirationDate
      ) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Expiration date is required for opening stock." }),
        });
      }
      harness.tracked = true;
      harness.onHand =
        body.openingQuantity && body.openingQuantity > 0 ? Number(body.openingQuantity) : 0;
      harness.sellable = harness.onHand;
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
        expirationDate?: string | null;
        lotId?: string | null;
      };
      if (!body.reason?.trim()) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "A reason is required for manual stock adjustments." }),
        });
      }
      if (harness.tracksExpiration && body.direction === "In" && !body.expirationDate) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Expiration date is required for stock in." }),
        });
      }
      if (harness.tracksExpiration && body.direction === "Out" && !body.lotId) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Lot is required for stock out." }),
        });
      }
      const effect = body.direction === "Out" ? -Number(body.quantity) : Number(body.quantity);
      harness.onHand += effect;
      if (body.direction === "Out" && body.reason === "Expired") {
        harness.expired = Math.max(0, harness.expired - Number(body.quantity));
      } else if (body.direction === "In") {
        harness.sellable += Number(body.quantity);
      }
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

test.describe("RMAP-08 lots / expiry", () => {
  test.use({ serviceWorkers: "block" });

  test("expiration surfaces on detail and expiring list", async ({ page }) => {
    const harness: LotHarness = {
      tracked: true,
      tracksExpiration: true,
      onHand: 25,
      sellable: 20,
      expired: 5,
      nearExpiry: 20,
      lots: defaultLots(),
      expiring: defaultExpiring(),
      movements: [],
    };
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await mockInventoryLotsApi(page, harness);

    await signInAndBindManager(page);
    await page.getByTestId("open-expiring-stock-home").click();
    await expect(page.getByTestId("inventory-expiration-page")).toBeVisible();
    await expect(page.getByTestId("inventory-expiry-counts")).toContainText(/Expired lots:\s*1/i);
    await expect(page.getByTestId(`expiring-lot-${LOT_EXPIRED_ID}`)).toContainText("Expired");
    await expect(page.getByTestId(`expiring-lot-${LOT_NEAR_ID}`)).toContainText(/Expires in/i);

    await page.getByTestId("inventory-expiry-window-Expired").click();
    await expect(page.getByTestId(`expiring-lot-${LOT_EXPIRED_ID}`)).toBeVisible();
    await expect(page.getByTestId(`expiring-lot-${LOT_NEAR_ID}`)).toHaveCount(0);

    await page.getByTestId(`expiring-lot-${LOT_EXPIRED_ID}`).click();
    await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
    await expect(page.getByTestId("inventory-expiry-totals")).toContainText("Sellable");
    await expect(page.getByTestId("inventory-expiry-totals")).toContainText("20");
    await expect(page.getByTestId("inventory-expiry-totals")).toContainText("Expired");
    await expect(page.getByTestId("inventory-lots")).toContainText("NEAR-1");
    await expect(page.getByTestId("inventory-lots")).toContainText("Expired");

    await page.getByTestId("inventory-adjust-direction").selectOption("In");
    await page.getByRole("textbox", { name: "Quantity" }).fill("3");
    await page.getByRole("textbox", { name: "Reason" }).fill("Delivery");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByText(/Expiration date is required/i)).toBeVisible();

    await page.getByTestId("inventory-adjust-expiry").fill("2026-09-15");
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByTestId("inventory-movements")).toContainText("3");

    await page.getByTestId("inventory-adjust-direction").selectOption("Out");
    await page.getByRole("textbox", { name: "Quantity" }).fill("2");
    await page.getByRole("textbox", { name: "Reason" }).fill("Expired");
    await page.getByTestId("inventory-adjust-lot").selectOption(LOT_EXPIRED_ID);
    await page.getByTestId("inventory-adjust").click();
    await expect(page.getByTestId("inventory-movements")).toContainText("Expired");
  });

  test("non-expiry product keeps simple inventory UX", async ({ page }) => {
    const harness: LotHarness = {
      tracked: true,
      tracksExpiration: false,
      onHand: 10,
      sellable: 10,
      expired: 0,
      nearExpiry: 0,
      lots: [],
      expiring: [],
      movements: [],
    };
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await mockInventoryLotsApi(page, harness);
    await signInAndBindManager(page);
    await clientNavigate(page, `/inventory/${MOCK_COKE_PRODUCT_ID}`);
    await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
    await expect(page.getByTestId("inventory-expiry-totals")).toHaveCount(0);
    await expect(page.getByTestId("inventory-lots")).toHaveCount(0);
    await expect(page.getByText(/On hand:\s*10/)).toBeVisible();
    await expect(page.getByTestId("inventory-adjust-expiry")).toHaveCount(0);
  });

  test("cashier denied inventory expiration", async ({ page }) => {
    await page.route("**/cashier-shifts/current**", async (route) => {
      await route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });
    await mockBoundCashierSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/inventory/expiration");
    await expect(page.getByTestId("inventory-view-denied")).toBeVisible();
  });

  test("product lots load more past page size 50", async ({ page }) => {
    const manyLots = Array.from({ length: 55 }, (_, index) => {
      const day = String((index % 28) + 1).padStart(2, "0");
      const lotId = `aaaaaaaa-${String(index).padStart(4, "0")}-4111-8111-aaaaaaaaaaaa`;
      return {
        lotId,
        productId: MOCK_COKE_PRODUCT_ID,
        lotNumber: `LOT-${index + 1}`,
        expirationDate: `2026-09-${day}`,
        quantityOnHand: 1,
        expiryStatus: "Ok",
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
      };
    });
    const harness: LotHarness = {
      tracked: true,
      tracksExpiration: true,
      onHand: 55,
      sellable: 55,
      expired: 0,
      nearExpiry: 0,
      lots: manyLots,
      expiring: manyLots.map((lot) => ({
        ...lot,
        productName: LONG_NAME,
        sku: "MILK-1L",
        warningDays: 7,
      })),
      movements: [],
    };
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await mockInventoryLotsApi(page, harness);
    await signInAndBindManager(page);
    await clientNavigate(page, `/inventory/${MOCK_COKE_PRODUCT_ID}`);
    await expect(page.getByTestId("inventory-lots")).toBeVisible();
    await expect(page.getByTestId(`inventory-lot-${manyLots[0]!.lotId}`)).toBeVisible();
    await expect(page.getByTestId(`inventory-lot-${manyLots[50]!.lotId}`)).toHaveCount(0);
    await page.getByTestId("inventory-lots-load-more").click();
    await expect(page.getByTestId(`inventory-lot-${manyLots[50]!.lotId}`)).toBeVisible();
    await page.getByTestId("inventory-adjust-direction").selectOption("Out");
    await page.getByTestId("inventory-adjust-lot").selectOption(manyLots[50]!.lotId);
    await expect(page.getByTestId("inventory-adjust-lot")).toHaveValue(manyLots[50]!.lotId);
  });

  for (const viewport of VIEWPORTS) {
    test(`expiration list+detail responsive ${viewport.width}x${viewport.height}`, async ({
      page,
    }) => {
      await page.setViewportSize(viewport);
      const harness: LotHarness = {
        tracked: true,
        tracksExpiration: true,
        onHand: 25,
        sellable: 20,
        expired: 5,
        nearExpiry: 20,
        lots: defaultLots(),
        expiring: defaultExpiring(),
        movements: [],
      };
      await mockBoundManagerSession(page);
      await mockPosCatalogAdminApi(page);
      await mockInventoryLotsApi(page, harness);
      await signInAndBindManager(page);

      await page.getByTestId("open-inventory").click();
      await expect(page.getByTestId("inventory-list-page")).toBeVisible();
      await assertMinTouchTarget(page.getByTestId("open-expiring-stock"));
      await page.getByTestId("open-expiring-stock").click();
      await expect(page.getByTestId("inventory-expiration-page")).toBeVisible();
      await expect(page.getByText(LONG_NAME).first()).toBeVisible();
      await assertNoHorizontalOverflow(page);

      await page.getByTestId(`expiring-lot-${LOT_NEAR_ID}`).click();
      await expect(page.getByTestId("inventory-detail-page")).toBeVisible();
      await assertMinTouchTarget(page.getByTestId("inventory-adjust"));
      await assertMinTouchTarget(page.getByTestId("inventory-disable"));
      await page.getByTestId("inventory-lots").scrollIntoViewIfNeeded();
      await assertNoHorizontalOverflow(page);
    });
  }
});

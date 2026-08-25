import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_ORG_ID,
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
  clientNavigate,
} from "./mock-bound-session";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const PO_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const SUPPLIER_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const PRODUCT_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const LINE_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const GRN_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const DPR_ID = "12121212-1212-4121-8121-121212121212";

type Tracker = {
  urls: string[];
  stockCalls: number;
  inventoryCalls: number;
  createCalls: number;
  submitCalls: number;
  receiveCalls: number;
  directCalls: number;
};

function supplier() {
  return {
    supplierId: SUPPLIER_ID,
    organizationId: E2E_ORG_ID,
    supplierCode: "SUP0001",
    name: "Island Wholesale",
    contactPerson: null,
    mobileNumber: null,
    telephoneNumber: null,
    email: null,
    addressLine1: null,
    addressLine2: null,
    cityMunicipality: null,
    province: null,
    postalCode: null,
    taxOrRegistrationNumber: null,
    notes: null,
    status: "Active",
    connectionType: "Manual",
    connectedRelationshipId: null,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
  };
}

function product() {
  return {
    productId: PRODUCT_ID,
    organizationId: E2E_ORG_ID,
    name: "Rice 25kg",
    sku: "RICE25",
    barcode: "4800000000001",
    unitOfMeasure: "kg",
    sellingMode: "Unit",
    sellingPrice: 120,
    status: "Active",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    isTracked: true,
    tracksExpiration: true,
  };
}

function po(overrides: Record<string, unknown> = {}) {
  return {
    purchaseOrderId: PO_ID,
    organizationId: E2E_ORG_ID,
    poNumber: "PO-1001",
    supplierId: SUPPLIER_ID,
    status: "Draft",
    orderDate: "2026-08-21",
    expectedDeliveryDate: null,
    supplierReference: null,
    notes: null,
    orderedAtUtc: null,
    orderedBy: null,
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
    lines: [
      {
        lineId: LINE_ID,
        productId: PRODUCT_ID,
        lineNumber: 1,
        nameSnapshot: "Rice 25kg",
        uomSnapshot: "kg",
        orderedQty: 10,
        unitPurchaseCost: 50,
        lineTotal: 500,
        receivedQty: 0,
        outstandingQty: 10,
        lineNotes: null,
        closedShortQty: 0,
      },
    ],
    displayStatus: "Draft",
    canReceiveConnected: true,
    supplierName: "Island Wholesale",
    paymentTerm: "Cash",
    paymentTermLabel: "Cash",
    ...overrides,
  };
}

async function mockPurchasingApi(
  page: import("@playwright/test").Page,
  opts: { wrongOrg?: boolean; denyReceiveConnected?: boolean } = {},
): Promise<Tracker> {
  const tracker: Tracker = {
    urls: [],
    stockCalls: 0,
    inventoryCalls: 0,
    createCalls: 0,
    submitCalls: 0,
    receiveCalls: 0,
    directCalls: 0,
  };

  let current = po(
    opts.denyReceiveConnected
      ? { status: "Ordered", displayStatus: "Ordered", canReceiveConnected: false }
      : {},
  );

  await page.route("**/pos-api/api/v1/pos/suppliers**", async (route) => {
    if (route.request().method() !== "GET") {
      return route.fallback();
    }
    tracker.urls.push(`GET suppliers`);
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items: [supplier()], totalCount: 1, page: 1, pageSize: 100 }),
    });
  });

  await page.route("**/pos-api/api/v1/pos/catalog/products**", async (route) => {
    if (route.request().method() !== "GET") {
      return route.fallback();
    }
    tracker.urls.push(`GET catalog/products`);
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items: [product()], totalCount: 1, page: 1, pageSize: 20 }),
    });
  });

  await page.route("**/pos-api/api/v1/pos/purchase-orders**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;
    tracker.urls.push(`${method} ${pathname}`);

    if (pathname.match(/\/purchase-orders\/?$/) && method === "GET") {
      const status = new URL(url).searchParams.get("status");
      const list =
        status === "Ordered" || status === "PartiallyReceived"
          ? current.status === status
            ? [current]
            : []
          : status
            ? current.status === status
              ? [current]
              : []
            : [current];
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: list, totalCount: list.length, page: 1, pageSize: 20 }),
      });
    }

    if (pathname.match(/\/purchase-orders\/?$/) && method === "POST") {
      tracker.createCalls += 1;
      current = po({ status: "Draft", displayStatus: "Draft" });
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(current),
      });
    }

    if (pathname.endsWith(`/purchase-orders/${PO_ID}`) && method === "GET") {
      if (opts.wrongOrg) {
        return route.fulfill({
          status: 404,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Purchase order was not found.", errorCode: "not_found" }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(current),
      });
    }

    if (pathname.endsWith(`/purchase-orders/${PO_ID}/submit`) && method === "POST") {
      tracker.submitCalls += 1;
      current = po({
        status: "Ordered",
        displayStatus: "Ordered",
        orderedAtUtc: "2026-08-21T00:10:00Z",
        canReceiveConnected: opts.denyReceiveConnected ? false : true,
      });
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(current),
      });
    }

    if (pathname.endsWith(`/purchase-orders/${PO_ID}/receive`) && method === "POST") {
      tracker.receiveCalls += 1;
      tracker.stockCalls += 1;
      const body = route.request().postDataJSON() as {
        lines?: Array<{ receiveQty?: number }>;
        goodsReceiptId?: string;
      };
      const qty = body.lines?.[0]?.receiveQty ?? 0;
      const received = (current.lines[0].receivedQty as number) + qty;
      const outstanding = Math.max(0, 10 - received);
      current = po({
        status: outstanding === 0 ? "Received" : "PartiallyReceived",
        displayStatus: outstanding === 0 ? "Received" : "PartiallyReceived",
        lines: [
          {
            ...current.lines[0],
            receivedQty: received,
            outstandingQty: outstanding,
          },
        ],
      });
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify({
          goodsReceiptId: body.goodsReceiptId ?? GRN_ID,
          organizationId: E2E_ORG_ID,
          purchaseOrderId: PO_ID,
          supplierId: SUPPLIER_ID,
          grnNumber: "GRN-1",
          receivedDate: "2026-08-21",
          deliveryReference: null,
          notes: null,
          receivedAtUtc: "2026-08-21T01:00:00Z",
          receivedBy: "99999999-9999-4999-8999-999999999999",
          lines: [
            {
              lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
              purchaseOrderLineId: LINE_ID,
              productId: PRODUCT_ID,
              lineNumber: 1,
              nameSnapshot: "Rice 25kg",
              uomSnapshot: "kg",
              quantityReceived: qty,
              unitPurchaseCostSnapshot: 50,
              lineTotalSnapshot: qty * 50,
              inventoryMovementId: "11111111-2222-4333-8444-555555555555",
            },
          ],
        }),
      });
    }

    return route.fallback();
  });

  await page.route("**/pos-api/api/v1/pos/direct-purchase-receipts**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;
    tracker.urls.push(`${method} ${pathname}`);

    if (pathname.match(/\/direct-purchase-receipts\/?$/) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      });
    }

    if (pathname.match(/\/direct-purchase-receipts\/?$/) && method === "POST") {
      tracker.directCalls += 1;
      tracker.stockCalls += 1;
      const body = route.request().postDataJSON() as {
        lines?: Array<{ expiryDate?: string }>;
      };
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify({
          directPurchaseReceiptId: DPR_ID,
          organizationId: E2E_ORG_ID,
          receiptNumber: "DPR-1",
          purchaseDate: "2026-08-21",
          supplierId: null,
          sourceNameSnapshot: "Market Stall",
          referenceNumber: null,
          notes: null,
          totalCost: 100,
          createdByUserId: "99999999-9999-4999-8999-999999999999",
          createdAtUtc: "2026-08-21T02:00:00Z",
          lines: [
            {
              lineId: "14141414-1414-4141-8141-141414141414",
              productId: PRODUCT_ID,
              lineNumber: 1,
              productNameSnapshot: "Rice 25kg",
              skuSnapshot: "RICE25",
              unitOfMeasure: "kg",
              quantity: 2,
              unitCost: 50,
              lineTotal: 100,
              expiryDate: body.lines?.[0]?.expiryDate ?? "2026-12-01",
              lotNumber: "L1",
              inventoryMovementId: "15151515-1515-4151-8151-151515151515",
            },
          ],
        }),
      });
    }

    if (pathname.endsWith(`/direct-purchase-receipts/${DPR_ID}`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          directPurchaseReceiptId: DPR_ID,
          organizationId: E2E_ORG_ID,
          receiptNumber: "DPR-1",
          purchaseDate: "2026-08-21",
          supplierId: null,
          sourceNameSnapshot: "Market Stall",
          referenceNumber: null,
          notes: null,
          totalCost: 100,
          createdByUserId: "99999999-9999-4999-8999-999999999999",
          createdAtUtc: "2026-08-21T02:00:00Z",
          lines: [
            {
              lineId: "14141414-1414-4141-8141-141414141414",
              productId: PRODUCT_ID,
              lineNumber: 1,
              productNameSnapshot: "Rice 25kg",
              skuSnapshot: "RICE25",
              unitOfMeasure: "kg",
              quantity: 2,
              unitCost: 50,
              lineTotal: 100,
              expiryDate: "2026-12-01",
              lotNumber: "L1",
              inventoryMovementId: "15151515-1515-4151-8151-151515151515",
            },
          ],
        }),
      });
    }

    return route.fallback();
  });

  await page.route("**/pos-api/api/v1/pos/inventory**", async (route) => {
    tracker.inventoryCalls += 1;
    return route.fallback();
  });

  return tracker;
}

async function signInOwnerOperations(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-operations")
    .waitFor({ state: "visible", timeout: 15000 });
  await page.getByTestId("workspace-destination-operations").click();
  await page.getByTestId("open-purchasing").waitFor({ state: "visible", timeout: 15000 });
}

test.describe("RMAP-17 purchasing + receiving", () => {
  test("PO create → submit → partial → complete; inventory only after receive", async ({
    page,
  }) => {
    await mockBoundOwnerSession(page);
    const tracker = await mockPurchasingApi(page);
    await signInOwnerOperations(page);

    await clientNavigate(page, "/purchasing/new");
    await expect(page.getByTestId("purchase-order-create-page")).toBeVisible();
    await page.getByTestId("po-supplier").selectOption(SUPPLIER_ID);
    await page.getByTestId("po-product-search").fill("Rice");
    await page.getByTestId(`po-product-${PRODUCT_ID}`).click();
    await page.getByTestId("po-line-qty").fill("10");
    await page.getByTestId("po-line-cost").fill("50");
    await page.getByTestId("po-add-line").click();
    await page.getByTestId("po-create-submit").click();
    await expect(page.getByTestId("purchase-order-detail-page")).toBeVisible();
    expect(tracker.createCalls).toBe(1);
    expect(tracker.receiveCalls).toBe(0);
    expect(tracker.inventoryCalls).toBe(0);

    await page.getByTestId("po-submit").click();
    await expect(page.getByTestId("po-banner")).toContainText(/submitted|Inventory unchanged/i);
    expect(tracker.submitCalls).toBe(1);
    expect(tracker.receiveCalls).toBe(0);
    expect(tracker.inventoryCalls).toBe(0);

    await page.getByTestId("po-receive").click();
    await expect(page.getByTestId("purchase-order-receive-page")).toBeVisible();
    await page.getByTestId(`receive-good-${PRODUCT_ID}`).fill("4");
    await page.getByTestId("receive-review").click();
    await page.getByTestId("receive-confirm").click();
    await expect(page.getByTestId("purchase-order-detail-page")).toBeVisible();
    expect(tracker.receiveCalls).toBe(1);

    await page.getByTestId("po-receive").click();
    await page.getByTestId(`receive-good-${PRODUCT_ID}`).fill("6");
    await page.getByTestId("receive-review").click();
    await page.getByTestId("receive-confirm").click();
    await expect(page.getByTestId("purchase-order-detail-page")).toBeVisible();
    expect(tracker.receiveCalls).toBe(2);
    expect(tracker.inventoryCalls).toBe(0);
  });

  test("connected receive gate blocks receive CTA", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPurchasingApi(page, { denyReceiveConnected: true });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/purchasing/${PO_ID}`);
    await expect(page.getByTestId("po-receive-gated")).toBeVisible();
    await expect(page.getByTestId("po-receive")).toHaveCount(0);
  });

  test("direct buy with expiry increases stock path", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const tracker = await mockPurchasingApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, "/purchasing/receive-stock");
    await page.getByTestId("direct-source-name").fill("Market Stall");
    await page.getByTestId("direct-product-search").fill("Rice");
    await page.getByTestId(`direct-product-${PRODUCT_ID}`).click();
    await page.getByTestId("direct-line-qty").fill("2");
    await page.getByTestId("direct-line-cost").fill("50");
    await page.getByTestId("direct-line-expiry").fill("2026-12-01");
    await page.getByTestId("direct-save-line").click();
    await page.getByTestId("direct-review").click();
    await page.getByTestId("direct-confirm").click();
    await expect(page.getByTestId("direct-purchase-detail-page")).toBeVisible();
    expect(tracker.directCalls).toBe(1);
  });

  test("wrong org PO detail not found", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPurchasingApi(page, { wrongOrg: true });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/purchasing/${PO_ID}`);
    await expect(page.getByText(/not found/i)).toBeVisible();
  });

  test("cashier denied purchasing hub", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/purchasing");
    await expect(page.getByTestId("purchasing-hub-denied")).toBeVisible();
  });

  test("locale smoke Filipino purchasing title", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPurchasingApi(page);
    await signInOwnerOperations(page);
    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: /Preferences|Mga setting/i }).click();
    await page.getByRole("radio", { name: /Filipino/i }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "fil-PH");
    await page.getByTestId("preferences-close").click();
    await clientNavigate(page, "/purchasing");
    await expect(page.getByTestId("purchasing-hub-page")).toBeVisible();
    await expect(page.getByTestId("purchasing-hub-page")).toContainText(/Pagbili/i);
  });

  for (const viewport of VIEWPORTS) {
    test(`responsive purchasing hub ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundOwnerSession(page);
      await mockPurchasingApi(page);
      await signInOwnerOperations(page);
      await clientNavigate(page, "/purchasing");
      await expect(page.getByTestId("purchasing-hub-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

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

const RELATIONSHIP_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const SUPPLIER_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const EXPOSURE_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const PRODUCT_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const LINK_ID = "99999999-9999-4999-8999-999999999999";
const BUYER_PRODUCT_ID = "88888888-8888-4888-8888-888888888888";

type ApiTracker = {
  urls: string[];
  shareCalls: number;
  linkCalls: number;
  createLinkCalls: number;
  unlinkCalls: number;
  inventoryCalls: number;
  catalogPage: number;
};

function relationship(overrides: Record<string, unknown> = {}) {
  return {
    relationshipId: RELATIONSHIP_ID,
    buyerOrganizationId: E2E_ORG_ID,
    supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
    status: "Pending",
    requestedAtUtc: "2026-08-01T00:00:00Z",
    requestedByUserId: null,
    respondedAtUtc: null,
    respondedByUserId: null,
    disconnectedAtUtc: null,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    counterpartyDisplayName: "Island Wholesale",
    counterpartyPublicOrganizationId: "ORG000042",
    ...overrides,
  };
}

function supplier(overrides: Record<string, unknown> = {}) {
  return {
    supplierId: SUPPLIER_ID,
    organizationId: E2E_ORG_ID,
    supplierCode: "SUP0009",
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
    connectionType: "ConnectedOrganization",
    connectedRelationshipId: RELATIONSHIP_ID,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    ...overrides,
  };
}

async function mockConnectedApi(
  page: import("@playwright/test").Page,
  opts: { emptyCatalog?: boolean; denyCatalog?: boolean; wrongOrg?: boolean } = {},
): Promise<ApiTracker> {
  const tracker: ApiTracker = {
    urls: [],
    shareCalls: 0,
    linkCalls: 0,
    createLinkCalls: 0,
    unlinkCalls: 0,
    inventoryCalls: 0,
    catalogPage: 1,
  };

  let pending = [relationship()];
  let activeSupplierSide = [] as ReturnType<typeof relationship>[];
  let shared = false;
  let buyerPrice: number | null = null;
  let linked = false;

  await page.route("**/pos-api/api/v1/pos/connected-suppliers**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;
    tracker.urls.push(`${method} ${pathname}`);

    if (pathname.includes("/connected-suppliers/relationships/request") && method === "POST") {
      const body = route.request().postDataJSON() as {
        supplierPublicOrganizationIdOrQrPayload?: string;
      };
      const payload = body.supplierPublicOrganizationIdOrQrPayload ?? "";
      if (
        /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(
          payload,
        )
      ) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "A Guid alone is not accepted.",
            errorCode: "domain.connected_supplier.requires_business_qr",
          }),
        });
      }
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(relationship({ status: "Pending" })),
      });
    }

    if (
      pathname.match(/\/connected-suppliers\/relationships\/?$/) ||
      (pathname.endsWith("/relationships") && method === "GET")
    ) {
      const view = new URL(url).searchParams.get("view") ?? "buyer";
      const rows =
        view === "supplier"
          ? [...pending, ...activeSupplierSide]
          : [relationship({ status: "Active", respondedAtUtc: "2026-08-02T00:00:00Z" })];
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(rows),
      });
    }

    if (pathname.endsWith(`/relationships/${RELATIONSHIP_ID}/approve`) && method === "POST") {
      pending = [];
      activeSupplierSide = [
        relationship({ status: "Active", respondedAtUtc: "2026-08-02T00:00:00Z" }),
      ];
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(activeSupplierSide[0]),
      });
    }

    if (pathname.endsWith(`/relationships/${RELATIONSHIP_ID}/decline`) && method === "POST") {
      pending = [];
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(relationship({ status: "Declined" })),
      });
    }

    if (pathname.includes("/buyer-product-shares")) {
      if (method === "POST" && pathname.includes("/bulk")) {
        tracker.shareCalls += 1;
        shared = true;
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ affectedCount: 1 }),
        });
      }
      if (method === "POST" && pathname.includes("/pricing/preview")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            affectedCount: 1,
            truncated: false,
            items: [
              {
                supplierProductId: PRODUCT_ID,
                name: "Rice 25kg",
                defaultPoPrice: 15,
                currentBuyerPrice: null,
                proposedBuyerPrice: 12.5,
                proposedEffectivePrice: 12.5,
              },
            ],
          }),
        });
      }
      if (method === "POST" && pathname.includes("/pricing/apply")) {
        tracker.shareCalls += 1;
        buyerPrice = 12.5;
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ affectedCount: 1 }),
        });
      }
      if (method === "GET") {
        const pageNum = Number(new URL(url).searchParams.get("page") ?? "1");
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            items: [
              {
                shareId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
                relationshipId: RELATIONSHIP_ID,
                buyerOrganizationId: E2E_ORG_ID,
                supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
                supplierProductId: PRODUCT_ID,
                isShared: shared,
                buyerSpecificPoPrice: buyerPrice,
                effectiveSupplierOrderPrice: buyerPrice ?? 15,
                syncVersion: 1,
                createdAtUtc: "2026-08-01T00:00:00Z",
                updatedAtUtc: "2026-08-01T00:00:00Z",
                nameSnapshot: "Rice 25kg",
                skuSnapshot: "RICE25",
                defaultPoPrice: 15,
              },
            ],
            matchingCount: 30,
            eligibleCount: 5,
            sharedCount: shared ? 1 : 0,
            page: pageNum,
            pageSize: 25,
            categories: [],
          }),
        });
      }
    }

    if (pathname.includes("/catalog/readiness")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          relationshipId: RELATIONSHIP_ID,
          ready: linked ? 1 : 0,
          new: linked ? 0 : 1,
          review: 0,
          conflict: 0,
          items: [
            {
              exposureId: EXPOSURE_ID,
              supplierProductId: PRODUCT_ID,
              supplierName: "Rice 25kg",
              supplierSku: "RICE25",
              supplierBarcode: null,
              unitOfMeasureCode: "Bag",
              poPrice: 15,
              status: linked ? "Ready" : "New",
              canAutoLink: false,
              candidateBuyerProductId: null,
              candidateBuyerProductName: null,
              nameMatched: false,
              skuMatched: false,
              barcodeMatched: false,
              unitCompatible: true,
              matchDetails: "",
              linkedBuyerProductId: linked ? BUYER_PRODUCT_ID : null,
            },
          ],
        }),
      });
    }

    if (pathname.includes("/match-suggestions")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          exposureId: EXPOSURE_ID,
          supplierName: "Rice 25kg",
          supplierSku: "RICE25",
          unitOfMeasureCode: "Bag",
          poPrice: 15,
          candidates: [
            {
              productId: BUYER_PRODUCT_ID,
              name: "Local Rice",
              sku: "LRICE",
              unitOfMeasure: "Bag",
              sellingPrice: 20,
              matchKind: "ExactName",
            },
          ],
        }),
      });
    }

    if (pathname.includes("/catalog") && method === "GET" && !pathname.includes("/match")) {
      if (opts.denyCatalog) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({ detail: "ViewPurchasing required." }),
        });
      }
      const pageNum = Number(new URL(url).searchParams.get("page") ?? "1");
      const query = new URL(url).searchParams.get("query") ?? "";
      tracker.catalogPage = pageNum;
      if (opts.emptyCatalog) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 25 }),
        });
      }
      if (query && !query.toLowerCase().includes("rice")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 25 }),
        });
      }
      const items =
        pageNum === 1
          ? [
              {
                exposureId: EXPOSURE_ID,
                supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
                productId: PRODUCT_ID,
                skuSnapshot: "RICE25",
                nameSnapshot: "Rice 25kg",
                categoryNameSnapshot: null,
                unitOfMeasureCode: "Bag",
                supplierOrderPrice: 15,
                isOrderable: true,
                isExposed: true,
                syncVersion: 1,
                createdAtUtc: "2026-08-01T00:00:00Z",
                updatedAtUtc: "2026-08-01T00:00:00Z",
              },
            ]
          : [
              {
                exposureId: "abababab-abab-4aba-8aba-abababababab",
                supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
                productId: "cdcdcdcd-cdcd-4cdc-8cdc-cdcdcdcdcdcd",
                skuSnapshot: "OIL1L",
                nameSnapshot: "Cooking Oil",
                categoryNameSnapshot: null,
                unitOfMeasureCode: "Bottle",
                supplierOrderPrice: 80,
                isOrderable: true,
                isExposed: true,
                syncVersion: 1,
                createdAtUtc: "2026-08-01T00:00:00Z",
                updatedAtUtc: "2026-08-01T00:00:00Z",
              },
            ];
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items,
          totalCount: 30,
          page: pageNum,
          pageSize: 25,
        }),
      });
    }

    if (pathname.endsWith("/links/create-and-link") && method === "POST") {
      tracker.createLinkCalls += 1;
      linked = true;
      const link = {
        linkId: LINK_ID,
        relationshipId: RELATIONSHIP_ID,
        buyerOrganizationId: E2E_ORG_ID,
        supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
        buyerProductId: BUYER_PRODUCT_ID,
        supplierProductId: PRODUCT_ID,
        supplierSkuSnapshot: "RICE25",
        supplierNameSnapshot: "Rice 25kg",
        unitOfMeasureCode: "Bag",
        lastKnownOrderPrice: 15,
        isActive: true,
        syncVersion: 1,
        createdAtUtc: "2026-08-01T00:00:00Z",
        updatedAtUtc: "2026-08-01T00:00:00Z",
      };
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          link,
          buyerProductId: BUYER_PRODUCT_ID,
          buyerProductName: "Rice 25kg",
          buyerSku: "RICE25",
          buyerSellingPrice: 20,
          createdNewProduct: true,
          alreadyLinked: false,
        }),
      });
    }

    if (pathname.endsWith("/links") && method === "POST") {
      tracker.linkCalls += 1;
      linked = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          linkId: LINK_ID,
          relationshipId: RELATIONSHIP_ID,
          buyerOrganizationId: E2E_ORG_ID,
          supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
          buyerProductId: BUYER_PRODUCT_ID,
          supplierProductId: PRODUCT_ID,
          supplierSkuSnapshot: "RICE25",
          supplierNameSnapshot: "Rice 25kg",
          unitOfMeasureCode: "Bag",
          lastKnownOrderPrice: 15,
          isActive: true,
          syncVersion: 1,
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        }),
      });
    }

    if (pathname.includes("/links") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          linked
            ? [
                {
                  linkId: LINK_ID,
                  relationshipId: RELATIONSHIP_ID,
                  buyerOrganizationId: E2E_ORG_ID,
                  supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
                  buyerProductId: BUYER_PRODUCT_ID,
                  supplierProductId: PRODUCT_ID,
                  supplierSkuSnapshot: "RICE25",
                  supplierNameSnapshot: "Rice 25kg",
                  unitOfMeasureCode: "Bag",
                  lastKnownOrderPrice: 15,
                  isActive: true,
                  syncVersion: 1,
                  createdAtUtc: "2026-08-01T00:00:00Z",
                  updatedAtUtc: "2026-08-01T00:00:00Z",
                },
              ]
            : [],
        ),
      });
    }

    if (pathname.includes(`/links/${LINK_ID}`) && method === "DELETE") {
      tracker.unlinkCalls += 1;
      linked = false;
      return route.fulfill({ status: 200, contentType: "application/json", body: "{}" });
    }

    return route.fulfill({
      status: 404,
      contentType: "application/json",
      body: JSON.stringify({ detail: `Unhandled connected mock ${method} ${pathname}` }),
    });
  });

  await page.route("**/pos-api/api/v1/pos/suppliers**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;
    tracker.urls.push(`${method} ${pathname}`);

    if (method === "GET" && pathname.endsWith(`/suppliers/${SUPPLIER_ID}`)) {
      if (opts.wrongOrg) {
        return route.fulfill({
          status: 404,
          contentType: "application/json",
          body: JSON.stringify({ detail: "Not found", errorCode: "pos.supplier.not_found" }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(supplier()),
      });
    }
    if (method === "GET" && pathname.endsWith("/suppliers")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [supplier()],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    }
    return route.continue();
  });

  await page.route("**/pos-api/api/v1/pos/catalog/products**", async (route) => {
    if (route.request().method() !== "GET") {
      return route.continue();
    }
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        items: [
          {
            productId: BUYER_PRODUCT_ID,
            name: "Local Rice",
            sku: "LRICE",
            status: "Active",
            sellingPrice: 20,
            unitOfMeasure: "Bag",
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      }),
    });
  });

  await page.route("**/pos-api/api/v1/pos/inventory**", async (route) => {
    tracker.inventoryCalls += 1;
    return route.fulfill({
      status: 500,
      contentType: "application/json",
      body: JSON.stringify({ detail: "inventory must not be called" }),
    });
  });

  return tracker;
}

test.describe("RMAP-16 connected suppliers", () => {
  test.use({ serviceWorkers: "block" });

  async function signInOwnerOperations(page: import("@playwright/test").Page) {
    await signInAndBindOwner(page);
    await page
      .getByTestId("workspace-destination-operations")
      .waitFor({ state: "visible", timeout: 15000 });
    await page.getByTestId("workspace-destination-operations").click();
    await expect(page.getByTestId("open-suppliers")).toBeVisible({ timeout: 15000 });
  }

  test("connect lifecycle, share, buyer price, catalog, link, unlink, no inventory", async ({
    page,
  }) => {
    await mockBoundOwnerSession(page);
    const tracker = await mockConnectedApi(page);
    await signInOwnerOperations(page);

    await clientNavigate(page, "/suppliers/connected/request");
    await expect(page.getByTestId("connected-request-page")).toBeVisible();
    await page.getByTestId("connected-request-input").fill("11111111-1111-1111-1111-111111111111");
    await page.getByTestId("connected-request-send").click();
    await expect(page.getByTestId("connected-request-error")).toContainText(/Guid/i);

    await page.getByTestId("connected-request-input").fill("ORG000042");
    await page.getByTestId("connected-request-send").click();
    await expect(page.getByTestId("connected-request-success")).toBeVisible();

    await clientNavigate(page, "/suppliers/connected/requests");
    await expect(page.getByTestId("connected-incoming-page")).toBeVisible();
    await page.getByTestId(`connected-approve-${RELATIONSHIP_ID}`).click();
    await expect(page.getByTestId("connected-share-prompt")).toBeVisible();
    await page.getByTestId("connected-share-now").click();
    await expect(page.getByTestId("connected-shared-products-page")).toBeVisible();
    await expect(page.getByTestId("connected-exposable-note")).toBeVisible();

    await page.getByTestId(`connected-share-check-${PRODUCT_ID}`).check();
    await page.getByTestId("connected-bulk-share").click();
    await expect(page.getByTestId("connected-share-message")).toBeVisible();
    expect(tracker.shareCalls).toBeGreaterThan(0);

    await page.getByTestId(`connected-share-check-${PRODUCT_ID}`).check();
    await page.getByTestId("connected-buyer-price-input").fill("12.5");
    await page.getByTestId("connected-apply-buyer-price").click();
    await expect(page.getByTestId("connected-share-message")).toContainText(/1/);

    await page.getByTestId("connected-share-next").click();
    await expect.poll(() => tracker.catalogPage >= 1).toBeTruthy();

    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}/connected-catalog`);
    await expect(page.getByTestId("connected-catalog-page")).toBeVisible();
    await expect(page.getByTestId("connected-readiness-chips")).toBeVisible();
    await page.getByTestId("connected-catalog-search").fill("xyz-no-match");
    await expect(
      page.getByText(
        /No matching|Walang tumugma|Walay tumugma|Awan ti agtugma|Wala sang nagsanto/i,
      ),
    ).toBeVisible({ timeout: 5000 });
    await page.getByTestId("connected-catalog-search").fill("rice");
    await expect(page.getByTestId(`connected-catalog-item-${EXPOSURE_ID}`)).toBeVisible();

    await page.getByTestId("connected-catalog-next").click();
    await expect.poll(() => tracker.catalogPage).toBe(2);

    await page.getByTestId("connected-catalog-prev").click();
    await page.getByTestId(`connected-create-link-${EXPOSURE_ID}`).click();
    await expect.poll(() => tracker.createLinkCalls).toBeGreaterThan(0);

    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}/linked-products`);
    await expect(page.getByTestId("linked-products-page")).toBeVisible();
    await page.getByTestId(`linked-unlink-${LINK_ID}`).click();
    await expect.poll(() => tracker.unlinkCalls).toBe(1);

    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}/connected-catalog`);
    await page.getByTestId(`connected-link-${EXPOSURE_ID}`).click();
    await expect.poll(() => tracker.linkCalls).toBeGreaterThan(0);

    expect(tracker.inventoryCalls).toBe(0);
    expect(
      tracker.urls.every(
        (u) =>
          !u.toLowerCase().includes("/inventory") && !u.toLowerCase().includes("/stock-counts"),
      ),
    ).toBe(true);
  });

  test("empty catalog and cross-org denial", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockConnectedApi(page, { emptyCatalog: true });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}/connected-catalog`);
    await expect(page.getByTestId("connected-catalog-page")).toBeVisible();
    await expect(
      page.getByText(
        /No products shared|Wala pang naka-share|Wala pay shared|Awan pay ti shared|Wala pa sang shared/i,
      ),
    ).toBeVisible();

    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}`);
    // Remount mock for wrong-org detail
    await page.unroute("**/pos-api/api/v1/pos/suppliers**");
    await mockConnectedApi(page, { wrongOrg: true });
    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}`);
    await expect(
      page.getByText(/not found|hindi nahanap|wala makita|saan a nabirokan/i),
    ).toBeVisible();
  });

  test("cashier denied suppliers and locales + responsive smoke", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockConnectedApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/suppliers");
    await expect(page.getByTestId("suppliers-view-denied")).toBeVisible();

    await mockBoundOwnerSession(page);
    await mockConnectedApi(page);
    await signInOwnerOperations(page);
    await page.evaluate(() => {
      localStorage.setItem("exits.pos.ui.locale", "fil-PH");
    });
    await clientNavigate(page, "/suppliers/connected/buyers");
    await expect(page.getByTestId("connected-buyers-page")).toBeVisible();
    await expect(page.getByText(/Mga konektadong buyer|Connected buyers/i)).toBeVisible();

    for (const viewport of VIEWPORTS) {
      await page.setViewportSize(viewport);
      await clientNavigate(page, "/suppliers");
      await expect(page.getByTestId("suppliers-list-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await expect(page.getByTestId("suppliers-connect")).toBeVisible();
    }
  });
});

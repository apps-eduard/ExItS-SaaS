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

const SUPPLIER_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const OTHER_SUPPLIER_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

type SupplierState = {
  creates: Array<Record<string, unknown>>;
  updates: Array<Record<string, unknown>>;
  status: string;
  updatedAtUtc: string;
  conflictOnce: boolean;
  duplicateField: "name" | "email" | "mobile" | "tax" | null;
  listPage: number;
  wrongOrgDetail: boolean;
};

function supplierBody(overrides: Record<string, unknown> = {}) {
  return {
    supplierId: SUPPLIER_ID,
    organizationId: E2E_ORG_ID,
    supplierCode: "SUP0001",
    name: "Metro Wholesale",
    contactPerson: "Ana Cruz",
    mobileNumber: "09171234567",
    telephoneNumber: null,
    email: "ana@example.com",
    addressLine1: "Quezon City",
    addressLine2: null,
    cityMunicipality: "Quezon City",
    province: "Metro Manila",
    postalCode: "1100",
    taxOrRegistrationNumber: "123-456",
    notes: null,
    status: "Active",
    connectionType: "External",
    connectedRelationshipId: null,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    ...overrides,
  };
}

async function mockSuppliersApi(
  page: import("@playwright/test").Page,
  opts: { allowView?: boolean; allowManage?: boolean } = {},
): Promise<SupplierState> {
  const allowView = opts.allowView ?? true;
  const allowManage = opts.allowManage ?? true;
  const state: SupplierState = {
    creates: [],
    updates: [],
    status: "Active",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    conflictOnce: false,
    duplicateField: null,
    listPage: 1,
    wrongOrgDetail: false,
  };

  await page.route("**/pos-api/api/v1/pos/suppliers**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname.replace(/\/$/, "");
    const search = new URL(url).searchParams;

    if (!allowView) {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "ViewSuppliers is required.",
          errorCode: "application.auth.capability.denied",
        }),
      });
    }

    if (method === "GET" && pathname.endsWith("/suppliers")) {
      const pageNum = Number(search.get("page") ?? "1");
      state.listPage = pageNum;
      const code = search.get("supplierCode") ?? "";
      const name = search.get("name") ?? "";
      const items =
        pageNum === 1
          ? [
              supplierBody({
                status: state.status,
                updatedAtUtc: state.updatedAtUtc,
                ...(code ? { supplierCode: code.toUpperCase() } : {}),
                ...(name ? { name: `Match ${name}` } : {}),
              }),
            ]
          : [
              supplierBody({
                supplierId: OTHER_SUPPLIER_ID,
                supplierCode: "SUP0002",
                name: "Second Page Co",
                status: state.status,
              }),
            ];
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items,
          totalCount: 25,
          page: pageNum,
          pageSize: 20,
        }),
      });
    }

    if (method === "POST" && pathname.endsWith("/suppliers")) {
      if (!allowManage) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "ManageSuppliers is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }
      const body = route.request().postDataJSON() as Record<string, unknown>;
      if (state.duplicateField === "name") {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Name conflict",
            errorCode: "pos.supplier.name.conflict",
          }),
        });
      }
      if (state.duplicateField === "email") {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Email conflict",
            errorCode: "pos.supplier.email.conflict",
          }),
        });
      }
      if (state.duplicateField === "mobile") {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Mobile conflict",
            errorCode: "pos.supplier.mobile.conflict",
          }),
        });
      }
      if (state.duplicateField === "tax") {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Tax conflict",
            errorCode: "pos.supplier.tax_number.conflict",
          }),
        });
      }
      state.creates.push(body);
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(
          supplierBody({
            name: String(body.name ?? "New"),
            mobileNumber: body.mobileNumber ?? null,
            email: body.email ?? null,
          }),
        ),
      });
    }

    if (method === "GET" && pathname.endsWith(`/suppliers/${SUPPLIER_ID}`)) {
      if (state.wrongOrgDetail) {
        return route.fulfill({
          status: 404,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Supplier was not found.",
            errorCode: "pos.supplier.not_found",
          }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          supplierBody({ status: state.status, updatedAtUtc: state.updatedAtUtc }),
        ),
      });
    }

    if (method === "PUT" && pathname.endsWith(`/suppliers/${SUPPLIER_ID}`)) {
      if (!allowManage) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "ManageSuppliers is required.",
            errorCode: "application.auth.capability.denied",
          }),
        });
      }
      const body = route.request().postDataJSON() as Record<string, unknown>;
      if (state.conflictOnce) {
        state.conflictOnce = false;
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Stale",
            errorCode: "pos.supplier.concurrency_conflict",
          }),
        });
      }
      state.updates.push(body);
      state.updatedAtUtc = "2026-08-21T12:00:00Z";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          supplierBody({
            name: String(body.name ?? "Metro Wholesale"),
            status: state.status,
            updatedAtUtc: state.updatedAtUtc,
          }),
        ),
      });
    }

    if (method === "POST" && pathname.endsWith(`/suppliers/${SUPPLIER_ID}/deactivate`)) {
      state.status = "Inactive";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(supplierBody({ status: "Inactive" })),
      });
    }

    if (method === "POST" && pathname.endsWith(`/suppliers/${SUPPLIER_ID}/activate`)) {
      state.status = "Active";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(supplierBody({ status: "Active" })),
      });
    }

    return route.fallback();
  });

  return state;
}

async function signInOwnerOperations(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-operations")
    .waitFor({ state: "visible", timeout: 15000 });
  await page.getByTestId("workspace-destination-operations").click();
  await expect(page.getByTestId("open-suppliers")).toBeVisible({ timeout: 15000 });
}

test.describe("RMAP-15 suppliers", () => {
  test.use({ serviceWorkers: "block" });

  test("Owner lists, searches by code, and paginates", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockSuppliersApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, "/suppliers");
    await expect(page.getByTestId("suppliers-list-page")).toBeVisible();
    await expect(page.getByTestId(`supplier-row-${SUPPLIER_ID}`)).toBeVisible();

    await page.getByTestId("suppliers-search").fill("SUP0099");
    await expect(page.getByTestId(`supplier-row-${SUPPLIER_ID}`)).toContainText("SUP0099");

    await page.getByTestId("suppliers-next").click();
    await expect(page.getByTestId(`supplier-row-${OTHER_SUPPLIER_ID}`)).toBeVisible();
    expect(state.listPage).toBe(2);
  });

  test("Owner creates supplier", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockSuppliersApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, "/suppliers/new");
    await expect(page.getByTestId("supplier-add-chooser")).toBeVisible();
    await page.getByTestId("supplier-add-manual").click();
    await expect(page.getByTestId("supplier-form-page")).toBeVisible();
    await page.getByTestId("supplier-name").fill("Fresh Farms");
    await page.getByTestId("supplier-mobile").fill("09180001111");
    await page.getByTestId("supplier-save").click();
    await expect(page.getByTestId("supplier-detail-page")).toBeVisible();
    expect(state.creates[0]?.name).toBe("Fresh Farms");
  });

  test("Owner edit concurrency conflict is friendly", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockSuppliersApi(page);
    state.conflictOnce = true;
    await signInOwnerOperations(page);
    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}/edit`);
    await expect(page.getByTestId("supplier-form-page")).toBeVisible();
    await page.getByTestId("supplier-name").fill("Metro Updated");
    await page.getByTestId("supplier-save").click();
    await expect(page.getByTestId("supplier-form-error")).toContainText(
      /changed elsewhere|Reload/i,
    );
  });

  test("Owner deactivates and activates", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockSuppliersApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}`);
    await expect(page.getByTestId("supplier-detail-page")).toBeVisible();
    await expect(page.getByText("Manual")).toBeVisible();
    await page.getByTestId("supplier-toggle-status").click();
    await expect(page.getByTestId("supplier-toggle-status")).toContainText(/Activate/i);
    await page.getByTestId("supplier-toggle-status").click();
    await expect(page.getByTestId("supplier-toggle-status")).toContainText(/Deactivate/i);
  });

  test("Duplicate name shows friendly conflict", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockSuppliersApi(page);
    state.duplicateField = "name";
    await signInOwnerOperations(page);
    await clientNavigate(page, "/suppliers/new");
    await page.getByTestId("supplier-add-manual").click();
    await page.getByTestId("supplier-name").fill("Taken Name");
    await page.getByTestId("supplier-save").click();
    await expect(page.getByTestId("supplier-form-error")).toContainText(/name already exists/i);
  });

  test("Wrong organization supplier is not found", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockSuppliersApi(page);
    state.wrongOrgDetail = true;
    await signInOwnerOperations(page);
    await clientNavigate(page, `/suppliers/${SUPPLIER_ID}`);
    await expect(page.getByText(/not found/i)).toBeVisible();
  });

  test("Cashier is denied suppliers list", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockSuppliersApi(page, { allowView: false });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/suppliers");
    await expect(page.getByTestId("suppliers-view-denied")).toBeVisible();
    await expect(page.getByTestId("suppliers-list-page")).toHaveCount(0);
  });

  test("locale smoke shows Filipino suppliers title", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockSuppliersApi(page);
    await signInOwnerOperations(page);
    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: /Preferences|Mga setting/i }).click();
    await page.getByRole("radio", { name: /Filipino/i }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "fil-PH");
    await page.getByTestId("preferences-close").click();
    await clientNavigate(page, "/suppliers");
    await expect(page.getByTestId("suppliers-list-page")).toContainText("Mga supplier");
  });

  for (const viewport of VIEWPORTS) {
    test(`suppliers list usable at ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundOwnerSession(page);
      await mockSuppliersApi(page);
      await signInOwnerOperations(page);
      await clientNavigate(page, "/suppliers");
      await expect(page.getByTestId("suppliers-list-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

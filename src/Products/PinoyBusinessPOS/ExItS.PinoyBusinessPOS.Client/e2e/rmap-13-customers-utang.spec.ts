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

const CUSTOMER_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const CREDIT_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const REPAYMENT_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const NET_AMOUNT_TO_PAY = 18.5;

type CustomerState = {
  creates: Array<Record<string, unknown>>;
  updates: Array<Record<string, unknown>>;
  repayments: Array<Record<string, unknown>>;
  outstanding: number;
  status: string;
};

function customerBody(overrides: Record<string, unknown> = {}) {
  return {
    customerId: CUSTOMER_ID,
    organizationId: E2E_ORG_ID,
    displayName: "Juan Dela Cruz",
    mobileNumber: "09171234567",
    address: "Manila",
    notes: null,
    status: "Active",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    linkedPersonalPublicUserId: null,
    ...overrides,
  };
}

async function mockCustomersUtangApi(
  page: import("@playwright/test").Page,
  opts: { allowView?: boolean } = {},
): Promise<CustomerState> {
  const allowView = opts.allowView ?? true;
  const state: CustomerState = {
    creates: [],
    updates: [],
    repayments: [],
    outstanding: NET_AMOUNT_TO_PAY,
    status: "Active",
  };

  await page.route("**/pos-api/api/v1/pos/customers**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname.replace(/\/$/, "");

    if (!allowView) {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "ViewCustomersAndHistory is required.",
          errorCode: "application.auth.capability.denied",
        }),
      });
    }

    if (method === "GET" && pathname.endsWith("/customers")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [customerBody({ status: state.status })],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        }),
      });
    }

    if (method === "POST" && pathname.endsWith("/customers")) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.creates.push(body);
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(
          customerBody({
            displayName: String(body.displayName ?? "New"),
            mobileNumber: body.mobileNumber ?? null,
            address: body.address ?? null,
            notes: body.notes ?? null,
          }),
        ),
      });
    }

    if (method === "GET" && pathname.endsWith(`/customers/${CUSTOMER_ID}`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(customerBody({ status: state.status })),
      });
    }

    if (method === "PUT" && pathname.endsWith(`/customers/${CUSTOMER_ID}`)) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.updates.push(body);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          customerBody({
            displayName: String(body.displayName ?? "Juan Dela Cruz"),
            status: state.status,
          }),
        ),
      });
    }

    if (method === "POST" && pathname.endsWith(`/customers/${CUSTOMER_ID}/deactivate`)) {
      state.status = "Inactive";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(customerBody({ status: "Inactive" })),
      });
    }

    if (method === "POST" && pathname.endsWith(`/customers/${CUSTOMER_ID}/reactivate`)) {
      state.status = "Active";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(customerBody({ status: "Active" })),
      });
    }

    if (method === "GET" && pathname.endsWith(`/customers/${CUSTOMER_ID}/credit-summary`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          customerId: CUSTOMER_ID,
          organizationId: E2E_ORG_ID,
          outstandingAmount: state.outstanding,
          activeEntryCount: 1,
          totalEntryCount: 1,
        }),
      });
    }

    if (method === "GET" && pathname.includes(`/customers/${CUSTOMER_ID}/credit-entries`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              creditEntryId: CREDIT_ID,
              organizationId: E2E_ORG_ID,
              customerId: CUSTOMER_ID,
              amount: NET_AMOUNT_TO_PAY,
              remarks: "Sale S-9012",
              status: "Active",
              createdAtUtc: "2026-08-21T02:00:00Z",
              sourceSaleId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    }

    if (method === "GET" && pathname.includes(`/customers/${CUSTOMER_ID}/repayments`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: state.repayments.map((item, index) => ({
            repaymentId: `${REPAYMENT_ID.slice(0, -1)}${index}`,
            organizationId: E2E_ORG_ID,
            customerId: CUSTOMER_ID,
            amount: Number(item.amount),
            remarks: item.remarks ?? null,
            status: "Active",
            recordedAtUtc: "2026-08-21T03:00:00Z",
            recordedBy: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          })),
          totalCount: state.repayments.length,
          page: 1,
          pageSize: 20,
        }),
      });
    }

    if (method === "POST" && pathname.endsWith(`/customers/${CUSTOMER_ID}/repayments`)) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.repayments.push(body);
      state.outstanding = Math.max(0, state.outstanding - Number(body.amount));
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify({
          repaymentId: REPAYMENT_ID,
          organizationId: E2E_ORG_ID,
          customerId: CUSTOMER_ID,
          amount: Number(body.amount),
          remarks: body.remarks ?? null,
          status: "Active",
          recordedAtUtc: "2026-08-21T03:00:00Z",
          recordedBy: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        }),
      });
    }

    if (method === "GET" && pathname.includes(`/customers/${CUSTOMER_ID}/statement`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: E2E_ORG_ID,
          organizationDisplayName: "E2E Org",
          customerId: CUSTOMER_ID,
          customerDisplayName: "Juan Dela Cruz",
          periodStart: "2026-08-01",
          periodEnd: "2026-08-31",
          openingBalance: 0,
          closingBalance: state.outstanding,
          periodCreditTotal: NET_AMOUNT_TO_PAY,
          periodRepaymentTotal: NET_AMOUNT_TO_PAY - state.outstanding,
          periodReversalCreditTotal: 0,
          periodReversalRepaymentTotal: 0,
          outstandingBalance: state.outstanding,
          overdueAmount: 0,
          overdueCreditCount: 0,
          generatedAtUtc: "2026-08-21T04:00:00Z",
          currencyCode: "PHP",
          cultureName: "en-PH",
          lines: [
            {
              entryId: CREDIT_ID,
              entryType: "Credit",
              recordedAtUtc: "2026-08-21T02:00:00Z",
              amount: NET_AMOUNT_TO_PAY,
              signedEffect: NET_AMOUNT_TO_PAY,
              status: "Active",
              remarks: "Sale S-9012",
              dueDate: null,
              dueStatus: null,
              isOverdue: false,
              isReversed: false,
              runningBalance: NET_AMOUNT_TO_PAY,
            },
          ],
        }),
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
  await expect(page.getByTestId("open-customers")).toBeVisible({ timeout: 15000 });
}

test.describe("RMAP-13 customers + Business Utang", () => {
  test.use({ serviceWorkers: "block" });

  test("Owner lists customers, opens detail with Amount owed and discounted credit", async ({
    page,
  }) => {
    await mockBoundOwnerSession(page);
    await mockCustomersUtangApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, "/customers");
    await expect(page.getByTestId("customers-list-page")).toBeVisible();
    await expect(page.getByTestId(`customer-row-${CUSTOMER_ID}`)).toBeVisible();
    await page.getByTestId(`customer-row-${CUSTOMER_ID}`).click();
    await expect(page.getByTestId("customer-detail-page")).toBeVisible();
    await expect(page.getByTestId("customer-amount-owed")).toContainText("Amount owed");
    await expect(page.getByTestId("customer-amount-owed-value")).toContainText("18.50");
    await expect(page.getByTestId(`customer-credit-${CREDIT_ID}`)).toContainText("18.50");
  });

  test("Owner creates customer", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockCustomersUtangApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, "/customers/new");
    await expect(page.getByTestId("customer-form-page")).toBeVisible();
    await page.getByTestId("customer-display-name").fill("Maria Santos");
    await page.getByTestId("customer-mobile").fill("09180001111");
    await page.getByTestId("customer-save").click();
    await expect(page.getByTestId("customer-detail-page")).toBeVisible();
    expect(state.creates[0]?.displayName).toBe("Maria Santos");
  });

  test("Owner records payment and shows Remaining balance preview", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockCustomersUtangApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, `/customers/${CUSTOMER_ID}/repay`);
    await expect(page.getByTestId("customer-repay-page")).toBeVisible();
    await expect(page.getByText("Amount owed")).toBeVisible();
    await page.getByTestId("customer-payment-amount").fill("10");
    await expect(page.getByTestId("customer-remaining-balance")).toContainText("Remaining balance");
    await expect(page.getByTestId("customer-remaining-balance")).toContainText("8.50");
    await page.getByTestId("customer-payment-submit").click();
    await expect(page.getByTestId("customer-detail-page")).toBeVisible();
    expect(state.repayments[0]?.amount).toBe(10);
  });

  test("Owner opens statement", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockCustomersUtangApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, `/customers/${CUSTOMER_ID}/statement`);
    await expect(page.getByTestId("customer-statement-page")).toBeVisible();
    await expect(page.getByTestId("statement-summary")).toContainText("Amount owed");
    await expect(page.getByTestId("statement-lines")).toBeVisible();
  });

  test("Cashier is denied customers list", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockCustomersUtangApi(page, { allowView: false });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/customers");
    await expect(page.getByTestId("customers-view-denied")).toBeVisible();
    await expect(page.getByTestId("customers-list-page")).toHaveCount(0);
  });

  for (const viewport of VIEWPORTS) {
    test(`customers list usable at ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundOwnerSession(page);
      await mockCustomersUtangApi(page);
      await signInOwnerOperations(page);
      await clientNavigate(page, "/customers");
      await expect(page.getByTestId("customers-list-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  chooseOwnerOperations,
  clientNavigate,
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const LOCALES = ["en", "fil-PH", "ceb-PH", "ilo-PH", "hil-PH"] as const;

function overviewBody() {
  return {
    businessDate: "2026-08-21",
    todaySalesTotal: 1500,
    todaySaleCount: 3,
    todayCashSalesTotal: 1000,
    todayUtangSalesTotal: 500,
    todayPaymentsReceived: 200,
    openUtangOutstanding: 800,
    lowStockProductCount: 2,
    expiredLotCount: 1,
    nearExpiryLotCount: 3,
    pendingTransferCount: 0,
    openShiftCount: 1,
    activeRegisterCount: 2,
  };
}

function dashboardBody() {
  return {
    fromDate: "2026-08-21",
    toDate: "2026-08-21",
    completedSalesTotal: 1500,
    completedSaleCount: 3,
    cashSalesTotal: 1000,
    manualGCashSalesTotal: 0,
    utangSalesTotal: 500,
    activeCustomerUtangOutstanding: 800,
    overdueUtangAmount: 100,
    recordedExpenseTotal: 50,
    lowStockProductCount: 2,
    voidedSaleCount: 1,
    voidedExpenseCount: 0,
    salesByDay: [{ date: "2026-08-21", amount: 1500, count: 3 }],
    expensesByDay: [],
    paymentMethodBreakdown: [
      { paymentMethod: "Cash", amount: 1000, count: 2 },
      { paymentMethod: "Utang", amount: 500, count: 1 },
    ],
    salesCountByDay: [{ date: "2026-08-21", amount: 0, count: 3 }],
    salesTotalComparison: null,
    expenseTotalComparison: null,
  };
}

function salesSummaryBody() {
  return {
    fromDate: "2026-08-21",
    toDate: "2026-08-21",
    completedGrossSales: 1500,
    voidedSales: 100,
    completedReturnsRefunds: 50,
    netSales: 1350,
    completedTransactionCount: 3,
    averageTransactionValue: 500,
  };
}

function salesByPaymentBody() {
  return {
    fromDate: "2026-08-21",
    toDate: "2026-08-21",
    rows: [
      {
        paymentMethod: "Cash",
        grossCompleted: 1000,
        voided: 0,
        refunded: 0,
        net: 1000,
      },
      {
        paymentMethod: "ManualGCash",
        grossCompleted: 0,
        voided: 0,
        refunded: 0,
        net: 0,
      },
      {
        paymentMethod: "Utang",
        grossCompleted: 500,
        voided: 0,
        refunded: 0,
        net: 500,
      },
    ],
  };
}

async function mockReportingApis(page: import("@playwright/test").Page) {
  const orgHeaders: string[] = [];

  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const org = route.request().headers()["x-pos-organization-id"];
    if (org) {
      orgHeaders.push(org);
    }

    if (url.includes("/api/v1/pos/management/overview")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(overviewBody()),
      });
    }
    if (url.includes("/api/v1/pos/dashboard")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(dashboardBody()),
      });
    }
    if (url.includes("/api/v1/pos/reports/sales-summary")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(salesSummaryBody()),
      });
    }
    if (url.includes("/api/v1/pos/reports/sales-by-payment")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(salesByPaymentBody()),
      });
    }
    if (url.includes("/api/v1/pos/reports/inventory-status")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          asOfDate: "2026-08-21",
          trackedCount: 10,
          lowStockCount: 2,
          outOfStockCount: 1,
          rows: [],
        }),
      });
    }
    if (url.includes("/api/v1/pos/reports/purchasing-summary")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          fromDate: "2026-08-21",
          toDate: "2026-08-21",
          orderCount: 2,
          orderedQuantity: 40,
          receivedQuantity: 20,
          outstandingQuantity: 20,
          byStatus: [],
        }),
      });
    }
    if (url.includes("/api/v1/pos/reports/sales")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          fromDate: "2026-08-21",
          toDate: "2026-08-21",
          completedSalesTotal: 1500,
          completedSaleCount: 3,
          voidedSalesTotal: 100,
          voidedSaleCount: 1,
          utangSalesTotal: 500,
          utangSaleCount: 1,
          byPaymentMethod: [{ paymentMethod: "Cash", amount: 1000, count: 2 }],
        }),
      });
    }

    // Preserve mock-bound-session operational-branch (and other) handlers.
    return route.fallback();
  });

  return { orgHeaders };
}

async function bindOwnerOperations(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-operations")
    .waitFor({ state: "visible", timeout: 15000 });
  await chooseOwnerOperations(page);
  await page.getByTestId("open-dashboard").waitFor({ state: "visible", timeout: 15000 });
}

test.describe("RMAP-20 reports + management dashboard", () => {
  test("owner dashboard and reports hub — no tax / fake P&L / buyer projection", async ({
    page,
  }) => {
    await mockBoundOwnerSession(page);
    const { orgHeaders } = await mockReportingApis(page);
    await bindOwnerOperations(page);
    await clientNavigate(page, "/dashboard");

    await expect(page.getByTestId("management-dashboard-page")).toBeVisible();
    await expect(page.getByTestId("kpi-today-sales")).toBeVisible();
    await expect(page.getByTestId("report-timezone-note")).toBeVisible();
    await expect(page.getByTestId("report-branch-filter")).toBeVisible();
    await expect(page.getByTestId("dashboard-no-pnl")).toHaveCount(0);
    await expect(page.getByTestId("dashboard-export-deferred")).toHaveCount(0);
    await expect(
      page.getByText(/RMAP_TAX_AUTHORIZED|RMAP-B04|contracts are not proven/i),
    ).toHaveCount(0);
    await expect(page.getByText(/ManualGCash/i)).toHaveCount(0);

    await page.getByTestId("report-preset-thisMonth").click();
    await expect(page.getByTestId("report-active-range")).toContainText("2026-08-01");

    await clientNavigate(page, "/reports");
    await expect(page.getByTestId("reports-hub-page")).toBeVisible();
    await expect(page.getByTestId("reports-no-tax")).toHaveCount(0);
    await expect(page.getByTestId("reports-no-pnl")).toHaveCount(0);
    await expect(page.getByTestId("reports-no-buyer-projection")).toHaveCount(0);
    await expect(page.getByTestId("reports-export-deferred")).toHaveCount(0);
    await expect(page.getByTestId("report-link-sales-summary")).toBeVisible();
    await expect(page.getByTestId("report-link-tax")).toHaveCount(0);
    await expect(page.getByTestId("report-link-pnl")).toHaveCount(0);
    await expect(
      page.locator(
        'a[href*="/tax"], a[href*="/vat"], a[href*="/bir"], a[href*="/pnl"], a[href*="profit-loss"], a[href*="purchase-history"]',
      ),
    ).toHaveCount(0);
    await expect(page.getByText(/RMAP_TAX_AUTHORIZED|RMAP-B04|Tax \/ VAT \/ BIR/i)).toHaveCount(0);
    await expect(page.getByText(/backend supports|contracts are not proven/i)).toHaveCount(0);

    await page.getByTestId("report-link-sales-summary").click();
    await expect(page.getByTestId("operational-report-page")).toBeVisible();
    await expect(page.getByTestId("report-results")).toContainText("Gross");
    await expect(page.getByTestId("report-results")).toContainText("Net");
    await expect(page.getByTestId("report-results")).toContainText("Commercial discounts");

    await page.getByTestId("report-back").click();
    await page.getByTestId("report-link-sales-by-payment").click();
    await expect(page.getByTestId("report-results")).toContainText("Cash");
    await expect(page.getByTestId("report-results")).toContainText("GCash");
    await expect(page.getByTestId("report-results")).toContainText("Utang");
    await expect(page.getByText(/ManualGCash/i)).toHaveCount(0);

    expect(orgHeaders.length).toBeGreaterThan(0);
  });

  test("cashier denied dashboard and sales reports; shifts allowed", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockReportingApis(page);
    await signInAndBindCashier(page);

    await clientNavigate(page, "/dashboard");
    await expect(page.getByTestId("dashboard-view-denied")).toBeVisible();

    await clientNavigate(page, "/reports");
    await expect(page.getByTestId("reports-hub-page")).toBeVisible();
    await expect(page.getByTestId("report-link-sales-summary")).toHaveCount(0);
    await expect(page.getByTestId("report-link-shifts")).toBeVisible();

    await clientNavigate(page, "/reports/operational/sales-summary");
    await expect(page).toHaveURL(/\/reports$/);

    await clientNavigate(page, "/personal/purchase-history");
    await expect(
      page.getByRole("heading", { name: /Page not found|Account type not allowed|Hindi/i }),
    ).toBeVisible();
  });

  test("responsive dashboard and reports hub", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReportingApis(page);
    await bindOwnerOperations(page);

    for (const viewport of VIEWPORTS) {
      await page.setViewportSize(viewport);
      await clientNavigate(page, "/dashboard");
      await expect(page.getByTestId("management-dashboard-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await clientNavigate(page, "/reports");
      await expect(page.getByTestId("reports-hub-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }
  });

  test("locale switch keeps reports hub labels", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReportingApis(page);
    await bindOwnerOperations(page);

    for (const locale of LOCALES) {
      await page.evaluate(
        ([key, code]) => {
          localStorage.setItem(key, JSON.stringify({ theme: "system", locale: code }));
        },
        ["exits.pos-client.ui-preferences.v1", locale] as const,
      );
      await page.reload();
      await expect(page.locator("html")).toHaveAttribute("lang", locale);
      // Reload restores session but may return to experience chooser — re-bind operations.
      const ops = page.getByTestId("workspace-destination-operations");
      const dash = page.getByTestId("open-dashboard");
      await Promise.race([
        ops.waitFor({ state: "visible", timeout: 15000 }),
        dash.waitFor({ state: "visible", timeout: 15000 }),
        page.getByTestId("reports-hub-page").waitFor({ state: "visible", timeout: 15000 }),
      ]);
      if (await ops.isVisible().catch(() => false)) {
        await chooseOwnerOperations(page);
        await dash.waitFor({ state: "visible", timeout: 15000 });
      }
      await clientNavigate(page, "/reports");
      await expect(page.getByTestId("reports-hub-page")).toBeVisible();
      await expect(page.getByTestId("report-link-sales-summary")).toBeVisible();
      await expect(page.getByText(/RMAP_TAX_AUTHORIZED|RMAP-B04/i)).toHaveCount(0);
    }
  });
});

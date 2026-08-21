import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  chooseOwnerOperations,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

async function openClosedShiftDetail(
  page: import("@playwright/test").Page,
  history: NonNullable<Parameters<typeof mockPosRegisterShiftApi>[1]>["history"] & {
    openingCashCountMode?: "Optional" | "Required";
    closingCashCountMode?: "Optional" | "Required";
  },
) {
  await mockBoundOwnerSession(page);
  await mockPosRegisterShiftApi(page, {
    openShift: true,
    closedShift: true,
    openingCashCountMode: history.openingCashCountMode ?? "Optional",
    closingCashCountMode: history.closingCashCountMode ?? "Optional",
    history,
  });
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-operations")
    .waitFor({ state: "visible", timeout: 15000 });
  await chooseOwnerOperations(page);
  await page.getByTestId("open-shifts").first().click();
  await expect(page.getByTestId("shifts-hub-page")).toBeVisible({ timeout: 15000 });
  await page.getByTestId("shift-open-detail").click();
  await expect(page.getByTestId("shift-detail-page")).toBeVisible({ timeout: 15000 });
}

test.describe("I18N-01 Repair 02 shift cash history", () => {
  test.use({ serviceWorkers: "block" });

  test("A optional opening skipped shows Not counted", async ({ page }) => {
    await openClosedShiftDetail(page, {
      openingCashCountMode: "Optional",
      closingCashCountMode: "Optional",
      openingCashCounted: false,
      openingCashAmount: 0,
      closingCashAmount: null,
      closingCashCountState: "NotPerformed",
    });
    await expect(page.getByTestId("shift-opening-policy-value")).toHaveText(
      /Optional|Opsyonal|Opsional/i,
    );
    await expect(page.getByTestId("shift-opening-history-not-counted")).toBeVisible();
    await expect(page.getByTestId("shift-opening-history-amount")).toHaveCount(0);
  });

  test("B required opening counted zero shows PHP 0.00", async ({ page }) => {
    await openClosedShiftDetail(page, {
      openingCashCountMode: "Required",
      closingCashCountMode: "Required",
      openingCashCounted: true,
      openingCashAmount: 0,
      closingCashAmount: 0,
      closingCashCountState: "Counted",
      expectedCashAmount: 0,
      cashVarianceAmount: 0,
    });
    await expect(page.getByTestId("shift-opening-policy-value")).toHaveText(
      /Required|Gikinahanglan|Masapul|Kinahanglan/i,
    );
    await expect(page.getByTestId("shift-opening-history-amount")).toContainText("₱0.00");
  });

  test("C opening denomination breakdown expands", async ({ page }) => {
    await openClosedShiftDetail(page, {
      openingCashCountMode: "Required",
      closingCashCountMode: "Required",
      openingCashCounted: true,
      openingCashAmount: 1500,
      openingDenominationLines: [
        { denominationValue: 1000, quantity: 1, lineTotal: 1000 },
        { denominationValue: 500, quantity: 1, lineTotal: 500 },
      ],
      closingCashAmount: 1500,
      closingCashCountState: "Counted",
      expectedCashAmount: 1500,
      cashVarianceAmount: 0,
    });
    await page.getByTestId("shift-opening-history-toggle").click();
    await expect(page.getByTestId("shift-opening-history-breakdown")).toBeVisible();
    await expect(page.getByTestId("shift-opening-history-line-1000")).toBeVisible();
  });

  test("D closed shift cash reconciliation rows", async ({ page }) => {
    await openClosedShiftDetail(page, {
      openingCashCountMode: "Required",
      closingCashCountMode: "Required",
      openingCashCounted: true,
      openingCashAmount: 500,
      openingDenominationLines: [{ denominationValue: 500, quantity: 1, lineTotal: 500 }],
      closingCashAmount: 1620,
      closingCashCountState: "Counted",
      closingDenominationLines: [
        { denominationValue: 1000, quantity: 1, lineTotal: 1000 },
        { denominationValue: 500, quantity: 1, lineTotal: 500 },
        { denominationValue: 100, quantity: 1, lineTotal: 100 },
        { denominationValue: 20, quantity: 1, lineTotal: 20 },
      ],
      cashSalesTotal: 1200,
      cashRefundsTotal: 100,
      totalCashIn: 40,
      totalCashOut: 20,
      expectedCashAmount: 1620,
      cashVarianceAmount: 0,
      gCashSalesTotal: 50,
      utangSalesTotal: 25,
    });
    await expect(page.getByTestId("shift-opening-policy-value")).toBeVisible();
    await expect(page.getByTestId("shift-opening-history-amount")).toContainText("₱500.00");
    await expect(page.getByTestId("shift-cash-sales")).toContainText("₱1,200.00");
    await expect(page.getByTestId("shift-cash-refunds")).toContainText("₱100.00");
    await expect(page.getByTestId("shift-cash-in")).toContainText("₱40.00");
    await expect(page.getByTestId("shift-cash-out")).toContainText("₱20.00");
    await expect(page.getByTestId("shift-expected-cash")).toContainText("₱1,620.00");
    await expect(page.getByTestId("shift-closing-policy-value")).toBeVisible();
    await expect(page.getByTestId("shift-closing-history-amount")).toContainText("₱1,620.00");
    await expect(page.getByTestId("shift-variance-balanced")).toBeVisible();
    await page.getByTestId("shift-closing-history-toggle").click();
    await expect(page.getByTestId("shift-closing-history-breakdown")).toBeVisible();
  });

  test("E optional closing skipped shows Not counted", async ({ page }) => {
    await openClosedShiftDetail(page, {
      openingCashCountMode: "Optional",
      closingCashCountMode: "Optional",
      openingCashCounted: true,
      openingCashAmount: 500,
      closingCashAmount: null,
      closingCashCountState: "NotPerformed",
    });
    await expect(page.getByTestId("shift-closing-history-not-counted")).toBeVisible();
    await expect(page.getByTestId("shift-closing-history-amount")).toHaveCount(0);
  });

  test("F historical 0.01 denomination still visible", async ({ page }) => {
    await openClosedShiftDetail(page, {
      openingCashCountMode: "Required",
      closingCashCountMode: "Required",
      openingCashCounted: true,
      openingCashAmount: 0.01,
      openingDenominationLines: [{ denominationValue: 0.01, quantity: 1, lineTotal: 0.01 }],
      closingCashAmount: 0.01,
      closingCashCountState: "Counted",
      closingDenominationLines: [{ denominationValue: 0.01, quantity: 1, lineTotal: 0.01 }],
      expectedCashAmount: 0.01,
      cashVarianceAmount: 0,
    });
    await page.getByTestId("shift-opening-history-toggle").click();
    await expect(page.getByTestId("shift-opening-history-line-0.01")).toBeVisible();
  });

  test("G current denomination config change does not rewrite history", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const api = await mockPosRegisterShiftApi(page, {
      openShift: true,
      closedShift: true,
      openingCashCountMode: "Required",
      closingCashCountMode: "Required",
      denominations: [{ value: 1000 }, { value: 500 }],
      history: {
        openingCashCounted: true,
        openingCashAmount: 100.01,
        openingDenominationLines: [
          { denominationValue: 100, quantity: 1, lineTotal: 100 },
          { denominationValue: 0.01, quantity: 1, lineTotal: 0.01 },
        ],
        closingCashAmount: 100.01,
        closingCashCountState: "Counted",
        expectedCashAmount: 100.01,
        cashVarianceAmount: 0,
      },
    });
    await signInAndBindOwner(page);
    await page
      .getByTestId("workspace-destination-operations")
      .waitFor({ state: "visible", timeout: 15000 });
    await chooseOwnerOperations(page);
    await page.getByTestId("open-shifts").first().click();
    await expect(page.getByTestId("shifts-hub-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("shift-open-detail").click();
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await page.getByTestId("shift-opening-history-toggle").click();
    await expect(page.getByTestId("shift-opening-history-line-0.01")).toBeVisible();
    api.setState({ denominations: [{ value: 999 }] });
    await expect(page.getByTestId("shift-opening-history-line-0.01")).toBeVisible();
    await expect(page.getByTestId("shift-opening-history-breakdown")).not.toContainText("999");
  });

  test("H regional language labels for ceb/ilo/hil", async ({ page }) => {
    test.setTimeout(90000);
    const locales: Array<{
      locale: "ceb-PH" | "ilo-PH" | "hil-PH";
      radio: RegExp;
      notCounted: RegExp;
    }> = [
      { locale: "ceb-PH", radio: /Bisaya \(Cebuano\)/i, notCounted: /Wala gibilang/i },
      { locale: "ilo-PH", radio: /Ilocano/i, notCounted: /Saan a nabilang/i },
      { locale: "hil-PH", radio: /Ilonggo \(Hiligaynon\)/i, notCounted: /Wala nabilang/i },
    ];

    await mockBoundOwnerSession(page);
    await mockPosRegisterShiftApi(page, {
      openShift: true,
      closedShift: true,
      openingCashCountMode: "Optional",
      closingCashCountMode: "Optional",
      history: {
        openingCashCounted: false,
        openingCashAmount: 0,
        closingCashAmount: null,
        closingCashCountState: "NotPerformed",
      },
    });
    await signInAndBindOwner(page);
    await page
      .getByTestId("workspace-destination-operations")
      .waitFor({ state: "visible", timeout: 15000 });
    await chooseOwnerOperations(page);

    for (const { locale, radio, notCounted } of locales) {
      await page.getByTestId("account-menu-trigger").click();
      await page.getByRole("menuitem", { name: /Preferences|Mga setting|Dagiti kaykayat/i }).click();
      await expect(page.getByTestId("preferences-close")).toBeVisible();
      await page.getByRole("radio", { name: radio }).click();
      await expect(page.locator("html")).toHaveAttribute("lang", locale);
      await page.getByTestId("preferences-close").click();

      if (await page.getByTestId("shift-open-detail").count()) {
        await page.getByTestId("shift-open-detail").click();
      } else {
        await page.getByTestId("open-shifts").first().click();
        await expect(page.getByTestId("shifts-hub-page")).toBeVisible({ timeout: 15000 });
        await page.getByTestId("shift-open-detail").click();
      }
      await expect(page.getByTestId("shift-detail-page")).toBeVisible({ timeout: 15000 });
      await expect(page.getByTestId("shift-opening-history-not-counted")).toHaveText(notCounted);
      await page.locator('a[href="/shifts"]').first().click();
      await expect(page.getByTestId("shifts-hub-page")).toBeVisible({ timeout: 15000 });
    }
  });

  for (const viewport of viewports) {
    test(`responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await openClosedShiftDetail(page, {
        openingCashCountMode: "Required",
        closingCashCountMode: "Required",
        openingCashCounted: true,
        openingCashAmount: 1500,
        openingDenominationLines: [
          { denominationValue: 1000, quantity: 1, lineTotal: 1000 },
          { denominationValue: 500, quantity: 1, lineTotal: 500 },
        ],
        closingCashAmount: 1500,
        closingCashCountState: "Counted",
        closingDenominationLines: [{ denominationValue: 1000, quantity: 1, lineTotal: 1000 }],
        cashSalesTotal: 0,
        expectedCashAmount: 1500,
        cashVarianceAmount: 0,
      });
      await page.getByTestId("shift-opening-history-toggle").click();
      await assertNoHorizontalOverflow(page);
    });
  }
});

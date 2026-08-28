import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
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

const SALE_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const SALE_LINE_ID = "99999999-9999-4999-8999-999999999999";
const WEIGHT_LINE_ID = "88888888-8888-4888-8888-888888888888";
const PACK_LINE_ID = "77777777-7777-4777-8777-777777777777";
const PRODUCT_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const RETURN_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const ACTOR_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";

type ReturnsApiState = {
  posts: Array<Record<string, unknown>>;
  refundableQty: number;
  refundableAmount: number;
  previouslyReturned: number;
  previouslyRefunded: number;
  paymentMethod: string;
  saleStatus: string;
  conflictOnce: boolean;
  noShift: boolean;
  voidBlocked: boolean;
  linesMode: "cash" | "weight" | "pack" | "discounted";
  createdReturns: Array<Record<string, unknown>>;
};

function baseRefundableLine(overrides: Record<string, unknown> = {}) {
  return {
    saleLineId: SALE_LINE_ID,
    productId: PRODUCT_ID,
    productNameSnapshot: "Coca-Cola 330ml",
    unitOfMeasure: "Piece",
    sellingMode: "PerItem",
    originalQuantity: 10,
    unitPriceSnapshot: 10,
    originalLineTotal: 80,
    previouslyReturnedQuantity: 0,
    refundableQuantity: 10,
    previouslyRefundedAmount: 0,
    refundableAmount: 80,
    ...overrides,
  };
}

function refundableBody(state: ReturnsApiState) {
  if (state.saleStatus !== "Completed") {
    return {
      saleId: SALE_ID,
      saleNumber: "S-9014",
      paymentMethod: state.paymentMethod,
      status: state.saleStatus,
      lines: [],
    };
  }

  if (state.linesMode === "weight") {
    return {
      saleId: SALE_ID,
      saleNumber: "S-9014",
      paymentMethod: state.paymentMethod,
      status: "Completed",
      lines: [
        baseRefundableLine({
          saleLineId: WEIGHT_LINE_ID,
          productNameSnapshot: "Rice",
          unitOfMeasure: "Kilogram",
          sellingMode: "ByWeight",
          originalQuantity: 1.25,
          unitPriceSnapshot: 80,
          originalLineTotal: 100,
          previouslyReturnedQuantity: 0,
          refundableQuantity: 1.25,
          previouslyRefundedAmount: 0,
          refundableAmount: 100,
        }),
      ],
    };
  }

  if (state.linesMode === "pack") {
    return {
      saleId: SALE_ID,
      saleNumber: "S-9014",
      paymentMethod: state.paymentMethod,
      status: "Completed",
      lines: [
        baseRefundableLine({
          saleLineId: PACK_LINE_ID,
          productNameSnapshot: "Sardines 12-pack",
          unitOfMeasure: "Pack",
          sellingMode: "PerItem",
          originalQuantity: 2,
          unitPriceSnapshot: 120,
          originalLineTotal: 240,
          previouslyReturnedQuantity: 0,
          refundableQuantity: 2,
          previouslyRefundedAmount: 0,
          refundableAmount: 240,
        }),
      ],
    };
  }

  const originalTotal = state.linesMode === "discounted" ? 80 : 100;
  return {
    saleId: SALE_ID,
    saleNumber: "S-9014",
    paymentMethod: state.paymentMethod,
    status: "Completed",
    lines:
      state.refundableQty > 0
        ? [
            baseRefundableLine({
              originalLineTotal: originalTotal,
              previouslyReturnedQuantity: state.previouslyReturned,
              refundableQuantity: state.refundableQty,
              previouslyRefundedAmount: state.previouslyRefunded,
              refundableAmount: state.refundableAmount,
            }),
          ]
        : [],
  };
}

function returnDto(state: ReturnsApiState, body: Record<string, unknown>) {
  const lines = (body.lines as Array<Record<string, unknown>>) ?? [];
  const qty = Number(lines[0]?.quantity ?? 0);
  const originalTotal =
    state.linesMode === "discounted" ? 80 : state.refundableAmount + state.previouslyRefunded;
  const refund =
    state.linesMode === "discounted"
      ? Math.round(((originalTotal * (state.previouslyReturned + qty)) / 10) * 100) / 100 -
        state.previouslyRefunded
      : Math.round(state.refundableAmount * (qty / Math.max(state.refundableQty, 1)) * 100) / 100;

  return {
    returnId: String(body.returnId ?? RETURN_ID),
    organizationId: E2E_ORG_ID,
    returnNumber: `R-${1000 + state.createdReturns.length}`,
    saleId: SALE_ID,
    refundMethod: state.paymentMethod,
    status: "Completed",
    returnDate: "2026-08-21",
    reason: String(body.reason ?? ""),
    notes: body.notes ?? null,
    totalRefundAmount: refund,
    createdAtUtc: "2026-08-21T04:00:00Z",
    createdBy: ACTOR_ID,
    completedAtUtc: "2026-08-21T04:00:00Z",
    cashierShiftId: state.paymentMethod === "Cash" ? "cccccccc-cccc-4ccc-8ccc-cccccccccccc" : null,
    lines: lines.map((line, index) => ({
      saleReturnLineId: `aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa${index}`,
      saleLineId: String(line.saleLineId),
      productId: PRODUCT_ID,
      productNameSnapshot: "Item",
      unitOfMeasure: "Piece",
      quantityReturned: Number(line.quantity),
      unitPriceSnapshot: 10,
      refundAmount: refund,
      restockDisposition: String(line.restockDisposition ?? "ReturnToStock"),
      lineReason: line.lineReason ?? null,
      inventoryMovementId:
        line.restockDisposition === "ReturnToStock" ? "12121212-1212-4121-8121-121212121212" : null,
    })),
  };
}

async function mockReturnsApi(
  page: import("@playwright/test").Page,
  opts: Partial<ReturnsApiState> = {},
): Promise<ReturnsApiState> {
  const state: ReturnsApiState = {
    posts: [],
    refundableQty: 10,
    refundableAmount: 80,
    previouslyReturned: 0,
    previouslyRefunded: 0,
    paymentMethod: "Cash",
    saleStatus: "Completed",
    conflictOnce: false,
    noShift: false,
    voidBlocked: false,
    linesMode: "discounted",
    createdReturns: [],
    ...opts,
  };

  await page.route("**/pos-api/api/v1/pos/sales**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname.replace(/\/$/, "");

    if (method === "GET" && pathname.endsWith("/sales")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              saleId: SALE_ID,
              organizationId: E2E_ORG_ID,
              saleNumber: "S-9014",
              status: state.saleStatus,
              paymentMethod: state.paymentMethod,
              subtotal: 80,
              total: 80,
              taxAmount: 0,
              recordedAtUtc: "2026-08-21T02:00:00Z",
              recordedBy: ACTOR_ID,
              updatedAtUtc: "2026-08-21T02:00:00Z",
              lines: [],
              branchId: E2E_BRANCH_ID,
              documentKind: "TransactionSummary",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 10,
        }),
      });
    }

    if (method === "GET" && pathname.endsWith(`/sales/${SALE_ID}`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: SALE_ID,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-9014",
          status: state.saleStatus,
          paymentMethod: state.paymentMethod,
          subtotal: 80,
          total: 80,
          taxAmount: 0,
          recordedAtUtc: "2026-08-21T02:00:00Z",
          recordedBy: ACTOR_ID,
          updatedAtUtc: "2026-08-21T02:00:00Z",
          lines: [
            {
              saleLineId: SALE_LINE_ID,
              productId: PRODUCT_ID,
              lineNumber: 1,
              name: "Coca-Cola 330ml",
              unitOfMeasure: "pc",
              sellingMode: "PerItem",
              unitPrice: 10,
              quantity: 10,
              lineTotal: 80,
            },
          ],
          branchId: E2E_BRANCH_ID,
          documentKind: "TransactionSummary",
        }),
      });
    }

    if (method === "POST" && pathname.endsWith(`/sales/${SALE_ID}/void`)) {
      if (state.voidBlocked || state.previouslyReturned > 0 || state.createdReturns.length > 0) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Sale already has returns.",
            errorCode: "pos.concurrency_conflict",
          }),
        });
      }
      state.saleStatus = "Voided";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          saleId: SALE_ID,
          organizationId: E2E_ORG_ID,
          saleNumber: "S-9014",
          status: "Voided",
          paymentMethod: state.paymentMethod,
          subtotal: 80,
          total: 80,
          taxAmount: 0,
          recordedAtUtc: "2026-08-21T02:00:00Z",
          recordedBy: ACTOR_ID,
          voidedAtUtc: "2026-08-21T05:00:00Z",
          voidReason: "Mistake",
          updatedAtUtc: "2026-08-21T05:00:00Z",
          lines: [],
          documentKind: "TransactionSummary",
        }),
      });
    }

    return route.fallback();
  });

  await page.route("**/pos-api/api/v1/pos/sale-returns**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname.replace(/\/$/, "");

    if (method === "GET" && pathname.endsWith(`/sale-returns/refundable/${SALE_ID}`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(refundableBody(state)),
      });
    }

    if (method === "GET" && pathname.endsWith("/sale-returns")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: state.createdReturns,
          totalCount: state.createdReturns.length,
          page: 1,
          pageSize: 20,
        }),
      });
    }

    if (method === "GET" && pathname.includes("/sale-returns/")) {
      const id = pathname.split("/").pop();
      const found =
        state.createdReturns.find((item) => item.returnId === id) ??
        state.createdReturns[0] ??
        returnDto(state, { returnId: id, reason: "Prior", lines: [] });
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(found),
      });
    }

    if (method === "POST" && pathname.endsWith("/sale-returns")) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.posts.push(body);

      if (state.noShift && state.paymentMethod === "Cash") {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Cash refunds require an open cashier shift for this actor.",
            errorCode: "pos.cashier_shift.no_open_shift",
          }),
        });
      }

      if (state.saleStatus === "Voided") {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Only completed sales can be returned.",
            errorCode: "pos.sale_return.sale_not_returnable",
          }),
        });
      }

      if (state.conflictOnce) {
        state.conflictOnce = false;
        state.refundableQty = 4;
        state.refundableAmount = 32;
        state.previouslyReturned = 6;
        state.previouslyRefunded = 48;
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Returnable quantity changed.",
            errorCode: "pos.concurrency_conflict",
          }),
        });
      }

      const existing = state.createdReturns.find((item) => item.returnId === body.returnId);
      if (existing) {
        return route.fulfill({
          status: 201,
          contentType: "application/json",
          body: JSON.stringify(existing),
        });
      }

      const created = returnDto(state, body);
      const qty = Number(((body.lines as Array<Record<string, unknown>>) ?? [])[0]?.quantity ?? 0);
      state.previouslyReturned += qty;
      state.previouslyRefunded += Number(created.totalRefundAmount);
      state.refundableQty = Math.max(0, state.refundableQty - qty);
      state.refundableAmount = Math.max(
        0,
        state.refundableAmount - Number(created.totalRefundAmount),
      );
      state.createdReturns.push(created);

      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(created),
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
  await expect(page.getByTestId("open-returns")).toBeVisible({ timeout: 15000 });
}

async function completePartialReturn(page: import("@playwright/test").Page) {
  await clientNavigate(page, `/returns/sale/${SALE_ID}`);
  await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
  const increase = page.getByTestId("quantity-stepper").getByRole("button").nth(1);
  for (let i = 0; i < 5; i += 1) {
    await increase.click();
  }
  await page.getByTestId("returns-reason").fill("Customer changed mind");
  await page.getByTestId("returns-continue").click();
  await expect(page.getByTestId("process-return-confirm")).toBeVisible();
  await page.getByTestId("returns-confirm-submit").click();
  await expect(page.getByTestId("process-return-success")).toBeVisible();
}

test.describe("RMAP-14 returns / refunds", () => {
  test.use({ serviceWorkers: "block" });

  test("A owner partial cash return with restock", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page, { linesMode: "discounted", paymentMethod: "Cash" });
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    await expect(page.getByTestId("returns-success-cash")).toContainText("Cash refund");
    expect(state.posts).toHaveLength(1);
    expect(state.posts[0]?.returnId).toBeTruthy();
    const line = (state.posts[0]?.lines as Array<Record<string, unknown>>)[0];
    expect(line?.restockDisposition).toBe("ReturnToStock");
    expect(Number(line?.quantity)).toBe(5);
  });

  test("B partial then second return remaining", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page, { linesMode: "discounted" });
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    await clientNavigate(page, "/returns");
    await expect(page.getByTestId("returns-hub-page")).toBeVisible();
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByText(/Still returnable/i)).toContainText("5");
    await page.getByTestId(`returns-return-all-${SALE_LINE_ID}`).click();
    await page.getByTestId("returns-reason").fill("Second return");
    await page.getByTestId("returns-continue").click();
    await page.getByTestId("returns-confirm-submit").click();
    await expect(page.getByTestId("process-return-success")).toBeVisible();
    expect(state.posts).toHaveLength(2);
    expect(state.refundableQty).toBe(0);
  });

  test("C discounted NET refund uses server total", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReturnsApi(page, { linesMode: "discounted", refundableAmount: 80 });
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    await expect(page.getByTestId("returns-final-refund")).toContainText("40");
    await expect(page.getByText("Invoice")).toHaveCount(0);
  });

  test("D ByWeight decimal quantity", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page, {
      linesMode: "weight",
      paymentMethod: "Cash",
      refundableQty: 1.25,
      refundableAmount: 100,
    });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId(`returns-qty-input-${WEIGHT_LINE_ID}`).fill("0.750");
    await page.getByTestId("returns-reason").fill("Wrong weight");
    await page.getByTestId("returns-continue").click();
    await page.getByTestId("returns-confirm-submit").click();
    await expect(page.getByTestId("process-return-success")).toBeVisible();
    const line = (state.posts[0]?.lines as Array<Record<string, unknown>>)[0];
    expect(Number(line?.quantity)).toBeCloseTo(0.75, 3);
  });

  test("E multi-UOM pack display", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReturnsApi(page, { linesMode: "pack", refundableQty: 2, refundableAmount: 240 });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByText("Sardines 12-pack")).toBeVisible();
    await expect(page.getByText(/Still returnable/i)).toContainText("Pack");
  });

  test("F DoNotRestock disposition", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId(`returns-no-restock-${SALE_LINE_ID}`).click();
    await page.getByTestId(`returns-return-all-${SALE_LINE_ID}`).click();
    await page.getByTestId("returns-reason").fill("Damaged");
    await page.getByTestId("returns-continue").click();
    await page.getByTestId("returns-confirm-submit").click();
    await expect(page.getByTestId("process-return-success")).toBeVisible();
    const line = (state.posts[0]?.lines as Array<Record<string, unknown>>)[0];
    expect(line?.restockDisposition).toBe("DoNotRestock");
    const createdLines = state.createdReturns[0]?.lines as Array<Record<string, unknown>>;
    expect(createdLines?.[0]?.inventoryMovementId).toBeNull();
  });

  test("G expiry tracked without lot selection UI", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReturnsApi(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByText(/lot/i)).toHaveCount(0);
    await expect(page.getByText(/FEFO/i)).toHaveCount(0);
  });

  test("H cash refund blocked without open shift", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReturnsApi(page, { noShift: true, paymentMethod: "Cash" });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId(`returns-return-all-${SALE_LINE_ID}`).click();
    await page.getByTestId("returns-reason").fill("Cash refund");
    await page.getByTestId("returns-continue").click();
    await page.getByTestId("returns-confirm-submit").click();
    await expect(page.getByTestId("returns-confirm-error")).toContainText(
      "Open a cashier shift before giving a cash refund.",
    );
  });

  test("I GCash label never ManualGCash", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReturnsApi(page, { paymentMethod: "ManualGCash" });
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    await expect(page.getByTestId("returns-success-gcash")).toContainText("GCash");
    await expect(page.getByText("ManualGCash")).toHaveCount(0);
  });

  test("J Utang amount owed reduced wording", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockReturnsApi(page, { paymentMethod: "Utang" });
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    await expect(page.getByTestId("returns-success-utang")).toContainText("Amount owed reduced");
    await expect(page.getByText(/cash refund/i)).toHaveCount(0);
  });

  test("K cashier cannot process return", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockReturnsApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/role/cashier");
    await expect(page.getByTestId("open-returns")).toBeVisible();
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("returns-process-denied")).toBeVisible();
    await clientNavigate(page, `/sell/sales/${SALE_ID}/summary`);
    await expect(page.getByTestId("summary-return-items")).toHaveCount(0);
  });

  test("L return vs void mutual exclusion", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page, { voidBlocked: true });
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    await clientNavigate(page, `/sell/sales/${SALE_ID}/summary`);
    await page.getByTestId("summary-void-trigger").click();
    await page.getByTestId("summary-void-reason").fill("Should fail");
    await page.getByTestId("summary-void-confirm").click();
    await expect(page.getByTestId("summary-void-error")).toBeVisible();
    expect(state.saleStatus).toBe("Completed");

    state.saleStatus = "Voided";
    state.refundableQty = 0;
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-not-returnable")).toBeVisible();
  });

  test("M stale conflict refreshes refundable without silent clamp", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page, { conflictOnce: true });
    await signInOwnerOperations(page);
    await clientNavigate(page, `/returns/sale/${SALE_ID}`);
    await expect(page.getByTestId("process-return-page")).toBeVisible({ timeout: 15000 });
    await page.getByTestId(`returns-return-all-${SALE_LINE_ID}`).click();
    await page.getByTestId("returns-reason").fill("Stale attempt");
    await page.getByTestId("returns-continue").click();
    await page.getByTestId("returns-confirm-submit").click();
    await expect(page.getByTestId("returns-stale-banner")).toBeVisible();
    await expect(page.getByTestId("process-return-page")).toBeVisible();
    await expect(page.getByText(/Still returnable/i)).toContainText("4");
    expect(state.posts).toHaveLength(1);
  });

  test("N idempotent returnId replay", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockReturnsApi(page);
    await signInOwnerOperations(page);
    await completePartialReturn(page);
    const firstId = String(state.posts[0]?.returnId);
    await page.evaluate(
      async ([org, branch, saleId, returnId, saleLineId]) => {
        await fetch("/pos-api/api/v1/pos/sale-returns", {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
            "X-Pos-Organization-Id": org,
            "X-Pos-Branch-Id": branch,
          },
          body: JSON.stringify({
            saleId,
            reason: "Replay",
            returnId,
            lines: [{ saleLineId, quantity: 5, restockDisposition: "ReturnToStock" }],
          }),
        });
      },
      [E2E_ORG_ID, E2E_BRANCH_ID, SALE_ID, firstId, SALE_LINE_ID] as const,
    );
    expect(state.createdReturns).toHaveLength(1);
    expect(state.posts.filter((p) => p.returnId === firstId).length).toBeGreaterThanOrEqual(2);
  });

  for (const viewport of VIEWPORTS) {
    test(`responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundOwnerSession(page);
      await mockReturnsApi(page);
      await signInOwnerOperations(page);
      await clientNavigate(page, "/returns");
      await expect(page.getByTestId("returns-hub-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await clientNavigate(page, `/returns/sale/${SALE_ID}`);
      await expect(page.getByTestId("process-return-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});

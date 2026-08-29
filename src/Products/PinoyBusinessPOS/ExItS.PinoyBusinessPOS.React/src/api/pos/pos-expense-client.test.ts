import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  expenseDtoSchema,
  expenseSummaryDtoSchema,
  expenseWorkspaceScope,
  recordExpense,
} from "@/api/pos/pos-expense-client";

vi.mock("@/api/platform/pos-access-token", () => ({
  getPosAccessToken: () => "test-access-token",
}));

vi.mock("@/api/pos/pos-mutation-idempotency", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-mutation-idempotency")>(
    "@/api/pos/pos-mutation-idempotency",
  );
  return {
    ...actual,
    buildPosMutationIdempotencyHeaders: vi.fn(async () => ({
      "Idempotency-Key": "idem-key",
      "X-Pos-Idempotency-Payload-Hash": "hash",
      "X-Pos-Offline-Operation-Type": "expense.create",
    })),
  };
});

describe("pos-expense-client", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it("expenseWorkspaceScope omits branchId", () => {
    expect(expenseWorkspaceScope("11111111-1111-1111-1111-111111111111")).toEqual({
      organizationId: "11111111-1111-1111-1111-111111111111",
    });
  });

  it("parses expense and summary DTOs", () => {
    const expense = expenseDtoSchema.parse({
      expenseId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      organizationId: "11111111-1111-1111-1111-111111111111",
      expenseNumber: "EXP-1",
      categoryId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      categoryName: "Rent",
      status: "Recorded",
      paymentMethod: "Cash",
      amount: 5000,
      description: "August rent",
      payee: null,
      gCashReference: null,
      expenseDate: "2026-08-29",
      recordedAtUtc: "2026-08-29T10:00:00Z",
      recordedBy: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      voidedAtUtc: null,
      voidedBy: null,
      voidReason: null,
      updatedAtUtc: "2026-08-29T10:00:00Z",
    });
    expect(expense.expenseNumber).toBe("EXP-1");

    const summary = expenseSummaryDtoSchema.parse({
      fromDate: null,
      toDate: null,
      grossTotal: 7000,
      voidedTotal: 2000,
      netTotal: 5000,
      recordedCount: 2,
      voidedCount: 1,
      byCategory: [],
      byPaymentMethod: [],
    });
    expect(summary.netTotal).toBe(5000);
  });

  it("recordExpense omits gCashReference for Cash and sends idempotency", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        expenseId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        organizationId: "11111111-1111-1111-1111-111111111111",
        expenseNumber: "EXP-1",
        categoryId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        categoryName: "Rent",
        status: "Recorded",
        paymentMethod: "Cash",
        amount: 100,
        description: "Test",
        payee: null,
        gCashReference: null,
        expenseDate: "2026-08-29",
        recordedAtUtc: "2026-08-29T10:00:00Z",
        recordedBy: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        voidedAtUtc: null,
        voidedBy: null,
        voidReason: null,
        updatedAtUtc: "2026-08-29T10:00:00Z",
      }),
      text: async () => "",
    });
    vi.stubGlobal("fetch", fetchMock);

    await recordExpense(
      { organizationId: "11111111-1111-1111-1111-111111111111" },
      {
        expenseId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        categoryId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        paymentMethod: "Cash",
        amount: 100,
        description: "Test",
        expenseDate: "2026-08-29",
        gCashReference: "should-not-send",
      },
    );

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    const body = JSON.parse(String(init.body)) as Record<string, unknown>;
    expect(body.gCashReference).toBeUndefined();
    expect(body.paymentMethod).toBe("Cash");
    const headers = new Headers(init.headers);
    expect(headers.get("X-Pos-Offline-Operation-Type")).toBe("expense.create");
    expect(headers.get("X-Pos-Branch-Id")).toBeNull();
  });
});

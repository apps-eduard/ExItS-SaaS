import { beforeEach, describe, expect, it, vi } from "vitest";
import { checkoutSale, getSale, listSales } from "@/api/pos/pos-sales-client";
import { PosApiError } from "@/api/pos/pos-http";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const saleId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const shiftId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const productId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";

function saleJson(extra: Record<string, unknown> = {}) {
  return {
    saleId,
    organizationId: workspace.organizationId,
    saleNumber: "S-9001",
    status: "Completed",
    paymentMethod: "Cash",
    subtotal: 25,
    total: 25,
    taxAmount: 0,
    amountTendered: 50,
    changeAmount: 25,
    recordedAtUtc: "2026-08-21T02:00:00Z",
    recordedBy: "ffffffff-ffff-4fff-8fff-ffffffffffff",
    updatedAtUtc: "2026-08-21T02:00:00Z",
    lines: [
      {
        saleLineId: "99999999-9999-4999-8999-999999999999",
        productId,
        lineNumber: 1,
        name: "Coke",
        sku: "COKE-330",
        unitOfMeasure: "pc",
        sellingMode: "PerItem",
        unitPrice: 25,
        quantity: 1,
        lineTotal: 25,
      },
    ],
    shiftId,
    shiftNumber: "S-1001",
    documentKind: "TransactionSummary",
    ...extra,
  };
}

describe("pos-sales-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("posts cash checkout without snapshot fields and parses sale", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify(saleJson()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const sale = await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      amountTendered: 50,
      saleId,
      shiftId,
    });

    expect(sale.saleNumber).toBe("S-9001");
    expect(sale.documentKind).toBe("TransactionSummary");

    const [, init] = vi.mocked(fetch).mock.calls[0];
    const body = JSON.parse(String(init?.body));
    expect(body.paymentMethod).toBe("Cash");
    expect(body.amountTendered).toBe(50);
    expect(body.saleId).toBe(saleId);
    expect(body.shiftId).toBe(shiftId);
    expect(body.lines[0]).toEqual({ productId, quantity: 1 });
    expect(body.lines[0].unitPriceSnapshot).toBeUndefined();
    expect(body.discounts).toBeUndefined();
  });

  it("includes sellingUnitId and enteredQuantity when provided", async () => {
    const unitId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify(saleJson()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await checkoutSale(workspace, {
      lines: [{ productId, quantity: 2, sellingUnitId: unitId, enteredQuantity: 2 }],
      paymentMethod: "Cash",
      amountTendered: 100,
      saleId,
      shiftId,
    });

    const body = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(body.lines[0]).toEqual({
      productId,
      quantity: 2,
      sellingUnitId: unitId,
      enteredQuantity: 2,
    });
  });

  it("gets and lists sales", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        new Response(JSON.stringify(saleJson()), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      )
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({ items: [saleJson()], totalCount: 1, page: 1, pageSize: 20 }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      );

    const one = await getSale(workspace, saleId);
    expect(one.saleId).toBe(saleId);

    const page = await listSales(workspace, { paymentMethod: "Cash" });
    expect(page.totalCount).toBe(1);
    expect(String(vi.mocked(fetch).mock.calls[1][0])).toContain("paymentMethod=Cash");
  });

  it("surfaces checkout failure as PosApiError", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          detail: "Amount tendered must be at least the sale total.",
          errorCode: "pos.sale.amount_tendered.below_total",
        }),
        { status: 400, headers: { "Content-Type": "application/json" } },
      ),
    );

    await expect(
      checkoutSale(workspace, {
        lines: [{ productId, quantity: 1 }],
        paymentMethod: "Cash",
        amountTendered: 1,
        saleId,
        shiftId,
      }),
    ).rejects.toBeInstanceOf(PosApiError);
  });
});

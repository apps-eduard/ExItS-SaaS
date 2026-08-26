import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  checkoutSale,
  formatPaymentMethodLabel,
  getSale,
  listSales,
  quoteSale,
  voidSale,
} from "@/api/pos/pos-sales-client";
import { PosApiError } from "@/api/pos/pos-http";
import { sha256Hex } from "@/api/pos/pos-mutation-idempotency";

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

function quoteJson(extra: Record<string, unknown> = {}) {
  return {
    grossSubtotal: 25,
    lineDiscountTotal: 0,
    saleDiscountTotal: 2.5,
    discountTotal: 2.5,
    subtotal: 22.5,
    taxAmount: 0,
    total: 22.5,
    lines: [
      {
        lineNumber: 1,
        productId,
        name: "Coke",
        unitOfMeasure: "pc",
        sellingMode: "PerItem",
        unitPrice: 25,
        quantity: 1,
        grossLineTotal: 25,
        lineDiscountAmount: 0,
        saleDiscountAllocatedAmount: 2.5,
        lineTotal: 22.5,
      },
    ],
    discounts: [
      {
        scope: "Sale",
        method: "Percentage",
        requestedValue: 10,
        calculatedAmount: 2.5,
        reason: "Bulk buyer courtesy",
      },
    ],
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

  it("parses checkout 2xx when server emits priceOverrides:null (SaleQueryService.Map)", async () => {
    // ASP.NET serializes Map()'s null PriceOverrides as JSON null — not omitted.
    // Zod .optional() rejects null; this previously surfaced as "Could not record the sale".
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify(
          saleJson({
            paymentMethod: "Utang",
            amountTendered: null,
            changeAmount: null,
            customerId: "820ed3b3-0ab6-4509-b48b-a6f825b926e3",
            linkedCreditEntryId: "6c6f99cf-7c31-46c7-bab5-4a1c0faaf27a",
            customerDisplayName: null,
            linkedCreditDueDate: "2026-09-01",
            customerOutstandingAfter: 205,
            buyerPartyKind: "WalkIn",
            documentKind: "TransactionSummary",
            grossSubtotal: 25,
            lineDiscountTotal: 0,
            saleDiscountTotal: 0,
            discountTotal: 0,
            priceOverrides: null,
          }),
        ),
        {
          status: 201,
          headers: { "Content-Type": "application/json" },
        },
      ),
    );

    const sale = await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Utang",
      saleId,
      shiftId,
      customerId: "820ed3b3-0ab6-4509-b48b-a6f825b926e3",
    });

    expect(sale.saleId).toBe(saleId);
    expect(sale.paymentMethod).toBe("Utang");
    expect(sale.priceOverrides).toBeNull();
    expect(sale.linkedCreditEntryId).toBe("6c6f99cf-7c31-46c7-bab5-4a1c0faaf27a");
  });

  it("sends sale idempotency headers keyed on saleId with the payload hash", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify(saleJson()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      amountTendered: 50,
      saleId,
      shiftId,
    });

    const [, init] = vi.mocked(fetch).mock.calls[0];
    const headers = new Headers(init?.headers);
    expect(headers.get("Idempotency-Key")).toBe(saleId.replace(/-/g, ""));
    expect(headers.get("X-Pos-Operation-Id")).toBe(saleId);
    expect(headers.get("X-Pos-Operation-Type")).toBe("sale.checkout");
    expect(headers.get("X-Pos-Payload-Hash")).toBe(await sha256Hex(String(init?.body ?? "")));
  });

  it("includes commercial discount intents on checkout and quote", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify(saleJson({ grossSubtotal: 25, discountTotal: 2.5, total: 22.5 })),
          {
            status: 200,
            headers: { "Content-Type": "application/json" },
          },
        ),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(quoteJson()), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );

    const discounts = [
      {
        scope: "Sale" as const,
        method: "Percentage" as const,
        value: 10,
        reason: "Bulk buyer courtesy",
      },
    ];

    await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      amountTendered: 25,
      saleId,
      shiftId,
      discounts,
    });

    const checkoutBody = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(checkoutBody.discounts).toEqual(discounts);
    expect(checkoutBody.lines[0].unitPrice).toBeUndefined();

    const quote = await quoteSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      discounts,
    });
    expect(quote.grossSubtotal).toBe(25);
    expect(quote.discountTotal).toBe(2.5);
    expect(quote.total).toBe(22.5);
    expect(String(vi.mocked(fetch).mock.calls[1][0])).toContain("/sales/quote");
  });

  it("posts and quotes priceOverrides with baseline/applied parse", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify(
            saleJson({
              total: 90,
              subtotal: 90,
              priceOverrides: [
                {
                  lineNumber: 1,
                  baselineUnitPrice: 100,
                  appliedUnitPrice: 90,
                  reason: "Price match",
                },
              ],
              lines: [
                {
                  saleLineId: "99999999-9999-4999-8999-999999999999",
                  productId,
                  lineNumber: 1,
                  name: "Coke",
                  sku: "COKE-330",
                  unitOfMeasure: "pc",
                  sellingMode: "PerItem",
                  unitPrice: 90,
                  quantity: 1,
                  lineTotal: 90,
                },
              ],
            }),
          ),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      )
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify(
            quoteJson({
              grossSubtotal: 90,
              discountTotal: 0,
              saleDiscountTotal: 0,
              subtotal: 90,
              total: 90,
              discounts: [],
              priceOverrides: [
                {
                  lineNumber: 1,
                  baselineUnitPrice: 100,
                  appliedUnitPrice: 90,
                  reason: "Price match",
                },
              ],
              lines: [
                {
                  lineNumber: 1,
                  productId,
                  name: "Coke",
                  unitOfMeasure: "pc",
                  sellingMode: "PerItem",
                  unitPrice: 90,
                  quantity: 1,
                  grossLineTotal: 90,
                  lineDiscountAmount: 0,
                  saleDiscountAllocatedAmount: 0,
                  lineTotal: 90,
                  baselineUnitPrice: 100,
                },
              ],
            }),
          ),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      );

    const priceOverrides = [
      {
        requestedUnitPrice: 90,
        reason: "Price match",
        lineNumber: 1,
        expectedBaselineUnitPrice: 100,
      },
    ];

    const sale = await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      amountTendered: 90,
      saleId,
      shiftId,
      priceOverrides,
    });
    expect(sale.priceOverrides?.[0]?.appliedUnitPrice).toBe(90);
    const checkoutBody = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(checkoutBody.priceOverrides).toEqual(priceOverrides);
    expect(checkoutBody.lines[0].unitPrice).toBeUndefined();

    const quote = await quoteSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      priceOverrides,
    });
    expect(quote.priceOverrides?.[0]?.baselineUnitPrice).toBe(100);
    expect(quote.lines[0]?.baselineUnitPrice).toBe(100);
    expect(quote.lines[0]?.unitPrice).toBe(90);
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

  it("surfaces cashier discount denial as PosApiError", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          detail: "ApplyCommercialDiscount is required.",
          errorCode: "application.auth.capability.denied",
        }),
        { status: 403, headers: { "Content-Type": "application/json" } },
      ),
    );

    await expect(
      checkoutSale(workspace, {
        lines: [{ productId, quantity: 1 }],
        paymentMethod: "Cash",
        amountTendered: 25,
        saleId,
        shiftId,
        discounts: [
          { scope: "Sale", method: "Percentage", value: 10, reason: "Should be rejected" },
        ],
      }),
    ).rejects.toMatchObject({ status: 403 });
  });

  it("posts ManualGCash without tender and with gCashReference", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify(
          saleJson({
            paymentMethod: "ManualGCash",
            amountTendered: null,
            changeAmount: null,
            gCashReference: "GC-123",
          }),
        ),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "ManualGCash",
      gCashReference: "GC-123",
      saleId,
      shiftId,
    });

    const body = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(body.paymentMethod).toBe("ManualGCash");
    expect(body.gCashReference).toBe("GC-123");
    expect(body.amountTendered).toBeUndefined();
  });

  it("posts Utang with customerId and optional dueDate without tender", async () => {
    const customerId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify(
          saleJson({
            paymentMethod: "Utang",
            amountTendered: null,
            changeAmount: null,
            customerId,
            linkedCreditEntryId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          }),
        ),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Utang",
      customerId,
      dueDate: "2026-09-01",
      saleId,
      shiftId,
    });

    const body = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(body.paymentMethod).toBe("Utang");
    expect(body.customerId).toBe(customerId);
    expect(body.dueDate).toBe("2026-09-01");
    expect(body.amountTendered).toBeUndefined();
  });

  it("posts discounted Utang with intents and no tender", async () => {
    const customerId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify(
          saleJson({
            paymentMethod: "Utang",
            amountTendered: null,
            changeAmount: null,
            total: 22.5,
            customerId,
            linkedCreditEntryId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
            discountTotal: 2.5,
          }),
        ),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    await checkoutSale(workspace, {
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Utang",
      customerId,
      saleId,
      shiftId,
      discounts: [{ scope: "Sale", method: "Percentage", value: 10, reason: "Regular buyer" }],
    });

    const body = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(body.paymentMethod).toBe("Utang");
    expect(body.discounts).toHaveLength(1);
    expect(body.amountTendered).toBeUndefined();
  });

  it("voids a sale with reason", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify(
          saleJson({
            status: "Voided",
            voidReason: "Wrong item",
            voidedAtUtc: "2026-08-21T03:00:00Z",
          }),
        ),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    const voided = await voidSale(workspace, saleId, { reason: "Wrong item" });
    expect(voided.status).toBe("Voided");
    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain(`/sales/${saleId}/void`);
    const body = JSON.parse(String(vi.mocked(fetch).mock.calls[0][1]?.body));
    expect(body.reason).toBe("Wrong item");
  });

  it("labels ManualGCash as GCash for users", () => {
    expect(formatPaymentMethodLabel("ManualGCash")).toBe("GCash");
    expect(formatPaymentMethodLabel("Cash")).toBe("Cash");
    expect(formatPaymentMethodLabel("Utang")).toBe("Utang");
  });
});

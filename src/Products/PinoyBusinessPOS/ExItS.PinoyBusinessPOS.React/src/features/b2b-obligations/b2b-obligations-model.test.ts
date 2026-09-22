import { describe, expect, it } from "vitest";
import {
  cleanSourceReference,
  countB2bObligationsByFilter,
  filterB2bObligations,
  mapPayableToObligation,
  mapReceivableToObligation,
  paginateB2bObligations,
  resolveB2bObligationSourceHref,
  type B2bObligationItem,
} from "@/features/b2b-obligations/b2b-obligations-model";

function item(partial: Partial<B2bObligationItem> & Pick<B2bObligationItem, "id">): B2bObligationItem {
  return {
    perspective: "receivable",
    status: "Open",
    isOverdue: false,
    sourceType: "Sale",
    sourceId: null,
    sourceReference: null,
    transactionDateUtc: "2026-09-17T00:00:00Z",
    originalAmount: 100,
    paidAtSourceAmount: 0,
    laterPaymentsAmount: 0,
    balance: 100,
    dueDate: "2026-10-17",
    ...partial,
  };
}

describe("b2b-obligations-model", () => {
  it("filters open overdue paid all", () => {
    const items = [
      item({ id: "1", status: "Open", balance: 50, isOverdue: false }),
      item({ id: "2", status: "PartiallyPaid", balance: 20, isOverdue: true }),
      item({ id: "3", status: "Paid", balance: 0, isOverdue: false }),
      item({ id: "4", status: "Open", balance: 10, isOverdue: true }),
    ];
    expect(filterB2bObligations(items, "open").map((x) => x.id)).toEqual(["1", "2", "4"]);
    expect(filterB2bObligations(items, "overdue").map((x) => x.id)).toEqual(["2", "4"]);
    expect(filterB2bObligations(items, "paid").map((x) => x.id)).toEqual(["3"]);
    expect(countB2bObligationsByFilter(items).all).toBe(4);
  });

  it("paginates load 10 more", () => {
    const items = Array.from({ length: 15 }, (_, i) => item({ id: String(i) }));
    const first = paginateB2bObligations(items, 10);
    expect(first.visible).toHaveLength(10);
    expect(first.hasMore).toBe(true);
    const more = paginateB2bObligations(items, 20);
    expect(more.visible).toHaveLength(15);
    expect(more.hasMore).toBe(false);
  });

  it("cleans ugly sale guid prefixes", () => {
    const raw =
      "sale:44444444-4444-4444-8444-444444444444|Product sale 260917-001";
    expect(cleanSourceReference(raw, null)).toBe("Product sale 260917-001");
  });

  it("maps receivable and payable without drifting filters", () => {
    const receivable = mapReceivableToObligation({
      creditEntryId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      sourceType: "PO",
      sourceReference: "PO-100",
      dueDate: "2026-10-01",
      outstandingBalance: 400,
      originalAmount: 400,
      createdAtUtc: "2026-09-01T00:00:00Z",
      remarks: null,
      status: "Open",
      isOverdue: false,
      paidAtSourceAmount: 0,
      laterPaymentsAmount: 0,
      sourceId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    });
    expect(receivable.sourceType).toBe("GoodsReceipt");
    expect(receivable.balance).toBe(400);

    const payable = mapPayableToObligation({
      payableId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      organizationId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      supplierId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      sourceType: "GoodsReceipt",
      sourceId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceReference: "PO-100",
      originalAmount: 643,
      paidAtReceiptAmount: 243,
      paidAmount: 243,
      balance: 400,
      status: "Open",
      dueDate: "2026-10-01",
      paymentMethodAtReceipt: null,
      createdAtUtc: "2026-09-01T00:00:00Z",
      createdBy: "ffffffff-ffff-ffff-ffff-ffffffffffff",
      updatedAtUtc: "2026-09-01T00:00:00Z",
      voidedAtUtc: null,
      voidedBy: null,
      voidReason: null,
      hasPostedPayments: false,
      isOverdue: false,
    });
    expect(payable.laterPaymentsAmount).toBe(0);
    expect(payable.paidAtSourceAmount).toBe(243);
    expect(payable.balance).toBe(400);
  });

  it("resolves seller and buyer source deep links", () => {
    const saleId = "11111111-1111-4111-8111-111111111111";
    expect(resolveB2bObligationSourceHref("receivable", "Sale", saleId)).toBe(
      `/sell/sales/${saleId}/summary`,
    );
    expect(resolveB2bObligationSourceHref("payable", "Sale", saleId)).toBe(
      `/purchasing/direct-purchases/b2b/${saleId}`,
    );
    expect(
      resolveB2bObligationSourceHref(
        "payable",
        "DirectPurchaseReceipt",
        "22222222-2222-4222-8222-222222222222",
      ),
    ).toBe("/purchasing/direct-purchases/22222222-2222-4222-8222-222222222222");
  });
});

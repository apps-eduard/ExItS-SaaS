import { describe, expect, it } from "vitest";
import {
  allocatePaymentAutomatically,
  countOpenAfterAllocation,
  validateManualAllocations,
  type OpenReceivableForAllocation,
} from "@/features/customers/business-payment-allocation";

const openTwo: OpenReceivableForAllocation[] = [
  {
    creditEntryId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    outstandingBalance: 300,
    dueDate: "2026-09-01",
    createdAtUtc: "2026-08-01T00:00:00Z",
    sourceType: "PO",
    sourceReference: "PO-100",
  },
  {
    creditEntryId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    outstandingBalance: 785,
    dueDate: "2026-09-15",
    createdAtUtc: "2026-08-10T00:00:00Z",
    sourceType: "DirectPurchase",
    sourceReference: "DR-44",
  },
];

describe("business-payment-allocation", () => {
  it("allocates 500 across two open receivables FIFO by due date", () => {
    const lines = allocatePaymentAutomatically(openTwo, 500);
    expect(lines).toHaveLength(2);
    expect(lines[0]).toMatchObject({
      creditEntryId: openTwo[0]!.creditEntryId,
      amount: 300,
      outstandingAfter: 0,
    });
    expect(lines[1]).toMatchObject({
      creditEntryId: openTwo[1]!.creditEntryId,
      amount: 200,
      outstandingAfter: 585,
    });
    expect(countOpenAfterAllocation(openTwo, lines)).toBe(1);
  });

  it("allocates 750 against 1085 total outstanding", () => {
    const lines = allocatePaymentAutomatically(openTwo, 750);
    const sum = lines.reduce((s, line) => s + line.amount, 0);
    expect(sum).toBe(750);
    expect(lines[0]!.amount).toBe(300);
    expect(lines[1]!.amount).toBe(450);
    expect(countOpenAfterAllocation(openTwo, lines)).toBe(1);
  });

  it("leaves unallocated remainder on overpayment of open lines", () => {
    const lines = allocatePaymentAutomatically(openTwo, 2000);
    const sum = lines.reduce((s, line) => s + line.amount, 0);
    expect(sum).toBe(1085);
    expect(countOpenAfterAllocation(openTwo, lines)).toBe(0);
  });

  it("validates manual allocations that sum to payment", () => {
    const ok = validateManualAllocations({
      open: openTwo,
      paymentAmount: 500,
      allocations: [
        { creditEntryId: openTwo[1]!.creditEntryId, amount: 400 },
        { creditEntryId: openTwo[0]!.creditEntryId, amount: 100 },
      ],
    });
    expect(ok.ok).toBe(true);
    if (ok.ok) {
      expect(ok.lines).toHaveLength(2);
      expect(countOpenAfterAllocation(openTwo, ok.lines)).toBe(2);
    }
  });

  it("rejects manual over-allocation and sum mismatch", () => {
    expect(
      validateManualAllocations({
        open: openTwo,
        paymentAmount: 500,
        allocations: [{ creditEntryId: openTwo[0]!.creditEntryId, amount: 400 }],
      }).ok,
    ).toBe(false);

    expect(
      validateManualAllocations({
        open: openTwo,
        paymentAmount: 100,
        allocations: [{ creditEntryId: openTwo[0]!.creditEntryId, amount: 301 }],
      }),
    ).toMatchObject({ ok: false, error: "exceeds_receivable" });

    expect(
      validateManualAllocations({
        open: openTwo,
        paymentAmount: 100,
        allocations: [{ creditEntryId: openTwo[0]!.creditEntryId, amount: 0 }],
      }),
    ).toMatchObject({ ok: false, error: "invalid_line" });
  });
});

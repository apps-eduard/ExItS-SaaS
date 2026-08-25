import { describe, expect, it } from "vitest";
import {
  formatLinkedCustomerActivityMeta,
  formatLinkedCustomerActivityTitle,
} from "@/features/personal/linked-merchants/format-linked-customer-activity";

describe("formatLinkedCustomerActivity", () => {
  it("formats online purchase utang charges with order reference", () => {
    expect(
      formatLinkedCustomerActivityTitle({
        activityId: "a",
        occurredAtUtc: "2026-08-24T02:00:00Z",
        type: "UtangCharge",
        referenceNumber: "SO-000123",
        chargeAmount: 800,
        paymentAmount: null,
        adjustmentAmount: null,
        balanceAfter: 1300,
        status: "Active",
        hasDetails: true,
        sourceSaleId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      }),
    ).toBe("Online purchase · Order SO-000123 · +800.00");
  });

  it("formats charge and payment titles", () => {
    expect(
      formatLinkedCustomerActivityTitle({
        activityId: "a",
        occurredAtUtc: "2026-08-21T02:00:00Z",
        type: "UtangCharge",
        referenceNumber: "CE-1",
        chargeAmount: 50,
        paymentAmount: null,
        adjustmentAmount: null,
        balanceAfter: 50,
        status: "Active",
        hasDetails: false,
        sourceSaleId: null,
      }),
    ).toBe("CE-1 · +50.00");

    expect(
      formatLinkedCustomerActivityTitle({
        activityId: "b",
        occurredAtUtc: "2026-08-21T03:00:00Z",
        type: "Payment",
        referenceNumber: "RP-1",
        chargeAmount: null,
        paymentAmount: 20,
        adjustmentAmount: null,
        balanceAfter: 30,
        status: "Active",
        hasDetails: false,
        sourceSaleId: null,
      }),
    ).toBe("RP-1 · −20.00");
  });

  it("includes type in meta", () => {
    const meta = formatLinkedCustomerActivityMeta({
      activityId: "c",
      occurredAtUtc: "2026-08-21T02:00:00Z",
      type: "Purchase",
      referenceNumber: "S-1",
      chargeAmount: null,
      paymentAmount: null,
      adjustmentAmount: null,
      balanceAfter: null,
      status: "Completed",
      hasDetails: true,
      sourceSaleId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    });
    expect(meta).toContain("Purchase");
  });
});

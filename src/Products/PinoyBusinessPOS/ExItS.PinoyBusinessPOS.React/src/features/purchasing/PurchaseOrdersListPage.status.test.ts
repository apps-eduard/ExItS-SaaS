import { describe, expect, it } from "vitest";
import type { PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";
import { purchaseOrderListStatusTone } from "@/features/purchasing/PurchaseOrdersListPage";

function po(partial: Partial<PosPurchaseOrderDto>): PosPurchaseOrderDto {
  return {
    purchaseOrderId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    organizationId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    supplierId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    status: "Draft",
    orderDate: "2026-09-11",
    createdAtUtc: "2026-09-11T00:00:00Z",
    updatedAtUtc: "2026-09-11T00:00:00Z",
    lines: [],
    ...partial,
  } as PosPurchaseOrderDto;
}

describe("purchaseOrderListStatusTone", () => {
  it("uses danger for Cancelled", () => {
    expect(purchaseOrderListStatusTone(po({ status: "Cancelled" }))).toBe("danger");
    expect(
      purchaseOrderListStatusTone(po({ status: "Ordered", displayStatus: "Cancelled" })),
    ).toBe("danger");
  });

  it("uses primary for WaitingForSupplier", () => {
    expect(
      purchaseOrderListStatusTone(
        po({ status: "Ordered", displayStatus: "WaitingForSupplier" }),
      ),
    ).toBe("primary");
  });

  it("keeps semantic tones for other statuses", () => {
    expect(purchaseOrderListStatusTone(po({ status: "Draft" }))).toBe("info");
    expect(purchaseOrderListStatusTone(po({ status: "Ordered" }))).toBe("success");
    expect(purchaseOrderListStatusTone(po({ status: "PartiallyReceived" }))).toBe("warning");
    expect(purchaseOrderListStatusTone(po({ status: "Received" }))).toBe("success");
  });
});

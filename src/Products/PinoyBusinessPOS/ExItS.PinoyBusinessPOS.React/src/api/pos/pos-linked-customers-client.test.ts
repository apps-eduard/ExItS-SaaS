import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  EXTENDED_HISTORY_REQUIRED,
  getLinkedCustomerSaleReceipt,
  getLinkedCustomerStatement,
  isExtendedHistoryRequiredError,
  listLinkedCustomerRecentActivity,
} from "@/api/pos/pos-linked-customers-client";
import { PosApiError } from "@/api/pos/pos-http";

const organizationId = "11111111-1111-1111-1111-111111111111";
const businessCustomerId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const saleId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("pos-linked-customers-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("loads linked customer statement summary", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({
        organizationId,
        platformBusinessCustomerId: businessCustomerId,
        posCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        linkedCustomerAppUserId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        merchantDisplayName: "Kizy Store",
        customerDisplayName: "Ana Reyes",
        outstandingBalance: 25.5,
        currency: "PHP",
        asOfUtc: "2026-08-22T00:00:00Z",
      }),
    );

    const summary = await getLinkedCustomerStatement(organizationId, businessCustomerId);
    expect(summary.outstandingBalance).toBe(25.5);
    const url = String(vi.mocked(fetch).mock.calls[0][0]);
    expect(url).toContain(`/api/v1/pos/personal/linked-customers/${businessCustomerId}/statement`);
    expect(url).toContain(`organizationId=${organizationId}`);
  });

  it("lists recent activity with page caps", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({
        organizationId,
        platformBusinessCustomerId: businessCustomerId,
        posCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        items: [
          {
            activityId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
            occurredAtUtc: "2026-08-21T02:00:00Z",
            type: "Purchase",
            referenceNumber: "S-1001",
            chargeAmount: null,
            paymentAmount: null,
            adjustmentAmount: null,
            balanceAfter: null,
            status: "Completed",
            hasDetails: true,
            sourceSaleId: saleId,
          },
        ],
        page: 1,
        pageSize: 10,
        hasMore: false,
        canAccessExtendedHistory: false,
        freeHistoryStartsAtUtc: "2026-05-01T00:00:00Z",
      }),
    );

    const page = await listLinkedCustomerRecentActivity(organizationId, businessCustomerId, {
      pageSize: 50,
    });
    expect(page.items[0]?.type).toBe("Purchase");
    const url = String(vi.mocked(fetch).mock.calls[0][0]);
    expect(url).toContain("/activity");
    expect(url).toContain("pageSize=20");
  });

  it("loads lazy receipt detail", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({
        organizationId,
        platformBusinessCustomerId: businessCustomerId,
        posCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        saleId,
        receiptNumber: "S-1001",
        occurredAtUtc: "2026-08-21T02:00:00Z",
        status: "Completed",
        paymentMethod: "Cash",
        currency: "PHP",
        subtotal: 100,
        taxAmount: 0,
        total: 100,
        lines: [
          {
            lineNumber: 1,
            productNameSnapshot: "Rice",
            quantity: 1,
            unitOfMeasure: "kg",
            sellingMode: "Standard",
            unitPriceSnapshot: 100,
            lineTotal: 100,
          },
        ],
      }),
    );

    const receipt = await getLinkedCustomerSaleReceipt(organizationId, businessCustomerId, saleId);
    expect(receipt.lines[0]?.productNameSnapshot).toBe("Rice");
  });

  it("detects extended history entitlement errors", () => {
    expect(
      isExtendedHistoryRequiredError(
        new PosApiError(403, { errorCode: EXTENDED_HISTORY_REQUIRED, detail: "locked" }),
      ),
    ).toBe(true);
    expect(isExtendedHistoryRequiredError(new Error("other"))).toBe(false);
  });
});

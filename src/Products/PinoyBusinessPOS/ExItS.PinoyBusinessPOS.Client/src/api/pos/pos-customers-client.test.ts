import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createCustomer,
  createCustomerRepayment,
  deactivateCustomer,
  getCustomer,
  getCustomerCreditSummary,
  getCustomerStatement,
  hasExItsPersonalLink,
  listCustomerCreditEntries,
  listCustomerLedger,
  listCustomers,
  reactivateCustomer,
  updateCustomer,
} from "@/api/pos/pos-customers-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const customerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

const customerBody = {
  customerId,
  organizationId: workspace.organizationId,
  displayName: "Juan Dela Cruz",
  mobileNumber: "09171234567",
  address: "Manila",
  notes: null,
  status: "Active",
  createdAtUtc: "2026-08-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
  linkedPersonalPublicUserId: "EXITS-PERSONAL-1",
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("pos-customers-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("lists Active customers with search", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({
        items: [customerBody],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      }),
    );

    const page = await listCustomers(workspace, { search: "Juan", status: "Active" });
    expect(page.items[0]?.displayName).toBe("Juan Dela Cruz");
    const url = String(vi.mocked(fetch).mock.calls[0][0]);
    expect(url).toContain("/api/v1/pos/customers");
    expect(url).toContain("status=Active");
    expect(url).toContain("search=Juan");
  });

  it("gets, creates, updates, and toggles customer lifecycle", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(jsonResponse(customerBody))
      .mockResolvedValueOnce(jsonResponse(customerBody, 201))
      .mockResolvedValueOnce(jsonResponse({ ...customerBody, displayName: "Juan Updated" }))
      .mockResolvedValueOnce(jsonResponse({ ...customerBody, status: "Inactive" }))
      .mockResolvedValueOnce(jsonResponse({ ...customerBody, status: "Active" }));

    await expect(getCustomer(workspace, customerId)).resolves.toMatchObject({
      displayName: "Juan Dela Cruz",
    });
    await expect(
      createCustomer(workspace, { displayName: "Juan Dela Cruz", mobileNumber: "09171234567" }),
    ).resolves.toMatchObject({ customerId });
    await expect(
      updateCustomer(workspace, customerId, { displayName: "Juan Updated" }),
    ).resolves.toMatchObject({ displayName: "Juan Updated" });
    await expect(deactivateCustomer(workspace, customerId)).resolves.toMatchObject({
      status: "Inactive",
    });
    await expect(reactivateCustomer(workspace, customerId)).resolves.toMatchObject({
      status: "Active",
    });

    expect(String(vi.mocked(fetch).mock.calls[1][0])).toContain("/api/v1/pos/customers");
    expect(vi.mocked(fetch).mock.calls[1][1]?.method).toBe("POST");
    expect(String(vi.mocked(fetch).mock.calls[3][0])).toContain("/deactivate");
    expect(String(vi.mocked(fetch).mock.calls[4][0])).toContain("/reactivate");
  });

  it("loads credit summary, credits, ledger, repayment, and statement", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(
        jsonResponse({
          customerId,
          organizationId: workspace.organizationId,
          outstandingAmount: 18.5,
          activeEntryCount: 1,
          totalEntryCount: 1,
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          items: [
            {
              creditEntryId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
              organizationId: workspace.organizationId,
              customerId,
              amount: 18.5,
              remarks: "Sale S-1",
              status: "Active",
              createdAtUtc: "2026-08-21T02:00:00Z",
              sourceSaleId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          repaymentId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
          organizationId: workspace.organizationId,
          customerId,
          amount: 10,
          remarks: "Partial",
          status: "Active",
          recordedAtUtc: "2026-08-21T03:00:00Z",
          recordedBy: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          organizationId: workspace.organizationId,
          customerId,
          customerDisplayName: "Juan Dela Cruz",
          periodStart: "2026-08-01",
          periodEnd: "2026-08-31",
          openingBalance: 0,
          closingBalance: 8.5,
          periodCreditTotal: 18.5,
          periodRepaymentTotal: 10,
          periodReversalCreditTotal: 0,
          periodReversalRepaymentTotal: 0,
          outstandingBalance: 8.5,
          overdueAmount: 0,
          overdueCreditCount: 0,
          generatedAtUtc: "2026-08-21T04:00:00Z",
          currencyCode: "PHP",
          cultureName: "en-PH",
          lines: [],
        }),
      );

    const summary = await getCustomerCreditSummary(workspace, customerId);
    expect(summary.outstandingAmount).toBe(18.5);

    const credits = await listCustomerCreditEntries(workspace, customerId);
    expect(credits.items[0]?.amount).toBe(18.5);

    await listCustomerLedger(workspace, customerId);
    await createCustomerRepayment(workspace, customerId, { amount: 10, remarks: "Partial" });
    const statement = await getCustomerStatement(workspace, customerId, {
      periodStart: "2026-08-01",
      periodEnd: "2026-08-31",
    });
    expect(statement.outstandingBalance).toBe(8.5);

    expect(String(vi.mocked(fetch).mock.calls[0][0])).toContain("/credit-summary");
    expect(String(vi.mocked(fetch).mock.calls[1][0])).toContain("/credit-entries");
    expect(String(vi.mocked(fetch).mock.calls[3][0])).toContain("/repayments");
    expect(vi.mocked(fetch).mock.calls[3][1]?.method).toBe("POST");
    expect(String(vi.mocked(fetch).mock.calls[4][0])).toContain("/statement");
  });

  it("detects read-only ExItS Personal link status", () => {
    expect(hasExItsPersonalLink(customerBody)).toBe(true);
    expect(hasExItsPersonalLink({ ...customerBody, linkedPersonalPublicUserId: null })).toBe(false);
  });

  it("proves discounted Utang credit amount equals net Amount to Pay from server", async () => {
    const netAmountToPay = 18.5;
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({
        items: [
          {
            creditEntryId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
            organizationId: workspace.organizationId,
            customerId,
            amount: netAmountToPay,
            remarks: "Sale S-9012",
            status: "Active",
            createdAtUtc: "2026-08-21T02:00:00Z",
            sourceSaleId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      }),
    );

    const credits = await listCustomerCreditEntries(workspace, customerId);
    expect(credits.items[0]?.amount).toBe(netAmountToPay);
  });
});

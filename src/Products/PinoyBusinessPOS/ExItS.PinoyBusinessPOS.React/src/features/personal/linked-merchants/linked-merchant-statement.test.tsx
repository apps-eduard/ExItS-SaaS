import { describe, expect, it, vi } from "vitest";

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: { userId: "me", displayName: "Kizy" },
  }),
}));

import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as linkedClient from "@/api/pos/pos-linked-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { LinkedMerchantStatementPage } from "@/features/personal/linked-merchants/LinkedMerchantStatementPage";

vi.mock("@/api/pos/pos-linked-customers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof linkedClient>();
  return {
    ...actual,
    getLinkedCustomerStatement: vi.fn(),
    listLinkedCustomerOpenDebtActivity: vi.fn(),
    listLinkedCustomerRecentActivity: vi.fn(),
  };
});

const organizationId = "11111111-1111-1111-1111-111111111111";
const businessCustomerId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

function renderStatement() {
  return render(
    <AppProviders>
      <MemoryRouter
        initialEntries={[`/personal/linked-merchants/${organizationId}/${businessCustomerId}`]}
      >
        <Routes>
          <Route
            path="/personal/linked-merchants/:organizationId/:businessCustomerId"
            element={<LinkedMerchantStatementPage />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

vi.mock("@/features/customer-ordering/useLinkedMerchantShopContext", () => ({
  useLinkedMerchantShopContext: () => ({
    data: {
      organizationDisplayName: "mica store",
      customerDisplayName: "Toto Uy",
      statementTo: null,
    },
  }),
}));

describe("LinkedMerchantStatementPage", () => {
  it("renders outstanding balance and recent activity", async () => {
    vi.mocked(linkedClient.getLinkedCustomerStatement).mockResolvedValue({
      organizationId,
      platformBusinessCustomerId: businessCustomerId,
      posCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      linkedCustomerAppUserId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
      merchantDisplayName: "Kizy Store",
      customerDisplayName: "Ana Reyes",
      outstandingBalance: 0,
      currency: "PHP",
      asOfUtc: "2026-08-22T00:00:00Z",
    });
    vi.mocked(linkedClient.listLinkedCustomerRecentActivity).mockResolvedValue({
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
          sourceSaleId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        },
      ],
      page: 1,
      pageSize: 10,
      hasMore: false,
      canAccessExtendedHistory: false,
      freeHistoryStartsAtUtc: "2026-05-01T00:00:00Z",
    });

    renderStatement();

    await waitFor(() => {
      expect(screen.getByTestId("linked-merchant-statement-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("linked-merchant-outstanding")).toHaveTextContent("0.00 PHP");
    expect(screen.getByTestId("linked-merchant-activity-receipt-link")).toBeInTheDocument();
  });

  it("renders connected + history-not-ready when statement returns 404", async () => {
    vi.mocked(linkedClient.getLinkedCustomerStatement).mockRejectedValue(
      new PosApiError(404, {
        errorCode: "pos.linked_customer.not_found",
        detail: "Linked customer was not found.",
      }),
    );

    renderStatement();

    await waitFor(() => {
      expect(screen.getByTestId("merchant-statement-status-historyNotReady")).toBeInTheDocument();
    });
    expect(screen.getByTestId("merchant-statement-connection-chip")).toBeInTheDocument();
    expect(screen.getByTestId("merchant-statement-status-historyNotReady")).toHaveTextContent(
      /not ready|connection is active/i,
    );
    expect(screen.queryByText("Linked customer was not found.")).not.toBeInTheDocument();
    expect(screen.queryByText("Purchase history unavailable")).not.toBeInTheDocument();
    expect(screen.getByTestId("merchant-statement-retry")).toBeInTheDocument();
  });

  it("renders connected + load-error without implying the relationship broke", async () => {
    vi.mocked(linkedClient.getLinkedCustomerStatement).mockRejectedValue(
      new PosApiError(503, {
        errorCode: "application.unavailable",
        detail: "Service unavailable",
      }),
    );

    renderStatement();

    await waitFor(() => {
      expect(screen.getByTestId("merchant-statement-status-historyLoadError")).toBeInTheDocument();
    });
    expect(screen.getByTestId("merchant-statement-connection-chip")).toBeInTheDocument();
    expect(screen.queryByText("Service unavailable")).not.toBeInTheDocument();
  });

  it("renders a friendly not-found panel when the linked customer is missing", async () => {
    vi.mocked(linkedClient.getLinkedCustomerStatement).mockRejectedValue(
      new PosApiError(404, {
        errorCode: "pos.linked_customer.not_found",
        detail: "Linked customer was not found.",
      }),
    );

    renderStatement();

    await waitFor(() => {
      expect(screen.getByTestId("merchant-statement-status-historyNotReady")).toBeInTheDocument();
    });
    expect(screen.getByTestId("page-header-back-merchant-statement")).toBeInTheDocument();
    expect(screen.getByTestId("merchant-statement-back-stores")).toBeInTheDocument();
    expect(screen.getByTestId("merchant-statement-retry")).toBeInTheDocument();
  });

  it("renders an empty-activity panel when balance and recent rows are zero", async () => {
    vi.mocked(linkedClient.getLinkedCustomerStatement).mockResolvedValue({
      organizationId,
      platformBusinessCustomerId: businessCustomerId,
      posCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      linkedCustomerAppUserId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
      merchantDisplayName: "mica store",
      customerDisplayName: "Toto Uy",
      outstandingBalance: 0,
      currency: "PHP",
      asOfUtc: "2026-08-22T00:00:00Z",
    });
    vi.mocked(linkedClient.listLinkedCustomerRecentActivity).mockResolvedValue({
      organizationId,
      platformBusinessCustomerId: businessCustomerId,
      posCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      items: [],
      page: 1,
      pageSize: 10,
      hasMore: false,
      canAccessExtendedHistory: false,
      freeHistoryStartsAtUtc: "2026-05-01T00:00:00Z",
    });

    renderStatement();

    await waitFor(() => {
      expect(screen.getByTestId("merchant-statement-status-empty")).toBeInTheDocument();
    });
    expect(screen.getByTestId("linked-merchant-outstanding")).toHaveTextContent("0.00 PHP");
  });
});

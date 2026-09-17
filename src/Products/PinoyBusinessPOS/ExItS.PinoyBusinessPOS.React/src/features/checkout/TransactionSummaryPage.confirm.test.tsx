import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as salesClient from "@/api/pos/pos-sales-client";
import { TransactionSummaryPage } from "@/features/checkout/TransactionSummaryPage";

vi.mock("@/api/pos/pos-sales-client", async (importOriginal) => {
  const actual = await importOriginal<typeof salesClient>();
  return {
    ...actual,
    getSale: vi.fn(),
    voidSale: vi.fn(),
  };
});

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: () => null,
    isResolving: false,
    sortedIds: [],
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    },
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
      membershipRole: "OrganizationMember",
      organizationManagementAuthority: false,
    },
  }),
}));

const saleId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

function completedSale() {
  return {
    saleId,
    organizationId: "11111111-1111-1111-1111-111111111111",
    saleNumber: "SALE-20260906-000001",
    status: "Completed",
    paymentMethod: "Cash",
    subtotal: 103.5,
    total: 103.5,
    taxAmount: 0,
    amountTendered: 150,
    changeAmount: 46.5,
    recordedAtUtc: "2026-09-06T05:29:33Z",
    recordedBy: "ffffffff-ffff-4fff-8fff-ffffffffffff",
    voidedAtUtc: null,
    voidedBy: null,
    voidReason: null,
    updatedAtUtc: "2026-09-06T05:29:33Z",
    lines: [
      {
        saleLineId: "99999999-9999-4999-8999-999999999999",
        productId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        lineNumber: 1,
        name: "Battery AA Pack",
        sku: "BAT-AA",
        unitOfMeasure: "pc",
        sellingMode: "PerItem",
        unitPrice: 45,
        quantity: 1,
        lineTotal: 45,
      },
    ],
    documentKind: "TransactionSummary",
  };
}

describe("TransactionSummaryPage confirm dialogs", () => {
  beforeEach(() => {
    vi.mocked(salesClient.getSale).mockReset();
    vi.mocked(salesClient.getSale).mockResolvedValue(completedSale() as never);
  });

  function renderPage() {
    return render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/sell/sales/${saleId}/summary`]}>
          <Routes>
            <Route path="/sell/sales/:saleId/summary" element={<TransactionSummaryPage />} />
            <Route path="/returns/sale/:saleId" element={<div data-testid="returns-page">Returns</div>} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
  }

  it("requires confirm before opening void reason sheet", async () => {
    const user = userEvent.setup();
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-void-trigger")).toBeInTheDocument());
    await user.click(screen.getByTestId("summary-void-trigger"));

    expect(screen.getByTestId("summary-void-confirm-dialog")).toBeInTheDocument();
    expect(screen.queryByTestId("summary-void-panel")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("summary-void-confirm-dialog-confirm"));
    await waitFor(() => expect(screen.getByTestId("summary-void-panel")).toBeInTheDocument());
  });

  it("requires confirm before navigating to return items", async () => {
    const user = userEvent.setup();
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-return-items")).toBeInTheDocument());
    await user.click(screen.getByTestId("summary-return-items"));

    expect(screen.getByTestId("summary-return-confirm-dialog")).toBeInTheDocument();
    expect(screen.queryByTestId("returns-page")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("summary-return-confirm-dialog-confirm"));
    await waitFor(() => expect(screen.getByTestId("returns-page")).toBeInTheDocument());
  });

  it("cancels return confirm without navigating", async () => {
    const user = userEvent.setup();
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-return-items")).toBeInTheDocument());
    await user.click(screen.getByTestId("summary-return-items"));
    await user.click(screen.getByTestId("summary-return-confirm-dialog-cancel"));

    expect(screen.queryByTestId("summary-return-confirm-dialog")).not.toBeInTheDocument();
    expect(screen.queryByTestId("returns-page")).not.toBeInTheDocument();
    expect(screen.getByTestId("transaction-summary-page")).toBeInTheDocument();
  });
});

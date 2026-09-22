import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as salesClient from "@/api/pos/pos-sales-client";
import * as returnBatchesClient from "@/api/pos/pos-return-batches-client";
import { TransactionSummaryPage } from "@/features/checkout/TransactionSummaryPage";

vi.mock("@/api/pos/pos-sales-client", async (importOriginal) => {
  const actual = await importOriginal<typeof salesClient>();
  return {
    ...actual,
    getSale: vi.fn(),
    voidSale: vi.fn(),
  };
});

vi.mock("@/api/pos/pos-return-batches-client", async (importOriginal) => {
  const actual = await importOriginal<typeof returnBatchesClient>();
  return {
    ...actual,
    listReturnBatches: vi.fn(),
  };
});

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (actorId: string | null | undefined) => {
      if (actorId === "ffffffff-ffff-4fff-8fff-ffffffffffff") {
        return { actorId, displayName: "Mica Uy", actorStatus: "Active" };
      }
      return null;
    },
    isResolving: false,
    sortedIds: [],
  }),
}));

const managerGrant = {
  productAccessAllowed: true,
  mappedPosRoleCode: "StoreManager",
  productLocalRoleCode: "StoreManager",
  membershipRole: "OrganizationMember",
  organizationManagementAuthority: false,
};

const cashierGrant = {
  productAccessAllowed: true,
  mappedPosRoleCode: "Cashier",
  productLocalRoleCode: "Cashier",
  membershipRole: "OrganizationMember",
  organizationManagementAuthority: false,
};

let sessionGrant: typeof managerGrant = managerGrant;

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    },
    sessionGrant,
  }),
}));

const saleId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

function completedSale(overrides: Record<string, unknown> = {}) {
  return {
    saleId,
    organizationId: "11111111-1111-1111-1111-111111111111",
    saleNumber: "260906-001",
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
    shiftNumber: "260906-002",
    costStatus: "Complete",
    totalCostSnapshot: 40,
    grossProfit: 63.5,
    grossMarginPercent: 61.4,
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
        lineCostSnapshot: 20,
      },
    ],
    documentKind: "TransactionSummary",
    ...overrides,
  };
}

function voidedSale() {
  return {
    ...completedSale(),
    status: "Voided",
    voidedAtUtc: "2026-09-06T06:00:00Z",
    voidedBy: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    voidReason: "Wrong tender",
  };
}

describe("TransactionSummaryPage post-pay cleanup", () => {
  beforeEach(() => {
    sessionGrant = managerGrant;
    vi.mocked(salesClient.getSale).mockReset();
    vi.mocked(salesClient.getSale).mockResolvedValue(completedSale() as never);
    vi.mocked(returnBatchesClient.listReturnBatches).mockReset();
    vi.mocked(returnBatchesClient.listReturnBatches).mockResolvedValue([]);
    vi.spyOn(window, "print").mockImplementation(() => undefined);
  });

  function renderPage() {
    return render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/sell/sales/${saleId}/summary`]}>
          <Routes>
            <Route path="/sell/sales/:saleId/summary" element={<TransactionSummaryPage />} />
            <Route path="/sell" element={<div data-testid="sell-page">Sell</div>} />
            <Route path="/returns/sale/:saleId" element={<div data-testid="returns-page">Returns</div>} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
  }

  it("keeps operational detail without embedding the A4 document canvas", async () => {
    renderPage();

    await waitFor(() => expect(screen.getByTestId("transaction-summary-page")).toBeInTheDocument());

    expect(screen.queryByTestId("summary-success-banner")).not.toBeInTheDocument();
    expect(screen.queryByTestId("summary-document-section")).not.toBeInTheDocument();
    expect(screen.queryByText(/^Document$/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("summary-document-preview")).not.toBeInTheDocument();
    expect(screen.getByTestId("summary-print-host")).toBeInTheDocument();
    expect(
      within(screen.getByTestId("summary-print-host")).getByTestId("customer-purchase-summary-document"),
    ).toBeInTheDocument();

    const header = screen.getByTestId("summary-header-actions");
    expect(within(header).getByTestId("summary-new-sale")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-preview")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-print")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-print")).toHaveTextContent(/^Print$/);
    expect(within(header).queryAllByTestId("summary-print")).toHaveLength(1);
    expect(within(header).getByTestId("summary-return-items")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-void-trigger")).toBeInTheDocument();

    expect(screen.getByTestId("summary-sale-number")).toHaveTextContent("260906-001");
    expect(screen.getByTestId("summary-shift")).toHaveTextContent("260906-002");
    expect(screen.getByTestId("summary-sold-by")).toHaveTextContent("Mica Uy");
    expect(screen.getByTestId("summary-status")).toHaveTextContent("Completed");
    expect(screen.getByTestId("summary-total")).toHaveTextContent("103.50");
    expect(screen.getByTestId("summary-tendered")).toBeInTheDocument();
    expect(screen.getByTestId("summary-change")).toBeInTheDocument();
    expect(screen.queryByTestId("transaction-summary-disclaimer")).not.toBeInTheDocument();
    expect(
      within(screen.getByTestId("summary-print-host")).getByTestId("business-document-disclaimer"),
    ).toHaveTextContent("NOT A BIR INVOICE");
  });

  it("opens canonical Customer Purchase Summary in Preview with Print and PDF", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => expect(screen.getByTestId("summary-preview")).toBeInTheDocument());

    await user.click(screen.getByTestId("summary-preview"));

    const preview = await screen.findByTestId("summary-document-preview");
    expect(within(preview).getByTestId("customer-purchase-summary-document")).toBeInTheDocument();
    expect(within(preview).getByTestId("business-document-title")).toHaveTextContent(
      "Customer Purchase Summary",
    );
    expect(within(preview).getByTestId("business-document-disclaimer")).toHaveTextContent(
      "NOT A BIR INVOICE",
    );
    expect(screen.queryByTestId("summary-print-host")).not.toBeInTheDocument();

    await user.click(within(preview).getByTestId("summary-document-preview-print"));
    expect(window.print).toHaveBeenCalled();

    await user.click(within(preview).getByTestId("summary-document-preview-pdf"));
    expect(window.print).toHaveBeenCalledTimes(2);
  });

  it("prints canonical document from header Print without opening Preview", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => expect(screen.getByTestId("summary-print")).toBeInTheDocument());
    await user.click(screen.getByTestId("summary-print"));
    expect(window.print).toHaveBeenCalled();
    expect(screen.getByTestId("summary-print-host")).toBeInTheDocument();
  });

  it("renders Utang balance instead of cash tendered/change on operational detail", async () => {
    vi.mocked(salesClient.getSale).mockResolvedValue(
      completedSale({
        paymentMethod: "Utang",
        amountTendered: null,
        changeAmount: null,
        customerDisplayName: "Ana",
      }) as never,
    );
    renderPage();
    await waitFor(() => expect(screen.getByTestId("summary-utang-balance")).toBeInTheDocument());
    expect(screen.queryByTestId("summary-tendered")).not.toBeInTheDocument();
    expect(screen.queryByTestId("summary-change")).not.toBeInTheDocument();
    expect(screen.getByTestId("summary-customer")).toHaveTextContent("Ana");
  });

  it("keeps Sold by under Shift without a separate sold-by timestamp", async () => {
    renderPage();
    await waitFor(() => expect(screen.getByTestId("summary-details-section")).toBeInTheDocument());

    const details = screen.getByTestId("summary-details-section").textContent ?? "";
    const shiftIdx = details.indexOf("Shift");
    const soldIdx = details.indexOf("Sold by");
    expect(shiftIdx).toBeGreaterThanOrEqual(0);
    expect(soldIdx).toBeGreaterThan(shiftIdx);
  });

  it("sticky bottom only has New sale and Print", async () => {
    renderPage();
    await waitFor(() => expect(screen.getByTestId("sticky-action-bar")).toBeInTheDocument());

    const sticky = screen.getByTestId("sticky-action-bar");
    expect(within(sticky).getByTestId("summary-new-sale-sticky")).toBeInTheDocument();
    expect(within(sticky).getByTestId("summary-print-sticky")).toBeInTheDocument();
    expect(within(sticky).getByTestId("summary-print-sticky")).toHaveTextContent(/^Print$/);
    expect(within(sticky).queryByTestId("summary-void-trigger")).not.toBeInTheDocument();
    expect(within(sticky).queryByTestId("summary-return-items")).not.toBeInTheDocument();
  });

  it("hides void/return for voided sales but keeps New sale, Preview, and Print", async () => {
    vi.mocked(salesClient.getSale).mockResolvedValue(voidedSale() as never);
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-voided-banner")).toBeInTheDocument());
    const header = screen.getByTestId("summary-header-actions");
    expect(within(header).getByTestId("summary-new-sale")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-preview")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-print")).toBeInTheDocument();
    expect(within(header).queryByTestId("summary-void-trigger")).not.toBeInTheDocument();
    expect(within(header).queryByTestId("summary-return-items")).not.toBeInTheDocument();
  });

  it("respects cashier permissions by denying void while keeping primary actions", async () => {
    sessionGrant = cashierGrant;
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-new-sale")).toBeInTheDocument());
    expect(screen.getByTestId("summary-preview")).toBeInTheDocument();
    expect(screen.getByTestId("summary-print")).toBeInTheDocument();
    expect(screen.queryByTestId("summary-void-trigger")).not.toBeInTheDocument();
    expect(screen.getByTestId("summary-void-denied")).toBeInTheDocument();
  });
});

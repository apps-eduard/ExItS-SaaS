import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
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
    shiftNumber: "SHIFT-20260906-000002",
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

function voidedSale() {
  return {
    ...completedSale(),
    status: "Voided",
    voidedAtUtc: "2026-09-06T06:00:00Z",
    voidedBy: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    voidReason: "Wrong tender",
  };
}

describe("TransactionSummaryPage post-pay actions", () => {
  beforeEach(() => {
    sessionGrant = managerGrant;
    vi.mocked(salesClient.getSale).mockReset();
    vi.mocked(salesClient.getSale).mockResolvedValue(completedSale() as never);
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

  it("puts primary actions in the header and sticky New sale / Print only", async () => {
    renderPage();

    await waitFor(() => expect(screen.getByTestId("transaction-summary-page")).toBeInTheDocument());

    const header = screen.getByTestId("summary-header-actions");
    expect(within(header).getByTestId("summary-new-sale")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-print")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-return-items")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-void-trigger")).toBeInTheDocument();

    const sticky = screen.getByTestId("sticky-action-bar");
    expect(within(sticky).getByTestId("summary-new-sale-sticky")).toBeInTheDocument();
    expect(within(sticky).getByTestId("summary-print-sticky")).toBeInTheDocument();
    expect(within(sticky).queryByTestId("summary-void-trigger")).not.toBeInTheDocument();
    expect(within(sticky).queryByTestId("summary-return-items")).not.toBeInTheDocument();

    expect(screen.getByTestId("summary-details-section")).toBeInTheDocument();
    expect(screen.getByTestId("summary-items-section")).toBeInTheDocument();
    expect(screen.getByTestId("summary-totals-section")).toBeInTheDocument();
    expect(screen.getByTestId("summary-sale-number")).toHaveTextContent("SALE-20260906-000001");
    expect(screen.getByTestId("summary-shift")).toHaveTextContent("SHIFT-20260906-000002");
    expect(screen.getByTestId("summary-total")).toBeInTheDocument();
    expect(screen.queryByTestId("summary-success-banner")).not.toBeInTheDocument();
  });

  it("keeps Void sale in the header action group as a destructive control", async () => {
    renderPage();
    await waitFor(() => expect(screen.getByTestId("summary-void-trigger")).toBeInTheDocument());

    const header = screen.getByTestId("summary-header-actions");
    const voidBtn = within(header).getByTestId("summary-void-trigger");
    expect(voidBtn.className).toMatch(/destructive|danger/i);
    expect(within(header).getByTestId("summary-new-sale")).toBeInTheDocument();
  });

  it("prints summary via browser print", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => expect(screen.getByTestId("summary-print")).toBeInTheDocument());
    await user.click(screen.getByTestId("summary-print"));
    expect(window.print).toHaveBeenCalled();
  });

  it("hides void/return for voided sales but keeps New sale and Print", async () => {
    vi.mocked(salesClient.getSale).mockResolvedValue(voidedSale() as never);
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-voided-banner")).toBeInTheDocument());
    const header = screen.getByTestId("summary-header-actions");
    expect(within(header).getByTestId("summary-new-sale")).toBeInTheDocument();
    expect(within(header).getByTestId("summary-print")).toBeInTheDocument();
    expect(within(header).queryByTestId("summary-void-trigger")).not.toBeInTheDocument();
    expect(within(header).queryByTestId("summary-return-items")).not.toBeInTheDocument();
  });

  it("respects cashier permissions by denying void while keeping primary actions", async () => {
    sessionGrant = cashierGrant;
    renderPage();

    await waitFor(() => expect(screen.getByTestId("summary-new-sale")).toBeInTheDocument());
    expect(screen.getByTestId("summary-print")).toBeInTheDocument();
    expect(screen.queryByTestId("summary-void-trigger")).not.toBeInTheDocument();
    expect(screen.getByTestId("summary-void-denied")).toBeInTheDocument();
  });
});

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DirectPurchasesListPage } from "@/features/purchasing/DirectPurchasesListPage";

const listDirectPurchases = vi.fn();

vi.mock("@/access/pos-capabilities", () => ({
  canManageInventory: () => true,
}));

vi.mock("@/api/pos/pos-direct-purchases-client", () => ({
  listDirectPurchases: (...args: unknown[]) => listDirectPurchases(...args),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    },
    sessionGrant: { capabilities: ["ViewInventory", "ManageInventory"] },
  }),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/purchasing/direct-purchases"]}>
        <Routes>
          <Route path="/purchasing/direct-purchases" element={<DirectPurchasesListPage />} />
          <Route path="/purchasing/direct-purchases/b2b/:saleId" element={<div>b2b-detail</div>} />
          <Route path="/purchasing/direct-purchases/:receiptId" element={<div>local-detail</div>} />
          <Route path="/purchasing/receive-stock" element={<div>receive</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("DirectPurchasesListPage", () => {
  beforeEach(() => {
    listDirectPurchases.mockReset();
    listDirectPurchases.mockResolvedValue({
      items: [
        {
          sourceId: "11111111-1111-1111-1111-111111111111",
          sourceType: "B2B",
          occurredAtUtc: "2026-09-10T08:00:00Z",
          purchaseDate: "2026-09-10",
          sellerDisplayName: "Mica Store",
          sellerOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          sellerPublicOrganizationId: "ORG000001",
          referenceNumber: "TXN-001245",
          lineCount: 2,
          totalAmount: 1250,
          status: "Completed",
          paymentMethod: "Cash",
          sellerStoreDisplayName: "Main Branch",
        },
        {
          sourceId: "22222222-2222-2222-2222-222222222222",
          sourceType: "Local",
          occurredAtUtc: "2026-09-09T00:00:00Z",
          purchaseDate: "2026-09-09",
          sellerDisplayName: "Public Market",
          sellerOrganizationId: null,
          sellerPublicOrganizationId: null,
          referenceNumber: "DPR-000044",
          lineCount: 5,
          totalAmount: 2100,
          status: "Completed",
          paymentMethod: null,
          sellerStoreDisplayName: null,
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 20,
    });
  });

  it("renders mixed Local+B2B rows with source badges and Record direct purchase", async () => {
    const user = userEvent.setup();
    renderPage();

    expect(await screen.findByTestId("direct-purchases-list-page")).toBeInTheDocument();
    expect(screen.getByTestId("direct-new")).toHaveTextContent("purchasing.recordDirectPurchase");
    expect(await screen.findAllByText("Mica Store")).not.toHaveLength(0);
    expect(screen.getAllByText("Public Market").length).toBeGreaterThan(0);
    expect(screen.getAllByText("purchasing.directBadgeB2b").length).toBeGreaterThan(0);
    expect(screen.getAllByText("purchasing.directBadgeLocal").length).toBeGreaterThan(0);

    await user.click(screen.getByTestId("direct-source-b2b"));
    expect(listDirectPurchases).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ sourceType: "B2B" }),
      expect.anything(),
    );
  });

  it("navigates B2B rows to buyer-safe detail", async () => {
    const user = userEvent.setup();
    renderPage();
    const link = await screen.findByTestId(
      "direct-row-b2b-11111111-1111-1111-1111-111111111111",
    );
    await user.click(link);
    expect(await screen.findByText("b2b-detail")).toBeInTheDocument();
  });
});

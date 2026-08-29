import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as directClient from "@/api/pos/pos-direct-purchase-receipts-client";
import { DirectPurchaseDetailPage } from "@/features/purchasing/DirectPurchaseDetailPage";
import { formatPeso } from "@/lib/format-money";

const receiptId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    },
  }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: () => ({
      actorId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      displayName: "Maria Santos",
      actorStatus: "Active",
    }),
    isResolving: false,
    sortedIds: [],
    isLoading: false,
    isFetching: false,
    data: [],
  }),
}));

describe("DirectPurchaseDetailPage cost UX", () => {
  beforeEach(() => {
    vi.spyOn(directClient, "getDirectPurchaseReceipt").mockResolvedValue({
      directPurchaseReceiptId: receiptId,
      organizationId: workspace.organizationId,
      receiptNumber: "DP-000045",
      purchaseDate: "2026-08-28",
      sourceNameSnapshot: "ABC Trading",
      referenceNumber: "OR-12345",
      notes: "Morning delivery",
      totalCost: 4500,
      createdByUserId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      createdAtUtc: "2026-08-28T14:15:00Z",
      lines: [
        {
          lineId: "11111111-1111-4111-8111-111111111111",
          productId: "22222222-2222-4222-8222-222222222222",
          lineNumber: 1,
          productNameSnapshot: "Bath Soap",
          unitOfMeasure: "Piece",
          quantity: 24,
          unitCost: 18,
          lineTotal: 432,
          expiryDate: "2027-12-30",
          lotNumber: "LOT-A123",
        },
      ],
    } as never);
  });

  it("shows purchase metadata, money formatting, expiry/lot, and actor", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/direct-purchases/${receiptId}`]}>
          <Routes>
            <Route
              path="/purchasing/direct-purchases/:receiptId"
              element={<DirectPurchaseDetailPage />}
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("direct-purchase-detail-page")).toBeInTheDocument();
    });
    expect(screen.getByText("DP-000045")).toBeInTheDocument();
    expect(screen.getByTestId("direct-purchase-source")).toHaveTextContent("ABC Trading");
    expect(screen.getByTestId("direct-purchase-reference")).toHaveTextContent("OR-12345");
    expect(screen.getByTestId("direct-purchase-notes")).toHaveTextContent("Morning delivery");
    expect(screen.getByTestId("direct-purchase-total")).toHaveTextContent(formatPeso(4500));
    expect(screen.getByText("Bath Soap")).toBeInTheDocument();
    expect(screen.getByText(formatPeso(18))).toBeInTheDocument();
    expect(screen.getByText(formatPeso(432))).toBeInTheDocument();
    expect(screen.getByText("2027-12-30")).toBeInTheDocument();
    expect(screen.getByText("LOT-A123")).toBeInTheDocument();
    expect(screen.getByText("Maria Santos")).toBeInTheDocument();
  });
});

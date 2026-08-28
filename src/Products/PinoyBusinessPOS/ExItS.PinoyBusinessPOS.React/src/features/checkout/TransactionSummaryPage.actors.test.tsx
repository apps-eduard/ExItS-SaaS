import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
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
    resolve: (actorId: string | null | undefined) => {
      if (!actorId) {
        return null;
      }
      if (actorId === "ffffffff-ffff-4fff-8fff-ffffffffffff") {
        return {
          actorId,
          displayName: "Cashier Ana",
          actorStatus: "Active",
        };
      }
      if (actorId === "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb") {
        return {
          actorId,
          displayName: "Manager Ben",
          actorStatus: "Active",
        };
      }
      return null;
    },
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
const productId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const recordedBy = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const voidedBy = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

function voidedSale() {
  return {
    saleId,
    organizationId: "11111111-1111-1111-1111-111111111111",
    saleNumber: "S-9001",
    status: "Voided",
    paymentMethod: "Cash",
    subtotal: 25,
    total: 25,
    taxAmount: 0,
    amountTendered: 50,
    changeAmount: 25,
    recordedAtUtc: "2026-08-21T02:00:00Z",
    recordedBy,
    voidedAtUtc: "2026-08-21T03:00:00Z",
    voidedBy,
    voidReason: "Wrong items",
    updatedAtUtc: "2026-08-21T03:00:00Z",
    lines: [
      {
        saleLineId: "99999999-9999-4999-8999-999999999999",
        productId,
        lineNumber: 1,
        name: "Coke",
        sku: "COKE-330",
        unitOfMeasure: "pc",
        sellingMode: "PerItem",
        unitPrice: 25,
        quantity: 1,
        lineTotal: 25,
      },
    ],
    documentKind: "TransactionSummary",
  };
}

describe("TransactionSummaryPage actor attribution", () => {
  beforeEach(() => {
    vi.mocked(salesClient.getSale).mockReset();
    vi.mocked(salesClient.getSale).mockResolvedValue(voidedSale() as never);
  });

  it("shows Sold by and Voided by as separate attributions", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/sell/sales/${saleId}/summary`]}>
          <Routes>
            <Route path="/sell/sales/:saleId/summary" element={<TransactionSummaryPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("summary-sold-by")).toBeInTheDocument();
    });

    expect(screen.getByTestId("summary-sold-by")).toHaveTextContent("Sold by");
    expect(screen.getByTestId("summary-sold-by")).toHaveTextContent("Cashier Ana");
    expect(screen.getByTestId("summary-voided-by")).toHaveTextContent("Voided by");
    expect(screen.getByTestId("summary-voided-by")).toHaveTextContent("Manager Ben");
    expect(screen.queryByText(recordedBy)).not.toBeInTheDocument();
    expect(screen.queryByText(voidedBy)).not.toBeInTheDocument();
  });
});

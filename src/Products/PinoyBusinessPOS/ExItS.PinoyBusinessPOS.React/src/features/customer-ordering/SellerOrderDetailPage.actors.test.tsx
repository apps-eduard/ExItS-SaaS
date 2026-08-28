import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as ordersClient from "@/api/pos/pos-customer-orders-client";
import { SellerOrderDetailPage } from "@/features/customer-ordering/SellerOrderDetailPage";

vi.mock("@/api/pos/pos-customer-orders-client", async (importOriginal) => {
  const actual = await importOriginal<typeof ordersClient>();
  return {
    ...actual,
    getSellerCustomerOrder: vi.fn(),
  };
});

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (actorId: string | null | undefined) => {
      if (actorId === "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa") {
        return {
          actorId,
          displayName: "Seller Mia",
          actorStatus: "Active",
        };
      }
      if (actorId === "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb") {
        return {
          actorId,
          displayName: "Runner Leo",
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

const orderId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const acceptedBy = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const readyBy = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

function orderDetail() {
  return {
    orderId,
    sellerOrganizationId: "11111111-1111-1111-1111-111111111111",
    orderNumber: "CO-1001",
    status: "Accepted",
    fulfillmentStatus: "Ready",
    paymentStatus: "Unpaid",
    paymentMethod: "Cash",
    fulfillmentType: "Pickup",
    fulfillmentBranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    branchNameSnapshot: "Main",
    customerPartyType: "WalkIn",
    customerDisplayName: "Ana",
    merchandiseSubtotal: 100,
    deliveryFee: 0,
    total: 100,
    stockReservationState: "Reserved",
    lines: [
      {
        lineId: "99999999-9999-4999-8999-999999999999",
        productId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        lineNumber: 1,
        nameSnapshot: "Coke",
        unitSnapshot: "pc",
        quantity: 2,
        unitPrice: 50,
        discount: 0,
        lineTotal: 100,
      },
    ],
    createdAtUtc: "2026-08-21T01:00:00Z",
    acceptedAtUtc: "2026-08-21T01:05:00Z",
    acceptedBy,
    readyAtUtc: "2026-08-21T01:20:00Z",
    readyBy,
    updatedAtUtc: "2026-08-21T01:20:00Z",
  };
}

describe("SellerOrderDetailPage activity timeline", () => {
  beforeEach(() => {
    vi.mocked(ordersClient.getSellerCustomerOrder).mockReset();
    vi.mocked(ordersClient.getSellerCustomerOrder).mockResolvedValue(orderDetail() as never);
  });

  it("renders timeline events with actor names when timestamps exist", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/orders/${orderId}`]}>
          <Routes>
            <Route path="/orders/:orderId" element={<SellerOrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("seller-order-activity")).toBeInTheDocument();
    });

    expect(screen.getByTestId("seller-order-activity-received")).toHaveTextContent(
      "Order received",
    );
    expect(screen.getByTestId("seller-order-activity-accepted")).toHaveTextContent("Accepted");
    expect(screen.getByTestId("seller-order-activity-accepted")).toHaveTextContent("Seller Mia");
    expect(screen.getByTestId("seller-order-activity-ready")).toHaveTextContent("Ready");
    expect(screen.getByTestId("seller-order-activity-ready")).toHaveTextContent("Runner Leo");
    expect(screen.queryByTestId("seller-order-activity-delivered")).not.toBeInTheDocument();
    expect(screen.queryByText(acceptedBy)).not.toBeInTheDocument();
    expect(screen.queryByText(readyBy)).not.toBeInTheDocument();
  });
});

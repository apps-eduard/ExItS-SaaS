import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { StockRequestListPage } from "@/features/replenishment/StockRequestListPage";
import * as stockRequestsClient from "@/api/pos/pos-stock-requests-client";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const WH_A = "11111111-1111-1111-1111-111111111111";

const items = [
  {
    stockRequestId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    requestNumber: "SR-1",
    status: "Pending",
    destinationLocationId: TEST_BRANCH_A_ID,
    destinationLocationName: "Pac Passi",
    requestedSourceLocationId: WH_A,
    requestedSourceLocationName: "Panay Warehouse",
    lineCount: 2,
    updatedAtUtc: "2026-01-02T00:00:00Z",
  },
  {
    stockRequestId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    requestNumber: "SR-2",
    status: "Preparing",
    destinationLocationId: TEST_BRANCH_A_ID,
    destinationLocationName: "Pac Passi",
    requestedSourceLocationId: WH_A,
    requestedSourceLocationName: "Panay Warehouse",
    lineCount: 1,
    updatedAtUtc: "2026-01-03T00:00:00Z",
  },
  {
    stockRequestId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    requestNumber: "SR-3",
    status: "InTransit",
    destinationLocationId: TEST_BRANCH_A_ID,
    destinationLocationName: "Pac Passi",
    requestedSourceLocationId: WH_A,
    requestedSourceLocationName: "Panay Warehouse",
    lineCount: 1,
    updatedAtUtc: "2026-01-04T00:00:00Z",
  },
];

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: TEST_BRANCH_A_ID,
      branchName: "Pac Passi",
      branchType: "Retail",
      experience: "operations",
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
    },
    workspaces: [],
  }),
}));

describe("StockRequestListPage tab filtering", () => {
  beforeEach(() => {
    vi.spyOn(stockRequestsClient, "listOutgoingStockRequests").mockResolvedValue({
      items,
      totalCount: items.length,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(stockRequestsClient, "listIncomingStockRequests").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
  });

  it("defaults to submitted and filters by retail tabs", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter>
          <StockRequestListPage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() =>
      expect(screen.getByTestId(`stock-request-row-${items[0]!.stockRequestId}`)).toBeInTheDocument(),
    );
    expect(screen.queryByTestId(`stock-request-row-${items[1]!.stockRequestId}`)).not.toBeInTheDocument();
    expect(stockRequestsClient.listOutgoingStockRequests).toHaveBeenCalled();

    await user.click(screen.getByRole("tab", { name: /in progress/i }));
    expect(screen.getByTestId(`stock-request-row-${items[1]!.stockRequestId}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`stock-request-row-${items[0]!.stockRequestId}`)).not.toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: /in transit/i }));
    expect(screen.getByTestId(`stock-request-row-${items[2]!.stockRequestId}`)).toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: /^all$/i }));
    expect(screen.getByTestId(`stock-request-row-${items[0]!.stockRequestId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`stock-request-row-${items[1]!.stockRequestId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`stock-request-row-${items[2]!.stockRequestId}`)).toBeInTheDocument();
  });
});

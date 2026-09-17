import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { RetailWarehouseOverviewPage } from "@/features/warehouse/RetailWarehouseOverviewPage";
import * as stockRequestsClient from "@/api/pos/pos-stock-requests-client";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const WH_A = "11111111-1111-1111-1111-111111111111";
const REQ_ID = "55555555-5555-5555-5555-555555555555";

vi.mock("@/features/warehouse/useRetailWarehouseResolve", () => ({
  useRetailWarehouseResolve: () => ({
    workspace: { organizationId: TEST_ORG_A_ID, branchId: TEST_BRANCH_A_ID },
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      branchId: TEST_BRANCH_A_ID,
      branchName: "Pac Passi",
      branchType: "Retail",
    },
    orgBranches: [],
    routesQuery: { data: [], isPending: false, isError: false },
    resolveState: {
      kind: "ready",
      supplyWarehouseId: WH_A,
      supplyWarehouseName: "Panay Warehouse",
      isPreferred: true,
    },
    isLoading: false,
    isError: false,
  }),
}));

describe("RetailWarehouseOverviewPage metrics", () => {
  beforeEach(() => {
    vi.spyOn(stockRequestsClient, "getOutgoingStockRequestSummary").mockResolvedValue({
      submittedCount: 2,
      inProgressCount: 1,
      inTransitCount: 3,
      recent: [
        {
          stockRequestId: REQ_ID,
          requestNumber: "SR-1",
          status: "Pending",
          destinationLocationId: TEST_BRANCH_A_ID,
          requestedSourceLocationId: WH_A,
          requestedSourceLocationName: "Panay Warehouse",
          lineCount: 2,
          updatedAtUtc: "2026-09-01T00:00:00Z",
        },
      ],
    });
  });

  it("renders needs-attention counts and recent requests", async () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <RetailWarehouseOverviewPage />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("retail-warehouse-metric-submitted")).toHaveTextContent("2");
    expect(screen.getByTestId("retail-warehouse-metric-in-progress")).toHaveTextContent("1");
    expect(screen.getByTestId("retail-warehouse-metric-in-transit")).toHaveTextContent("3");
    expect(screen.getByTestId(`retail-warehouse-recent-${REQ_ID}`)).toBeInTheDocument();
    expect(screen.getByTestId("retail-warehouse-supply-card")).toHaveTextContent("Panay Warehouse");
  });
});

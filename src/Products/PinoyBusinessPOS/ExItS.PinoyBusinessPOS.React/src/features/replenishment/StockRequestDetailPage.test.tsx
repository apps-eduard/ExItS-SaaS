import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as stockRequestsClient from "@/api/pos/pos-stock-requests-client";
import { StockRequestDetailPage } from "@/features/replenishment/StockRequestDetailPage";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const WH_A = "11111111-1111-1111-1111-111111111111";
const REQUEST_ID = "22222222-2222-2222-2222-222222222222";
const PRODUCT_A = "33333333-3333-3333-3333-333333333333";
const LINE_ID = "44444444-4444-4444-4444-444444444444";
const ACTOR_ID = "55555555-5555-5555-5555-555555555555";

const workspaceState = {
  branchId: TEST_BRANCH_A_ID,
  branchName: "Pac Passi",
  branchType: "Retail",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: workspaceState.branchId,
      branchName: workspaceState.branchName,
      branchType: workspaceState.branchType,
      experience: "operations",
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      organizationManagementAuthority: true,
    },
    workspaces: [],
  }),
}));

function detailDto() {
  return {
    stockRequestId: REQUEST_ID,
    organizationId: TEST_ORG_A_ID,
    destinationLocationId: TEST_BRANCH_A_ID,
    destinationLocationName: "Pac Passi",
    requestedSourceLocationId: WH_A,
    requestedSourceLocationName: "Panay Warehouse",
    requestNumber: "SR-1001",
    status: "Pending",
    notes: null,
    requestedBy: ACTOR_ID,
    createdAtUtc: "2026-09-27T00:00:00Z",
    updatedAtUtc: "2026-09-27T00:00:00Z",
    lines: [
      {
        lineId: LINE_ID,
        productId: PRODUCT_A,
        lineNumber: 1,
        nameSnapshot: "Sardines",
        unitOfMeasure: "pcs",
        requestedQuantity: 10,
        approvedQuantity: null,
        fulfilledQuantity: 0,
        damagedQuantity: 0,
        sentQuantity: 0,
        inProgressQuantity: 0,
        remainingToDispatchQuantity: 10,
      },
    ],
    linkedTransfers: [],
  };
}

function renderDetail(path = `/warehouse/requests/${REQUEST_ID}`) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/warehouse/requests/:stockRequestId" element={<StockRequestDetailPage />} />
          <Route
            path="/inventory/stock-requests/:stockRequestId"
            element={<StockRequestDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("StockRequestDetailPage transfer-style header", () => {
  beforeEach(() => {
    workspaceState.branchId = TEST_BRANCH_A_ID;
    workspaceState.branchName = "Pac Passi";
    workspaceState.branchType = "Retail";
    vi.spyOn(stockRequestsClient, "getStockRequest").mockResolvedValue(detailDto() as never);
    vi.spyOn(stockRequestsClient, "getStockRequestActivity").mockResolvedValue([
      {
        eventId: "66666666-6666-6666-6666-666666666666",
        eventType: "Requested",
        occurredAtUtc: "2026-09-27T00:00:00Z",
        actorId: ACTOR_ID,
      },
    ] as never);
  });

  it("shows process header actions with timeline, preview, and export", async () => {
    const user = userEvent.setup();
    renderDetail();

    expect(await screen.findByTestId("stock-request-detail")).toBeInTheDocument();
    expect(screen.getByTestId("page-header")).toHaveTextContent(/Request summary/i);
    expect(screen.getByTestId("po-process-header-actions")).toBeInTheDocument();
    expect(screen.getByTestId("stock-request-route-summary")).toHaveTextContent(/Panay Warehouse/);
    expect(screen.getByTestId("stock-request-route-summary")).toHaveTextContent(/Pac Passi/);
    expect(screen.getByTestId("stock-request-number-summary")).toHaveTextContent(/SR-1001/);
    expect(screen.getByTestId("stock-request-timeline-open")).toBeEnabled();
    expect(screen.getByTestId("stock-request-document-preview-open")).toBeInTheDocument();
    expect(screen.queryByTestId("stock-request-activity")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("stock-request-timeline-open"));
    await waitFor(() => {
      expect(screen.getByTestId("stock-request-timeline-drawer")).toBeInTheDocument();
    });
    expect(screen.getByTestId("stock-request-activity-timeline")).toBeInTheDocument();
  });

  it("opens decline confirmation with reason before submitting decline", async () => {
    const user = userEvent.setup();
    workspaceState.branchId = WH_A;
    workspaceState.branchName = "Panay Warehouse";
    workspaceState.branchType = "Warehouse";
    vi.spyOn(stockRequestsClient, "rejectStockRequest").mockResolvedValue({} as never);
    renderDetail();

    expect(await screen.findByTestId("stock-request-decline")).toBeInTheDocument();
    expect(screen.queryByTestId("stock-request-decline-reason")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("stock-request-decline"));
    expect(await screen.findByTestId("stock-request-decline-dialog")).toBeInTheDocument();
    expect(screen.getByTestId("stock-request-decline-confirm")).toBeDisabled();

    await user.type(screen.getByTestId("stock-request-decline-reason"), "Out of stock");
    expect(screen.getByTestId("stock-request-decline-confirm")).toBeEnabled();
    await user.click(screen.getByTestId("stock-request-decline-confirm"));

    await waitFor(() => {
      expect(stockRequestsClient.rejectStockRequest).toHaveBeenCalledWith(
        expect.anything(),
        REQUEST_ID,
        "Out of stock",
      );
    });
  });

  it("uses warehouse list back target on warehouse request path", async () => {
    renderDetail();
    expect(await screen.findByTestId("page-header-back-warehouse-requests")).toBeInTheDocument();
  });

  it("opens Ready to receive into transfer receive mode", async () => {
    const transferId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    workspaceState.branchId = TEST_BRANCH_A_ID;
    workspaceState.branchType = "Retail";
    const dto = detailDto();
    dto.status = "InTransit";
    dto.lines[0]!.sentQuantity = 10;
    dto.lines[0]!.inProgressQuantity = 10;
    dto.lines[0]!.remainingToDispatchQuantity = 0;
    dto.linkedTransfers = [
      {
        transferId,
        transferNumber: "TR-9",
        status: "InTransit",
        totalSentQty: 10,
        totalReceivedQty: 0,
        totalOutstandingQty: 10,
        totalClosedQty: 0,
        createdAtUtc: "2026-09-27T00:00:00Z",
        createdBy: ACTOR_ID,
        updatedAtUtc: "2026-09-27T01:00:00Z",
      },
    ] as never;
    vi.spyOn(stockRequestsClient, "getStockRequest").mockResolvedValue(dto as never);
    renderDetail(`/inventory/stock-requests/${REQUEST_ID}`);

    const receive = await screen.findByTestId("stock-request-receive");
    expect(receive).toHaveAttribute(
      "href",
      `/inventory/transfers/${transferId}?mode=receive`,
    );
  });

  it("lists multiple receivable transfers instead of picking one", async () => {
    workspaceState.branchId = TEST_BRANCH_A_ID;
    workspaceState.branchType = "Retail";
    const dto = detailDto();
    dto.status = "PartiallyFulfilled";
    dto.linkedTransfers = [
      {
        transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        transferNumber: "TR-1",
        status: "InTransit",
        totalSentQty: 5,
        totalReceivedQty: 0,
        totalOutstandingQty: 5,
        totalClosedQty: 0,
        createdAtUtc: "2026-09-27T00:00:00Z",
        createdBy: ACTOR_ID,
        updatedAtUtc: "2026-09-27T01:00:00Z",
      },
      {
        transferId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        transferNumber: "TR-R1",
        status: "PartiallyReceived",
        totalSentQty: 5,
        totalReceivedQty: 2,
        totalOutstandingQty: 3,
        totalClosedQty: 0,
        createdAtUtc: "2026-09-27T02:00:00Z",
        createdBy: ACTOR_ID,
        updatedAtUtc: "2026-09-27T03:00:00Z",
      },
    ] as never;
    vi.spyOn(stockRequestsClient, "getStockRequest").mockResolvedValue(dto as never);
    renderDetail(`/inventory/stock-requests/${REQUEST_ID}`);

    expect(await screen.findByTestId("stock-request-receive-choices")).toBeInTheDocument();
    expect(screen.queryByTestId("stock-request-receive")).not.toBeInTheDocument();
    expect(
      screen.getByTestId("stock-request-receive-aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
    ).toHaveAttribute(
      "href",
      "/inventory/transfers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa?mode=receive",
    );
  });

  it("Approve & prepare creates or reuses transfer Draft and navigates to it", async () => {
    const user = userEvent.setup();
    const draftId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    workspaceState.branchId = WH_A;
    workspaceState.branchName = "Panay Warehouse";
    workspaceState.branchType = "Warehouse";
    vi.spyOn(stockRequestsClient, "approveStockRequest").mockResolvedValue({} as never);
    vi.spyOn(stockRequestsClient, "prepareStockRequestTransfer").mockResolvedValue({
      transferId: draftId,
      status: "Draft",
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/stock-requests/${REQUEST_ID}`]}>
          <Routes>
            <Route
              path="/inventory/stock-requests/:stockRequestId"
              element={<StockRequestDetailPage />}
            />
            <Route
              path="/inventory/transfers/:transferId"
              element={<div data-testid="transfer-draft-dest" />}
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await user.click(await screen.findByTestId("stock-request-approve-prepare"));
    await waitFor(() => {
      expect(stockRequestsClient.approveStockRequest).toHaveBeenCalled();
      expect(stockRequestsClient.prepareStockRequestTransfer).toHaveBeenCalled();
    });
    expect(await screen.findByTestId("transfer-draft-dest")).toBeInTheDocument();
  });
});

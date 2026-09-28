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
  useOptionalWorkspace: () => ({
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

  it("shows footer back, edit, and cancel for pending destination request", async () => {
    renderDetail();

    expect(await screen.findByTestId("stock-request-detail-actions")).toBeInTheDocument();
    expect(screen.getByTestId("stock-request-detail-back")).toBeInTheDocument();
    expect(screen.getByTestId("stock-request-edit")).toHaveTextContent(/Edit request/i);
    expect(screen.getByTestId("stock-request-cancel")).toHaveTextContent(/Cancel request/i);
  });

  it("shows approved qty as zero before warehouse approval", async () => {
    renderDetail();

    expect(await screen.findByTestId("stock-request-detail")).toBeInTheDocument();
    const approvedCells = screen.getAllByTestId(`stock-request-approved-${PRODUCT_A}`);
    expect(approvedCells.length).toBeGreaterThan(0);
    for (const cell of approvedCells) {
      expect(cell).toHaveTextContent(/^0$/);
    }
  });

  it("hides edit when request is no longer pending", async () => {
    const dto = detailDto();
    dto.status = "Approved";
    vi.spyOn(stockRequestsClient, "getStockRequest").mockResolvedValue(dto as never);
    renderDetail();

    expect(await screen.findByTestId("stock-request-detail-actions")).toBeInTheDocument();
    expect(screen.getByTestId("stock-request-detail-back")).toBeInTheDocument();
    expect(screen.queryByTestId("stock-request-edit")).not.toBeInTheDocument();
    expect(screen.getByTestId("stock-request-cancel")).toBeInTheDocument();
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

    expect(await screen.findByTestId("stock-request-linked-in-summary")).toBeInTheDocument();
    expect(screen.getByTestId(`stock-request-linked-transfer-${transferId}`)).toHaveTextContent(
      /TR-9/,
    );
    expect(screen.getByTestId(`stock-request-linked-transfer-${transferId}`)).toHaveTextContent(
      /Sent:\s*10/,
    );
    expect(screen.getByTestId(`stock-request-linked-transfer-${transferId}`)).toHaveTextContent(
      /Outstanding:\s*10/,
    );
    expect(screen.getByTestId(`stock-request-view-transfer-${transferId}`)).toBeInTheDocument();

    const actions = screen.getByTestId("stock-request-detail-actions");
    expect(actions).toContainElement(screen.getByTestId("stock-request-detail-back"));
    const receive = screen.getByTestId("stock-request-receive");
    expect(actions).toContainElement(receive);
    expect(receive).toHaveAttribute(
      "href",
      `/inventory/transfers/${transferId}?mode=receive`,
    );
  });

  it("opens linked transfer drawer from View transfer", async () => {
    const user = userEvent.setup();
    const transferId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    workspaceState.branchId = TEST_BRANCH_A_ID;
    workspaceState.branchType = "Retail";
    const dto = detailDto();
    dto.status = "InTransit";
    dto.linkedTransfers = [
      {
        transferId,
        transferNumber: "TR-260928-001",
        status: "InTransit",
        totalSentQty: 20,
        totalReceivedQty: 0,
        totalOutstandingQty: 20,
        totalClosedQty: 0,
        createdAtUtc: "2026-09-27T00:00:00Z",
        createdBy: ACTOR_ID,
        updatedAtUtc: "2026-09-27T01:00:00Z",
      },
    ] as never;
    vi.spyOn(stockRequestsClient, "getStockRequest").mockResolvedValue(dto as never);
    const transferClient = await import("@/api/pos/pos-inventory-transfer-client");
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: TEST_ORG_A_ID,
      transferNumber: "TR-260928-001",
      sourceBranchId: WH_A,
      sourceBranchName: "Panay Warehouse",
      destinationBranchId: TEST_BRANCH_A_ID,
      destinationBranchName: "Pac Passi",
      status: "InTransit",
      notes: null,
      createdBy: ACTOR_ID,
      createdAtUtc: "2026-09-27T00:00:00Z",
      updatedAtUtc: "2026-09-27T01:00:00Z",
      totalSentQty: 20,
      totalReceivedQty: 0,
      totalClosedQty: 0,
      totalOutstandingQty: 20,
      totalDifferenceQty: 0,
      receiptCount: 0,
      lines: [],
    } as never);

    renderDetail(`/inventory/stock-requests/${REQUEST_ID}`);

    await user.click(await screen.findByTestId(`stock-request-view-transfer-${transferId}`));
    expect(await screen.findByTestId("inventory-movement-transaction-drawer")).toBeInTheDocument();
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

  it("approve succeeds then prepare fails → Approved with Prepare transfer retry only", async () => {
    const user = userEvent.setup();
    const draftId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    workspaceState.branchId = WH_A;
    workspaceState.branchName = "Panay Warehouse";
    workspaceState.branchType = "Warehouse";

    const pending = detailDto();
    pending.requestedSourceLocationId = WH_A;
    pending.destinationLocationId = TEST_BRANCH_A_ID;
    const approved = {
      ...pending,
      status: "Approved",
      lines: pending.lines.map((line) => ({
        ...line,
        approvedQuantity: 10,
        remainingToDispatchQuantity: 10,
      })),
    };

    const getSpy = vi
      .spyOn(stockRequestsClient, "getStockRequest")
      .mockResolvedValueOnce(pending as never)
      .mockResolvedValue(approved as never);
    vi.spyOn(stockRequestsClient, "approveStockRequest").mockResolvedValue(approved as never);
    const prepareSpy = vi
      .spyOn(stockRequestsClient, "prepareStockRequestTransfer")
      .mockRejectedValueOnce(new Error("prepare failed"))
      .mockResolvedValueOnce({ transferId: draftId, status: "Draft" } as never);

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
      expect(stockRequestsClient.approveStockRequest).toHaveBeenCalledTimes(1);
      expect(prepareSpy).toHaveBeenCalledTimes(1);
    });
    expect(
      await screen.findByText(
        /Request was approved, but the transfer could not be prepared\. You can prepare it again\./i,
      ),
    ).toBeInTheDocument();
    expect(await screen.findByTestId("stock-request-detail")).toHaveAttribute(
      "data-status",
      "Approved",
    );
    expect(screen.getByTestId("stock-request-fulfill-remaining")).toBeInTheDocument();
    expect(screen.queryByTestId("stock-request-approve-prepare")).not.toBeInTheDocument();
    expect(getSpy.mock.calls.length).toBeGreaterThan(1);

    await user.click(screen.getByTestId("stock-request-fulfill-remaining"));
    await waitFor(() => {
      expect(prepareSpy).toHaveBeenCalledTimes(2);
      expect(stockRequestsClient.approveStockRequest).toHaveBeenCalledTimes(1);
    });
    expect(await screen.findByTestId("transfer-draft-dest")).toBeInTheDocument();
  });

  it("scopes detail and activity query keys by branch so workspace switch does not reuse cache", async () => {
    workspaceState.branchId = WH_A;
    workspaceState.branchType = "Warehouse";
    const { unmount } = renderDetail(`/inventory/stock-requests/${REQUEST_ID}`);
    expect(await screen.findByTestId("stock-request-detail")).toBeInTheDocument();
    await waitFor(() => {
      expect(stockRequestsClient.getStockRequest).toHaveBeenCalledWith(
        expect.objectContaining({ branchId: WH_A }),
        REQUEST_ID,
        expect.anything(),
      );
      expect(stockRequestsClient.getStockRequestActivity).toHaveBeenCalledWith(
        expect.objectContaining({ branchId: WH_A }),
        REQUEST_ID,
        expect.anything(),
      );
    });
    unmount();

    workspaceState.branchId = TEST_BRANCH_A_ID;
    workspaceState.branchType = "Retail";
    renderDetail(`/inventory/stock-requests/${REQUEST_ID}`);
    expect(await screen.findByTestId("stock-request-detail")).toBeInTheDocument();

    await waitFor(() => {
      expect(stockRequestsClient.getStockRequest).toHaveBeenCalledWith(
        expect.objectContaining({ branchId: TEST_BRANCH_A_ID }),
        REQUEST_ID,
        expect.anything(),
      );
      expect(stockRequestsClient.getStockRequestActivity).toHaveBeenCalledWith(
        expect.objectContaining({ branchId: TEST_BRANCH_A_ID }),
        REQUEST_ID,
        expect.anything(),
      );
    });
    expect(stockRequestsClient.getStockRequest).toHaveBeenCalledTimes(2);
    expect(stockRequestsClient.getStockRequestActivity).toHaveBeenCalledTimes(2);
  });
});

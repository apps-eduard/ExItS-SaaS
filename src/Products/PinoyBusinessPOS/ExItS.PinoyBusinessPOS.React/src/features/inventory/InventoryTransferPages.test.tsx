import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as transferClient from "@/api/pos/pos-inventory-transfer-client";
import { InventoryTransferDetailPage } from "@/features/inventory/InventoryTransferDetailPage";
import { InventoryTransferListPage } from "@/features/inventory/InventoryTransferListPage";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const mainId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const branchBId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const transferId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const cokeId = "11111111-1111-1111-1111-111111111111";
const lineId = "22222222-2222-2222-2222-222222222222";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Store",
    branchId: mainId,
    branchName: "Main Store",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
  },
  workspaces: [
    {
      organizationId: orgId,
      displayName: "Store",
      branches: [
        {
          branchId: mainId,
          name: "Main Store",
          secondaryLine: "",
          isPrimary: true,
          isActive: true,
        },
        {
          branchId: branchBId,
          name: "Branch B",
          secondaryLine: "",
          isPrimary: false,
          isActive: true,
        },
      ],
    },
  ],
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

vi.mock("@/offline/organization-offline-context", () => ({
  useOrganizationOfflineContext: () => null,
}));

function draftTransfer() {
  return {
    transferId,
    organizationId: orgId,
    transferNumber: null,
    sourceBranchId: mainId,
    sourceBranchName: "Main Store",
    destinationBranchId: branchBId,
    destinationBranchName: "Branch B",
    status: "Draft",
    notes: null,
    createdBy: "99999999-9999-9999-9999-999999999999",
    createdAtUtc: "2026-08-29T08:00:00Z",
    updatedAtUtc: "2026-08-29T08:00:00Z",
    dispatchedAtUtc: null,
    dispatchedBy: null,
    receivedAtUtc: null,
    receivedBy: null,
    cancelledAtUtc: null,
    cancelledBy: null,
    totalSentQty: 24,
    totalReceivedQty: 0,
    totalDifferenceQty: 24,
    lines: [
      {
        lineId,
        productId: cokeId,
        productName: "Coke 330ml",
        unitOfMeasure: "pcs",
        lineNumber: 1,
        sentQty: 24,
        receivedQty: 0,
        differenceQty: 24,
        lineStatus: "Missing",
        discrepancyReason: null,
        discrepancyNote: null,
        sourceLotId: null,
        lotNumber: null,
        expirationDate: null,
      },
    ],
  };
}

function inTransitTransfer() {
  return {
    ...draftTransfer(),
    transferNumber: "TR-20260829-0001",
    status: "InTransit",
    dispatchedAtUtc: "2026-08-29T09:00:00Z",
    dispatchedBy: "99999999-9999-9999-9999-999999999999",
  };
}

describe("Inventory Transfer React flow", () => {
  beforeEach(() => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    vi.spyOn(transferClient, "listInventoryTransfers").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows empty state and multi-branch new CTA", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/transfers"]}>
          <Routes>
            <Route path="/inventory/transfers" element={<InventoryTransferListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("inventory-transfer-list-page")).toBeInTheDocument();
    expect(await screen.findByText("No inventory transfers yet")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-empty-cta")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-current-branch")).toHaveTextContent("Main Store");
  });

  it("filters outgoing transfers relative to acting branch", async () => {
    vi.spyOn(transferClient, "listInventoryTransfers").mockResolvedValue({
      items: [
        {
          transferId,
          transferNumber: "TR-1",
          sourceBranchId: mainId,
          sourceBranchName: "Main Store",
          destinationBranchId: branchBId,
          destinationBranchName: "Branch B",
          status: "InTransit",
          lineCount: 1,
          totalSentQty: 24,
          totalReceivedQty: 0,
          totalDifferenceQty: 24,
          updatedAtUtc: "2026-08-29T09:00:00Z",
          createdBy: "99999999-9999-9999-9999-999999999999",
          dispatchedBy: "99999999-9999-9999-9999-999999999999",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/transfers"]}>
          <Routes>
            <Route path="/inventory/transfers" element={<InventoryTransferListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId(`transfer-row-${transferId}`)).toHaveTextContent(
      "Main Store → Branch B",
    );
    await userEvent.click(screen.getByTestId("transfer-direction-outgoing"));
    await waitFor(() => {
      expect(transferClient.listInventoryTransfers).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ direction: "outgoing" }),
        expect.anything(),
      );
    });
  });

  it("source draft can dispatch", async () => {
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(draftTransfer() as never);
    const dispatchSpy = vi
      .spyOn(transferClient, "dispatchInventoryTransfer")
      .mockResolvedValue(inTransitTransfer() as never);
    const { QueryClient } = await import("@tanstack/react-query");
    const invalidateSpy = vi.spyOn(QueryClient.prototype, "invalidateQueries");

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("inventory-transfer-detail-page")).toHaveAttribute(
      "data-status",
      "Draft",
    );
    expect(screen.getByTestId("transfer-dispatch")).toBeInTheDocument();
    expect(screen.queryByTestId("transfer-receive")).not.toBeInTheDocument();

    await userEvent.click(screen.getByTestId("transfer-dispatch"));
    expect(dispatchSpy).not.toHaveBeenCalled();
    expect(await screen.findByTestId("transfer-dispatch-confirm")).toBeInTheDocument();

    await userEvent.click(screen.getByTestId("transfer-dispatch-confirm-cancel"));
    expect(dispatchSpy).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.queryByTestId("transfer-dispatch-confirm")).not.toBeInTheDocument();
    });

    await userEvent.click(screen.getByTestId("transfer-dispatch"));
    await userEvent.click(await screen.findByTestId("transfer-dispatch-confirm-confirm"));
    await waitFor(() => expect(dispatchSpy).toHaveBeenCalledTimes(1));
    await waitFor(() => {
      expect(screen.getByTestId("inventory-transfer-detail-page")).toHaveAttribute(
        "data-status",
        "InTransit",
      );
    });
    expect(await screen.findByText("TR-20260829-0001")).toBeInTheDocument();
    expect(screen.queryByTestId("transfer-dispatch")).not.toBeInTheDocument();
    expect(invalidateSpy).toHaveBeenCalledWith(
      expect.objectContaining({ queryKey: ["inventory-transfers"] }),
    );
    expect(invalidateSpy).toHaveBeenCalledWith(
      expect.objectContaining({ queryKey: ["inventory"] }),
    );
  });

  it("failed dispatch remains Draft and surfaces exact server detail", async () => {
    const { PosApiError } = await import("@/api/pos/pos-http");
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(draftTransfer() as never);
    let rejectDispatch!: (reason?: unknown) => void;
    const pending = new Promise<never>((_resolve, reject) => {
      rejectDispatch = reject;
    });
    const dispatchSpy = vi
      .spyOn(transferClient, "dispatchInventoryTransfer")
      .mockImplementation(() => pending);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("inventory-transfer-detail-page")).toHaveAttribute(
      "data-status",
      "Draft",
    );

    await userEvent.click(screen.getByTestId("transfer-dispatch"));
    const confirm = await screen.findByTestId("transfer-dispatch-confirm-confirm");
    await userEvent.click(confirm);
    await userEvent.click(confirm);
    await waitFor(() => expect(dispatchSpy).toHaveBeenCalledTimes(1));
    expect(confirm).toBeDisabled();
    expect(confirm).toHaveTextContent("Dispatching");

    rejectDispatch(
      new PosApiError(409, {
        errorCode: "pos.insufficient_stock",
        detail: "Insufficient available stock to dispatch 'Bath Soap Bar'.",
      }),
    );

    await waitFor(() => {
      expect(screen.queryByTestId("transfer-dispatch-confirm")).not.toBeInTheDocument();
    });
    expect(screen.getByTestId("inventory-transfer-detail-page")).toHaveAttribute(
      "data-status",
      "Draft",
    );
    const alert = await screen.findByTestId("transfer-local-error");
    expect(alert).toHaveTextContent("Cannot dispatch transfer");
    expect(alert).toHaveTextContent("Insufficient available stock to dispatch 'Bath Soap Bar'.");
    const toast = await screen.findByTestId("exits-toast");
    expect(toast).toHaveAttribute("data-tone", "error");
    expect(toast).toHaveTextContent("Insufficient available stock to dispatch 'Bath Soap Bar'.");
    expect(dispatchSpy).toHaveBeenCalledTimes(1);
  });


  it("source in-transit can cancel but not receive", async () => {
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("inventory-transfer-detail-page")).toHaveAttribute(
      "data-status",
      "InTransit",
    );
    expect(screen.queryByTestId("transfer-receive")).not.toBeInTheDocument();
    expect(screen.getByTestId("transfer-cancel")).toBeInTheDocument();
  });

  it("destination in-transit can receive but not cancel", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("inventory-transfer-detail-page")).toHaveAttribute(
      "data-status",
      "InTransit",
    );
    expect(screen.getByTestId("transfer-receive")).toBeInTheDocument();
    expect(screen.queryByTestId("transfer-cancel")).not.toBeInTheDocument();
  });

  it("receive defaults sent qty and submits discrepancy-capable payload", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    const receiveSpy = vi.spyOn(transferClient, "receiveInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "PartiallyReceived",
      totalReceivedQty: 22,
      totalDifferenceQty: 2,
      lines: [
        {
          ...inTransitTransfer().lines[0]!,
          receivedQty: 22,
          differenceQty: 2,
          lineStatus: "Short",
          discrepancyReason: "ShortShipment",
        },
      ],
    } as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    await userEvent.click(await screen.findByTestId("transfer-receive"));
    expect(await screen.findByTestId("inventory-transfer-receive-page")).toBeInTheDocument();
    const qty = screen.getByTestId(`transfer-receive-qty-${lineId}`);
    expect(qty).toHaveValue("24");
    await userEvent.clear(qty);
    await userEvent.type(qty, "22");
    expect(screen.getByTestId("transfer-receive-submit")).toBeDisabled();
    await userEvent.selectOptions(screen.getByTestId(`transfer-discrepancy-${lineId}`), "ShortShipment");
    expect(screen.getByTestId("transfer-receive-submit")).toBeEnabled();
    await userEvent.click(screen.getByTestId("transfer-receive-submit"));
    await userEvent.click(await screen.findByTestId("transfer-receive-confirm-confirm"));
    await waitFor(() => expect(receiveSpy).toHaveBeenCalled());
    expect(receiveSpy.mock.calls[0]?.[2]).toEqual({
      lines: [
        expect.objectContaining({
          lineId,
          productId: cokeId,
          receivedQty: 22,
          discrepancyReason: "ShortShipment",
        }),
      ],
    });
  });

  it("rejects received quantity above sent and disables receive", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    await userEvent.click(await screen.findByTestId("transfer-receive"));
    const qty = await screen.findByTestId(`transfer-receive-qty-${lineId}`);
    await userEvent.clear(qty);
    await userEvent.type(qty, "25");
    expect(await screen.findByTestId(`transfer-receive-qty-error-${lineId}`)).toHaveTextContent(
      "Received quantity cannot exceed sent quantity (24)",
    );
    expect(screen.getByTestId("transfer-receive-submit")).toBeDisabled();
  });
});

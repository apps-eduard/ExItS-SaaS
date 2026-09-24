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
    transferNumber: "260829-001",
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
    expect(screen.getByTestId("transfer-list-desktop")).toHaveTextContent("Lines");
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
      expect(screen.getByTestId("transfer-number-summary")).toHaveTextContent("260829-001");
    });
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

  async function classifyTransferLine(
    user: ReturnType<typeof userEvent.setup>,
    goodQty: string,
    note: string,
    options?: { damaged?: string; missing?: string },
  ) {
    await user.click(screen.getByTestId(`transfer-receive-edit-menu-${lineId}`));
    const qty = await screen.findByTestId(`transfer-receive-qty-${lineId}`);
    await user.clear(qty);
    await user.type(qty, goodQty);
    await user.click(screen.getByTestId(`transfer-receive-edit-save-${lineId}`));
    await waitFor(() => {
      expect(screen.getByTestId("receive-discrepancy-dialog")).toBeInTheDocument();
    });
    if (options?.damaged != null || options?.missing != null) {
      if (options.damaged != null) {
        const damaged = screen.getByTestId(`receive-discrepancy-damaged-${cokeId}`);
        await user.clear(damaged);
        await user.type(damaged, options.damaged);
      }
      if (options.missing != null) {
        const missing = screen.getByTestId(`receive-discrepancy-not-delivered-${cokeId}`);
        await user.clear(missing);
        await user.type(missing, options.missing);
      }
    } else {
      await user.click(screen.getByTestId(`receive-discrepancy-all-not-delivered-${cokeId}`));
    }
    await user.type(screen.getByTestId(`receive-discrepancy-remarks-${cokeId}`), note);
    await user.click(screen.getByTestId("receive-discrepancy-confirm"));
    await waitFor(() => {
      expect(screen.queryByTestId("receive-discrepancy-dialog")).not.toBeInTheDocument();
    });
  }

  it("receive defaults outstanding qty and submits full-good payload", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    const receiveSpy = vi.spyOn(transferClient, "receiveInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "Received",
      totalReceivedQty: 24,
    } as never);
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    await user.click(await screen.findByTestId("transfer-receive"));
    expect(await screen.findByTestId("inventory-transfer-receive-page")).toBeInTheDocument();
    // Destination receive keeps canonical process header (status / timeline / preview / export).
    expect(screen.getByTestId("po-process-header-actions")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-timeline-open")).toBeEnabled();
    expect(screen.getByTestId("transfer-document-preview-open")).toBeEnabled();
    expect(screen.getByTestId(`transfer-receive-row-${lineId}`)).toHaveTextContent("24");
    await user.click(screen.getByTestId("transfer-receive-review"));
    await waitFor(() =>
      expect(screen.getByTestId("transfer-receive-review-summary")).toBeInTheDocument(),
    );
    await user.click(screen.getByTestId("transfer-receive-confirm"));
    await waitFor(() => expect(receiveSpy).toHaveBeenCalled());
    expect(receiveSpy.mock.calls[0]?.[2]).toEqual({
      lines: [
        expect.objectContaining({
          lineId,
          productId: cokeId,
          goodQty: 24,
          receivedQty: 24,
        }),
      ],
    });
  });

  it("classifies discrepancy, decides missing disposition, and submits mixed payload", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    const receiveSpy = vi.spyOn(transferClient, "receiveInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "PartiallyReceived",
      totalReceivedQty: 20,
    } as never);
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    await user.click(await screen.findByTestId("transfer-receive"));
    await classifyTransferLine(user, "20", "Mixed receipt", { damaged: "1", missing: "3" });
    await user.click(screen.getByTestId("transfer-receive-review"));
    await waitFor(() =>
      expect(screen.getByTestId("transfer-receive-follow-up")).toBeInTheDocument(),
    );
    await user.click(screen.getByTestId("transfer-receive-confirm"));
    await waitFor(() => expect(receiveSpy).toHaveBeenCalled());
    expect(receiveSpy.mock.calls[0]?.[2]).toEqual({
      lines: [
        expect.objectContaining({
          lineId,
          productId: cokeId,
          goodQty: 20,
          damagedQty: 1,
          damagedFollowUp: "RequestReplacement",
          damagedCustodyDecision: "KeepAtDestination",
          missingQty: 3,
          missingDisposition: "ExpectedLater",
          discrepancyNote: "Mixed receipt",
        }),
      ],
    });
  });

  it("hides fulfillment panel on a lone draft with only remaining-to-dispatch", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...draftTransfer(),
      remainingToDispatchQty: 10,
      satisfiedAtDestinationQty: 0,
      openInTransitQty: 0,
      waivedQty: 0,
      familyMembers: [
        {
          transferId,
          transferNumber: null,
          status: "Draft",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 0,
          totalOutstandingQty: 10,
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
    expect(await screen.findByTestId("inventory-transfer-detail-page")).toBeInTheDocument();
    expect(screen.queryByTestId("transfer-family-coverage")).not.toBeInTheDocument();
    expect(screen.queryByText(/Original 94209ce4/i)).not.toBeInTheDocument();
  });

  it("shows replacement family coverage panel", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    const familyTransfer = {
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalReceivedQty: 10,
      totalOutstandingQty: 5,
      satisfiedAtDestinationQty: 12,
      openInTransitQty: 5,
      remainingToDispatchQty: 3,
      waivedQty: 0,
      stockRequestId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 10,
              quantityDamaged: 5,
              quantityMissing: 0,
              quantityOther: 0,
              damagedFollowUp: "RequestReplacement",
            },
          ],
        },
      ],
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 24,
          totalReceivedQty: 10,
          totalOutstandingQty: 5,
          totalDamagedQty: 5,
        },
        {
          transferId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          transferNumber: "260829-001-R1",
          status: "Draft",
          replacementSequence: 1,
          isRoot: false,
          totalSentQty: 3,
          totalReceivedQty: 0,
          totalOutstandingQty: 3,
        },
      ],
      damageCustodies: [
        {
          custodyId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          productId: cokeId,
          quantity: 5,
          decision: "KeepAtDestination",
          followUpIntent: "RequestReplacement",
          status: "HeldAtDestination",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedDamagedQty: 0,
          waivedQty: 0,
          destinationRecoveredSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
        },
      ],
    };
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(familyTransfer as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("transfer-family-coverage")).toBeInTheDocument();
    expect(screen.getByText("Fulfillment")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-this-shipment")).toBeInTheDocument();
    expect(screen.getByTestId("this-shipment-sent")).toHaveTextContent("24");
    expect(screen.getByTestId("this-shipment-good")).toHaveTextContent("10");
    expect(screen.getByTestId("this-shipment-damaged")).toHaveTextContent("5");
    expect(screen.getByTestId("transfer-family-members")).toBeInTheDocument();
    expect(screen.getByText("Replacement R1")).toBeInTheDocument();
    expect(screen.getByText("260829-001-R1")).toBeInTheDocument();
    expect(
      screen.getByTestId("transfer-family-member-link-eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
    ).toHaveTextContent("Replacement R1");
    expect(screen.getByTestId("transfer-family-member-eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")).toBeInTheDocument();
    expect(
      screen.getByTestId("transfer-family-member-open-eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("transfer-damage-custodies")).toBeInTheDocument();
    expect(screen.queryByTestId("transfer-receiving-decision")).not.toBeInTheDocument();
    // Keep-at-destination damage is already classified; destination must not re-inspect.
    expect(
      screen.queryByTestId("transfer-custody-inspect-ffffffff-ffff-ffff-ffff-ffffffffffff"),
    ).not.toBeInTheDocument();
    expect(screen.queryByText("KeepAtDestination")).not.toBeInTheDocument();
    expect(screen.queryByText("HeldAtDestination")).not.toBeInTheDocument();
    expect(screen.queryByText("RequestReplacement")).not.toBeInTheDocument();
    expect(screen.queryByText("ClosedWithDiscrepancy")).not.toBeInTheDocument();
    expect(screen.getAllByText(/Good received/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Needs fulfillment/i).length).toBeGreaterThan(0);
    // Destination branch must not see source-only fulfill CTA.
    expect(screen.queryByTestId("transfer-fulfill-remaining")).not.toBeInTheDocument();
    expect(screen.getByTestId("transfer-view-stock-request")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-lines-desktop")).toBeInTheDocument();
    expect(screen.getAllByText("Good").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Damaged").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Missing").length).toBeGreaterThan(0);
    // Header still exposes timeline / status chip tooling.
    expect(screen.getByTestId("transfer-timeline-open")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-route-summary")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-number-summary")).toHaveTextContent("260829-001");
  });

  it("keeps original this-shipment totals after replacement satisfies family", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const originalAfterR1 = {
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalOutstandingQty: 0,
      satisfiedAtDestinationQty: 10,
      openInTransitQty: 0,
      remainingToDispatchQty: 0,
      waivedQty: 0,
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityDamaged: 5,
              quantityMissing: 0,
              quantityOther: 0,
              damagedFollowUp: "RequestReplacement",
            },
          ],
        },
      ],
      familyMembers: [
        {
          transferId,
          transferNumber: "TR-260922-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 5,
        },
        {
          transferId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          transferNumber: "TR-260922-001-R1",
          status: "Received",
          replacementSequence: 1,
          isRoot: false,
          totalSentQty: 5,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
        },
      ],
      damageCustodies: [
        {
          custodyId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          productId: cokeId,
          quantity: 5,
          decision: "KeepAtDestination",
          followUpIntent: "RequestReplacement",
          status: "HeldAtDestination",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedDamagedQty: 0,
          waivedQty: 0,
          destinationRecoveredSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
        },
      ],
    };
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(originalAfterR1 as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("this-shipment-sent")).toHaveTextContent("10");
    expect(screen.getByTestId("this-shipment-good")).toHaveTextContent("5");
    expect(screen.getByTestId("this-shipment-damaged")).toHaveTextContent("5");
    expect(screen.getByTestId("transfer-fulfillment-target")).toHaveTextContent("10");
    expect(screen.getByTestId("transfer-fulfillment-good")).toHaveTextContent("10");
    expect(screen.getByTestId("transfer-fulfillment-needs-replacement")).toHaveTextContent("0");
    expect(screen.queryByTestId("transfer-fulfill-remaining")).not.toBeInTheDocument();
  });

  it("does not render Receiving decision card for return-to-source damage", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityDamaged: 5,
              damagedFollowUp: "AcceptShortage",
            },
          ],
        },
      ],
      damageCustodies: [
        {
          custodyId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          productId: cokeId,
          quantity: 5,
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "AwaitingReturn",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedDamagedQty: 0,
          waivedQty: 0,
          destinationRecoveredSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
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
    expect(await screen.findByTestId("this-shipment-damaged")).toHaveTextContent("5");
    expect(screen.queryByTestId("transfer-receiving-decision")).not.toBeInTheDocument();
    expect(screen.queryByText("AcceptShortage")).not.toBeInTheDocument();
    expect(screen.queryByText("ReturnToSource")).not.toBeInTheDocument();
    expect(screen.queryByText("AwaitingReturn")).not.toBeInTheDocument();
  });

  it("shows Send back to source for exception custody at destination", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    const custodyId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "Other",
              otherReasonNote: "Wrong box",
              otherFollowUp: "AcceptShortage",
              otherCustodyDecision: "ReturnToSource",
              actualReceivedProductId: actualId,
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          expectedProductId: cokeId,
          actualProductId: actualId,
          expectedProductName: "Coke 330ml",
          actualProductName: "Pepsi 330ml",
          quantity: 5,
          reasonCode: "Other",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "AwaitingReturn",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
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

    expect(await screen.findByTestId(`transfer-family-member-dispatch-return-${custodyId}`)).toBeEnabled();
    expect(screen.queryByTestId("transfer-receiving-decision")).not.toBeInTheDocument();
    expect(screen.queryByText("7477f670")).not.toBeInTheDocument();

    const familyCard = screen.getByTestId(`transfer-family-member-${transferId}`);
    expect(familyCard).toHaveTextContent("Original");
    const sendBackOnOriginal = screen.getByTestId(
      `transfer-family-member-dispatch-return-${custodyId}`,
    );
    expect(sendBackOnOriginal).toBeEnabled();
    expect(sendBackOnOriginal).toHaveTextContent(/Send back 5 Pepsi 330ml/);
    expect(sendBackOnOriginal).toHaveTextContent(/Main Store/);
    expect(screen.queryByTestId(`transfer-family-member-return-status-${custodyId}`)).not.toBeInTheDocument();
    expect(familyCard).toHaveTextContent(/Sent 10/);
    expect(familyCard).toHaveTextContent(/Other 5/);
    expect(
      screen.queryByTestId(`transfer-exception-custody-dispatch-return-${custodyId}`),
    ).not.toBeInTheDocument();
  });

  it("opens movement transaction drawer when Original family card header is clicked", async () => {
    const user = userEvent.setup();
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [],
      exceptionCustodies: [],
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

    await user.click(await screen.findByTestId(`transfer-family-member-open-${transferId}`));
    expect(screen.getByTestId(`transfer-family-member-open-${transferId}`)).toHaveTextContent(
      "View details",
    );
    expect(await screen.findByTestId("inventory-movement-transaction-drawer")).toBeInTheDocument();
    expect(screen.getByTestId("inventory-movement-transaction-transfer-header")).toHaveTextContent(
      "260829-001",
    );
    expect(screen.queryByTestId("inventory-movement-transaction-this-movement")).not.toBeInTheDocument();
    expect(screen.getByTestId("inventory-movement-transaction-this-transfer")).toBeInTheDocument();
  });

  it("shows Return in transit status at destination after exception return is sent", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    const custodyId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "Other",
              otherReasonNote: "Wrong box",
              otherFollowUp: "AcceptShortage",
              otherCustodyDecision: "ReturnToSource",
              actualReceivedProductId: actualId,
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          expectedProductId: cokeId,
          actualProductId: actualId,
          expectedProductName: "Coke 330ml",
          actualProductName: "Pepsi 330ml",
          quantity: 5,
          reasonCode: "Other",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "ReturnInTransit",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
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

    expect(
      screen.queryByTestId(`transfer-family-member-dispatch-return-${custodyId}`),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByTestId(`transfer-family-member-receive-return-${custodyId}`),
    ).not.toBeInTheDocument();
    const status = await screen.findByTestId(
      `transfer-family-member-return-status-${custodyId}`,
    );
    expect(status).toHaveTextContent(/5 Pepsi 330ml/);
    expect(status).toHaveTextContent(/Main Store/);
    expect(status).toHaveTextContent(/Return in transit/);
  });

  it("shows Incoming return and Receive return at source when return is in transit", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const custodyId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "Other",
              otherReasonNote: "Wrong box",
              otherFollowUp: "AcceptShortage",
              otherCustodyDecision: "ReturnToSource",
              actualReceivedProductId: actualId,
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          expectedProductId: cokeId,
          actualProductId: actualId,
          expectedProductName: "Coke 330ml",
          actualProductName: "Pepsi 330ml",
          quantity: 5,
          reasonCode: "Other",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "ReturnInTransit",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
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

    const status = await screen.findByTestId(
      `transfer-family-member-return-status-${custodyId}`,
    );
    expect(status).toHaveTextContent(/Incoming return/);
    expect(status).toHaveTextContent(/5 Pepsi 330ml/);
    expect(status).toHaveTextContent(/Branch B/);
    expect(
      screen.getByTestId(`transfer-family-member-receive-return-${custodyId}`),
    ).toBeEnabled();
    expect(
      screen.getByTestId(`transfer-family-member-receive-return-${custodyId}`),
    ).toHaveTextContent(/Receive return/);
    expect(
      screen.queryByTestId(`transfer-family-member-dispatch-return-${custodyId}`),
    ).not.toBeInTheDocument();
  });

  it("shows Returned to source completed state after receive", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const custodyId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "Other",
              otherReasonNote: "Wrong box",
              otherFollowUp: "AcceptShortage",
              otherCustodyDecision: "ReturnToSource",
              actualReceivedProductId: actualId,
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          expectedProductId: cokeId,
          actualProductId: actualId,
          expectedProductName: "Coke 330ml",
          actualProductName: "Pepsi 330ml",
          quantity: 5,
          reasonCode: "Other",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "ReceivedAtSource",
          heldBranchId: mainId,
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
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

    const status = await screen.findByTestId(
      `transfer-family-member-return-status-${custodyId}`,
    );
    expect(status).toHaveTextContent(/Returned to source/);
    expect(status).toHaveTextContent(/5 Pepsi 330ml received from Branch B/);
    expect(
      screen.queryByTestId(`transfer-family-member-receive-return-${custodyId}`),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByTestId(`transfer-family-member-dispatch-return-${custodyId}`),
    ).not.toBeInTheDocument();
  });

  it("hides Receive return after source receive succeeds and shows Returned to source", async () => {
    const user = userEvent.setup();
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const custodyId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    const inTransitCustody = {
      custodyId,
      transferId,
      rootTransferId: transferId,
      receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
      expectedProductId: cokeId,
      actualProductId: actualId,
      expectedProductName: "Coke 330ml",
      actualProductName: "Pepsi 330ml",
      quantity: 5,
      reasonCode: "WrongVariant",
      decision: "ReturnToSource",
      followUpIntent: "AcceptShortage",
      status: "ReturnInTransit",
      heldBranchId: branchBId,
      recoveredSellableQty: 0,
      confirmedNonSellableQty: 0,
      replacementDemandQty: 0,
      createdAtUtc: "2026-08-29T10:00:00Z",
      updatedAtUtc: "2026-08-29T10:00:00Z",
      returnDispatchedAtUtc: "2026-08-29T11:00:00Z",
    };
    const receivedCustody = {
      ...inTransitCustody,
      status: "ReceivedAtSource",
      heldBranchId: mainId,
      returnReceivedAtUtc: "2026-08-29T12:00:00Z",
      updatedAtUtc: "2026-08-29T12:00:00Z",
    };
    const baseTransfer = {
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [],
      exceptionCustodies: [inTransitCustody],
    };
    vi.spyOn(transferClient, "getInventoryTransfer")
      .mockResolvedValueOnce(baseTransfer as never)
      .mockResolvedValue({ ...baseTransfer, exceptionCustodies: [receivedCustody] } as never);
    vi.spyOn(transferClient, "receiveInventoryTransferExceptionReturn").mockResolvedValue({
      ...receivedCustody,
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

    const receiveBtn = await screen.findByTestId(
      `transfer-family-member-receive-return-${custodyId}`,
    );
    expect(receiveBtn).toBeEnabled();
    await user.click(receiveBtn);

    expect(
      await screen.findByTestId(`transfer-family-member-return-status-${custodyId}`),
    ).toHaveTextContent(/Returned to source/);
    expect(
      screen.queryByTestId(`transfer-family-member-receive-return-${custodyId}`),
    ).not.toBeInTheDocument();
    expect(transferClient.receiveInventoryTransferExceptionReturn).toHaveBeenCalled();
    expect(transferClient.getInventoryTransfer).toHaveBeenCalledTimes(2);
  });

  it("shows Waiting for return at source before destination sends back", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const custodyId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      remainingToDispatchQty: 0,
      waivedQty: 5,
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 0,
          totalOtherQty: 5,
        },
      ],
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "Other",
              otherFollowUp: "AcceptShortage",
              otherCustodyDecision: "ReturnToSource",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
          expectedProductId: cokeId,
          actualProductId: cokeId,
          expectedProductName: "Coke 330ml",
          actualProductName: "Coke 330ml",
          quantity: 5,
          reasonCode: "Other",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "AwaitingReturn",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
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

    expect(
      await screen.findByTestId(`transfer-family-member-return-status-${custodyId}`),
    ).toHaveTextContent(/Waiting for return/);
    expect(
      screen.queryByTestId(`transfer-family-member-dispatch-return-${custodyId}`),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByTestId(`transfer-family-member-receive-return-${custodyId}`),
    ).not.toBeInTheDocument();
  });

  it("hides Fulfill remaining after R2 AcceptShortage waives family remainder", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const r2Id = "22222222-2222-2222-2222-222222222222";
    const r2LineId = "33333333-3333-3333-3333-333333333333";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      transferId: r2Id,
      transferNumber: "TR-260922-003-R2",
      rootTransferId: transferId,
      status: "ClosedWithDiscrepancy",
      totalSentQty: 4,
      totalReceivedQty: 2,
      totalOutstandingQty: 0,
      satisfiedAtDestinationQty: 8,
      openInTransitQty: 0,
      remainingToDispatchQty: 0,
      waivedQty: 2,
      lines: [
        {
          lineId: r2LineId,
          productId: cokeId,
          productName: "Coke 1.5L",
          unitOfMeasure: "Piece",
          lineNumber: 1,
          sentQty: 4,
          receivedQty: 2,
          outstandingQty: 0,
          closedQty: 2,
          waivedQty: 2,
          differenceQty: 2,
          lineStatus: "Closed",
          discrepancyReason: "ShortShipment",
          discrepancyNote: null,
        },
      ],
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId: r2LineId,
              productId: cokeId,
              quantityReceived: 2,
              quantityDamaged: 0,
              quantityMissing: 2,
              quantityOther: 0,
              missingDisposition: "AcceptShortage",
              quantityWaived: 2,
            },
          ],
        },
      ],
      familyMembers: [
        {
          transferId,
          transferNumber: "TR-260922-003",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 5,
        },
        {
          transferId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          transferNumber: "TR-260922-003-R1",
          status: "ClosedWithDiscrepancy",
          replacementSequence: 1,
          isRoot: false,
          totalSentQty: 5,
          totalReceivedQty: 1,
          totalOutstandingQty: 0,
          totalDamagedQty: 4,
        },
        {
          transferId: r2Id,
          transferNumber: "TR-260922-003-R2",
          status: "ClosedWithDiscrepancy",
          replacementSequence: 2,
          isRoot: false,
          totalSentQty: 4,
          totalReceivedQty: 2,
          totalOutstandingQty: 0,
          totalDamagedQty: 0,
          totalMissingQty: 2,
        },
      ],
      damageCustodies: [],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${r2Id}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("this-shipment-sent")).toHaveTextContent("4");
    expect(screen.getByTestId("this-shipment-good")).toHaveTextContent("2");
    expect(screen.getByTestId("this-shipment-damaged")).toHaveTextContent("0");
    expect(screen.getByTestId("this-shipment-missing")).toHaveTextContent("2");
    expect(screen.queryByTestId("transfer-receiving-decision")).not.toBeInTheDocument();
    expect(screen.getByTestId("transfer-fulfillment-target")).toHaveTextContent("10");
    expect(screen.getByTestId("transfer-fulfillment-good")).toHaveTextContent("8");
    expect(screen.getByTestId("transfer-fulfillment-needs-replacement")).toHaveTextContent("0");
    expect(screen.queryByTestId("transfer-fulfill-remaining")).not.toBeInTheDocument();
    expect(screen.queryByText(/Fulfill remaining 2/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/2 still needs fulfillment/i)).not.toBeInTheDocument();
  });

  it("shows missing disposition from persisted receipt decision", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      ...inTransitTransfer(),
      status: "PartiallyReceived",
      totalSentQty: 10,
      totalReceivedQty: 5,
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 5,
      remainingToDispatchQty: 0,
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityDamaged: 0,
              quantityMissing: 5,
              missingDisposition: "ExpectedLater",
            },
          ],
        },
      ],
      damageCustodies: [],
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
    expect(await screen.findByTestId("this-shipment-missing")).toHaveTextContent("5");
    expect(screen.queryByTestId("transfer-receiving-decision")).not.toBeInTheDocument();
    expect(screen.queryByText("ExpectedLater")).not.toBeInTheDocument();
  });

  it("source can fulfill remaining from transfer coverage panel", async () => {
    workspaceMock.boundWorkspace.branchId = mainId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const draftId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const familyTransfer = {
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalReceivedQty: 5,
      totalOutstandingQty: 0,
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 5,
      waivedQty: 0,
      stockRequestId: null,
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1",
          sequence: 1,
          receivedAtUtc: "2026-08-29T10:00:00Z",
          receivedBy: "99999999-9999-9999-9999-999999999999",
          lines: [
            {
              receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2",
              lineId,
              productId: cokeId,
              quantityReceived: 5,
              quantityDamaged: 5,
              damagedFollowUp: "RequestReplacement",
            },
          ],
        },
      ],
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
          totalDamagedQty: 5,
        },
      ],
      damageCustodies: [
        {
          custodyId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          productId: cokeId,
          quantity: 5,
          decision: "KeepAtDestination",
          followUpIntent: "RequestReplacement",
          status: "HeldAtDestination",
          heldBranchId: branchBId,
          recoveredSellableQty: 0,
          confirmedDamagedQty: 0,
          waivedQty: 0,
          destinationRecoveredSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-08-29T10:00:00Z",
          updatedAtUtc: "2026-08-29T10:00:00Z",
        },
      ],
    };
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(familyTransfer as never);
    const prepareSpy = vi
      .spyOn(transferClient, "prepareInventoryTransferRemaining")
      .mockResolvedValue({
        ...draftTransfer(),
        transferId: draftId,
        rootTransferId: transferId,
        replacementSequence: 1,
        transferNumber: "260829-001-R1",
      } as never);
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("transfer-family-coverage")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-fulfillment-needs-replacement")).toHaveTextContent("5");
    expect(screen.queryByTestId("transfer-view-stock-request")).not.toBeInTheDocument();
    expect(screen.getByTestId("transfer-fulfill-remaining-notice")).toBeInTheDocument();
    expect(screen.getByTestId("transfer-fulfill-remaining-header")).toHaveTextContent(
      /Fulfill remaining 5/i,
    );
    expect(screen.getByTestId("transfer-fulfill-remaining-actions")).toHaveTextContent(
      /Fulfill remaining 5/i,
    );
    const fulfillBtn = screen.getByTestId("transfer-fulfill-remaining");
    expect(fulfillBtn).toHaveTextContent(/Fulfill remaining 5/i);
    await user.click(fulfillBtn);
    await waitFor(() => expect(prepareSpy).toHaveBeenCalled());
    expect(prepareSpy.mock.calls[0]?.[1]).toBe(transferId);
  });

  it("source can fulfill remaining when branch ids differ only by case", async () => {
    workspaceMock.boundWorkspace.branchId = mainId.toUpperCase();
    workspaceMock.boundWorkspace.branchName = "Main Store";
    const familyTransfer = {
      ...inTransitTransfer(),
      status: "ClosedWithDiscrepancy",
      totalReceivedQty: 5,
      totalOutstandingQty: 0,
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 5,
      waivedQty: 0,
      stockRequestId: null,
      sourceBranchId: mainId.toLowerCase(),
      familyMembers: [
        {
          transferId,
          transferNumber: "260829-001",
          status: "ClosedWithDiscrepancy",
          replacementSequence: null,
          isRoot: true,
          totalSentQty: 10,
          totalReceivedQty: 5,
          totalOutstandingQty: 0,
        },
      ],
      damageCustodies: [],
    };
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(familyTransfer as never);
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(await screen.findByTestId("transfer-fulfill-remaining-header")).toHaveTextContent(
      /Fulfill remaining 5/i,
    );
    expect(screen.getByTestId("inventory-transfer-detail-page")).toHaveAttribute(
      "data-can-fulfill-remaining",
      "true",
    );
  });

  it("rejects received quantity above sent and disables receive", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(inTransitTransfer() as never);
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/transfers/${transferId}`]}>
          <Routes>
            <Route path="/inventory/transfers/:transferId" element={<InventoryTransferDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    await user.click(await screen.findByTestId("transfer-receive"));
    await user.click(await screen.findByTestId(`transfer-receive-edit-menu-${lineId}`));
    const qty = await screen.findByTestId(`transfer-receive-qty-${lineId}`);
    await user.clear(qty);
    await user.type(qty, "25");
    await user.click(screen.getByTestId(`transfer-receive-edit-save-${lineId}`));
    expect(await screen.findByTestId(`transfer-receive-qty-error-${lineId}`)).toHaveTextContent(
      "Receive now cannot exceed outstanding quantity (24)",
    );
    expect(screen.getByTestId("transfer-receive-review")).toBeDisabled();
  });

  it("partially received destination can receive remaining and close remainder", async () => {
    workspaceMock.boundWorkspace.branchId = branchBId;
    workspaceMock.boundWorkspace.branchName = "Branch B";
    const partial = {
      ...inTransitTransfer(),
      status: "PartiallyReceived",
      totalReceivedQty: 10,
      totalOutstandingQty: 14,
      lines: [
        {
          ...inTransitTransfer().lines[0]!,
          receivedQty: 10,
          outstandingQty: 14,
          closedQty: 0,
          differenceQty: 14,
        },
      ],
    };
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue(partial as never);
    const closeSpy = vi
      .spyOn(transferClient, "closeRemainderInventoryTransfer")
      .mockResolvedValue({
        ...partial,
        status: "ClosedWithDiscrepancy",
        totalOutstandingQty: 0,
        totalClosedQty: 14,
        lines: [
          {
            ...partial.lines[0]!,
            closedQty: 14,
            outstandingQty: 0,
            discrepancyReason: "ShortShipment",
          },
        ],
      } as never);
    const user = userEvent.setup();
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
      "PartiallyReceived",
    );
    expect(screen.getByTestId("transfer-receive")).toHaveTextContent("Receive remaining");
    expect(screen.getByTestId("transfer-close-remainder")).toBeInTheDocument();
    await user.click(screen.getByTestId("transfer-close-remainder"));
    await user.selectOptions(
      await screen.findByTestId(`transfer-close-reason-${lineId}`),
      "ShortShipment",
    );
    await user.click(screen.getByTestId("transfer-close-remainder-confirm"));
    await waitFor(() => expect(closeSpy).toHaveBeenCalled());
    await waitFor(() =>
      expect(screen.getByTestId("inventory-transfer-detail-page")).toHaveAttribute(
        "data-status",
        "ClosedWithDiscrepancy",
      ),
    );
  });
});

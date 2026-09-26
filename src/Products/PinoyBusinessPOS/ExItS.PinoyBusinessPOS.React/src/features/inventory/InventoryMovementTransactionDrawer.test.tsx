import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import * as transferClient from "@/api/pos/pos-inventory-transfer-client";
import { AppProviders } from "@/app/providers";
import { InventoryMovementTransactionDrawer } from "@/features/inventory/InventoryMovementTransactionDrawer";

const transferId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

function transferOutMovement(): PosStockMovementDto {
  return {
    movementId: "mov-1",
    productId: "prod-1",
    inventoryAccountId: "acc-1",
    movementType: "TransferOut",
    quantityEffect: -10,
    reason: "Transfer out TR-260922-001",
    sourceType: "InventoryTransfer",
    sourceId: transferId,
    transactionType: "InventoryTransfer",
    transactionId: transferId,
    transactionReference: "TR-260922-001",
    recordedAtUtc: "2026-09-22T19:07:00Z",
    recordedBy: "actor-1",
    sellableBefore: 100,
    sellableDelta: -10,
    sellableAfter: 90,
  };
}

describe("InventoryMovementTransactionDrawer", () => {
  beforeEach(() => {
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260922-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Iloilo Branch",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-22T18:00:00Z",
      updatedAtUtc: "2026-09-22T19:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [],
      receipts: [],
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 5,
      waivedQty: 0,
      damageCustodies: [
        {
          custodyId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          productId: "prod-1",
          quantity: 5,
          decision: "KeepAtDestination",
          followUpIntent: "RequestReplacement",
          status: "HeldAtDestination",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedDamagedQty: 0,
          waivedQty: 0,
          destinationRecoveredSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-22T19:00:00Z",
          updatedAtUtc: "2026-09-22T19:00:00Z",
        },
      ],
      familyMembers: [],
    } as never);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows transfer transaction details and navigates via View full transfer", async () => {
    const user = userEvent.setup();
    const movement = transferOutMovement();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
            <Route
              path="/inventory/transfers/:transferId"
              element={<div data-testid="transfer-detail-route">Transfer detail</div>}
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("inventory-movement-transaction-drawer")).toBeInTheDocument();
    expect(screen.queryByTestId("inventory-reservations-drawer")).not.toBeInTheDocument();
    expect(await screen.findByText("TR-260922-001")).toBeInTheDocument();
    expect(screen.getByTestId("inventory-movement-transaction-this-movement")).toHaveTextContent(
      /Transfer out/i,
    );

    const sellableBalance = screen.getByTestId("inventory-movement-sellable-balance");
    expect(sellableBalance).toHaveTextContent(/Sellable before/);
    expect(sellableBalance).toHaveTextContent(/100\s*Kilogram/);
    expect(sellableBalance).toHaveTextContent(/This movement/);
    expect(sellableBalance).toHaveTextContent(/-10\s*Kilogram/);
    expect(sellableBalance).toHaveTextContent(/Sellable after/);
    expect(sellableBalance).toHaveTextContent(/90\s*Kilogram/);

    await waitFor(() => {
      expect(screen.getByTestId("inventory-movement-transaction-drawer")).toHaveAttribute(
        "data-interactive",
        "true",
      );
    });

    // Prefer loaded transfer route details when the query resolves.
    await waitFor(() => {
      expect(transferClient.getInventoryTransfer).toHaveBeenCalled();
    });
    expect(await screen.findByText(/Main Branch/)).toBeInTheDocument();
    expect(screen.getByText(/Iloilo Branch/)).toBeInTheDocument();

    const trLink = screen.getByTestId("inventory-movement-transaction-transfer-number");
    expect(trLink).toHaveTextContent("TR-260922-001");
    expect(trLink).toHaveAttribute(
      "href",
      expect.stringContaining(`/inventory/transfers/${transferId}`),
    );
    await user.click(trLink);
    await waitFor(() => {
      expect(screen.getByTestId("transfer-detail-route")).toBeInTheDocument();
    });
  });

  it("makes drawer TR# clickable by resolving transfer id from TR number when transactionId is missing", async () => {
    const user = userEvent.setup();
    const restockId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
    vi.spyOn(transferClient, "listInventoryTransfers").mockResolvedValue({
      items: [
        {
          transferId: restockId,
          transferNumber: "TR-260924-001",
          status: "ClosedWithDiscrepancy",
          sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          sourceBranchName: "Main Branch",
          destinationBranchName: "Branch 2",
          createdAtUtc: "2026-09-24T11:00:00Z",
          totalSentQty: 10,
          totalReceivedQty: 5,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 5,
    } as never);
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId: restockId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260924-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-24T11:00:00Z",
      updatedAtUtc: "2026-09-24T11:25:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [],
      receipts: [],
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 0,
      waivedQty: 0,
      damageCustodies: [],
      exceptionCustodies: [],
      familyMembers: [],
    } as never);

    const movement: PosStockMovementDto = {
      movementId: "mov-restock-no-trx",
      productId: "prod-banana",
      inventoryAccountId: "acc-1",
      movementType: "TransferExceptionReturnRestock",
      quantityEffect: 5,
      reason: "Wrong item return received TR-260924-001",
      sourceType: "InventoryTransfer",
      sourceId: "custody-id-1",
      // transactionId intentionally omitted — reproduces live drawer TR as plain text
      recordedAtUtc: "2026-09-24T11:25:59Z",
      recordedBy: "actor-1",
      sellableBefore: 95,
      sellableDelta: 5,
      sellableAfter: 100,
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-banana"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
            <Route
              path="/inventory/transfers/:transferId"
              element={<div data-testid="transfer-detail-route">Transfer detail</div>}
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const trLink = await screen.findByTestId("inventory-movement-transaction-transfer-number");
    expect(trLink).toHaveTextContent("TR-260924-001");
    expect(trLink).toHaveAttribute(
      "href",
      expect.stringContaining(`/inventory/transfers/${restockId}`),
    );
    expect(transferClient.listInventoryTransfers).toHaveBeenCalled();
    await user.click(trLink);
    await waitFor(() => {
      expect(screen.getByTestId("transfer-detail-route")).toBeInTheDocument();
    });
  });

  it("shows damage return received route, inventory effect, and disposition", async () => {
    const movement: PosStockMovementDto = {
      movementId: "mov-return-in",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "TransferDamageReturnIn",
      quantityEffect: 5,
      reason: "Transfer damage return in TR-260922-001",
      sourceType: "InventoryTransfer",
      sourceId: transferId,
      transactionType: "InventoryTransfer",
      transactionId: transferId,
      transactionReference: "TR-260922-001",
      recordedAtUtc: "2026-09-22T20:00:00Z",
      recordedBy: "actor-1",
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-transaction-this-movement")).toHaveTextContent(
      /Damaged return received/i,
    );
    expect(screen.queryByTestId("inventory-reservations-drawer")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("inventory-movement-damage-return-route")).toBeInTheDocument();
    });
    const route = screen.getByTestId("inventory-movement-damage-return-route");
    expect(route).toHaveTextContent(/Iloilo Branch/);
    expect(route).toHaveTextContent(/Main Branch/);
    expect(screen.getByTestId("inventory-movement-transaction-inventory-effect")).toHaveTextContent(
      /Physical:\s*\+5/,
    );
    expect(screen.getByTestId("inventory-movement-transaction-inventory-effect")).toHaveTextContent(
      /Sellable:\s*0/,
    );
    expect(screen.getByTestId("inventory-movement-transaction-inventory-effect")).toHaveTextContent(
      /Inspection hold:\s*\+5/,
    );
    expect(screen.getByTestId("inventory-movement-damage-disposition")).toHaveTextContent(
      /Returned to source/i,
    );
    expect(screen.getByTestId("inventory-movement-view-full-transfer")).toBeInTheDocument();
  });

  it("shows replacement and return-to-source under Damaged transfer received", async () => {
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260922-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Iloilo Branch",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-22T18:00:00Z",
      updatedAtUtc: "2026-09-22T19:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [],
      receipts: [],
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 5,
      waivedQty: 0,
      damageCustodies: [
        {
          custodyId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          productId: "prod-1",
          quantity: 5,
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "AwaitingReturn",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedDamagedQty: 0,
          waivedQty: 0,
          destinationRecoveredSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-22T19:00:00Z",
          updatedAtUtc: "2026-09-22T19:00:00Z",
        },
      ],
      familyMembers: [],
    } as never);

    const movement: PosStockMovementDto = {
      movementId: "mov-damage",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "TransferDamageHold",
      quantityEffect: 5,
      reason: "Transfer damage hold TR-260922-001 · Return to source · Replacement requested",
      sourceType: "InventoryTransfer",
      sourceId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      transactionType: "InventoryTransfer",
      transactionId: transferId,
      transactionReference: "TR-260922-001",
      recordedAtUtc: "2026-09-22T19:07:00Z",
      recordedBy: "actor-1",
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-transaction-this-movement")).toHaveTextContent(
      /Damaged transfer received/i,
    );
    expect(await screen.findByTestId("inventory-movement-damage-hold-decision")).toHaveTextContent(
      /Replacement requested.*Return to source/i,
    );
  });

  it("keeps R2 this-transfer metrics separate from overall fulfillment", async () => {
    const r2Id = "22222222-2222-2222-2222-222222222222";
    const rootId = "11111111-1111-1111-1111-111111111111";
    const r2LineId = "33333333-3333-3333-3333-333333333333";
    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId: r2Id,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260922-003-R2",
      rootTransferId: rootId,
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Iloilo Branch",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-22T18:00:00Z",
      updatedAtUtc: "2026-09-22T20:00:00Z",
      totalSentQty: 4,
      totalReceivedQty: 2,
      totalClosedQty: 2,
      totalOutstandingQty: 0,
      totalDifferenceQty: 2,
      receiptCount: 1,
      lines: [
        {
          lineId: r2LineId,
          productId: "prod-1",
          productName: "Apple",
          unitOfMeasure: "Kilogram",
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
          receiptId: "44444444-4444-4444-4444-444444444444",
          sequence: 1,
          receivedAtUtc: "2026-09-22T20:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId: "55555555-5555-5555-5555-555555555555",
              lineId: r2LineId,
              productId: "prod-1",
              quantityReceived: 2,
              quantityDamaged: 0,
              quantityMissing: 2,
              quantityOther: 0,
              missingDisposition: "AcceptShortage",
              quantityWaived: 2,
              note: "Accepted short delivery from truck",
            },
          ],
        },
      ],
      satisfiedAtDestinationQty: 8,
      openInTransitQty: 0,
      remainingToDispatchQty: 0,
      waivedQty: 2,
      damageCustodies: [],
      familyMembers: [
        {
          transferId: rootId,
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
          transferId: "66666666-6666-6666-6666-666666666666",
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
    } as never);

    const movement: PosStockMovementDto = {
      movementId: "mov-r2-in",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "TransferIn",
      quantityEffect: 2,
      reason: "Transfer in TR-260922-003-R2",
      sourceType: "InventoryTransfer",
      sourceId: r2Id,
      transactionType: "InventoryTransfer",
      transactionId: r2Id,
      transactionReference: "TR-260922-003-R2",
      recordedAtUtc: "2026-09-22T20:00:00Z",
      recordedBy: "actor-1",
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const thisTransfer = await screen.findByTestId("inventory-movement-transaction-this-transfer");
    expect(thisTransfer).toHaveTextContent(/Sent/);
    expect(thisTransfer).toHaveTextContent("4");
    expect(thisTransfer).toHaveTextContent(/Good received/);
    expect(thisTransfer).toHaveTextContent("2");
    expect(thisTransfer).toHaveTextContent(/Missing/);
    expect(thisTransfer).toHaveTextContent(/Accepted \/ waived/);
    expect(thisTransfer).not.toHaveTextContent(/Receiving decision/);

    const receivingDecision = screen.getByTestId(
      "inventory-movement-transaction-receiving-decision",
    );
    expect(receivingDecision).toHaveTextContent(/Receiving decision/);
    expect(receivingDecision).toHaveTextContent(/Missing/);
    expect(receivingDecision).toHaveTextContent(/Accepted shortage/i);
    expect(screen.getByTestId("inventory-movement-receiving-note")).toHaveTextContent(
      "Accepted short delivery from truck",
    );
    expect(receivingDecision).toHaveTextContent(/Remarks/i);

    const overall = screen.getByTestId("inventory-movement-transaction-overall-fulfillment");
    expect(overall).toHaveTextContent(/Target \/ requested/);
    expect(overall).toHaveTextContent("10");
    expect(overall).toHaveTextContent(/Needs fulfillment/);
    expect(overall).toHaveTextContent("0");
    // Must not mix R2 sent=4 with family good=8 and family damaged=9 in one Transfer section.
    expect(screen.queryByTestId("inventory-movement-transaction-transfer-summary")).not.toBeInTheDocument();
  });

  it("shows sellable before / this movement / after including zero sellable delta", async () => {
    const movement: PosStockMovementDto = {
      movementId: "mov-hold",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "TransferDamageHold",
      quantityEffect: 5,
      reason: "Transfer damage hold",
      sourceType: "InventoryTransfer",
      sourceId: transferId,
      transactionType: "InventoryTransfer",
      transactionId: transferId,
      transactionReference: "TR-260922-001",
      recordedAtUtc: "2026-09-22T20:00:00Z",
      recordedBy: "actor-1",
      sellableBefore: 45,
      sellableDelta: 0,
      sellableAfter: 45,
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const balance = await screen.findByTestId("inventory-movement-sellable-balance");
    expect(balance).toHaveTextContent(/Sellable before/);
    expect(balance).toHaveTextContent(/45\s*Kilogram/);
    expect(balance).toHaveTextContent(/This movement/);
    expect(balance).toHaveTextContent(/0\s*Kilogram/);
    expect(balance).toHaveTextContent(/Sellable after/);
  });

  it("shows opening stock sellable progression from zero", async () => {
    const movement: PosStockMovementDto = {
      movementId: "mov-open",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "OpeningStock",
      quantityEffect: 100,
      reason: "Opening stock",
      sourceType: "Manual",
      recordedAtUtc: "2026-09-01T10:00:00Z",
      recordedBy: "actor-1",
      sellableBefore: 0,
      sellableDelta: 100,
      sellableAfter: 100,
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const balance = await screen.findByTestId("inventory-movement-sellable-balance");
    expect(balance).toHaveTextContent(/Sellable before/);
    expect(balance).toHaveTextContent(/0\s*Kilogram/);
    expect(balance).toHaveTextContent(/\+100\s*Kilogram/);
    expect(balance).toHaveTextContent(/Sellable after/);
    expect(balance).toHaveTextContent(/100\s*Kilogram/);
  });

  it("opens transfer transaction details from transferContext without a movement", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={null}
                  transferContext={{
                    transferId,
                    transferNumber: "TR-260922-001",
                  }}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-transaction-drawer")).toBeInTheDocument();
    expect(screen.getByTestId("inventory-movement-transaction-transfer-header")).toHaveTextContent(
      "TR-260922-001",
    );
    expect(screen.queryByTestId("inventory-movement-transaction-this-movement")).not.toBeInTheDocument();
    expect(await screen.findByTestId("inventory-movement-transaction-this-transfer")).toBeInTheDocument();
    expect(screen.getByTestId("inventory-movement-view-full-transfer")).toBeInTheDocument();
  });

  it("shows WrongItem expected Apple / actual Banana with branch-aware custody", async () => {
    const appleId = "33333333-3333-3333-3333-333333333333";
    const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const lineId = "22222222-2222-2222-2222-222222222222";
    const receiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260922-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-22T18:00:00Z",
      updatedAtUtc: "2026-09-22T19:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [
        {
          lineId,
          productId: appleId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
          discrepancyReason: null,
          discrepancyNote: null,
          sourceLotId: null,
          lotNumber: null,
          expirationDate: null,
        },
      ],
      receipts: [
        {
          receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          sequence: 1,
          receivedAtUtc: "2026-09-22T19:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId,
              lineId,
              productId: appleId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Banana was packed instead of Apple",
              otherFollowUp: "RequestReplacement",
              actualReceivedProductId: bananaId,
              otherCustodyDecision: "ReturnToSource",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          transferId,
          rootTransferId: transferId,
          receiptLineId,
          expectedProductId: appleId,
          actualProductId: bananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "AwaitingReturn",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-22T19:00:00Z",
          updatedAtUtc: "2026-09-22T19:00:00Z",
        },
      ],
      damageCustodies: [],
      familyMembers: [],
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 5,
      waivedQty: 0,
    } as never);

    const movement: PosStockMovementDto = {
      movementId: "mov-exc",
      productId: bananaId,
      inventoryAccountId: "acc-1",
      movementType: "TransferExceptionActualOut",
      quantityEffect: -5,
      reason: "Transfer exception actual out TR-260922-001",
      sourceType: "InventoryTransfer",
      sourceId: receiptLineId,
      transactionType: "InventoryTransfer",
      transactionId: transferId,
      transactionReference: "TR-260922-001",
      recordedAtUtc: "2026-09-22T19:00:00Z",
      recordedBy: "actor-1",
      sellableBefore: 100,
      sellableDelta: -5,
      sellableAfter: 95,
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-transaction-transfer-header")).toHaveTextContent(
      "TR-260922-001",
    );
    const trNumberLink = screen.getByTestId("inventory-movement-transaction-transfer-number");
    expect(trNumberLink).toHaveAttribute(
      "href",
      expect.stringContaining(`/inventory/transfers/${transferId}`),
    );
    expect(await screen.findByTestId("inventory-movement-type-label")).toHaveTextContent(
      "Wrong item sent",
    );
    expect(await screen.findByTestId("inventory-movement-exception-actual-item")).toHaveTextContent(
      "Banana",
    );
    expect(screen.getByTestId("inventory-movement-exception-expected-item")).toHaveTextContent("Apple");
    expect(screen.getByTestId("inventory-movement-exception-reason")).toHaveTextContent(/Wrong item/i);
    expect(screen.getByTestId("inventory-movement-exception-note")).toHaveTextContent(
      "Banana was packed instead of Apple",
    );
    expect(await screen.findByTestId("inventory-movement-exception-route")).toHaveTextContent(
      "Main Branch",
    );
    expect(screen.getByTestId("inventory-movement-exception-route")).toHaveTextContent("Branch 2");

    const receiving = await screen.findByTestId(
      "inventory-movement-transaction-receiving-decision",
    );
    expect(receiving).toHaveTextContent(/Wrong item/i);
    expect(screen.getByTestId("inventory-movement-receiving-expected-item")).toHaveTextContent(
      "Apple",
    );
    expect(screen.getByTestId("inventory-movement-receiving-actual-item")).toHaveTextContent(
      "Banana",
    );
    expect(screen.getByTestId("inventory-movement-receiving-actual-item")).not.toHaveTextContent(
      "Apple",
    );
    expect(screen.getByTestId("inventory-movement-receiving-note")).toHaveTextContent(
      "Banana was packed instead of Apple",
    );
    expect(screen.getByTestId("inventory-movement-receiving-exception-custody")).toHaveTextContent(
      "Return to Main Branch",
    );
    expect(screen.getByTestId("inventory-movement-receiving-exception-custody")).not.toHaveTextContent(
      "Branch 2",
    );
    expect(screen.getByTestId("inventory-movement-receiving-return-status")).toHaveTextContent(
      "Waiting to return",
    );
    expect(receiving).not.toHaveTextContent("ReturnToSource");
    expect(receiving).not.toHaveTextContent("AwaitingReturn");
    expect(receiving).not.toHaveTextContent(bananaId);
    expect(screen.getByTestId("inventory-movement-view-full-transfer")).toBeInTheDocument();
  });

  it("shows Returning to Main Branch after return dispatch", async () => {
    const appleId = "33333333-3333-3333-3333-333333333333";
    const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const lineId = "22222222-2222-2222-2222-222222222222";
    const receiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260922-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-22T18:00:00Z",
      updatedAtUtc: "2026-09-22T19:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [
        {
          lineId,
          productId: appleId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
          discrepancyReason: null,
          discrepancyNote: null,
          sourceLotId: null,
          lotNumber: null,
          expirationDate: null,
        },
      ],
      receipts: [
        {
          receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          sequence: 1,
          receivedAtUtc: "2026-09-22T19:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId,
              lineId,
              productId: appleId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Banana was packed instead of Apple",
              otherFollowUp: "RequestReplacement",
              actualReceivedProductId: bananaId,
              otherCustodyDecision: "ReturnToSource",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          transferId,
          rootTransferId: transferId,
          receiptLineId,
          expectedProductId: appleId,
          actualProductId: bananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "ReturnInTransit",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-22T19:00:00Z",
          updatedAtUtc: "2026-09-22T19:30:00Z",
        },
      ],
      damageCustodies: [],
      familyMembers: [],
      satisfiedAtDestinationQty: 5,
      openInTransitQty: 0,
      remainingToDispatchQty: 5,
      waivedQty: 0,
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/prod-1"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={null}
                  transferContext={{ transferId, transferNumber: "TR-260922-001" }}
                  unitOfMeasure="pcs"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(
      await screen.findByTestId("inventory-movement-receiving-return-status"),
    ).toHaveTextContent("Returning to Main Branch");
    expect(screen.getByTestId("inventory-movement-receiving-exception-custody")).toHaveTextContent(
      "Return to Main Branch",
    );
  });

  it("matches Banana ActualOut custody when multiple exceptions exist", async () => {
    const appleId = "33333333-3333-3333-3333-333333333333";
    const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const orangeId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    const mangoId = "cccccccc-cccc-cccc-cccc-000000000001";
    const appleLineId = "22222222-2222-2222-2222-222222222222";
    const orangeLineId = "22222222-2222-2222-2222-222222222223";
    const appleReceiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const orangeReceiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef";

    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260924-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-24T10:00:00Z",
      updatedAtUtc: "2026-09-24T11:00:00Z",
      totalSentQty: 20,
      totalReceivedQty: 12,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 8,
      receiptCount: 1,
      lines: [
        {
          lineId: appleLineId,
          productId: appleId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
        },
        {
          lineId: orangeLineId,
          productId: orangeId,
          productName: "Orange",
          unitOfMeasure: "pcs",
          lineNumber: 2,
          sentQty: 10,
          receivedQty: 7,
          differenceQty: 3,
          lineStatus: "Partial",
        },
      ],
      receipts: [
        {
          receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          sequence: 1,
          receivedAtUtc: "2026-09-24T11:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId: appleReceiptLineId,
              lineId: appleLineId,
              productId: appleId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Banana sent instead of Apple",
              otherFollowUp: "RequestReplacement",
              actualReceivedProductId: bananaId,
              otherCustodyDecision: "ReturnToSource",
            },
            {
              receiptLineId: orangeReceiptLineId,
              lineId: orangeLineId,
              productId: orangeId,
              quantityReceived: 7,
              quantityOther: 3,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Mango sent instead of Orange",
              otherFollowUp: "RequestReplacement",
              actualReceivedProductId: mangoId,
              otherCustodyDecision: "ReturnToSource",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          transferId,
          rootTransferId: transferId,
          receiptLineId: appleReceiptLineId,
          expectedProductId: appleId,
          actualProductId: bananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana Lakatan",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "AwaitingReturn",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-24T11:00:00Z",
          updatedAtUtc: "2026-09-24T11:00:00Z",
        },
        {
          custodyId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          transferId,
          rootTransferId: transferId,
          receiptLineId: orangeReceiptLineId,
          expectedProductId: orangeId,
          actualProductId: mangoId,
          expectedProductName: "Orange",
          actualProductName: "Mango",
          quantity: 3,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "AwaitingReturn",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 3,
          createdAtUtc: "2026-09-24T11:00:00Z",
          updatedAtUtc: "2026-09-24T11:00:00Z",
        },
      ],
      damageCustodies: [],
      familyMembers: [],
    } as never);

    const movement: PosStockMovementDto = {
      movementId: "mov-banana",
      productId: bananaId,
      inventoryAccountId: "acc-1",
      movementType: "TransferExceptionActualOut",
      quantityEffect: -5,
      reason: "Transfer exception actual out TR-260924-001",
      sourceType: "InventoryTransfer",
      sourceId: appleReceiptLineId,
      transactionType: "InventoryTransfer",
      transactionId: transferId,
      transactionReference: "TR-260924-001",
      recordedAtUtc: "2026-09-24T11:00:00Z",
      recordedBy: "actor-1",
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/banana"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={movement}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-exception-actual-item")).toHaveTextContent(
      "Banana Lakatan",
    );
    expect(screen.getByTestId("inventory-movement-exception-expected-item")).toHaveTextContent(
      "Apple",
    );
    expect(screen.getByTestId("inventory-movement-receiving-actual-item")).toHaveTextContent(
      "Banana Lakatan",
    );
    expect(screen.getByTestId("inventory-movement-receiving-note")).toHaveTextContent(
      "Banana sent instead of Apple",
    );
    expect(screen.queryByText("Mango")).not.toBeInTheDocument();
    expect(screen.queryByText("Orange")).not.toBeInTheDocument();
  });

  it("shows Expected item restored context for TransferExceptionExpectedRestore", async () => {
    const appleId = "33333333-3333-3333-3333-333333333333";
    const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const receiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260924-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-24T10:00:00Z",
      updatedAtUtc: "2026-09-24T11:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [
        {
          lineId: "22222222-2222-2222-2222-222222222222",
          productId: appleId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
        },
      ],
      receipts: [
        {
          receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          sequence: 1,
          receivedAtUtc: "2026-09-24T11:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId,
              lineId: "22222222-2222-2222-2222-222222222222",
              productId: appleId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Banana sent instead of Apple",
              actualReceivedProductId: bananaId,
              otherCustodyDecision: "ReturnToSource",
              otherFollowUp: "RequestReplacement",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          transferId,
          rootTransferId: transferId,
          receiptLineId,
          expectedProductId: appleId,
          actualProductId: bananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana Lakatan",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "AwaitingReturn",
          heldBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-24T11:00:00Z",
          updatedAtUtc: "2026-09-24T11:00:00Z",
        },
      ],
      damageCustodies: [],
      familyMembers: [],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/apple"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={{
                    movementId: "mov-restore",
                    productId: appleId,
                    inventoryAccountId: "acc-1",
                    movementType: "TransferExceptionExpectedRestore",
                    quantityEffect: 5,
                    reason: "Transfer exception expected restore TR-260924-001",
                    sourceType: "InventoryTransfer",
                    sourceId: receiptLineId,
                    transactionType: "InventoryTransfer",
                    transactionId: transferId,
                    transactionReference: "TR-260924-001",
                    recordedAtUtc: "2026-09-24T11:00:00Z",
                    recordedBy: "actor-1",
                  }}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-type-label")).toHaveTextContent(
      "Expected item restored",
    );
    expect(await screen.findByTestId("inventory-movement-exception-expected-item")).toHaveTextContent(
      "Apple",
    );
    expect(screen.getByTestId("inventory-movement-exception-actual-item")).toHaveTextContent(
      "Banana Lakatan",
    );
  });

  it("shows reverse Branch 2 → Main Branch route for ReturnOut", async () => {
    const appleId = "33333333-3333-3333-3333-333333333333";
    const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const receiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const custodyId = "12121212-1212-1212-1212-121212121212";
    const returnTransferId = "13131313-1313-1313-1313-131313131313";

    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId: returnTransferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260924-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-24T10:00:00Z",
      updatedAtUtc: "2026-09-24T12:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [
        {
          lineId: "22222222-2222-2222-2222-222222222222",
          productId: appleId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
        },
      ],
      receipts: [
        {
          receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          sequence: 1,
          receivedAtUtc: "2026-09-24T11:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId,
              lineId: "22222222-2222-2222-2222-222222222222",
              productId: appleId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Banana sent instead of Apple",
              actualReceivedProductId: bananaId,
              otherCustodyDecision: "ReturnToSource",
              otherFollowUp: "RequestReplacement",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId: returnTransferId,
          rootTransferId: returnTransferId,
          receiptLineId,
          expectedProductId: appleId,
          actualProductId: bananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana Lakatan",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "ReturnInTransit",
          heldBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-24T11:00:00Z",
          updatedAtUtc: "2026-09-24T12:00:00Z",
        },
      ],
      damageCustodies: [],
      familyMembers: [],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/banana"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={{
                    movementId: "mov-return-out",
                    productId: bananaId,
                    inventoryAccountId: "acc-1",
                    movementType: "TransferExceptionReturnOut",
                    quantityEffect: -5,
                    reason: "Transfer exception return out TR-260924-001",
                    sourceType: "InventoryTransfer",
                    sourceId: custodyId,
                    transactionType: "InventoryTransfer",
                    transactionId: returnTransferId,
                    transactionReference: "TR-260924-001",
                    recordedAtUtc: "2026-09-24T12:00:00Z",
                    recordedBy: "actor-1",
                  }}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-type-label")).toHaveTextContent(
      "Wrong item returned to source",
    );
    const route = await screen.findByTestId("inventory-movement-exception-route");
    expect(route).toHaveTextContent(/From/);
    expect(route).toHaveTextContent("Branch 2");
    expect(route).toHaveTextContent("Main Branch");
  });

  it("shows Returned to Main Branch for ReturnRestock without inspection wording", async () => {
    const appleId = "33333333-3333-3333-3333-333333333333";
    const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const receiptLineId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    const custodyId = "14141414-1414-1414-1414-141414141414";
    const restockTransferId = "15151515-1515-1515-1515-151515151515";

    vi.spyOn(transferClient, "getInventoryTransfer").mockResolvedValue({
      transferId: restockTransferId,
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260924-001",
      sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sourceBranchName: "Main Branch",
      destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      destinationBranchName: "Branch 2",
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: "actor-1",
      createdAtUtc: "2026-09-24T10:00:00Z",
      updatedAtUtc: "2026-09-24T13:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalClosedQty: 0,
      totalOutstandingQty: 0,
      totalDifferenceQty: 5,
      receiptCount: 1,
      lines: [
        {
          lineId: "22222222-2222-2222-2222-222222222222",
          productId: appleId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
        },
      ],
      receipts: [
        {
          receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          sequence: 1,
          receivedAtUtc: "2026-09-24T11:00:00Z",
          receivedBy: "actor-1",
          lines: [
            {
              receiptLineId,
              lineId: "22222222-2222-2222-2222-222222222222",
              productId: appleId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongItem",
              otherReasonNote: "Banana sent instead of Apple",
              actualReceivedProductId: bananaId,
              otherCustodyDecision: "ReturnToSource",
              otherFollowUp: "RequestReplacement",
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId: restockTransferId,
          rootTransferId: restockTransferId,
          receiptLineId,
          expectedProductId: appleId,
          actualProductId: bananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana Lakatan",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "ReceivedAtSource",
          heldBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-24T11:00:00Z",
          updatedAtUtc: "2026-09-24T13:00:00Z",
        },
      ],
      damageCustodies: [],
      familyMembers: [],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/banana"]}>
          <Routes>
            <Route
              path="/inventory/:productId"
              element={
                <InventoryMovementTransactionDrawer
                  open
                  onOpenChange={() => undefined}
                  movement={{
                    movementId: "mov-restock",
                    productId: bananaId,
                    inventoryAccountId: "acc-1",
                    movementType: "TransferExceptionReturnRestock",
                    quantityEffect: 5,
                    reason: "Wrong item return received TR-260924-001",
                    sourceType: "InventoryTransfer",
                    sourceId: custodyId,
                    transactionType: "InventoryTransfer",
                    transactionId: restockTransferId,
                    transactionReference: "TR-260924-001",
                    recordedAtUtc: "2026-09-24T13:00:00Z",
                    recordedBy: "actor-1",
                  }}
                  unitOfMeasure="Kilogram"
                  workspace={{
                    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  }}
                  resolveActor={() => ({ displayName: "Mica Uy", email: null })}
                  actorsLoading={false}
                />
              }
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("inventory-movement-type-label")).toHaveTextContent(
      "Wrong item return received",
    );
    expect(await screen.findByTestId("inventory-movement-receiving-return-status")).toHaveTextContent(
      "Returned to Main Branch",
    );
    expect(screen.queryByText(/inspection/i)).not.toBeInTheDocument();
  });
});

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

    const viewLink = screen.getByTestId("inventory-movement-view-full-transfer");
    expect(viewLink).toHaveAttribute("href", expect.stringContaining(`/inventory/transfers/${transferId}`));
    await user.click(viewLink);
    await waitFor(() => {
      expect(screen.getByTestId("transfer-detail-route")).toBeInTheDocument();
    });
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
});

import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import type { PosInventoryReservationsDto } from "@/api/pos/pos-inventory-client";
import {
  InventoryReservationsDrawer,
  isTransferCommitmentItem,
  resolveTransferRouteLabel,
} from "@/features/inventory/InventoryReservationsDrawer";

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

function baseDto(
  overrides: Partial<PosInventoryReservationsDto> = {},
): PosInventoryReservationsDto {
  return {
    productId: "prod-apple",
    productName: "Apple",
    unitOfMeasure: "Kilogram",
    onHandQuantity: 80,
    reservedQuantity: 0,
    availableQuantity: 80,
    inTransitOutboundQuantity: 0,
    inTransitInboundQuantity: 0,
    reservations: [],
    ...overrides,
  };
}

function renderDrawer(data: PosInventoryReservationsDto) {
  vi.spyOn(inventoryClient, "getInventoryProductReservations").mockResolvedValue(data);
  return render(
    <AppProviders>
      <MemoryRouter>
        <InventoryReservationsDrawer
          open
          onOpenChange={() => undefined}
          workspace={workspace}
          productId="prod-apple"
          productNameFallback="Apple"
        />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("InventoryReservationsDrawer commitments", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("classifies transfer items separately from true reservations", () => {
    expect(
      isTransferCommitmentItem({
        reservationId: "t1",
        sourceType: "InventoryTransfer",
        connectedPurchaseOrderId: "00000000-0000-0000-0000-000000000000",
        reservedQuantity: 3,
        reservationType: "TransferOutbound",
        status: "InTransit",
        branchId: workspace.branchId,
        createdAtUtc: "2026-09-24T20:29:00Z",
      }),
    ).toBe(true);
    expect(
      isTransferCommitmentItem({
        reservationId: "r1",
        sourceType: "ConnectedPurchaseOrder",
        connectedPurchaseOrderId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        reservedQuantity: 5,
        reservationType: "ConfirmedOrder",
        status: "Confirmed",
        branchId: workspace.branchId,
        createdAtUtc: "2026-09-24T20:29:00Z",
      }),
    ).toBe(false);
  });

  it("builds Main → Iloilo route for outbound and inbound", () => {
    const t = (key: string) =>
      key === "inventory.transferRoute"
        ? "{from} → {to}"
        : key === "inventory.inTransitBranchFallback"
          ? "branch"
          : key === "inventory.thisBranch"
            ? "this branch"
            : key;

    expect(
      resolveTransferRouteLabel(
        {
          reservationId: "t1",
          sourceType: "InventoryTransfer",
          connectedPurchaseOrderId: "00000000-0000-0000-0000-000000000000",
          counterpartyName: "Iloilo Branch",
          branchName: "Main Branch",
          reservedQuantity: 3,
          reservationType: "TransferOutbound",
          status: "InTransit",
          branchId: workspace.branchId,
          createdAtUtc: "2026-09-24T20:29:00Z",
        },
        t,
      ),
    ).toBe("Main Branch → Iloilo Branch");

    expect(
      resolveTransferRouteLabel(
        {
          reservationId: "t2",
          sourceType: "InventoryTransfer",
          connectedPurchaseOrderId: "00000000-0000-0000-0000-000000000000",
          counterpartyName: "Main Branch",
          branchName: "Iloilo Branch",
          reservedQuantity: 3,
          reservationType: "TransferInbound",
          status: "InTransit",
          branchId: workspace.branchId,
          createdAtUtc: "2026-09-24T20:29:00Z",
        },
        t,
      ),
    ).toBe("Main Branch → Iloilo Branch");
  });

  it("shows reserved=3 from outbound transfer when sellable reserved is 0", async () => {
    renderDrawer(
      baseDto({
        onHandQuantity: 80,
        reservedQuantity: 0,
        availableQuantity: 80,
        // Simulate older API omitting DTO transit totals while still returning transfer rows.
        inTransitOutboundQuantity: undefined,
        inTransitInboundQuantity: undefined,
        reservations: [
          {
            reservationId: "tr-id",
            sourceType: "InventoryTransfer",
            connectedPurchaseOrderId: "00000000-0000-0000-0000-000000000000",
            referenceNumber: "TR-260924-002-R1",
            counterpartyName: "Iloilo Branch",
            branchName: "Main Branch",
            reservedQuantity: 3,
            reservationType: "TransferOutbound",
            status: "InTransit",
            branchId: workspace.branchId,
            createdAtUtc: "2026-09-24T20:29:00Z",
            inventoryTransferId: "tr-id",
          },
        ],
      }),
    );

    expect(await screen.findByText("Inventory commitments")).toBeInTheDocument();
    expect(await screen.findByTestId("inventory-reservations-on-hand")).toHaveTextContent(/80/);
    expect(screen.getByTestId("inventory-reservations-reserved")).toHaveTextContent(/3 Kilogram/);
    expect(screen.getByTestId("inventory-reservations-in-transit-out")).toHaveTextContent(/3/);
    expect(screen.getByTestId("inventory-reservations-available")).toHaveTextContent(/80/);
    expect(screen.queryByTestId("inventory-commitments-reservations-group")).not.toBeInTheDocument();
    expect(screen.getByTestId("inventory-commitments-in-transit-group")).toBeInTheDocument();
    expect(screen.getByTestId("inventory-reservation-route-tr-id")).toHaveTextContent(
      "Main Branch → Iloilo Branch",
    );
    expect(screen.getByTestId("inventory-reservation-row-tr-id")).toHaveTextContent(
      /Transfer out · In transit/,
    );
  });

  it("derives reserved and in-transit out from transfer rows when DTO totals are 0", async () => {
    renderDrawer(
      baseDto({
        onHandQuantity: 80,
        reservedQuantity: 0,
        availableQuantity: 80,
        inTransitOutboundQuantity: 0,
        reservations: [
          {
            reservationId: "tr-id",
            sourceType: "InventoryTransfer",
            connectedPurchaseOrderId: "00000000-0000-0000-0000-000000000000",
            referenceNumber: "TR-260924-002-R1",
            counterpartyName: "Iloilo Branch",
            branchName: "Main Branch",
            reservedQuantity: 3,
            reservationType: "TransferOutbound",
            status: "InTransit",
            branchId: workspace.branchId,
            createdAtUtc: "2026-09-24T20:29:00Z",
            inventoryTransferId: "tr-id",
          },
        ],
      }),
    );

    expect(await screen.findByTestId("inventory-reservations-in-transit-out")).toHaveTextContent(
      /3 Kilogram/,
    );
    expect(screen.getByTestId("inventory-reservations-reserved")).toHaveTextContent(/3 Kilogram/);
  });

  it("keeps true PO reserved separate from in-transit without double-deducting available", async () => {
    renderDrawer(
      baseDto({
        onHandQuantity: 80,
        reservedQuantity: 5,
        availableQuantity: 75,
        inTransitOutboundQuantity: 3,
        reservations: [
          {
            reservationId: "res-1",
            sourceType: "ConnectedPurchaseOrder",
            connectedPurchaseOrderId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
            referenceNumber: "PO-1",
            counterpartyName: "Buyer Co",
            reservedQuantity: 5,
            reservationType: "ConfirmedOrder",
            status: "Confirmed",
            branchId: workspace.branchId,
            branchName: "Main Branch",
            createdAtUtc: "2026-09-24T18:00:00Z",
          },
          {
            reservationId: "tr-id",
            sourceType: "InventoryTransfer",
            connectedPurchaseOrderId: "00000000-0000-0000-0000-000000000000",
            referenceNumber: "TR-260924-002-R1",
            counterpartyName: "Iloilo Branch",
            branchName: "Main Branch",
            reservedQuantity: 3,
            reservationType: "TransferOutbound",
            status: "InTransit",
            branchId: workspace.branchId,
            createdAtUtc: "2026-09-24T20:29:00Z",
            inventoryTransferId: "tr-id",
          },
        ],
      }),
    );

    await waitFor(() => {
      expect(screen.getByTestId("inventory-reservations-reserved")).toHaveTextContent(/5/);
    });
    expect(screen.getByTestId("inventory-reservations-in-transit-out")).toHaveTextContent(/3/);
    expect(screen.getByTestId("inventory-reservations-available")).toHaveTextContent(/75/);
    expect(screen.getByTestId("inventory-commitments-reservations-group")).toBeInTheDocument();
    expect(screen.getByTestId("inventory-commitments-in-transit-group")).toBeInTheDocument();
  });
});

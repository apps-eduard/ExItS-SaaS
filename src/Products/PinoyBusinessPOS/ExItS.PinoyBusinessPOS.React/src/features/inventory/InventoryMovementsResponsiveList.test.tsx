import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { AppProviders } from "@/app/providers";
import { InventoryMovementsResponsiveList } from "@/features/inventory/InventoryMovementsResponsiveList";

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    workspaces: [],
    sessionGrant: null,
  }),
  useOptionalWorkspace: () => ({
    boundWorkspace: workspace,
    workspaces: [],
    sessionGrant: null,
  }),
}));

const getDirectPurchaseReceipt = vi.fn();
vi.mock("@/api/pos/pos-direct-purchase-receipts-client", () => ({
  getDirectPurchaseReceipt: (...args: unknown[]) => getDirectPurchaseReceipt(...args),
}));

function transferOutMovement(
  overrides: Partial<PosStockMovementDto> = {},
): PosStockMovementDto {
  return {
    movementId: "mov-1",
    productId: "prod-1",
    inventoryAccountId: "acc-1",
    movementType: "TransferOut",
    quantityEffect: -10,
    reason: "Transfer out TR-260922-001",
    sourceType: "InventoryTransfer",
    sourceId: "transfer-id-1",
    transactionType: "InventoryTransfer",
    transactionId: "transfer-id-1",
    transactionReference: "TR-260922-001",
    recordedAtUtc: "2026-09-22T19:07:00Z",
    recordedBy: "actor-1",
    ...overrides,
  };
}

function damageHoldMovement(): PosStockMovementDto {
  return transferOutMovement({
    movementId: "mov-damage",
    movementType: "TransferDamageHold",
    quantityEffect: 5,
    reason: "Transfer damage hold TR-260922-001 · Return to source · Replacement requested",
    sourceId: "receipt-line-id",
    transactionId: "transfer-id-1",
    transactionReference: "TR-260922-001",
  });
}

function renderList(
  movements: PosStockMovementDto[],
  onOpenMovement?: (m: PosStockMovementDto) => void,
) {
  return render(
    <MemoryRouter>
      <AppProviders>
        <InventoryMovementsResponsiveList
          movements={movements}
          unitOfMeasure="Kilogram"
          resolveActor={() => ({ displayName: "Mica Uy", email: null })}
          actorsLoading={false}
          onOpenMovement={onOpenMovement}
        />
      </AppProviders>
    </MemoryRouter>,
  );
}

describe("InventoryMovementsResponsiveList transaction open", () => {
  it("opens transaction callback when transfer number area is clicked (not reservations)", async () => {
    const onOpen = vi.fn();
    const user = userEvent.setup();
    const movement = transferOutMovement();
    renderList([movement], onOpen);

    const refs = screen.getAllByTestId(`inventory-movement-transfer-ref-${movement.movementId}`);
    await user.click(refs[0]!);
    expect(onOpen).toHaveBeenCalledTimes(1);
    expect(onOpen).toHaveBeenCalledWith(movement);
  });

  it("shows a clickable Direct Buy receipt id that navigates without opening the drawer", async () => {
    const onOpen = vi.fn();
    const user = userEvent.setup();
    const movement: PosStockMovementDto = {
      movementId: "mov-dp",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "DirectPurchaseReceipt",
      quantityEffect: 12,
      reason: "Direct purchase",
      sourceType: "DirectPurchase",
      sourceId: "receipt-guid",
      transactionType: "DirectPurchase",
      transactionId: "receipt-guid",
      transactionReference: "DP-260926-045",
      recordedAtUtc: "2026-09-26T12:00:00Z",
      recordedBy: "actor-1",
    };
    renderList([movement], onOpen);

    const links = screen.getAllByTestId(`inventory-movement-direct-purchase-ref-${movement.movementId}`);
    expect(links[0]).toHaveTextContent("DP-260926-045");
    expect(links[0]).toHaveAttribute("href", "/purchasing/direct-purchases/receipt-guid");
    await user.click(links[0]!);
    expect(onOpen).not.toHaveBeenCalled();
    expect(getDirectPurchaseReceipt).not.toHaveBeenCalled();
  });

  it("loads Direct Buy receipt number when movement omits transactionReference", async () => {
    getDirectPurchaseReceipt.mockResolvedValue({
      directPurchaseReceiptId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      organizationId: workspace.organizationId,
      receiptNumber: "DP-260926-001",
      purchaseDate: "2026-09-26",
      totalCost: 18000,
      createdByUserId: "actor-1",
      createdAtUtc: "2026-09-26T12:00:00Z",
      lines: [],
      status: "Posted",
    });
    const movement: PosStockMovementDto = {
      movementId: "mov-dp-fetch",
      productId: "prod-1",
      inventoryAccountId: "acc-1",
      movementType: "DirectPurchaseReceipt",
      quantityEffect: 100,
      reason: "Direct purchase receipt",
      sourceType: "DirectPurchase",
      sourceId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      transactionType: "DirectPurchase",
      transactionId: "752a42f2-bf73-4608-896b-8ca9832d4c8a",
      transactionReference: null,
      recordedAtUtc: "2026-09-26T12:00:00Z",
      recordedBy: "actor-1",
    };
    renderList([movement]);

    await waitFor(() => {
      expect(
        screen.getAllByTestId(`inventory-movement-direct-purchase-ref-${movement.movementId}`)[0],
      ).toHaveTextContent("DP-260926-001");
    });
    expect(getDirectPurchaseReceipt).toHaveBeenCalled();
  });

  it("opens transaction callback when empty area of the row is clicked", async () => {
    const onOpen = vi.fn();
    const user = userEvent.setup();
    const movement = transferOutMovement();
    renderList([movement], onOpen);

    // Force desktop table visibility for the row interaction.
    const row = screen.getByTestId(`inventory-movement-row-${movement.movementId}`);
    await user.click(row);
    expect(onOpen).toHaveBeenCalledTimes(1);
    expect(onOpen).toHaveBeenCalledWith(movement);
  });

  it("opens transaction callback on Enter and Space for focused row", async () => {
    const onOpen = vi.fn();
    const user = userEvent.setup();
    const movement = transferOutMovement();
    renderList([movement], onOpen);

    const row = screen.getByTestId(`inventory-movement-row-${movement.movementId}`);
    row.focus();
    await user.keyboard("{Enter}");
    expect(onOpen).toHaveBeenCalledTimes(1);
    await user.keyboard(" ");
    expect(onOpen).toHaveBeenCalledTimes(2);
  });

  it("opens transaction callback from mobile card click", async () => {
    const onOpen = vi.fn();
    const user = userEvent.setup();
    const movement = transferOutMovement();
    renderList([movement], onOpen);

    await user.click(screen.getByTestId(`inventory-movement-card-${movement.movementId}`));
    expect(onOpen).toHaveBeenCalledTimes(1);
    expect(onOpen).toHaveBeenCalledWith(movement);
  });

  it("resolves TransferDamageHold via transactionId without using receipt-line sourceId as transfer", async () => {
    const onOpen = vi.fn();
    const user = userEvent.setup();
    const movement = damageHoldMovement();
    renderList([movement], onOpen);

    expect(
      screen.getAllByTestId(`inventory-movement-damage-hold-decision-${movement.movementId}`)[0],
    ).toHaveTextContent(/Replacement requested.*Return to source/i);

    await user.click(screen.getByTestId(`inventory-movement-row-${movement.movementId}`));
    expect(onOpen).toHaveBeenCalledWith(
      expect.objectContaining({
        transactionId: "transfer-id-1",
        sourceId: "receipt-line-id",
        movementType: "TransferDamageHold",
      }),
    );
  });

  it("shows damaged return received type without bucket breakdown", () => {
    const movement = transferOutMovement({
      movementId: "mov-return-in",
      movementType: "TransferDamageReturnIn",
      quantityEffect: 5,
      reason: "Transfer damage return in TR-260922-001",
    });
    renderList([movement]);

    expect(screen.queryByTestId("inventory-movement-bucket-effects")).not.toBeInTheDocument();
    expect(screen.getAllByText(/Damaged return received/i)[0]).toBeInTheDocument();
  });

  it("renders Sellable after tag in qty area on cards and table", () => {
    const movement = transferOutMovement({
      sellableBefore: 100,
      sellableDelta: -10,
      sellableAfter: 90,
    });
    renderList([movement]);

    const tags = screen.getAllByTestId("inventory-movement-sellable-after");
    expect(tags.length).toBeGreaterThanOrEqual(1);
    for (const tag of tags) {
      expect(tag).toHaveTextContent(/Sellable after:\s*90\s*Kilogram/);
    }
  });

  it("still shows Sellable after when sellable delta is zero", () => {
    const movement = transferOutMovement({
      movementId: "mov-hold",
      movementType: "TransferDamageHold",
      quantityEffect: 5,
      sellableBefore: 45,
      sellableDelta: 0,
      sellableAfter: 45,
    });
    renderList([movement]);

    const tags = screen.getAllByTestId("inventory-movement-sellable-after");
    expect(tags[0]).toHaveTextContent(/Sellable after:\s*45\s*Kilogram/);
  });

  it("colors negative qty red and positive qty green", () => {
    const negative = transferOutMovement({
      movementId: "mov-neg",
      quantityEffect: -5,
    });
    const positive = transferOutMovement({
      movementId: "mov-pos",
      quantityEffect: 5,
      movementType: "TransferIn",
      reason: "Transfer in TR-260922-001",
    });
    renderList([negative, positive]);

    const negQty = screen.getAllByTestId(`inventory-movement-qty-${negative.movementId}`)[0]!;
    const posQty = screen.getAllByTestId(`inventory-movement-qty-${positive.movementId}`)[0]!;
    expect(negQty).toHaveTextContent(/-5\s*Kilogram/);
    expect(negQty.className).toMatch(/exits-danger/);
    expect(posQty).toHaveTextContent(/\+5\s*Kilogram/);
    expect(posQty.className).toMatch(/exits-success/);
  });
});

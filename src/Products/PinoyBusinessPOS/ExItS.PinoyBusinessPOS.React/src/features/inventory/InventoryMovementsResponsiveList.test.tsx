import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { AppProviders } from "@/app/providers";
import { InventoryMovementsResponsiveList } from "@/features/inventory/InventoryMovementsResponsiveList";

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
    <AppProviders>
      <InventoryMovementsResponsiveList
        movements={movements}
        unitOfMeasure="Kilogram"
        resolveActor={() => ({ displayName: "Mica Uy", email: null })}
        actorsLoading={false}
        onOpenMovement={onOpenMovement}
      />
    </AppProviders>,
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

  it("shows inspection-hold bucket effects for damaged return received", () => {
    const movement = transferOutMovement({
      movementId: "mov-return-in",
      movementType: "TransferDamageReturnIn",
      quantityEffect: 5,
      reason: "Transfer damage return in TR-260922-001",
    });
    renderList([movement]);

    const effects = screen.getAllByTestId("inventory-movement-bucket-effects")[0]!;
    expect(effects).toHaveTextContent(/Physical:\s*\+5/);
    expect(effects).toHaveTextContent(/Sellable:\s*0/);
    expect(effects).toHaveTextContent(/Inspection hold:\s*\+5/);
    expect(screen.getAllByText(/Damaged return received/i)[0]).toBeInTheDocument();
  });
});

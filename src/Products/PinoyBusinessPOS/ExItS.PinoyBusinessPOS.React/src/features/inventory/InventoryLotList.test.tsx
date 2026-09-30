import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { InventoryLotList } from "@/features/inventory/InventoryLotList";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

function wrap(ui: React.ReactElement) {
  return render(
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>,
  );
}

function lot(partial: Partial<PosInventoryLotDto> & Pick<PosInventoryLotDto, "lotId">): PosInventoryLotDto {
  return {
    productId: "p1",
    branchId: "b1",
    lotNumber: null,
    expirationDate: "2026-12-31",
    quantityOnHand: 50,
    expiryStatus: "Ok",
    createdAtUtc: "2026-09-27T00:00:00Z",
    updatedAtUtc: "2026-09-27T00:00:00Z",
    canEditIdentity: false,
    identityLockReason: "Used",
    ...partial,
  };
}

describe("InventoryLotList identity actions", () => {
  it("shows edit for pristine editable lot on desktop and mobile", async () => {
    const onEdit = vi.fn();
    const editable = lot({
      lotId: "lot-edit",
      canEditIdentity: true,
      identityLockReason: "None",
    });

    wrap(
      <InventoryLotList
        lots={[editable]}
        unitOfMeasure="kg"
        formatStatus={() => "Good"}
        onEditLotIdentity={onEdit}
      />,
    );

    const buttons = screen.getAllByTestId("lot-edit-identity-lot-edit");
    expect(buttons.length).toBeGreaterThanOrEqual(1);
    await userEvent.click(buttons[0]!);
    expect(onEdit).toHaveBeenCalledWith(editable);
  });

  it("shows lock and no edit for used blank lot", () => {
    const onEdit = vi.fn();
    wrap(
      <InventoryLotList
        lots={[
          lot({
            lotId: "lot-used",
            lotNumber: null,
            canEditIdentity: false,
            identityLockReason: "Used",
          }),
        ]}
        unitOfMeasure="kg"
        formatStatus={() => "Good"}
        onEditLotIdentity={onEdit}
      />,
    );

    expect(screen.queryByTestId("lot-edit-identity-lot-used")).toBeNull();
    expect(screen.getAllByTestId("lot-identity-locked-lot-used").length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/already been used/i).length).toBeGreaterThanOrEqual(1);
  });

  it("shows active draft lock helper", () => {
    wrap(
      <InventoryLotList
        lots={[
          lot({
            lotId: "lot-draft",
            canEditIdentity: false,
            identityLockReason: "ActiveTransferDraft",
          }),
        ]}
        unitOfMeasure="kg"
        formatStatus={() => "Good"}
        onEditLotIdentity={() => undefined}
      />,
    );

    expect(screen.getAllByText(/active transfer draft/i).length).toBeGreaterThanOrEqual(1);
  });
});

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { EditLotIdentityDialog } from "@/features/inventory/EditLotIdentityDialog";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

function wrap(ui: React.ReactElement) {
  return render(
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>,
  );
}

const sampleLot: PosInventoryLotDto = {
  lotId: "lot-1",
  productId: "p1",
  branchId: "b1",
  lotNumber: null,
  expirationDate: "2026-12-31",
  quantityOnHand: 50,
  expiryStatus: "Ok",
  createdAtUtc: "2026-09-27T00:00:00Z",
  updatedAtUtc: "2026-09-27T12:00:00Z",
  canEditIdentity: true,
  identityLockReason: "None",
};

describe("EditLotIdentityDialog", () => {
  it("requires reason and saves current values", async () => {
    const onSave = vi.fn();
    const user = userEvent.setup();

    wrap(
      <EditLotIdentityDialog
        open
        lot={sampleLot}
        productName="Apple"
        locationName="Iloilo Branch"
        unitOfMeasure="kg"
        busy={false}
        onCancel={() => undefined}
        onSave={onSave}
      />,
    );

    expect(screen.getByTestId("edit-lot-identity-expiry")).toHaveValue("2026-12-31");
    expect(screen.getByText(/50 kg/)).toBeInTheDocument();

    const save = screen.getByTestId("edit-lot-identity-save");
    expect(save).toBeDisabled();

    await user.type(screen.getByTestId("edit-lot-identity-batch"), "LOT-A123");
    await user.type(screen.getByTestId("edit-lot-identity-reason"), "Corrected supplier label");
    expect(save).toBeEnabled();
    await user.click(save);

    expect(onSave).toHaveBeenCalledWith({
      expirationDate: "2026-12-31",
      lotNumber: "LOT-A123",
      reason: "Corrected supplier label",
      expectedUpdatedAtUtc: "2026-09-27T12:00:00Z",
    });
  });

  it("shows identity conflict error", () => {
    wrap(
      <EditLotIdentityDialog
        open
        lot={sampleLot}
        productName="Apple"
        locationName="Iloilo Branch"
        unitOfMeasure="kg"
        busy={false}
        errorMessage="A lot with this expiration date and batch/lot number already exists at this location."
        onCancel={() => undefined}
        onSave={() => undefined}
      />,
    );

    expect(screen.getByRole("alert")).toHaveTextContent(/already exists/i);
  });
});

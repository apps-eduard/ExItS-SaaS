import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { EnableExpirationTrackingDialog } from "@/features/inventory/EnableExpirationTrackingDialog";

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};
const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    sessionGrant: { capabilities: ["Inventory.View", "Inventory.Manage"] },
  }),
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

function renderDialog(onHandQuantity = 40) {
  const onClose = vi.fn();
  const onSuccess = vi.fn();
  render(
    <AppProviders>
      <EnableExpirationTrackingDialog
        open
        workspace={workspace}
        productId={productId}
        productName="Milk 1L"
        onHandQuantity={onHandQuantity}
        unitOfMeasure="Piece"
        expirationWarningDays={7}
        onClose={onClose}
        onSuccess={onSuccess}
      />
    </AppProviders>,
  );
  return { onClose, onSuccess };
}

describe("EnableExpirationTrackingDialog", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "enableExpirationTracking").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 40,
      lots: [],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("defaults first row quantity to on-hand and disables submit when under-allocated", async () => {
    const user = userEvent.setup();
    renderDialog(40);
    await screen.findByTestId("enable-expiration-tracking-dialog");

    expect(screen.getByTestId("enable-expiration-qty-0")).toHaveValue("40");
    expect(screen.getByTestId("enable-expiration-allocated")).toHaveTextContent("40");
    expect(screen.getByTestId("enable-expiration-submit")).toBeDisabled();

    await user.clear(screen.getByTestId("enable-expiration-qty-0"));
    await user.type(screen.getByTestId("enable-expiration-qty-0"), "30");
    await user.type(screen.getByTestId("enable-expiration-expiry-0"), "2027-12-01");

    expect(screen.getByTestId("enable-expiration-allocated")).toHaveTextContent("30");
    expect(screen.getByTestId("enable-expiration-remaining")).toHaveTextContent("10");
    expect(screen.getByTestId("enable-expiration-submit")).toBeDisabled();
  });

  it("enables submit on exact allocation and posts expected payload", async () => {
    const user = userEvent.setup();
    const { onSuccess } = renderDialog(40);
    await screen.findByTestId("enable-expiration-tracking-dialog");

    await user.clear(screen.getByTestId("enable-expiration-qty-0"));
    await user.type(screen.getByTestId("enable-expiration-qty-0"), "25");
    await user.type(screen.getByTestId("enable-expiration-expiry-0"), "2027-01-15");

    await user.click(screen.getByTestId("enable-expiration-add-row"));
    await user.type(screen.getByTestId("enable-expiration-qty-1"), "15");
    await user.type(screen.getByTestId("enable-expiration-expiry-1"), "2027-06-01");
    await user.type(screen.getByTestId("enable-expiration-lot-1"), "LOT-B");

    expect(screen.getByTestId("enable-expiration-submit")).toBeEnabled();
    await user.click(screen.getByTestId("enable-expiration-submit"));

    await waitFor(() =>
      expect(inventoryClient.enableExpirationTracking).toHaveBeenCalledWith(
        workspace,
        productId,
        expect.objectContaining({
          expectedOnHandQuantity: 40,
          expirationWarningDays: 7,
          existingStockLots: [
            { quantity: 25, expiryDate: "2027-01-15", lotNumber: null },
            { quantity: 15, expiryDate: "2027-06-01", lotNumber: "LOT-B" },
          ],
        }),
      ),
    );
    expect(onSuccess).toHaveBeenCalled();
  });
});

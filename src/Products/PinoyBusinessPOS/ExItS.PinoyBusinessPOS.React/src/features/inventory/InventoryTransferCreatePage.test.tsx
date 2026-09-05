import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { PosApiError } from "@/api/pos/pos-http";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import * as transferClient from "@/api/pos/pos-inventory-transfer-client";
import { InventoryTransferCreatePage } from "@/features/inventory/InventoryTransferCreatePage";
import {
  canAddTransferQuantity,
  evaluateTransferLineStock,
} from "@/features/inventory/inventory-transfer-stock-guard";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const mainId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const branchBId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const soapId = "11111111-1111-1111-1111-111111111111";
const zeroId = "22222222-2222-2222-2222-222222222222";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Store",
    branchId: mainId,
    branchName: "Main Branch",
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
          name: "Main Branch",
          secondaryLine: "",
          isPrimary: true,
          isActive: true,
        },
        {
          branchId: branchBId,
          name: "Iloilo Branch",
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

function account(
  productId: string,
  name: string,
  onHandQuantity: number,
): inventoryClient.PosInventoryAccountDto {
  return {
    productId,
    organizationId: orgId,
    name,
    unitOfMeasure: "Piece",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity,
    stockStatus: onHandQuantity > 0 ? "InStock" : "OutOfStock",
    isLowStock: false,
    createdAtUtc: "2026-08-29T08:00:00Z",
    updatedAtUtc: "2026-08-29T08:00:00Z",
    tracksExpiration: false,
  };
}

function renderCreate() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/inventory/transfers/new"]}>
        <Routes>
          <Route path="/inventory/transfers/new" element={<InventoryTransferCreatePage />} />
          <Route path="/inventory/transfers/:transferId" element={<div>detail</div>} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("inventory-transfer-stock-guard helpers", () => {
  it("blocks over-stock and allows exact stock", () => {
    expect(
      canAddTransferQuantity({
        quantity: 11,
        availableQuantity: 10,
        lotAvailableQuantity: null,
        tracksExpiration: false,
        existingProductDemand: 0,
        existingLotDemand: 0,
      }),
    ).toBe("over_stock");
    expect(
      canAddTransferQuantity({
        quantity: 10,
        availableQuantity: 10,
        lotAvailableQuantity: null,
        tracksExpiration: false,
        existingProductDemand: 0,
        existingLotDemand: 0,
      }),
    ).toBeNull();
  });

  it("aggregates product demand across lines", () => {
    const lines = [
      {
        key: "a",
        productId: soapId,
        quantity: 7,
        sourceLotId: "lot-a",
        availableQuantity: 10,
        lotAvailableQuantity: 7,
        tracksExpiration: true,
        isTracked: true,
      },
      {
        key: "b",
        productId: soapId,
        quantity: 7,
        sourceLotId: "lot-b",
        availableQuantity: 10,
        lotAvailableQuantity: 7,
        tracksExpiration: true,
        isTracked: true,
      },
    ];
    expect(evaluateTransferLineStock(lines[1]!, lines)).toBe("over_stock");
  });
});

describe("InventoryTransferCreatePage stock guard", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [account(soapId, "Bath Soap Bar", 10), account(zeroId, "Zero Stock Item", 0)],
      totalCount: 2,
      page: 1,
      pageSize: 40,
    });
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows source availability and blocks zero-stock add", async () => {
    const user = userEvent.setup();
    renderCreate();

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-picker-available-${soapId}`)).toHaveTextContent(
        /Available:\s*10\s*Piece/,
      );
    });
    expect(screen.getByTestId(`transfer-picker-available-${zeroId}`)).toHaveTextContent(/Out of stock/i);
    expect(screen.getByTestId(`transfer-picker-unavailable-${zeroId}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`transfer-add-${zeroId}`)).not.toBeInTheDocument();

    await user.selectOptions(screen.getByTestId("transfer-destination-branch"), branchBId);
    await user.type(screen.getByTestId(`transfer-picker-qty-${soapId}`), "10");
    await user.click(screen.getByTestId(`transfer-add-${soapId}`));

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-${soapId}:none`)).toBeInTheDocument();
    });
    expect(screen.getByTestId("transfer-save-draft")).not.toBeDisabled();
  });

  it("rejects manual over-entry and disables create", async () => {
    const user = userEvent.setup();
    renderCreate();
    await waitFor(() => screen.getByTestId(`transfer-add-${soapId}`));

    await user.selectOptions(screen.getByTestId("transfer-destination-branch"), branchBId);
    await user.type(screen.getByTestId(`transfer-picker-qty-${soapId}`), "11");
    await user.click(screen.getByTestId(`transfer-add-${soapId}`));

    await waitFor(() => {
      expect(screen.getByTestId("transfer-create-error")).toHaveTextContent(
        /Only 10 Piece available at Main Branch/i,
      );
    });
    expect(screen.queryByTestId(`transfer-line-${soapId}:none`)).not.toBeInTheDocument();
    expect(screen.getByTestId("transfer-save-draft")).toBeDisabled();
  });

  it("allows exact available quantity on a line", async () => {
    const user = userEvent.setup();
    renderCreate();
    await waitFor(() => screen.getByTestId(`transfer-add-${soapId}`));
    await user.selectOptions(screen.getByTestId("transfer-destination-branch"), branchBId);
    await user.type(screen.getByTestId(`transfer-picker-qty-${soapId}`), "10");
    await user.click(screen.getByTestId(`transfer-add-${soapId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${soapId}:none`));

    const line = screen.getByTestId(`transfer-line-${soapId}:none`);
    expect(within(line).getByTestId(`transfer-line-available-${soapId}:none`)).toHaveTextContent(
      /Available:\s*10\s*Piece/,
    );
    expect(screen.getByTestId("transfer-save-draft")).not.toBeDisabled();
  });

  it("keeps lines and shows backend insufficient-stock error", async () => {
    const user = userEvent.setup();
    vi.spyOn(transferClient, "createInventoryTransfer").mockRejectedValue(
      new PosApiError(409, {
        title: "Conflict",
        status: 409,
        detail: "Bath Soap Bar has only 6 Piece available at Main Branch. Requested: 10.",
        errorCode: "pos.inventory.insufficient_stock",
      }),
    );

    renderCreate();
    await waitFor(() => screen.getByTestId(`transfer-add-${soapId}`));
    await user.selectOptions(screen.getByTestId("transfer-destination-branch"), branchBId);
    await user.type(screen.getByTestId(`transfer-picker-qty-${soapId}`), "10");
    await user.click(screen.getByTestId(`transfer-add-${soapId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${soapId}:none`));
    await user.click(screen.getByTestId("transfer-save-draft"));

    await waitFor(() => {
      expect(screen.getByTestId("transfer-create-error")).toHaveTextContent(/only 6 Piece/i);
    });
    expect(screen.getByTestId(`transfer-line-${soapId}:none`)).toBeInTheDocument();
  });
});

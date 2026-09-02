import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { InventoryDetailPage } from "@/features/inventory/InventoryDetailPage";

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const workspaceScope = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

const workspace = {
  ...workspaceScope,
  branchName: "Kalibo Branch",
  organizationDisplayName: "mica store",
};

function baseAccount(extra: Record<string, unknown> = {}) {
  return {
    productId,
    organizationId: workspace.organizationId,
    name: "Milk 1L",
    unitOfMeasure: "Piece",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity: 40,
    hasOpeningStock: true,
    stockStatus: "InStock",
    isLowStock: false,
    tracksExpiration: false,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...extra,
  };
}

const lotA = {
  lotId: "11111111-1111-1111-1111-111111111111",
  productId,
  lotNumber: "LOT-A1",
  expirationDate: "2026-09-05",
  quantityOnHand: 8,
  expiryStatus: "NearExpiry",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

const lotB = {
  lotId: "22222222-2222-2222-2222-222222222222",
  productId,
  lotNumber: null,
  expirationDate: "2026-12-30",
  quantityOnHand: 20,
  expiryStatus: "Ok",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    workspaces: [
      {
        organizationId: workspace.organizationId,
        displayName: "mica store",
        branches: [
          {
            branchId: workspace.branchId,
            name: "Kalibo Branch",
            secondaryLine: "",
            isPrimary: true,
            isActive: true,
          },
          {
            branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
            name: "Iloilo Branch",
            secondaryLine: "",
            isPrimary: false,
            isActive: true,
          },
        ],
      },
    ],
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    },
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

vi.mock("@/lib/secure-mutation-id", () => ({
  createSecureMutationId: () => ({ ok: true, id: "99999999-9999-9999-9999-999999999999" }),
}));

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/inventory/${productId}`]}>
        <Routes>
          <Route path="/inventory/:productId" element={<InventoryDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

async function expandStatusDetails() {
  const toggle = await screen.findByTestId("inventory-status-toggle");
  if (toggle.getAttribute("aria-expanded") !== "true") {
    await userEvent.click(toggle);
  }
  await screen.findByTestId("inventory-status-details");
}

describe("InventoryDetailPage expiration UX", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue(baseAccount() as never);
    vi.spyOn(inventoryClient, "getOrganizationInventorySummary").mockResolvedValue({
      productId,
      productName: "Milk 1L",
      unitOfMeasure: "Piece",
      organizationOnHandQuantity: 85,
      organizationReservedQuantity: 4,
      organizationAvailableQuantity: 81,
      branches: [
        {
          branchId: workspace.branchId,
          onHandQuantity: 40,
          reservedQuantity: 4,
          availableQuantity: 36,
        },
        {
          branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          onHandQuantity: 45,
          reservedQuantity: 0,
          availableQuantity: 45,
        },
      ],
    });
    vi.spyOn(inventoryClient, "listInventoryMovements").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "adjustInventoryStock").mockResolvedValue(baseAccount() as never);
    vi.spyOn(inventoryClient, "disableInventoryTracking").mockResolvedValue(baseAccount() as never);
    vi.spyOn(catalogClient, "getCatalogProduct").mockResolvedValue({
      productId,
      name: "Milk 1L",
      unitOfMeasure: "Piece",
      sellingPrice: 55,
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);
    vi.spyOn(catalogClient, "updateCatalogProduct").mockResolvedValue({} as never);
    vi.spyOn(inventoryClient, "enableExpirationTracking").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      tracksExpiration: true,
      isTracked: true,
      onHandQuantity: 40,
      lots: [],
    } as never);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("keeps simple adjustment UI when expiration tracking is off", async () => {
    renderPage();
    await screen.findByTestId("inventory-detail-page");
    expect(screen.getByTestId("inventory-enable-expiration")).toBeInTheDocument();
    expect(screen.queryByTestId("inventory-adjust-expiry")).not.toBeInTheDocument();
    expect(screen.queryByTestId("inventory-expiration-summary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("inventory-lots")).not.toBeInTheDocument();
    expect(screen.getByTestId("inventory-adjust-form")).toBeInTheDocument();
  });

  it("shows organization inventory aggregate and branch breakdown", async () => {
    renderPage();
    await expandStatusDetails();
    await screen.findByTestId("inventory-organization-summary");
    expect(screen.getByTestId("inventory-org-on-hand")).toHaveTextContent("85");
    expect(screen.getByTestId("inventory-org-reserved")).toHaveTextContent("4");
    expect(screen.getByTestId("inventory-org-available")).toHaveTextContent("81");
    expect(screen.getByTestId("inventory-branch-breakdown")).toBeInTheDocument();
    expect(
      screen.getByTestId(`inventory-branch-row-${workspace.branchId}`),
    ).toHaveTextContent("Kalibo Branch");
    expect(
      screen.getByTestId("inventory-branch-row-cccccccc-cccc-cccc-cccc-cccccccccccc"),
    ).toHaveTextContent("Iloilo Branch");
    expect(screen.queryByText(/bbbbbbbb/i)).not.toBeInTheDocument();
  });

  it("shows expiry-required increase form and summary when tracking is on", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({
        tracksExpiration: true,
        expirationWarningDays: 7,
        sellableQuantity: 30,
        nearExpiryQuantity: 8,
        expiredQuantity: 2,
      }) as never,
    );
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [lotB, lotA],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });

    renderPage();
    await screen.findByTestId("inventory-expiration-summary");
    expect(screen.getByTestId("inventory-expiry-totals")).toHaveTextContent("Good");
    expect(screen.getByTestId("inventory-expiry-totals")).toHaveTextContent("22");
    expect(screen.getByTestId("inventory-expiry-totals")).toHaveTextContent("8");
    expect(screen.getByTestId("inventory-expiry-totals")).toHaveTextContent("2");
    await waitFor(() => {
      expect(screen.getByTestId("inventory-lots")).toBeInTheDocument();
      expect(
        screen.getAllByTestId("inventory-lot-11111111-1111-1111-1111-111111111111").length,
      ).toBeGreaterThan(0);
    });
    expect(screen.getByTestId("inventory-adjust-expiry")).toBeInTheDocument();

    const lots = screen.getByTestId("inventory-lots");
    const adjust = screen.getByTestId("inventory-adjust-form");
    expect(lots.compareDocumentPosition(adjust) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("links to expiration settings when enabling expiration tracking", async () => {
    renderPage();
    const enable = await screen.findByTestId("inventory-enable-expiration");
    expect(enable).toHaveAttribute("href", `/inventory/${productId}/expiration`);
    expect(inventoryClient.enableExpirationTracking).not.toHaveBeenCalled();
  });

  it("links to expiration settings when on-hand is zero", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ onHandQuantity: 0, hasOpeningStock: true }) as never,
    );
    renderPage();
    const enable = await screen.findByTestId("inventory-enable-expiration");
    expect(enable).toHaveAttribute("href", `/inventory/${productId}/expiration`);
    expect(inventoryClient.enableExpirationTracking).not.toHaveBeenCalled();
  });

  it("requires expiry on increase and refreshes after successful adjust", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ tracksExpiration: true, expirationWarningDays: 7 }) as never,
    );
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("inventory-adjust-expiry");

    await user.type(screen.getByLabelText(/Quantity/i), "24");
    await user.type(screen.getByLabelText(/Reason/i), "Stock count correction");
    await user.click(screen.getByTestId("inventory-adjust"));
    expect(await screen.findByText(/Expiration date is required/i)).toBeInTheDocument();

    await user.type(screen.getByTestId("inventory-adjust-expiry"), "2027-12-30");
    await user.click(screen.getByTestId("inventory-adjust"));

    await waitFor(() =>
      expect(inventoryClient.adjustInventoryStock).toHaveBeenCalledWith(
        workspaceScope,
        productId,
        expect.objectContaining({
          direction: "In",
          quantity: 24,
          expirationDate: "2027-12-30",
        }),
      ),
    );
  });

  it("hides expiry input on decrease and supports automatic FEFO mode", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ tracksExpiration: true }) as never,
    );
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [lotA],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("inventory-adjust-form");
    await user.click(screen.getByLabelText(/Decrease \(Out\)/i));
    expect(screen.getByTestId("inventory-deduct-auto")).toBeChecked();
    expect(screen.queryByTestId("inventory-adjust-expiry")).not.toBeInTheDocument();

    await user.type(screen.getByLabelText(/^Quantity/i), "5");
    await user.type(screen.getByLabelText(/Reason/i), "Sample out");
    await user.click(screen.getByTestId("inventory-adjust"));

    await waitFor(() =>
      expect(inventoryClient.adjustInventoryStock).toHaveBeenCalledWith(
        workspaceScope,
        productId,
        expect.objectContaining({
          direction: "Out",
          quantity: 5,
          lotId: null,
        }),
      ),
    );
  });

  it("requires lot selection in manual decrease mode", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ tracksExpiration: true }) as never,
    );
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [lotA],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("inventory-adjust-form");
    await user.click(screen.getByLabelText(/Decrease \(Out\)/i));
    await user.click(screen.getByTestId("inventory-deduct-manual"));
    await user.type(screen.getByLabelText(/Quantity/i), "5");
    await user.type(screen.getByLabelText(/Reason/i), "Expired");
    await user.click(screen.getByTestId("inventory-adjust"));
    expect(await screen.findByText(/Select a lot/i)).toBeInTheDocument();
  });

  it("links manage expiration to settings page and hides disable on detail", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ tracksExpiration: true, onHandQuantity: 12, expirationWarningDays: 7 }) as never,
    );
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [lotA],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();
    const manage = await screen.findByTestId("inventory-manage-expiration");
    expect(manage).toHaveAttribute("href", `/inventory/${productId}/expiration?focus=warning`);
    await expandStatusDetails();
    expect(screen.getByTestId("inventory-expiration-status")).toHaveTextContent(
      /Expiration tracking ON · 7-day warning/i,
    );
    expect(screen.queryByTestId("inventory-disable-expiration")).not.toBeInTheDocument();
  });

  it("shows setup required banner when tracking is on without lots", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ tracksExpiration: true, onHandQuantity: 12, expirationWarningDays: 7 }) as never,
    );
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    renderPage();
    await expandStatusDetails();
    await screen.findByTestId("inventory-expiration-setup-required");
    expect(screen.getByTestId("inventory-expiration-setup-assign")).toHaveAttribute(
      "href",
      `/inventory/${productId}/expiration?focus=assign`,
    );
    expect(screen.getByTestId("inventory-manage-expiration")).toHaveAttribute(
      "href",
      `/inventory/${productId}/expiration?focus=warning`,
    );
    expect(screen.getByTestId("inventory-expiration-pending")).toBeInTheDocument();
    expect(screen.queryByTestId("inventory-expiry-totals")).not.toBeInTheDocument();
    expect(screen.queryByText(/No lots on hand yet/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("enable-expiration-tracking-dialog")).not.toBeInTheDocument();
  });

  it("disables inventory disable tracking when on hand is positive", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ onHandQuantity: 12, isTracked: true }) as never,
    );
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [lotA],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();
    const disable = await screen.findByTestId("inventory-disable");
    expect(disable).toBeDisabled();
  });

  it("shows add opening stock panel when tracked without opening movement", async () => {
    vi.mocked(inventoryClient.getInventoryProduct).mockResolvedValue(
      baseAccount({ onHandQuantity: 0, hasOpeningStock: false }) as never,
    );
    renderPage();
    await screen.findByTestId("inventory-add-opening-stock");
    expect(screen.queryByTestId("inventory-adjust-form")).not.toBeInTheDocument();
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { ExpirationSettingsPage } from "@/features/inventory/ExpirationSettingsPage";

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
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
    tracksExpiration: true,
    expirationWarningDays: 7,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...extra,
  };
}

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

function renderPage(initialEntry = `/inventory/${productId}/expiration`) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/inventory/:productId/expiration" element={<ExpirationSettingsPage />} />
          <Route path="/inventory/:productId" element={<div data-testid="inventory-detail-stub" />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("ExpirationSettingsPage", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue(baseAccount() as never);
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [
        {
          lotId: "11111111-1111-1111-1111-111111111111",
          productId,
          lotNumber: "LOT-A",
          expirationDate: "2026-12-30",
          quantityOnHand: 40,
          expiryStatus: "Ok",
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders ON state with warning controls and manage route chrome", async () => {
    renderPage();
    await screen.findByTestId("expiration-settings-page");
    expect(screen.getByTestId("expiration-settings-tracking-status")).toHaveTextContent(
      /Expiration tracking ON/i,
    );
    expect(screen.getByTestId("expiration-settings-warning-days")).toBeInTheDocument();
    expect(screen.getByTestId("expiration-settings-save")).toBeInTheDocument();
    expect(screen.getByTestId("expiration-settings-disable")).toBeInTheDocument();
    expect(screen.getByTestId("expiration-settings-view-lots")).toHaveAttribute(
      "href",
      `/inventory/${productId}`,
    );
    expect(screen.queryByTestId("expiration-settings-repair-banner")).not.toBeInTheDocument();
  });

  it("shows repair banner when tracking is ON with on-hand and no lots", async () => {
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });

    renderPage();
    await screen.findByTestId("expiration-settings-repair-banner");
    expect(screen.getByText(/Expiration setup required/i)).toBeInTheDocument();
    expect(screen.getByTestId("assign-expiration-lots-form")).toBeInTheDocument();
    expect(screen.queryByTestId("expiration-settings-repair")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("expiration-settings-save")).toBeDisabled();
    });
  });

  it("highlights repair banner when opened with assign focus", async () => {
    vi.mocked(inventoryClient.listProductLots).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });

    renderPage(`/inventory/${productId}/expiration?focus=assign`);
    const banner = await screen.findByTestId("expiration-settings-repair-banner");
    expect(banner).toHaveAttribute("data-highlighted", "true");
  });

  it("highlights warning card when opened with warning focus", async () => {
    renderPage(`/inventory/${productId}/expiration?focus=warning`);
    const card = await screen.findByTestId("expiration-settings-warning-card");
    expect(card).toHaveAttribute("data-highlighted", "true");
  });
});

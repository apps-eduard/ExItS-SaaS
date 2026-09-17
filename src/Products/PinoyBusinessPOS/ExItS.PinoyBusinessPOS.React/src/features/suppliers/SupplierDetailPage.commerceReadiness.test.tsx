import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { SupplierDetailPage } from "@/features/suppliers/SupplierDetailPage";
import {
  getBuyerConnectedSupplierCommerceReadiness,
  listRelationships,
} from "@/api/pos/pos-connected-suppliers-client";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const supplierId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const relationshipId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Kizy Store",
    branchId,
    branchName: "Main Branch",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  } as Record<string, unknown>,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/features/suppliers/SupplierCreditSection", () => ({
  SupplierCreditSection: () => null,
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

vi.mock("@/api/pos/pos-suppliers-client", () => ({
  getSupplier: vi.fn(async () => ({
    supplierId,
    organizationId: orgId,
    supplierCode: "SUP0001",
    name: "Fresh Farms",
    status: "Active",
    connectionType: "Connected",
    contactPerson: null,
    mobileNumber: null,
    telephoneNumber: null,
    email: null,
    addressLine1: null,
    addressLine2: null,
    cityMunicipality: null,
    province: null,
    postalCode: null,
    taxOrRegistrationNumber: null,
    notes: null,
    connectedRelationshipId: relationshipId,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    supplierBranchName: "Main Branch",
    connectedBusinessPublicId: "ORG999999",
  })),
  activateSupplier: vi.fn(),
  deactivateSupplier: vi.fn(),
  isConnectedSupplier: () => true,
}));

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  listRelationships: vi.fn(async () => [
    {
      relationshipId,
      buyerOrganizationId: orgId,
      supplierOrganizationId: "22222222-2222-2222-2222-222222222222",
      status: "Active",
      requestedAtUtc: "2026-08-01T00:00:00Z",
      requestedByUserId: null,
      respondedAtUtc: "2026-08-01T01:00:00Z",
      respondedByUserId: null,
      disconnectedAtUtc: null,
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T01:00:00Z",
      counterpartyDisplayName: "Fresh Farms",
      counterpartyPublicOrganizationId: "ORG999999",
      catalogSharingMode: "SelectedOnly",
      customerDiscountPercent: null,
      supplierBranchId: branchId,
      supplierBranchName: "Main Branch",
    },
  ]),
  isRelationshipActive: (r: { status: string }) => r.status.trim().toLowerCase() === "active",
  isRelationshipPending: (r: { status: string }) => r.status.trim().toLowerCase() === "pending",
  cancelConnectionRequest: vi.fn(),
  updateSupplierLocation: vi.fn(),
  getBuyerConnectedSupplierCommerceReadiness: vi.fn(),
}));

const readinessMock = vi.mocked(getBuyerConnectedSupplierCommerceReadiness);
const listRelationshipsMock = vi.mocked(listRelationships);

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/suppliers/${supplierId}`]}>
        <Routes>
          <Route path="/suppliers/:supplierId" element={<SupplierDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("SupplierDetailPage commerce readiness", () => {
  beforeEach(() => {
    readinessMock.mockReset();
    listRelationshipsMock.mockClear();
  });

  it("shows ready status near supplier name when supplier is ready", async () => {
    readinessMock.mockResolvedValue({
      relationshipId,
      isReady: true,
      supportedFulfillmentMethods: ["Pickup", "Delivery"],
      requirements: null,
      blockerCategories: [],
    });

    renderPage();

    expect(await screen.findByTestId("supplier-ready-for-po")).toHaveTextContent(
      /Ready for purchase orders/i,
    );
    expect(screen.queryByTestId("supplier-not-ready-for-po-banner")).not.toBeInTheDocument();
    const createPo = await screen.findByTestId("supplier-create-purchase-order");
    expect(createPo).toHaveAttribute("href", `/purchasing/new?supplierId=${supplierId}`);
    expect(screen.queryByText(/Selling \/ fulfillment branch/i)).not.toBeInTheDocument();
  });

  it("shows fulfillment category messaging and never internal checklist details", async () => {
    readinessMock.mockResolvedValue({
      relationshipId,
      isReady: false,
      supportedFulfillmentMethods: ["Pickup"],
      requirements: null,
      blockerCategories: ["Fulfillment"],
    });

    renderPage();

    const banner = await screen.findByTestId("supplier-not-ready-for-po-banner");
    expect(banner).toHaveTextContent(/Supplier not ready for purchase orders/i);
    expect(banner).toHaveTextContent(/fulfillment setup/i);
    expect(banner).toHaveTextContent(/Issue:\s*Fulfillment setup/i);
    expect(screen.queryByText(/Responsible contact/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Shared catalog/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/SellingBranch|PickupConfig|DeliveryConfig/i)).not.toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId("supplier-create-purchase-order")).toBeDisabled();
    });
  });

  it("dismisses banner without enabling Create PO", async () => {
    readinessMock.mockResolvedValue({
      relationshipId,
      isReady: false,
      supportedFulfillmentMethods: [],
      requirements: null,
      blockerCategories: ["Payment", "Catalog"],
    });

    renderPage();

    const banner = await screen.findByTestId("supplier-not-ready-for-po-banner");
    expect(banner).toHaveTextContent(/fulfillment|payment|catalog/i);
    expect(screen.getByTestId("supplier-not-ready-for-po-banner-issues")).toHaveTextContent(
      /Issues:\s*Payment · Catalog/i,
    );

    await userEvent.click(screen.getByTestId("supplier-not-ready-for-po-banner-dismiss"));
    expect(screen.queryByTestId("supplier-not-ready-for-po-banner")).not.toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId("supplier-create-purchase-order")).toBeDisabled();
    });
  });
});

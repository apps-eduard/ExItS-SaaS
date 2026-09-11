import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { SupplierDetailPage } from "@/features/suppliers/SupplierDetailPage";
import {
  cancelConnectionRequest,
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

const { getSupplierMock } = vi.hoisted(() => ({
  getSupplierMock: vi.fn(async () => ({
    supplierId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    organizationId: "11111111-1111-1111-1111-111111111111",
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
    connectedRelationshipId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    supplierBranchName: "Main Branch",
    connectedBusinessPublicId: "ORG999999",
  })),
}));

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

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  listRelationships: vi.fn(async () => [
    {
      relationshipId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      buyerOrganizationId: "11111111-1111-1111-1111-111111111111",
      supplierOrganizationId: "22222222-2222-2222-2222-222222222222",
      status: "Pending",
      requestedAtUtc: "2026-08-01T00:00:00Z",
      requestedByUserId: null,
      respondedAtUtc: null,
      respondedByUserId: null,
      disconnectedAtUtc: null,
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
      counterpartyDisplayName: "Buyer Co",
      counterpartyPublicOrganizationId: "ORG000001",
      catalogSharingMode: "SelectedOnly",
      customerDiscountPercent: null,
      supplierBranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      supplierBranchName: "Main Branch",
    },
  ]),
  isRelationshipActive: (r: { status: string }) => r.status.trim().toLowerCase() === "active",
  isRelationshipPending: (r: { status: string }) => r.status.trim().toLowerCase() === "pending",
  cancelConnectionRequest: vi.fn(),
  updateSupplierLocation: vi.fn(),
}));

vi.mock("@/api/pos/pos-suppliers-client", () => ({
  getSupplier: getSupplierMock,
  activateSupplier: vi.fn(),
  deactivateSupplier: vi.fn(),
  isConnectedSupplier: (supplier: { connectionType: string }) =>
    supplier.connectionType.trim().toLowerCase() === "connectedorganization" ||
    supplier.connectionType.trim().toLowerCase() === "connected",
}));

const cancelConnectionRequestMock = vi.mocked(cancelConnectionRequest);
const listRelationshipsMock = vi.mocked(listRelationships);

describe("SupplierDetailPage: cancel connection request", () => {
  it("shows Cancel request for Pending and calls cancel endpoint", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);

    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/suppliers/${supplierId}`]}>
          <Routes>
            <Route path="/suppliers/:supplierId" element={<SupplierDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const cancelButton = await screen.findByTestId("supplier-cancel-request");
    expect(cancelButton).toBeInTheDocument();

    await user.click(cancelButton);

    expect(cancelConnectionRequestMock).toHaveBeenCalledTimes(1);
    expect(cancelConnectionRequestMock.mock.calls[0][1]).toBe(relationshipId);
  });

  it("links Create new purchase order to new PO with this supplier selected", async () => {
    listRelationshipsMock.mockResolvedValueOnce([
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
        counterpartyDisplayName: "Mica Grocery",
        counterpartyPublicOrganizationId: "ORG742108",
        catalogSharingMode: "SelectedOnly",
        customerDiscountPercent: null,
        supplierBranchId: branchId,
        supplierBranchName: "Main Branch",
      },
    ]);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/suppliers/${supplierId}`]}>
          <Routes>
            <Route path="/suppliers/:supplierId" element={<SupplierDetailPage />} />
            <Route path="/purchasing/new" element={<div data-testid="po-create-page">new po</div>} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const createPo = await screen.findByTestId("supplier-create-purchase-order");
    expect(createPo).toHaveAttribute("href", `/purchasing/new?supplierId=${supplierId}`);
  });
});

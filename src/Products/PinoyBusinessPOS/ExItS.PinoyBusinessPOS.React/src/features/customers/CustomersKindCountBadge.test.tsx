import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as connectedClient from "@/api/pos/pos-connected-suppliers-client";
import * as customersClient from "@/api/pos/pos-customers-client";
import { CustomersListPage } from "@/features/customers/CustomersListPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const connectionId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const buyerOrgId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Paul Supply",
    branchId,
    branchName: "Main Branch",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  },
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/workspace/use-pos-workspace-scope", () => ({
  usePosWorkspaceScope: () => ({
    organizationId: orgId,
    branchId,
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

vi.mock("@/features/customers/use-organization-customer-link-overlay", () => ({
  useOrganizationCustomerLinkOverlay: () => ({
    connectedBusinessCustomerIds: new Set(),
    pendingBusinessCustomerIds: new Set(),
    isLoading: false,
  }),
}));

vi.mock("@/api/platform/business-customer-delivery-client", () => ({
  listOrganizationBusinessCustomers: vi.fn(async () => ({ items: [] })),
}));

describe("Customers kind CountBadge", () => {
  beforeEach(() => {
    vi.spyOn(customersClient, "listCustomers").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(connectedClient, "listBusinessCustomers").mockResolvedValue([
      {
        connectionId,
        supplierOrganizationId: orgId,
        buyerOrganizationId: buyerOrgId,
        organizationDisplayName: "Kizy Fruits",
        organizationPublicId: "ORG400597",
        relationshipStatus: "Active",
        catalogSharingMode: "SelectedOnly",
        initiatedByParty: "Supplier",
        customerDiscountPercent: 0,
        eligibleCount: 20,
        sharedCount: 20,
        excludedCount: 0,
        overrideCount: 0,
        connectedSinceUtc: "2026-08-01T00:00:00Z",
        createdAtUtc: "2026-08-01T00:00:00Z",
        updatedAtUtc: "2026-08-01T00:00:00Z",
        displayNameIsLive: false,
        actionRequired: false,
        supplierBranchId: null,
        supplierBranchName: null,
        contactSource: "Custom",
        organizationMemberId: null,
        organizationMemberAvailable: null,
        contactPersonName: null,
        contactDepartment: null,
        contactRole: null,
        contactPhone: null,
        contactEmail: null,
        preferredContactMethod: null,
        deliveryInstructions: null,
        billingContactNotes: null,
        internalNotes: null,
        customerDeliveryOverride: "inherit",
        orgOfferDelivery: false,
        effectiveDeliveryAllowed: false,
      },
    ]);
    vi.spyOn(connectedClient, "listRelationships").mockResolvedValue([]);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders kind filters as label + CountBadge including zero", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/customers"]}>
          <Routes>
            <Route path="/customers" element={<CustomersListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("customers-kind-filters")).toBeInTheDocument();

    const all = await screen.findByTestId("customers-kind-all");
    const people = screen.getByTestId("customers-kind-people");
    const businesses = screen.getByTestId("customers-kind-businesses");

    expect(all).toHaveAccessibleName("All, 1");
    expect(people).toHaveAccessibleName("People, 0");
    expect(businesses).toHaveAccessibleName("Businesses, 1");

    expect(within(all).getByText("All")).toBeInTheDocument();
    expect(within(all).getByText("1").closest("[data-shape]")).toHaveAttribute("data-shape", "pill");
    expect(within(people).getByText("0")).toBeInTheDocument();
    expect(within(businesses).getByText("1")).toBeInTheDocument();

    // Label text must not embed the number (All 1).
    expect(within(all).getByText("All").textContent).toBe("All");
    expect(within(people).getByText("People").textContent).toBe("People");
    expect(screen.getByTestId("customers-kind-deactivated")).toBeInTheDocument();
  });

  it("lists deactivated customers and reactivates one", async () => {
    const customerId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    vi.mocked(customersClient.listCustomers).mockImplementation(async (_workspace, options) => ({
      items:
        options?.status === "Inactive"
          ? [
              {
                customerId,
                organizationId: orgId,
                displayName: "Ana Inactive",
                status: "Inactive",
                createdAtUtc: "2026-08-01T00:00:00Z",
                updatedAtUtc: "2026-08-01T00:00:00Z",
                onlineOrderingAccess: "Default",
              },
            ]
          : [],
      totalCount: options?.status === "Inactive" ? 1 : 0,
      page: 1,
      pageSize: 50,
    }));
    vi.spyOn(customersClient, "reactivateCustomer").mockResolvedValue({
      customerId,
      organizationId: orgId,
      displayName: "Ana Inactive",
      status: "Active",
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
      onlineOrderingAccess: "Default",
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/customers"]}>
          <Routes>
            <Route path="/customers" element={<CustomersListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    fireEvent.click(await screen.findByTestId("customers-kind-deactivated"));

    expect(await screen.findByTestId(`customer-row-${customerId}`)).toHaveTextContent("Ana Inactive");
    expect(screen.getByTestId(`customer-reactivate-${customerId}`)).toHaveTextContent("Reactivate");
    expect(screen.queryByTestId("customers-people-section")).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId(`customer-reactivate-${customerId}`));

    await waitFor(() => {
      expect(customersClient.reactivateCustomer).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: orgId }),
        customerId,
      );
    });
  });
});

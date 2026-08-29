import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as connectedClient from "@/api/pos/pos-connected-suppliers-client";
import { BusinessCustomerDetailPage } from "@/features/customers/BusinessCustomerDetailPage";
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

function businessCustomer(
  overrides: Partial<connectedClient.BusinessCustomer> = {},
): connectedClient.BusinessCustomer {
  return {
    connectionId,
    supplierOrganizationId: orgId,
    buyerOrganizationId: buyerOrgId,
    organizationDisplayName: "Kizy Mini Store",
    organizationPublicId: "ORGKIZY01",
    relationshipStatus: "Active",
    catalogSharingMode: "SelectedOnly",
    customerDiscountPercent: 5,
    eligibleCount: 10,
    sharedCount: 4,
    excludedCount: 0,
    overrideCount: 1,
    connectedSinceUtc: "2026-08-01T00:00:00Z",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    displayNameIsLive: false,
    ...overrides,
  };
}

describe("Business Customer identity display", () => {
  beforeEach(() => {
    vi.spyOn(connectedClient, "listBusinessCustomers").mockResolvedValue([businessCustomer()]);
    vi.spyOn(connectedClient, "getBusinessCustomer").mockResolvedValue(businessCustomer());
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("list and detail show the same snapshot organization name and public id", async () => {
    const listView = render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/customers?kind=businesses`]}>
          <Routes>
            <Route path="/customers" element={<CustomersListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId(`business-customer-name-${connectionId}`)).toHaveTextContent(
      "Kizy Mini Store",
    );
    listView.unmount();

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/customers/business/${connectionId}`]}>
          <Routes>
            <Route path="/customers/business/:connectionId" element={<BusinessCustomerDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("business-customer-display-name")).toHaveTextContent(
      "Kizy Mini Store",
    );
    expect(screen.getByTestId("business-customer-public-id")).toHaveTextContent("ORGKIZY01");
    expect(screen.getByTestId("page-header-back-customers")).toHaveAttribute(
      "href",
      "/customers?kind=businesses",
    );
  });

  it("does not invent a live rename on detail when API returns snapshot", async () => {
    vi.spyOn(connectedClient, "getBusinessCustomer").mockResolvedValue(
      businessCustomer({
        organizationDisplayName: "Kizy Mini Store",
        displayNameIsLive: false,
      }),
    );

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/customers/business/${connectionId}`]}>
          <Routes>
            <Route path="/customers/business/:connectionId" element={<BusinessCustomerDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("business-customer-display-name")).toHaveTextContent(
      "Kizy Mini Store",
    );
    expect(screen.queryByText("Kizy Wholesale Trading")).not.toBeInTheDocument();
  });
});

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

vi.mock("@/api/pos/pos-customer-branch-access-client", () => ({
  listCustomerBranchAccess: vi.fn(async () => ({
    customerId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    homeBranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    items: [
      {
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        grantSource: "CreateAtBranch",
        grantedAtUtc: "2026-08-01T00:00:00Z",
      },
    ],
  })),
  grantCustomerBranchAccess: vi.fn(),
  revokeCustomerBranchAccess: vi.fn(),
  listBusinessCustomerBranchAccess: vi.fn(async () => ({
    customerId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    homeBranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    items: [
      {
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        grantSource: "CreateAtBranch",
        grantedAtUtc: "2026-08-01T00:00:00Z",
      },
    ],
  })),
  grantBusinessCustomerBranchAccess: vi.fn(),
  revokeBusinessCustomerBranchAccess: vi.fn(),
}));

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchManagementSummaries: vi.fn(async () => [
    {
      id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      name: "Main Branch",
      areaId: null,
      areaName: null,
      status: "Active",
    },
  ]),
}));

vi.mock("@/api/platform/organization-areas-client", () => ({
  listOrganizationAreas: vi.fn(async () => ({ areas: [] })),
}));

vi.mock("@/api/pos/pos-business-credit-policy-client", () => ({
  getBusinessCustomerCreditPolicy: vi.fn(async () => ({
    connectionId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    sellerOrganizationId: "11111111-1111-1111-1111-111111111111",
    buyerOrganizationId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    status: "NotConfigured",
    creditLimit: null,
    defaultTermDays: null,
    outstandingAmount: 0,
    availableCredit: 0,
    configuredByUserId: null,
    configuredAtUtc: null,
    approvedByUserId: null,
    approvedAtUtc: null,
    updatedByUserId: null,
    updatedAtUtc: null,
    expectedUpdatedAtUtc: null,
  })),
  listBusinessCustomerCreditPolicyHistory: vi.fn(async () => ({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
  })),
  upsertBusinessCustomerCreditPolicy: vi.fn(),
  approveBusinessCustomerCreditPolicy: vi.fn(),
  disableBusinessCustomerCreditPolicy: vi.fn(),
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
    initiatedByParty: "Supplier",
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

    expect(await screen.findByTestId("business-org-name")).toHaveTextContent("Kizy Mini Store");
    expect(screen.getByTestId("business-org-exits-id")).toHaveTextContent("ORGKIZY01");
    expect(screen.getByTestId("business-customer-status-chips")).toBeInTheDocument();
    expect(await screen.findByTestId("business-credit-policy-section")).toBeInTheDocument();
    expect(await screen.findByTestId("business-credit-policy-status")).toHaveTextContent(
      "Not configured",
    );
    expect(screen.getByTestId("business-credit-policy-repay")).toHaveAttribute(
      "href",
      `/customers/business/${connectionId}/repay`,
    );
    expect(screen.getByTestId("business-credit-policy-statement")).toHaveAttribute(
      "href",
      `/customers/business/${connectionId}/statement`,
    );
    expect(screen.getByTestId("page-header-back-customers")).toHaveAttribute(
      "href",
      "/customers?kind=businesses",
    );
    const manageCatalog = screen.getByTestId("business-customer-manage-catalog");
    expect(manageCatalog).toHaveAttribute(
      "href",
      `/suppliers/connected/buyers/${connectionId}/shared-products`,
    );
    expect(screen.getByTestId("business-org-information")).toBeInTheDocument();
    expect(screen.getByTestId("business-org-managed-by")).toBeInTheDocument();
    expect(screen.getByTestId("business-relationship-contact-empty")).toBeInTheDocument();
    expect(screen.getByTestId("business-add-relationship-contact")).toBeInTheDocument();
  });

  it("pending detail still allows relationship contact edit without Connected since", async () => {
    vi.spyOn(connectedClient, "getBusinessCustomer").mockResolvedValue(
      businessCustomer({
        relationshipStatus: "Pending",
        connectedSinceUtc: null,
        contactPersonName: "Juan",
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

    expect(await screen.findByTestId("business-customer-pending-banner")).toBeInTheDocument();
    expect(screen.queryByTestId("business-customer-connected-since")).not.toBeInTheDocument();
    expect(screen.getByTestId("business-relationship-contact")).toBeInTheDocument();
    expect(screen.getByTestId("business-contact-person")).toHaveTextContent("Juan");
    expect(screen.getByTestId("business-edit-relationship-contact")).toBeInTheDocument();
    expect(screen.queryByTestId("business-add-relationship-contact")).not.toBeInTheDocument();
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

    expect(await screen.findByTestId("business-org-name")).toHaveTextContent("Kizy Mini Store");
    expect(screen.queryByText("Kizy Wholesale Trading")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-customer-identity")).not.toBeInTheDocument();
  });

  it("pending detail never shows Connected since and shows request-sent copy", async () => {
    vi.spyOn(connectedClient, "getBusinessCustomer").mockResolvedValue(
      businessCustomer({
        relationshipStatus: "Pending",
        connectedSinceUtc: null,
        createdAtUtc: "2026-09-14T05:18:00Z",
        initiatedByParty: "Supplier",
        actionRequired: false,
        supplierBranchId: branchId,
        supplierBranchName: "Main Branch",
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

    expect(await screen.findByTestId("business-customer-pending-banner")).toBeInTheDocument();
    expect(screen.queryByTestId("business-customer-connected-since")).not.toBeInTheDocument();
    expect(screen.queryByText(/Connected organization/i)).not.toBeInTheDocument();
    expect(screen.getByTestId("business-customer-request-sent-at")).toBeInTheDocument();
    expect(await screen.findByTestId("business-customer-branch-access")).toBeInTheDocument();
    expect(screen.getByTestId("business-customer-branch-access-mode-this_branch")).toBeInTheDocument();
    expect(screen.getByTestId("business-customer-branch-access-mode-selected")).toBeInTheDocument();
    expect(screen.getByTestId("business-customer-branch-access-mode-all")).toBeInTheDocument();
    expect(screen.getByTestId("business-customer-branch-access-save")).toBeInTheDocument();
  });

  it("connected detail shows Connected since timestamp", async () => {
    vi.spyOn(connectedClient, "getBusinessCustomer").mockResolvedValue(
      businessCustomer({
        relationshipStatus: "Active",
        connectedSinceUtc: "2026-08-01T00:00:00Z",
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

    expect(await screen.findByTestId("business-customer-connected-since")).toBeInTheDocument();
    expect(screen.getByTestId("business-org-information")).toContainElement(
      screen.getByTestId("business-customer-status-chips"),
    );
    expect(screen.getByTestId("business-org-information")).toContainElement(
      screen.getByTestId("business-customer-connected-since"),
    );
    expect(screen.queryByTestId("business-customer-pending-banner")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-customer-identity")).not.toBeInTheDocument();
  });
});

import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { catalogs } from "@/i18n/messages";
import { BusinessCustomerDetailPage } from "@/features/customers/BusinessCustomerDetailPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const connectionId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Seller Co",
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

vi.mock("@/workspace/use-pos-workspace-scope", () => ({
  usePosWorkspaceScope: () => ({
    organizationId: orgId,
    branchId,
  }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

vi.mock("@/hooks/usePreferences", () => ({
  usePreferences: () => ({ preferences: { locale: "en-PH" } }),
}));

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}));

const getBusinessCustomer = vi.fn();
const getSupplierConnectedSupplierCommerceReadiness = vi.fn();
const getBusinessCustomerUtangSummary = vi.fn();

vi.mock("@/api/pos/pos-connected-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-connected-suppliers-client")>();
  return {
    ...actual,
    getBusinessCustomer: (...args: unknown[]) => getBusinessCustomer(...args),
    getSupplierConnectedSupplierCommerceReadiness: (...args: unknown[]) =>
      getSupplierConnectedSupplierCommerceReadiness(...args),
    getBusinessCustomerUtangSummary: (...args: unknown[]) => getBusinessCustomerUtangSummary(...args),
    cancelConnectionRequest: vi.fn(),
  };
});

vi.mock("@/api/platform/organization-b2b-public-profile-client", () => ({
  getOrganizationB2bPublicProfile: vi.fn(async () => null),
  formatPublicBusinessAddress: () => null,
}));

vi.mock("@/features/customers/CustomerBranchVisibilitySection", () => ({
  CustomerBranchVisibilitySection: () => null,
}));

vi.mock("@/features/customers/BusinessCreditPolicySection", () => ({
  BusinessCreditPolicySection: () => null,
}));

vi.mock("@/features/customers/BusinessRelationshipContactEditDrawer", () => ({
  BusinessRelationshipContactEditDrawer: () => null,
}));

vi.mock("@/features/customers/RecordPaymentModal", () => ({
  RecordPaymentModal: () => null,
}));

function renderPage() {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, networkMode: "always" },
    },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[`/customers/business/${connectionId}`]}>
        <Routes>
          <Route path="/customers/business/:connectionId" element={<BusinessCustomerDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BusinessCustomerDetailPage supplier readiness", () => {
  beforeEach(() => {
    getBusinessCustomer.mockReset();
    getSupplierConnectedSupplierCommerceReadiness.mockReset();
    getBusinessCustomerUtangSummary.mockReset();
    getBusinessCustomerUtangSummary.mockResolvedValue({
      outstandingAmount: 0,
      availableCredit: 0,
      creditLimit: 0,
      status: "NotConfigured",
    });
    getBusinessCustomer.mockResolvedValue({
      connectionId,
      supplierOrganizationId: orgId,
      buyerOrganizationId: "22222222-2222-2222-2222-222222222222",
      organizationDisplayName: "Buyer Bakery",
      organizationPublicId: "ORG622085",
      relationshipStatus: "Active",
      catalogSharingMode: "SelectedOnly",
      customerDiscountPercent: null,
      eligibleCount: 10,
      sharedCount: 0,
      excludedCount: 0,
      overrideCount: 0,
      connectedSinceUtc: "2026-08-01T00:00:00Z",
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
      supplierBranchId: branchId,
      supplierBranchName: "Main Branch",
      contactSource: "Custom",
      contactPersonName: null,
      contactPhone: null,
      contactEmail: null,
    });
  });

  it("shows Supplier Readiness checklist with missing requirements and Complete setup", async () => {
    getSupplierConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId: connectionId,
      isReady: false,
      supportedFulfillmentMethods: [],
      requirements: [
        {
          code: "SharedCatalog",
          status: "Missing",
          title: "Shared catalog",
          detail: "Share at least one product with this business customer.",
        },
        {
          code: "ResponsibleContact",
          status: "Missing",
          title: "Responsible contact",
          detail: "Set a contact person, phone, or email for this connection.",
        },
        {
          code: "CreditPolicy",
          status: "NotApplicable",
          title: "Utang credit policy",
          detail: null,
        },
      ],
    });

    renderPage();

    expect(await screen.findByTestId("business-customer-commerce-readiness")).toBeInTheDocument();
    expect(screen.getByTestId("business-customer-commerce-readiness-status")).toHaveTextContent(
      /Setup required/i,
    );
    expect(screen.getByTestId("commerce-readiness-SharedCatalog")).toHaveAttribute(
      "data-status",
      "Missing",
    );
    expect(screen.getByTestId("commerce-readiness-ResponsibleContact")).toBeInTheDocument();
    expect(screen.queryByTestId("commerce-readiness-CreditPolicy")).not.toBeInTheDocument();
    expect(screen.getByTestId("business-customer-complete-setup")).toBeInTheDocument();
  });
});

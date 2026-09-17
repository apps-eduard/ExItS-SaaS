import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
          <Route
            path="/suppliers/connected/buyers/:relationshipId/shared-products"
            element={<div data-testid="shared-products-page" />}
          />
          <Route
            path="/org/branches/:branchId/fulfillment"
            element={<div data-testid="fulfillment-page" />}
          />
          <Route path="/org/payment-methods" element={<div data-testid="payment-methods-page" />} />
          <Route path="/org/branches/:branchId" element={<div data-testid="branch-settings-page" />} />
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

  it("defaults to Needs setup, shows counts, Fix n items, and deep-links rows", async () => {
    const user = userEvent.setup();
    getSupplierConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId: connectionId,
      isReady: false,
      supportedFulfillmentMethods: [],
      requirements: [
        {
          code: "SellingBranch",
          status: "Complete",
          title: "Selling/fulfillment location",
          detail: "Choose the branch that fulfills purchase orders for this connection.",
        },
        {
          code: "FulfillmentMethod",
          status: "Missing",
          title: "Fulfillment methods",
          detail: "Enable Delivery and/or Pickup on the selling branch.",
        },
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
    expect(screen.getByTestId("commerce-readiness-filter-needsSetup")).toHaveTextContent(
      /Needs setup \(3\)/i,
    );
    expect(screen.getByTestId("commerce-readiness-filter-complete")).toHaveTextContent(/Complete \(1\)/i);
    expect(screen.getByTestId("commerce-readiness-filter-all")).toHaveTextContent(/All \(4\)/i);

    const needsSetupRadio = screen.getByRole("radio", { name: /Needs setup \(3\)/i });
    expect(needsSetupRadio).toHaveAttribute("aria-checked", "true");

    expect(screen.getByTestId("commerce-readiness-FulfillmentMethod")).toBeInTheDocument();
    expect(screen.getByTestId("commerce-readiness-SharedCatalog")).toBeInTheDocument();
    expect(screen.queryByTestId("commerce-readiness-SellingBranch")).not.toBeInTheDocument();
    expect(screen.queryByTestId("commerce-readiness-CreditPolicy")).not.toBeInTheDocument();

    const fixAction = screen.getByTestId("business-customer-complete-setup");
    expect(fixAction).toHaveTextContent("Fix 3 items");
    expect(fixAction).toHaveAttribute("href", `/org/branches/${branchId}/fulfillment`);

    expect(screen.getByTestId("commerce-readiness-SharedCatalog")).toHaveAttribute(
      "href",
      `/suppliers/connected/buyers/${connectionId}/shared-products`,
    );
    expect(screen.getByTestId("commerce-readiness-FulfillmentMethod")).toHaveAttribute(
      "href",
      `/org/branches/${branchId}/fulfillment`,
    );

    await user.click(screen.getByRole("radio", { name: /Complete \(1\)/i }));
    const completeRow = screen.getByTestId("commerce-readiness-SellingBranch");
    expect(completeRow).toBeInTheDocument();
    expect(completeRow).toHaveAttribute("href", `/org/branches/${branchId}`);
    expect(screen.queryByTestId("commerce-readiness-SharedCatalog")).not.toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: /All \(4\)/i }));
    const checklist = screen.getByTestId("business-customer-commerce-readiness-checklist");
    expect(within(checklist).getByTestId("commerce-readiness-SellingBranch")).toBeInTheDocument();
    expect(within(checklist).getByTestId("commerce-readiness-SharedCatalog")).toBeInTheDocument();
    expect(within(checklist).queryByTestId("commerce-readiness-CreditPolicy")).not.toBeInTheDocument();

    await user.click(within(checklist).getByTestId("commerce-readiness-SellingBranch"));
    expect(await screen.findByTestId("branch-settings-page")).toBeInTheDocument();
  });

  it("hides Supplier Readiness card when status is ready", async () => {
    getSupplierConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId: connectionId,
      isReady: true,
      supportedFulfillmentMethods: ["Pickup"],
      requirements: [
        {
          code: "SellingBranch",
          status: "Complete",
          title: "Selling/fulfillment location",
          detail: "Choose the branch that fulfills purchase orders for this connection.",
        },
        {
          code: "PaymentMethods",
          status: "Complete",
          title: "Accepted payment methods",
          detail: "Enable at least one payment method buyers can use on purchase orders.",
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

    expect(await screen.findByTestId("business-customer-detail")).toBeInTheDocument();
    expect(screen.queryByTestId("business-customer-commerce-readiness")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-customer-complete-setup")).not.toBeInTheDocument();
  });
});

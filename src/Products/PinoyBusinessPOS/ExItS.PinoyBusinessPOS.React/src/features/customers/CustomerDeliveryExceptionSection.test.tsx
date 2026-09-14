import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { CustomerDeliveryExceptionSection } from "@/features/customers/CustomerDeliveryExceptionSection";
import { CustomerDetailPage } from "@/features/customers/CustomerDetailPage";
import * as deliveryClient from "@/api/platform/business-customer-delivery-client";
import * as customersClient from "@/api/pos/pos-customers-client";

vi.mock("@/access/pos-capabilities", () => ({
  canEditCustomer: () => true,
  canRecordRepayment: () => false,
  canViewStatement: () => false,
  canManageCustomerCreditPolicy: () => false,
  canApproveCustomerCreditPolicy: () => false,
  canManageCustomerBranchAccess: () => false,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: { organizationId: "11111111-1111-1111-1111-111111111111" },
    sessionGrant: { productRole: "Owner" },
  }),
}));

vi.mock("@/workspace/use-pos-workspace-scope", () => ({
  usePosWorkspaceScope: () => ({
    organizationId: "11111111-1111-1111-1111-111111111111",
    branchId: "22222222-2222-2222-2222-222222222222",
  }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/offline/organization-offline-context", () => ({
  useOrganizationOfflineContext: () => null,
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({ actorsById: new Map() }),
}));

vi.mock("@/features/customers/CreditPolicySection", () => ({
  CreditPolicySection: () => null,
}));

vi.mock("@/features/customers/CustomerBranchVisibilitySection", () => ({
  CustomerBranchVisibilitySection: () => null,
}));

vi.mock("@/features/customers/CustomerStoreDetailsEditDrawer", () => ({
  CustomerStoreDetailsEditDrawer: () => null,
}));

vi.mock("@/api/platform/public-identity-client", () => ({
  resolvePublicUserId: vi.fn().mockResolvedValue({
    displayName: "John Dela Cruz",
    maskedEmail: null,
  }),
}));

vi.mock("@/api/pos/pos-customers-client", () => ({
  getCustomer: vi.fn(),
  getCustomerCreditSummary: vi.fn().mockResolvedValue({
    customerId: "33333333-3333-3333-3333-333333333333",
    organizationId: "11111111-1111-1111-1111-111111111111",
    outstandingAmount: 0,
    activeEntryCount: 0,
    totalEntryCount: 0,
  }),
  listCustomerCreditEntries: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
  listCustomerRepayments: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
  deactivateCustomer: vi.fn(),
  reactivateCustomer: vi.fn(),
}));

vi.mock("@/api/platform/customer-link-status-client", () => ({
  getCustomerLinkStatus: vi.fn().mockResolvedValue({
    businessCustomerId: "44444444-4444-4444-4444-444444444444",
    organizationId: "11111111-1111-1111-1111-111111111111",
    status: "Linked",
  }),
  listCustomerLinkRequestHistory: vi.fn().mockResolvedValue([]),
  remindCustomerLinkRequest: vi.fn(),
  revokeCustomerLinkRequest: vi.fn(),
  createCustomerLinkRequestForCustomer: vi.fn(),
}));

vi.mock("@/api/platform/business-customer-delivery-client", () => ({
  getOrganizationBusinessCustomer: vi.fn(),
  updateBusinessCustomerDeliveryPreferences: vi.fn(),
  listOrganizationBusinessCustomers: vi.fn(),
}));

const customerId = "33333333-3333-3333-3333-333333333333";
const platformCustomerId = "44444444-4444-4444-4444-444444444444";

describe("CustomerDeliveryExceptionSection", () => {
  it("toggles via onToggle", () => {
    const onToggle = vi.fn();
    render(
      <dl>
        <CustomerDeliveryExceptionSection
          allowBeyond={false}
          canEdit
          pending={false}
          t={(key) => key}
          onToggle={onToggle}
        />
      </dl>,
    );
    fireEvent.click(screen.getByTestId("customer-delivery-distance-exception"));
    expect(onToggle).toHaveBeenCalledWith(true);
  });
});

describe("CustomerDetailPage delivery placement", () => {
  it("renders Delivery card inside Store customer details", async () => {
    vi.mocked(customersClient.getCustomer).mockResolvedValue({
      customerId,
      organizationId: "11111111-1111-1111-1111-111111111111",
      displayName: "John Dela Cruz",
      mobileNumber: null,
      address: null,
      notes: null,
      status: "Active",
      platformBusinessCustomerId: platformCustomerId,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      linkedPersonalPublicUserId: "EXITS-1",
    } as never);
    vi.mocked(deliveryClient.getOrganizationBusinessCustomer).mockResolvedValue({
      id: platformCustomerId,
      organizationId: "11111111-1111-1111-1111-111111111111",
      displayName: "John Dela Cruz",
      status: "Active",
      allowDeliveryBeyondNormalDistance: false,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);

    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/customers/${customerId}`]}>
          <Routes>
            <Route path="/customers/:customerId" element={<CustomerDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    const section = await screen.findByTestId("customer-delivery-section");
    await waitFor(() => {
      expect(screen.getByTestId("customer-store-details")).toContainElement(section);
    });
  });
});

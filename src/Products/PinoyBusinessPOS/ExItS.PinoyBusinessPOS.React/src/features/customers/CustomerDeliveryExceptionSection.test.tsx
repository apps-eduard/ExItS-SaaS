import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { CustomerDetailPage } from "@/features/customers/CustomerDetailPage";

const getOrganizationBusinessCustomer = vi.fn();
const updateBusinessCustomerDeliveryPreferences = vi.fn();
const getCustomer = vi.fn();

vi.mock("@/access/pos-capabilities", () => ({
  canEditCustomer: () => true,
  canRecordRepayment: () => false,
  canViewStatement: () => false,
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

vi.mock("@/api/pos/pos-customers-client", () => ({
  getCustomer: (...args: unknown[]) => getCustomer(...args),
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
  getOrganizationBusinessCustomer: (...args: unknown[]) =>
    getOrganizationBusinessCustomer(...args),
  updateBusinessCustomerDeliveryPreferences: (...args: unknown[]) =>
    updateBusinessCustomerDeliveryPreferences(...args),
}));

const customerId = "33333333-3333-3333-3333-333333333333";
const platformCustomerId = "44444444-4444-4444-4444-444444444444";

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[`/customers/${customerId}`]}>
        <Routes>
          <Route path="/customers/:customerId" element={<CustomerDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("CustomerDetailPage delivery exception", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getCustomer.mockResolvedValue({
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
    });
    getOrganizationBusinessCustomer.mockResolvedValue({
      id: platformCustomerId,
      organizationId: "11111111-1111-1111-1111-111111111111",
      displayName: "John Dela Cruz",
      status: "Active",
      allowDeliveryBeyondNormalDistance: false,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    });
  });

  it("shows Delivery section default OFF and toggles ON via API", async () => {
    const user = userEvent.setup();
    updateBusinessCustomerDeliveryPreferences.mockResolvedValue({
      id: platformCustomerId,
      organizationId: "11111111-1111-1111-1111-111111111111",
      displayName: "John Dela Cruz",
      status: "Active",
      allowDeliveryBeyondNormalDistance: true,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    });

    renderPage();
    expect(await screen.findByTestId("customer-delivery-section")).toBeInTheDocument();
    const toggle = await screen.findByTestId("customer-delivery-distance-exception");
    expect(toggle).toHaveAttribute("aria-checked", "false");

    await user.click(toggle);
    await waitFor(() => {
      expect(updateBusinessCustomerDeliveryPreferences).toHaveBeenCalledWith(
        "11111111-1111-1111-1111-111111111111",
        platformCustomerId,
        true,
      );
    });
  });

  it("keeps OFF when update fails", async () => {
    const user = userEvent.setup();
    updateBusinessCustomerDeliveryPreferences.mockRejectedValue(new Error("denied"));
    renderPage();
    const toggle = await screen.findByTestId("customer-delivery-distance-exception");
    await user.click(toggle);
    await waitFor(() => {
      expect(updateBusinessCustomerDeliveryPreferences).toHaveBeenCalled();
    });
    expect(toggle).toHaveAttribute("aria-checked", "false");
  });
});

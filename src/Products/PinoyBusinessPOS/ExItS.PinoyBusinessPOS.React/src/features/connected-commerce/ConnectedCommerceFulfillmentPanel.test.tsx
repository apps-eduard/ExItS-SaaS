import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { ConnectedCommerceFulfillmentPanel } from "@/features/connected-commerce/ConnectedCommerceFulfillmentPanel";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    sessionGrant: { productRole: "Owner", organizationManagementAuthority: true },
  }),
}));

const getOrganizationOfferDelivery = vi.fn();
const updateOrganizationOfferDelivery = vi.fn();
const updateOrganizationBranchFulfillmentDefaults = vi.fn();
const listOrganizationBranchesForFulfillment = vi.fn();

vi.mock("@/api/pos/pos-connected-commerce-client", () => ({
  getOrganizationOfferDelivery: (...args: unknown[]) => getOrganizationOfferDelivery(...args),
  updateOrganizationOfferDelivery: (...args: unknown[]) => updateOrganizationOfferDelivery(...args),
  updateOrganizationBranchFulfillmentDefaults: (...args: unknown[]) =>
    updateOrganizationBranchFulfillmentDefaults(...args),
  updateBranchFulfillmentSettingsViaPos: vi.fn(),
}));

vi.mock("@/api/platform/branch-fulfillment-client", () => ({
  listOrganizationBranchesForFulfillment: (...args: unknown[]) =>
    listOrganizationBranchesForFulfillment(...args),
  updateBranchFulfillmentSettings: vi.fn(),
  setBranchOnlineOrdersPaused: vi.fn(),
}));

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "22222222-2222-2222-2222-222222222222",
};

function renderPanel() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <ConnectedCommerceFulfillmentPanel
          workspace={workspace}
          organizationId={workspace.organizationId}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("ConnectedCommerceFulfillmentPanel", () => {
  beforeEach(() => {
    getOrganizationOfferDelivery.mockResolvedValue({
      organizationId: workspace.organizationId,
      offerDelivery: false,
      defaultPickupEnabled: false,
      defaultDeliveryEnabled: false,
      defaultOnlineOrdersEnabled: false,
    });
    listOrganizationBranchesForFulfillment.mockResolvedValue([
      {
        id: "22222222-2222-2222-2222-222222222222",
        organizationId: workspace.organizationId,
        code: "MAIN",
        name: "Main Branch",
        branchType: "Retail",
        isPrimary: true,
        status: "Active",
        pickupEnabled: true,
        deliveryEnabled: true,
        customerOrderingEnabled: false,
        onlineOrdersPaused: false,
        pickupReady: true,
        deliveryReady: true,
        customerOrderingReady: false,
        pickupSectionsComplete: 2,
        pickupSectionsTotal: 2,
        deliverySectionsComplete: 5,
        deliverySectionsTotal: 5,
        canUseDelivery: true,
        canUseCustomerOrdering: true,
      },
    ]);
  });

  it("is the editable Offer Delivery surface and preserves branch Delivery when globally paused", async () => {
    renderPanel();

    await waitFor(() => {
      expect(screen.getByTestId("connected-commerce-offer-delivery")).toBeInTheDocument();
    });
    expect(screen.getByTestId("connected-commerce-org-fulfillment")).toBeInTheDocument();
    expect(screen.getByTestId("connected-commerce-branch-defaults")).toBeInTheDocument();
    expect(screen.getByTestId("connected-commerce-branch-table")).toBeInTheDocument();
    expect(screen.getByTestId("connected-commerce-delivery-paused-chip")).toBeInTheDocument();
    expect(screen.getByTestId("cc-branch-delivery-22222222-2222-2222-2222-222222222222")).toHaveTextContent(
      "connectedCommerce.chip.readyGloballyPaused",
    );
    expect(screen.getByTestId("cc-delivery-switch-22222222-2222-2222-2222-222222222222")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("cc-delivery-switch-22222222-2222-2222-2222-222222222222")).toHaveClass(
      "exits-switch",
    );
  });
});

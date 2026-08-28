import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as publicIdentityClient from "@/api/platform/public-identity-client";
import * as customersClient from "@/api/pos/pos-customers-client";
import { CustomerCreatePage } from "@/features/customers/CustomerFormPage";

vi.mock("@/api/platform/public-identity-client", async (importOriginal) => {
  const actual = await importOriginal<typeof publicIdentityClient>();
  return {
    ...actual,
    resolvePublicUserId: vi.fn(),
    createBusinessCustomerWithPersonalLink: vi.fn(),
    evaluateCustomerLinkEligibility: vi.fn(),
  };
});

vi.mock("@/api/pos/pos-customers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof customersClient>();
  return {
    ...actual,
    createCustomer: vi.fn(),
    findCustomerByLinkedPersonalPublicUserId: vi.fn(),
    searchCheckoutCustomers: vi.fn(),
  };
});

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      branchId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    },
    sessionGrant: {
      capabilities: ["Customers.View", "Customers.Edit"],
    },
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

const platformBusinessCustomerId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const linkRequestId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const posCustomerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const userIdentityId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

function renderCreate() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/customers/new"]}>
        <Routes>
          <Route path="/customers/new" element={<CustomerCreatePage />} />
          <Route
            path="/customers/:customerId"
            element={<div data-testid="customer-detail-stub" />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("CustomerFormPage save vs resolve link", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  beforeEach(() => {
    vi.mocked(customersClient.findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(null);
    vi.mocked(customersClient.searchCheckoutCustomers).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.mocked(customersClient.createCustomer).mockResolvedValue({
      customerId: posCustomerId,
      organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      displayName: "Walk-in Ana",
      mobileNumber: null,
      address: null,
      notes: null,
      status: "Active",
      platformBusinessCustomerId: null,
      createdAtUtc: "2026-08-20T00:00:00Z",
      updatedAtUtc: "2026-08-20T00:00:00Z",
      linkedPersonalPublicUserId: null,
      linkedBuyerOrganizationId: null,
      linkedBuyerPublicOrganizationId: null,
    });
    vi.mocked(publicIdentityClient.createBusinessCustomerWithPersonalLink).mockResolvedValue({
      customerId: platformBusinessCustomerId,
      linkRequestId,
      linkStatus: "Pending",
    });
    vi.mocked(publicIdentityClient.resolvePublicUserId).mockResolvedValue({
      publicUserId: "EX-1234-5678",
      userIdentityId,
      displayName: "Rosa Personal",
      maskedEmail: "r***@example.com",
      status: "Active",
      isSelf: false,
    });
    vi.mocked(publicIdentityClient.evaluateCustomerLinkEligibility).mockResolvedValue({
      status: "Eligible",
      message: "Eligible to invite.",
      publicUserId: "EX-1234-5678",
      displayName: "Rosa Personal",
      userIdentityId,
      existingBusinessCustomerId: null,
      existingPendingRequestId: null,
    });
  });

  it("walk-in local save does not call createBusinessCustomerWithPersonalLink", async () => {
    const user = userEvent.setup();
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-walkin"));
    await user.type(screen.getByTestId("customer-display-name"), "Walk-in Ana");
    await user.click(screen.getByTestId("customer-save"));

    await waitFor(() => {
      expect(customersClient.createCustomer).toHaveBeenCalled();
    });
    expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).not.toHaveBeenCalled();
    expect(customersClient.createCustomer).toHaveBeenCalledWith(
      expect.objectContaining({
        organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        branchId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      }),
      expect.objectContaining({
        displayName: "Walk-in Ana",
        platformBusinessCustomerId: null,
      }),
    );
  });

  it("exits path Save calls createBusinessCustomerWithPersonalLink then createCustomer with platform id", async () => {
    const user = userEvent.setup();
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    expect(screen.queryByTestId("customer-info-section")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-display-name")).not.toBeInTheDocument();
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("customer-info-section")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("customer-personal-link-confirm")).not.toBeInTheDocument();
    expect(screen.getByTestId("customer-display-name")).toHaveValue("Rosa Personal");
    expect(screen.getByTestId("customer-exits-id")).toHaveValue("EX-1234-5678");
    expect(screen.getByTestId("customer-exits-id")).toHaveAttribute("readonly");
    expect(screen.getByTestId("customer-email")).toHaveValue("r***@example.com");
    expect(screen.getByTestId("customer-email")).toHaveAttribute("readonly");
    expect(screen.getByTestId("customer-save")).toHaveTextContent("Save and invite");

    await user.click(screen.getByTestId("customer-save"));

    await waitFor(() => {
      expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).toHaveBeenCalled();
      expect(customersClient.createCustomer).toHaveBeenCalled();
    });

    const linkOrder = vi.mocked(publicIdentityClient.createBusinessCustomerWithPersonalLink).mock
      .invocationCallOrder[0]!;
    const createOrder = vi.mocked(customersClient.createCustomer).mock.invocationCallOrder[0]!;
    expect(linkOrder).toBeLessThan(createOrder);

    expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).toHaveBeenCalledWith(
      "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      expect.objectContaining({
        publicUserId: "EX-1234-5678",
        targetUserIdentityId: userIdentityId,
      }),
    );
    expect(customersClient.createCustomer).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        platformBusinessCustomerId: platformBusinessCustomerId,
        linkedPersonalPublicUserId: "EX-1234-5678",
      }),
    );
    expect(screen.queryByTestId("customer-save-local-instead")).not.toBeInTheDocument();
  });

  it("CustomerPersonalLinkPanel resolve alone does not call createBusinessCustomerWithPersonalLink", async () => {
    const user = userEvent.setup();
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(publicIdentityClient.resolvePublicUserId).toHaveBeenCalled();
      expect(screen.getByTestId("customer-info-section")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("customer-personal-link-confirm")).not.toBeInTheDocument();

    expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).not.toHaveBeenCalled();
    expect(customersClient.createCustomer).not.toHaveBeenCalled();
  });

  it("does not send add/link when that Personal ID is already a POS customer", async () => {
    const user = userEvent.setup();
    vi.mocked(customersClient.findCustomerByLinkedPersonalPublicUserId).mockResolvedValue({
      customerId: posCustomerId,
      displayName: "Rosa Personal",
      mobileNumber: null,
      status: "Active",
    });
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("customer-already-in-contacts")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("customer-info-section")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-personal-link-confirm-btn")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-save")).not.toBeInTheDocument();
    expect(screen.getByTestId("customer-already-in-contacts")).toHaveAttribute("role", "alert");
    expect(screen.getByTestId("customer-already-in-contacts-open")).toHaveAttribute(
      "href",
      `/customers/${posCustomerId}`,
    );
    expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).not.toHaveBeenCalled();
    expect(customersClient.createCustomer).not.toHaveBeenCalled();
  });

  it("does not offer save as local when creating with an ExItS ID", async () => {
    const user = userEvent.setup();
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    expect(screen.queryByTestId("customer-save-local-instead")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-create-kind-change")).not.toBeInTheDocument();
    expect(screen.getByTestId("customer-create-kind-exits")).toHaveAttribute("aria-pressed", "true");
    expect(screen.queryByTestId("customer-info-section")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-display-name")).not.toBeInTheDocument();
    expect(screen.getByTestId("customer-personal-link-panel")).toBeInTheDocument();
  });

  it("shows and fills customer info after an ExItS ID search finds the person", async () => {
    const user = userEvent.setup();
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    expect(screen.queryByTestId("customer-info-section")).not.toBeInTheDocument();

    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("customer-info-section")).toBeInTheDocument();
    });
    expect(screen.getByTestId("customer-display-name")).toHaveValue("Rosa Personal");
    expect(screen.getByTestId("customer-exits-id")).toHaveValue("EX-1234-5678");
    expect(screen.getByTestId("customer-exits-id")).toHaveAttribute("readonly");
    expect(screen.getByTestId("customer-email")).toHaveValue("r***@example.com");
    expect(screen.getByTestId("customer-email")).toHaveAttribute("readonly");
    expect(screen.getByTestId("customer-address")).toBeInTheDocument();
    expect(screen.getByTestId("customer-notes")).toBeInTheDocument();
    expect(screen.getByTestId("customer-save")).toHaveTextContent("Save and invite");
    expect(screen.getByTestId("customer-exits-invite-hint")).toHaveAttribute("role", "status");

    await user.click(screen.getByTestId("qr-manual-clear"));
    await waitFor(() => {
      expect(screen.queryByTestId("customer-info-section")).not.toBeInTheDocument();
    });
    expect(screen.queryByTestId("customer-save")).not.toBeInTheDocument();
  });

  it("hides Save when eligibility reports owner self", async () => {
    const user = userEvent.setup();
    vi.mocked(publicIdentityClient.evaluateCustomerLinkEligibility).mockResolvedValue({
      status: "OwnerOfOrganization",
      message: "You're already the owner of this business.",
      publicUserId: "EX-1234-5678",
      displayName: "Rosa Personal",
      userIdentityId,
      existingBusinessCustomerId: null,
      existingPendingRequestId: null,
    });
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("customer-link-eligibility-OwnerOfOrganization")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("customer-save")).not.toBeInTheDocument();
    expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).not.toHaveBeenCalled();
  });

  it("hides Save when eligibility reports organization staff", async () => {
    const user = userEvent.setup();
    vi.mocked(publicIdentityClient.evaluateCustomerLinkEligibility).mockResolvedValue({
      status: "OrganizationStaff",
      message: "staff",
      publicUserId: "EX-1234-5678",
      displayName: "Rosa Personal",
      userIdentityId,
      existingBusinessCustomerId: null,
      existingPendingRequestId: null,
    });
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("customer-link-eligibility-OrganizationStaff")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("customer-save")).not.toBeInTheDocument();
  });
});

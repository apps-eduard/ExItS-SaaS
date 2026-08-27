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
  };
});

vi.mock("@/api/pos/pos-customers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof customersClient>();
  return {
    ...actual,
    createCustomer: vi.fn(),
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
          <Route path="/customers/:customerId" element={<div data-testid="customer-detail-stub" />} />
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
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("customer-personal-link-confirm")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("customer-personal-link-confirm-btn"));
    await waitFor(() => {
      expect(screen.getByTestId("customer-personal-link-selected")).toBeInTheDocument();
    });

    const displayName = screen.getByTestId("customer-display-name") as HTMLInputElement;
    if (!displayName.value.trim()) {
      await user.type(displayName, "Rosa Personal");
    }

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
      }),
    );
  });

  it("CustomerPersonalLinkPanel resolve alone does not call createBusinessCustomerWithPersonalLink", async () => {
    const user = userEvent.setup();
    renderCreate();

    await user.click(screen.getByTestId("customer-create-kind-exits"));
    await user.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(publicIdentityClient.resolvePublicUserId).toHaveBeenCalled();
      expect(screen.getByTestId("customer-personal-link-confirm")).toBeInTheDocument();
    });

    expect(publicIdentityClient.createBusinessCustomerWithPersonalLink).not.toHaveBeenCalled();
    expect(customersClient.createCustomer).not.toHaveBeenCalled();
  });
});

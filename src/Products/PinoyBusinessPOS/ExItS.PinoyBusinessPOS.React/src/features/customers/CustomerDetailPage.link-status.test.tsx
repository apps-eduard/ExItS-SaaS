import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as linkStatusClient from "@/api/platform/customer-link-status-client";
import * as customersClient from "@/api/pos/pos-customers-client";
import { CustomerDetailPage } from "@/features/customers/CustomerDetailPage";

vi.mock("@/api/platform/customer-link-status-client", async (importOriginal) => {
  const actual = await importOriginal<typeof linkStatusClient>();
  return {
    ...actual,
    getCustomerLinkStatus: vi.fn(),
    listCustomerLinkRequestHistory: vi.fn(),
  };
});

vi.mock("@/api/pos/pos-customers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof customersClient>();
  return {
    ...actual,
    getCustomer: vi.fn(),
    getCustomerCreditSummary: vi.fn(),
    listCustomerCreditEntries: vi.fn(),
    listCustomerRepayments: vi.fn(),
  };
});

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      branchId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    },
    sessionGrant: {
      capabilities: ["Customers.View", "Customers.Edit", "Utang.RecordRepayment", "Utang.ViewStatement"],
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

vi.mock("@/access/pos-capabilities", () => ({
  canEditCustomer: () => true,
  canRecordRepayment: () => true,
  canViewStatement: () => true,
}));

const customerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const platformBusinessCustomerId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const organizationId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

function linkStatus(
  overrides: Partial<linkStatusClient.CustomerLinkStatusDto> = {},
): linkStatusClient.CustomerLinkStatusDto {
  return {
    businessCustomerId: platformBusinessCustomerId,
    organizationId,
    status: "Pending",
    linkedUserIdentityId: null,
    latestLinkRequestId: null,
    latestLinkRequestStatus: null,
    reminderCount: 0,
    lastRemindedAtUtc: null,
    nextReminderEligibleAtUtc: null,
    invitationSentAtUtc: null,
    ...overrides,
  };
}

const baseCustomer = {
  customerId,
  organizationId,
  displayName: "Ana Reyes",
  mobileNumber: "09171234567",
  address: null,
  notes: "exits-id:EX-1234-5678",
  status: "Active",
  platformBusinessCustomerId,
  createdAtUtc: "2026-08-20T00:00:00Z",
  updatedAtUtc: "2026-08-20T00:00:00Z",
  linkedPersonalPublicUserId: "EX-1234-5678",
  linkedBuyerOrganizationId: null,
  linkedBuyerPublicOrganizationId: null,
};

function renderDetail(path = `/customers/${customerId}`) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/customers/:customerId" element={<CustomerDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("CustomerDetailPage Platform link status", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  beforeEach(() => {
    vi.mocked(customersClient.getCustomer).mockResolvedValue(baseCustomer);
    vi.mocked(customersClient.getCustomerCreditSummary).mockResolvedValue({
      customerId,
      outstandingAmount: 0,
      currency: "PHP",
      asOfUtc: "2026-08-20T00:00:00Z",
    } as never);
    vi.mocked(customersClient.listCustomerCreditEntries).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.mocked(customersClient.listCustomerRepayments).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.mocked(linkStatusClient.listCustomerLinkRequestHistory).mockResolvedValue([]);
  });

  async function expectStatus(label: RegExp | string) {
    await waitFor(() => {
      expect(screen.getByTestId("customer-link-status")).toHaveTextContent(label);
    });
  }

  it("shows Local customer when platformBusinessCustomerId is missing", async () => {
    vi.mocked(customersClient.getCustomer).mockResolvedValue({
      ...baseCustomer,
      platformBusinessCustomerId: null,
      linkedPersonalPublicUserId: null,
      notes: null,
    });
    renderDetail();
    await expectStatus(/Local customer/i);
    expect(linkStatusClient.getCustomerLinkStatus).not.toHaveBeenCalled();
  });

  it("shows Request sent from Platform even when EX-ID is stored", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Pending",
      }),
    );
    renderDetail();
    await expectStatus(/Request sent/i);
    expect(screen.getByTestId("customer-link-pending-banner")).toBeInTheDocument();
    expect(screen.getByTestId("customer-exits-id")).toHaveTextContent("EX-1234-5678");
    expect(screen.getByTestId("customer-link-exits-id-panel")).toBeInTheDocument();
    expect(screen.getByTestId("customer-link-status")).not.toHaveTextContent(/^Linked$/);
    expect(screen.queryByText(platformBusinessCustomerId)).not.toBeInTheDocument();
  });

  it("shows after-create success hint when pendingLink=1 and request is pending", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Pending",
        invitationSentAtUtc: "2026-08-28T00:26:00Z",
      }),
    );
    renderDetail(`/customers/${customerId}?pendingLink=1`);
    await expectStatus(/Request sent/i);
    expect(screen.getByTestId("customer-link-after-create-success")).toBeInTheDocument();
    expect(screen.getByText(/What happens next/i)).toBeInTheDocument();
  });

  it("shows Linked from Platform", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status: "Linked",
        linkedUserIdentityId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Active",
      }),
    );
    renderDetail();
    await expectStatus(/^Linked$/);
    expect(screen.queryByTestId("customer-link-pending-banner")).not.toBeInTheDocument();
  });

  it("shows compact connection history when Platform returns link-requests", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status: "Linked",
        linkedUserIdentityId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Active",
      }),
    );
    vi.mocked(linkStatusClient.listCustomerLinkRequestHistory).mockResolvedValue([
      {
        id: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        status: "Active",
        createdAtUtc: "2026-08-18T08:00:00Z",
        resolvedAtUtc: null,
      },
      {
        id: "ffffffff-ffff-4fff-8fff-ffffffffffff",
        status: "Pending",
        createdAtUtc: "2026-08-10T08:00:00Z",
        resolvedAtUtc: null,
      },
    ]);
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("customer-link-history")).toBeInTheDocument();
    });
    expect(screen.getByTestId("customer-link-history")).toHaveTextContent(/Connection history/i);
    expect(screen.getByTestId("customer-link-history")).toHaveTextContent(/Linked/i);
    expect(screen.getByTestId("customer-link-history")).toHaveTextContent(/Request sent/i);
  });

  it("hides connection history when Platform returns an empty list", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(linkStatus());
    vi.mocked(linkStatusClient.listCustomerLinkRequestHistory).mockResolvedValue([]);
    renderDetail();
    await expectStatus(/Request sent/i);
    expect(screen.queryByTestId("customer-link-history")).not.toBeInTheDocument();
  });

  it.each([
    ["Declined", /Declined/i],
    ["Expired", /Expired/i],
    ["Revoked", /Revoked/i],
  ] as const)("maps Platform %s without showing Linked", async (status, label) => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status,
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: status,
      }),
    );
    renderDetail();
    await expectStatus(label);
    expect(screen.getByTestId("customer-link-status")).not.toHaveTextContent(/^Linked$/);
  });

  it("shows unavailable on Platform fetch error and does not invent Linked", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockRejectedValue(new Error("boom"));
    renderDetail();
    await expectStatus(/Unavailable/i);
  });

  it("shows Unavailable from Platform without saying blocked", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status: "Unavailable",
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Declined",
        invitationSentAtUtc: "2026-08-20T00:00:00Z",
      }),
    );
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("customer-link-unavailable-banner")).toBeInTheDocument();
    });
    expect(screen.getByTestId("customer-link-status")).toHaveTextContent(/Unavailable/i);
    expect(screen.queryByText(/blocked you/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-link-remind")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-link-invite-again")).not.toBeInTheDocument();
  });

  it("lets Platform Linked win over pendingLink=1 query hint", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status: "Linked",
        linkedUserIdentityId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Active",
      }),
    );
    renderDetail(`/customers/${customerId}?pendingLink=1`);
    await expectStatus(/^Linked$/);
    expect(screen.queryByTestId("customer-link-pending-banner")).not.toBeInTheDocument();
  });

  it("lets Platform Declined win over pendingLink=1 query hint", async () => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status: "Declined",
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: "Declined",
      }),
    );
    renderDetail(`/customers/${customerId}?pendingLink=1`);
    await expectStatus(/Declined/i);
    expect(screen.queryByTestId("customer-link-pending-banner")).not.toBeInTheDocument();
  });

  it.each([
    "Pending",
    "Linked",
    "Declined",
    "Expired",
    "Revoked",
    "Unavailable",
  ] as const)("keeps Record payment available when link status is %s", async (status) => {
    vi.mocked(linkStatusClient.getCustomerLinkStatus).mockResolvedValue(
      linkStatus({
        status,
        linkedUserIdentityId: status === "Linked" ? "ffffffff-ffff-4fff-8fff-ffffffffffff" : null,
        latestLinkRequestId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        latestLinkRequestStatus: status === "Linked" ? "Active" : status,
      }),
    );
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("customer-repay")).toBeInTheDocument();
    });
  });
});

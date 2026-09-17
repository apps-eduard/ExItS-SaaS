import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import type { PosSupplier } from "@/api/pos/pos-suppliers-client";
import type {
  PosSupplierPayableDto,
  PosSupplierPayablePaymentDto,
  PosSupplierPayableSummaryDto,
} from "@/api/pos/pos-supplier-payables-client";
import { SupplierDetailPage } from "@/features/suppliers/SupplierDetailPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const supplierId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const payableId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const paidPayableId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const voidedPayableId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
const partialPayableId = "ffffffff-ffff-ffff-ffff-ffffffffffff";
const actorId = "99999999-9999-9999-9999-999999999999";
const paymentId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

const getSupplier = vi.fn();
const getSupplierPayableSummary = vi.fn();
const listSupplierPayables = vi.fn();
const listSupplierPayablePayments = vi.fn();
const getOrganizationOnlineSupplierPaymentsCapability = vi.fn();
const listPaymentMethods = vi.fn();
const getBusinessCustomerCreditPolicy = vi.fn();
const relationshipId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

const comingSoonOnlineMethod = {
  methodCode: "OnlineGCash",
  requiredCapability: "OnlinePayments",
  integrationMode: "Online",
  settlementMode: "ExternalConfirmation",
  availability: "ComingSoon",
  entitled: true,
  isEnabled: false,
  isCheckoutEligible: false,
  comingSoon: true,
  requireReference: false,
  branchScope: "AllBranches",
  selectedBranchIds: [],
  canConfigure: false,
};

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Kizy Store",
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

vi.mock("@/api/pos/pos-business-credit-policy-client", () => ({
  getBusinessCustomerCreditPolicy: (...args: unknown[]) =>
    getBusinessCustomerCreditPolicy(...args),
}));

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  listRelationships: vi.fn(async () => []),
  cancelConnectionRequest: vi.fn(),
  isRelationshipActive: () => false,
  isRelationshipPending: () => false,
  getBuyerConnectedSupplierCommerceReadiness: vi.fn(async () => ({
    relationshipId: null,
    isReady: true,
    supportedFulfillmentMethods: [],
    requirements: null,
  })),
}));

vi.mock("@/api/pos/pos-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-suppliers-client")>();
  return {
    ...actual,
    getSupplier: (...args: unknown[]) => getSupplier(...args),
    activateSupplier: vi.fn(),
    deactivateSupplier: vi.fn(),
  };
});

vi.mock("@/api/pos/pos-supplier-payables-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-supplier-payables-client")>();
  return {
    ...actual,
    getSupplierPayableSummary: (...args: unknown[]) => getSupplierPayableSummary(...args),
    listSupplierPayables: (...args: unknown[]) => listSupplierPayables(...args),
    listSupplierPayablePayments: (...args: unknown[]) => listSupplierPayablePayments(...args),
  };
});

vi.mock("@/api/platform/organization-online-supplier-payments-client", () => ({
  getOrganizationOnlineSupplierPaymentsCapability: (...args: unknown[]) =>
    getOrganizationOnlineSupplierPaymentsCapability(...args),
}));

vi.mock("@/api/pos/pos-payment-methods-client", () => ({
  listPaymentMethods: (...args: unknown[]) => listPaymentMethods(...args),
}));

function supplierDto(overrides: Partial<PosSupplier> = {}): PosSupplier {
  return {
    supplierId,
    organizationId: orgId,
    supplierCode: "SUP0001",
    name: "Fresh Farms",
    status: "Active",
    connectionType: "Manual",
    contactPerson: null,
    mobileNumber: null,
    telephoneNumber: null,
    email: null,
    addressLine1: null,
    addressLine2: null,
    cityMunicipality: null,
    province: null,
    postalCode: null,
    taxOrRegistrationNumber: null,
    notes: null,
    connectedRelationshipId: null,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    ...overrides,
  };
}

function payableDto(overrides: Partial<PosSupplierPayableDto> = {}): PosSupplierPayableDto {
  return {
    payableId,
    organizationId: orgId,
    supplierId,
    supplierName: "Fresh Farms",
    sourceType: "GoodsReceipt",
    sourceId: "11111111-2222-3333-4444-555555555555",
    sourceReference: "PO-100",
    originalAmount: 1000,
    paidAtReceiptAmount: 200,
    paidAmount: 200,
    balance: 800,
    status: "Open",
    dueDate: "2026-09-15",
    paymentMethodAtReceipt: "Cash",
    createdAtUtc: "2026-08-20T00:00:00Z",
    createdBy: actorId,
    updatedAtUtc: "2026-08-20T00:00:00Z",
    voidedAtUtc: null,
    voidedBy: null,
    voidReason: null,
    hasPostedPayments: false,
    isOverdue: false,
    ...overrides,
  };
}

function summaryDto(
  overrides: Partial<PosSupplierPayableSummaryDto> = {},
): PosSupplierPayableSummaryDto {
  return {
    supplierId,
    outstandingTotal: 800,
    overdueTotal: 0,
    openCount: 1,
    ...overrides,
  };
}

function renderDetail() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/suppliers/${supplierId}`]}>
        <Routes>
          <Route path="/suppliers/:supplierId" element={<SupplierDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("SupplierDetailPage supplier credit", () => {
  beforeEach(() => {
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
      mappedPosRoleCode: "Owner",
    };
    getSupplier.mockResolvedValue(supplierDto());
    getSupplierPayableSummary.mockResolvedValue(summaryDto());
    listSupplierPayables.mockResolvedValue({
      items: [payableDto()],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    listSupplierPayablePayments.mockResolvedValue([] as PosSupplierPayablePaymentDto[]);
    getOrganizationOnlineSupplierPaymentsCapability.mockResolvedValue({
      organizationId: orgId,
      status: "Disabled",
      updatedAtUtc: null,
      updatedByActorReference: null,
      reason: null,
    });
    listPaymentMethods.mockResolvedValue([comingSoonOnlineMethod]);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders supplier payable summary", async () => {
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-outstanding")).toBeInTheDocument();
    });
    expect(screen.getByTestId("supplier-credit-overdue")).toBeInTheDocument();
    expect(screen.getByTestId("supplier-credit-open-count")).toHaveTextContent("1");
    expect(screen.getByTestId("supplier-credit-approved-limit")).toHaveTextContent("—");
    expect(screen.queryByTestId("supplier-credit-paid-count")).not.toBeInTheDocument();
    expect(screen.getByTestId("supplier-credit-payable-filters")).toBeInTheDocument();
  });

  it("shows credit exposure cards and utilization for connected supplier", async () => {
    getSupplier.mockResolvedValue(
      supplierDto({
        connectionType: "ConnectedOrganization",
        connectedRelationshipId: relationshipId,
        name: "Mica store",
      }),
    );
    getSupplierPayableSummary.mockResolvedValue(
      summaryDto({ outstandingTotal: 643, overdueTotal: 0, openCount: 1 }),
    );
    getBusinessCustomerCreditPolicy.mockResolvedValue({
      connectionId: relationshipId,
      sellerOrganizationId: "22222222-2222-4222-8222-222222222222",
      buyerOrganizationId: orgId,
      status: "Approved",
      creditLimit: 30000,
      defaultTermDays: 30,
      outstandingAmount: 643,
      reservedByActivePos: 747,
      availableCredit: 28610,
      configuredByUserId: actorId,
      configuredAtUtc: "2026-08-01T00:00:00Z",
      approvedByUserId: actorId,
      approvedAtUtc: "2026-08-02T00:00:00Z",
      updatedByUserId: actorId,
      updatedAtUtc: "2026-08-02T00:00:00Z",
      expectedUpdatedAtUtc: "2026-08-02T00:00:00Z",
    });

    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-approved-limit")).toBeInTheDocument();
    });
    expect(getBusinessCustomerCreditPolicy).toHaveBeenCalled();
    expect(screen.getByTestId("supplier-credit-approved-limit").textContent).toMatch(/30[,.]?000/);
    expect(screen.getByTestId("supplier-credit-outstanding").textContent).toMatch(/643/);
    expect(screen.getByTestId("supplier-credit-reserved").textContent).toMatch(/747/);
    expect(screen.getByTestId("supplier-credit-available").textContent).toMatch(/28[,.]?610/);
    expect(screen.getByTestId("supplier-credit-utilization-caption").textContent).toMatch(
      /4\.6%/,
    );
    expect(screen.getByTestId("supplier-credit-utilization-bar")).toBeInTheDocument();
  });

  it("defaults payable list to Open and supports Paid / All filters", async () => {
    const user = userEvent.setup();
    listSupplierPayables.mockResolvedValue({
      items: [
        payableDto({ payableId, status: "Open", balance: 800, paidAmount: 200 }),
        payableDto({
          payableId: partialPayableId,
          status: "PartiallyPaid",
          paidAmount: 500,
          balance: 500,
          hasPostedPayments: true,
        }),
        payableDto({
          payableId: paidPayableId,
          status: "Paid",
          paidAmount: 1000,
          balance: 0,
        }),
        payableDto({
          payableId: voidedPayableId,
          status: "Voided",
          balance: 0,
          voidedAtUtc: "2026-08-29T00:00:00Z",
          voidReason: "Receipt reversed",
        }),
      ],
      totalCount: 4,
      page: 1,
      pageSize: 50,
    });
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId(`supplier-payable-${payableId}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId(`supplier-payable-${partialPayableId}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`supplier-payable-${paidPayableId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId(`supplier-payable-${voidedPayableId}`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId("supplier-credit-filter-paid"));
    expect(screen.getByTestId(`supplier-payable-${paidPayableId}`)).toHaveAttribute(
      "data-status",
      "Paid",
    );
    expect(screen.queryByTestId(`supplier-payable-${payableId}`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId("supplier-credit-filter-all"));
    expect(screen.getByTestId(`supplier-payable-${payableId}`)).toHaveAttribute(
      "data-status",
      "Open",
    );
    expect(screen.getByTestId(`supplier-payable-${partialPayableId}`)).toHaveAttribute(
      "data-status",
      "PartiallyPaid",
    );
    expect(screen.getByTestId(`supplier-payable-${paidPayableId}`)).toHaveAttribute(
      "data-status",
      "Paid",
    );
    expect(screen.getByTestId(`supplier-payable-${voidedPayableId}`)).toHaveAttribute(
      "data-status",
      "Voided",
    );
  });

  it("hides Pay now when platform online supplier payments are Disabled", async () => {
    listSupplierPayables.mockResolvedValue({
      items: [
        payableDto({ payableId, status: "Open", balance: 800 }),
        payableDto({
          payableId: partialPayableId,
          status: "PartiallyPaid",
          balance: 400,
          paidAmount: 600,
          hasPostedPayments: true,
        }),
        payableDto({
          payableId: paidPayableId,
          status: "Paid",
          balance: 0,
          paidAmount: 1000,
        }),
        payableDto({
          payableId: voidedPayableId,
          status: "Voided",
          balance: 0,
        }),
      ],
      totalCount: 4,
      page: 1,
      pageSize: 50,
    });
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId(`supplier-payable-${payableId}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`supplier-payable-record-${payableId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId(`supplier-payable-pay-now-${payableId}`)).not.toBeInTheDocument();
    expect(
      screen.queryByTestId(`supplier-payable-pay-now-${partialPayableId}`),
    ).not.toBeInTheDocument();
    expect(screen.queryByTestId("supplier-credit-online-unavailable")).not.toBeInTheDocument();
  });

  it("shows unavailable state when Available but online methods are ComingSoon", async () => {
    getOrganizationOnlineSupplierPaymentsCapability.mockResolvedValue({
      organizationId: orgId,
      status: "Available",
      updatedAtUtc: null,
      updatedByActorReference: null,
      reason: null,
    });
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-online-unavailable")).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`supplier-payable-record-${payableId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId(`supplier-payable-pay-now-${payableId}`)).not.toBeInTheDocument();
  });

  it("shows Pay now when Available and a ready online method exists", async () => {
    getOrganizationOnlineSupplierPaymentsCapability.mockResolvedValue({
      organizationId: orgId,
      status: "Available",
      updatedAtUtc: null,
      updatedByActorReference: null,
      reason: null,
    });
    listPaymentMethods.mockResolvedValue([
      {
        ...comingSoonOnlineMethod,
        availability: "Configurable",
        comingSoon: false,
        entitled: true,
      },
    ]);
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId(`supplier-payable-pay-now-${payableId}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`supplier-payable-record-${payableId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId("supplier-credit-online-unavailable")).not.toBeInTheDocument();
  });

  it("lists outstanding payables without offering manual Record Payment", async () => {
    renderDetail();

    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-outstanding")).toBeInTheDocument();
    });
    expect(screen.getByTestId("supplier-credit-list")).toBeInTheDocument();
    expect(screen.queryByTestId(`supplier-payable-record-${payableId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId("supplier-payment-dialog")).not.toBeInTheDocument();
  });

  it("never offers buyer manual settle dialog", async () => {
    getOrganizationOnlineSupplierPaymentsCapability.mockResolvedValue({
      organizationId: orgId,
      status: "Available",
      updatedAtUtc: null,
      updatedByActorReference: null,
      reason: null,
    });
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-list")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("supplier-payment-dialog")).not.toBeInTheDocument();
    expect(screen.queryByTestId("supplier-payment-confirm")).not.toBeInTheDocument();
  });

  it("renders payment history in payable detail", async () => {
    const user = userEvent.setup();
    listSupplierPayablePayments.mockResolvedValue([
      {
        paymentId,
        payableId,
        amount: 150,
        paymentMethod: "BankTransfer",
        reference: "REF-1",
        notes: "Partial",
        paidAtUtc: "2026-08-25T10:00:00Z",
        recordedBy: actorId,
        recordedAtUtc: "2026-08-25T10:00:00Z",
      },
    ] as PosSupplierPayablePaymentDto[]);
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId(`supplier-payable-detail-${payableId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`supplier-payable-detail-${payableId}`));
    await waitFor(() => {
      expect(screen.getByTestId("supplier-payable-payment-history")).toBeInTheDocument();
    });
    expect(screen.getByTestId(`supplier-payment-row-${paymentId}`)).toBeInTheDocument();
  });

  it("hides supplier credit when purchasing view is denied", async () => {
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationMember",
      productLocalRoleCode: "Cashier",
      mappedPosRoleCode: "Cashier",
    };
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("supplier-detail-page")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("supplier-credit-section")).not.toBeInTheDocument();
  });

  it("shows credit for view-only purchasing but hides record payment", async () => {
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationMember",
      productLocalRoleCode: "ReportingUser",
      mappedPosRoleCode: "ReportingUser",
    };
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-list")).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`supplier-payable-record-${payableId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`supplier-payable-detail-${payableId}`)).toBeInTheDocument();
  });
});

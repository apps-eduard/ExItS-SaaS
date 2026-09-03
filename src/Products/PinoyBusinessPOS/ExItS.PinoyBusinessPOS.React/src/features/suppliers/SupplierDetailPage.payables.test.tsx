import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
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
const recordSupplierPayablePayment = vi.fn();

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

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  listRelationships: vi.fn(async () => []),
  cancelConnectionRequest: vi.fn(),
  isRelationshipActive: () => false,
  isRelationshipPending: () => false,
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
    recordSupplierPayablePayment: (...args: unknown[]) => recordSupplierPayablePayment(...args),
  };
});

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
    recordSupplierPayablePayment.mockResolvedValue({
      paymentId,
      payableId,
      amount: 100,
      paymentMethod: "Cash",
      reference: null,
      notes: null,
      paidAtUtc: "2026-08-30T08:00:00Z",
      recordedBy: actorId,
      recordedAtUtc: "2026-08-30T08:00:00Z",
    });
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
  });

  it("lists payables with backend statuses including Paid and Voided", async () => {
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
      expect(screen.getByTestId(`supplier-payable-${payableId}`)).toHaveAttribute(
        "data-status",
        "Open",
      );
    });
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

  it("shows record payment for Open and PartiallyPaid only", async () => {
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
      expect(screen.getByTestId(`supplier-payable-record-${payableId}`)).toBeInTheDocument();
    });
    expect(
      screen.getByTestId(`supplier-payable-record-${partialPayableId}`),
    ).toBeInTheDocument();
    expect(
      screen.queryByTestId(`supplier-payable-record-${paidPayableId}`),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByTestId(`supplier-payable-record-${voidedPayableId}`),
    ).not.toBeInTheDocument();
  });

  it("lists outstanding payables and records a payment that refreshes summary", async () => {
    const user = userEvent.setup();
    renderDetail();

    await waitFor(() => {
      expect(screen.getByTestId("supplier-credit-outstanding")).toBeInTheDocument();
    });
    expect(screen.getByTestId("supplier-credit-list")).toBeInTheDocument();

    await user.click(screen.getByTestId(`supplier-payable-record-${payableId}`));
    const dialog = screen.getByTestId("supplier-payment-dialog");
    const amount = within(dialog).getByTestId("supplier-payment-amount");
    await user.clear(amount);
    await user.type(amount, "100");
    await user.click(within(dialog).getByTestId("supplier-payment-confirm"));

    await waitFor(() => {
      expect(recordSupplierPayablePayment).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: orgId }),
        payableId,
        expect.objectContaining({ amount: 100, paymentMethod: "Cash" }),
      );
    });
    await waitFor(() => {
      expect(getSupplierPayableSummary).toHaveBeenCalled();
      expect(listSupplierPayables).toHaveBeenCalled();
    });
  });

  it("blocks overpay before calling the API", async () => {
    const user = userEvent.setup();
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId(`supplier-payable-record-${payableId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`supplier-payable-record-${payableId}`));
    const dialog = screen.getByTestId("supplier-payment-dialog");
    const amount = within(dialog).getByTestId("supplier-payment-amount");
    await user.clear(amount);
    await user.type(amount, "900");
    await user.click(within(dialog).getByTestId("supplier-payment-confirm"));

    expect(await screen.findByTestId("supplier-payment-error")).toBeInTheDocument();
    expect(recordSupplierPayablePayment).not.toHaveBeenCalled();
  });

  it("blocks zero and negative payment amounts", async () => {
    const user = userEvent.setup();
    renderDetail();
    await waitFor(() => {
      expect(screen.getByTestId(`supplier-payable-record-${payableId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`supplier-payable-record-${payableId}`));
    const dialog = screen.getByTestId("supplier-payment-dialog");
    const amount = within(dialog).getByTestId("supplier-payment-amount");
    await user.clear(amount);
    await user.type(amount, "0");
    await user.click(within(dialog).getByTestId("supplier-payment-confirm"));
    expect(await screen.findByTestId("supplier-payment-error")).toBeInTheDocument();
    expect(recordSupplierPayablePayment).not.toHaveBeenCalled();

    await user.clear(amount);
    await user.type(amount, "-5");
    await user.click(within(dialog).getByTestId("supplier-payment-confirm"));
    expect(screen.getByTestId("supplier-payment-error")).toBeInTheDocument();
    expect(recordSupplierPayablePayment).not.toHaveBeenCalled();
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

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
const actorId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

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
    sourceId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
    originalAmount: 1000,
    paidAtReceiptAmount: 200,
    paidAmount: 0,
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
      paymentId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
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

  it("lists outstanding payables and records a payment", async () => {
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
    expect(screen.getByTestId(`supplier-payable-history-${payableId}`)).toBeInTheDocument();
  });
});

import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { PaymentMethodsSettingsPage } from "@/features/settings/PaymentMethodsSettingsPage";
import { listPaymentMethods, upsertPaymentMethod } from "@/api/pos/pos-payment-methods-client";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";

const workspaceMock = vi.hoisted(() => ({
  boundWorkspace: {
    organizationId: "11111111-1111-1111-1111-111111111111",
    branchId: null as string | null,
  },
  sessionGrant: {
    enabledFeatureCodes: ["store-basic-payments", "store-payment-management"],
  },
}));

const ORG_ID = "11111111-1111-1111-1111-111111111111";
const BRANCH_MAIN = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const BRANCH_ILOILO = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

vi.mock("@/access/pos-capabilities", () => ({
  canManagePaymentMethods: () =>
    workspaceMock.sessionGrant.enabledFeatureCodes.includes("store-payment-management"),
  canUseOnlinePayments: () =>
    workspaceMock.sessionGrant.enabledFeatureCodes.includes("store-online-payments"),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}));

vi.mock("@/api/pos/pos-payment-methods-client", () => ({
  listPaymentMethods: vi.fn(),
  upsertPaymentMethod: vi.fn(),
}));

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchManagementSummaries: vi.fn(),
}));

const methods = [
  {
    methodCode: "Cash",
    requiredCapability: "BasicPayments",
    integrationMode: "BuiltIn",
    settlementMode: "Immediate",
    availability: "BuiltIn",
    entitled: true,
    isEnabled: true,
    isCheckoutEligible: true,
    comingSoon: false,
    displayName: "Cash",
    requireReference: false,
    branchScope: "AllBranches",
    selectedBranchIds: [],
    instructions: null,
    accountHint: null,
    canConfigure: false,
  },
  {
    methodCode: "ManualGCash",
    requiredCapability: "BasicPayments",
    integrationMode: "Manual",
    settlementMode: "Immediate",
    availability: "BuiltIn",
    entitled: true,
    isEnabled: true,
    isCheckoutEligible: true,
    comingSoon: false,
    displayName: "GCash",
    requireReference: true,
    branchScope: "AllBranches",
    selectedBranchIds: [],
    instructions: null,
    accountHint: null,
    canConfigure: false,
  },
  {
    methodCode: "Utang",
    requiredCapability: "BasicPayments",
    integrationMode: "BuiltIn",
    settlementMode: "Deferred",
    availability: "BuiltIn",
    entitled: true,
    isEnabled: true,
    isCheckoutEligible: true,
    comingSoon: false,
    displayName: "Utang",
    requireReference: false,
    branchScope: "AllBranches",
    selectedBranchIds: [],
    instructions: null,
    accountHint: null,
    canConfigure: false,
  },
  {
    methodCode: "BankTransfer",
    requiredCapability: "PaymentManagement",
    integrationMode: "Manual",
    settlementMode: "Manual",
    availability: "Configurable",
    entitled: true,
    isEnabled: true,
    isCheckoutEligible: true,
    comingSoon: false,
    displayName: "Bank Transfer",
    requireReference: true,
    branchScope: "AllBranches",
    selectedBranchIds: [],
    instructions: null,
    accountHint: null,
    canConfigure: true,
  },
  {
    methodCode: "OnlineGCash",
    requiredCapability: "OnlinePayments",
    integrationMode: "Online",
    settlementMode: "Provider",
    availability: "ComingSoon",
    entitled: false,
    isEnabled: false,
    isCheckoutEligible: false,
    comingSoon: true,
    displayName: "GCash Online",
    requireReference: false,
    branchScope: "AllBranches",
    selectedBranchIds: [],
    instructions: null,
    accountHint: null,
    canConfigure: false,
  },
];

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <PaymentMethodsSettingsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("PaymentMethodsSettingsPage", () => {
  beforeEach(() => {
    workspaceMock.boundWorkspace = { organizationId: ORG_ID, branchId: null };
    workspaceMock.sessionGrant = {
      enabledFeatureCodes: ["store-basic-payments", "store-payment-management"],
    };
    vi.mocked(listPaymentMethods).mockResolvedValue(methods as never);
    vi.mocked(upsertPaymentMethod).mockResolvedValue(methods[3] as never);
    vi.mocked(listBranchManagementSummaries).mockResolvedValue({
      ok: true,
      value: [
        {
          id: BRANCH_MAIN,
          organizationId: ORG_ID,
          code: "MAIN",
          name: "Main Branch",
          branchType: "Retail",
          isPrimary: true,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 0,
          activeDeviceCount: 0,
          areaId: null,
          areaName: null,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
        {
          id: BRANCH_ILOILO,
          organizationId: ORG_ID,
          code: "ILO",
          name: "Iloilo",
          branchType: "Retail",
          isPrimary: false,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 0,
          activeDeviceCount: 0,
          areaId: null,
          areaName: null,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
      ],
    } as never);
  });

  it("opens from Manage Business with branchId=null and does not ask to choose a branch", async () => {
    renderPage();

    expect(await screen.findByTestId("payment-methods-page")).toBeInTheDocument();
    expect(screen.queryByText("workspace.branchRequiredTitle")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-required-panel")).not.toBeInTheDocument();
    expect(listPaymentMethods).toHaveBeenCalledWith(
      { organizationId: ORG_ID, branchId: null },
      expect.anything(),
    );
    expect(screen.getByTestId("payment-methods-built-in")).toHaveTextContent("Cash");
    expect(screen.getByTestId("payment-methods-manual")).toHaveTextContent("Bank Transfer");
  });

  it("lets Pro configure branch availability for manual methods", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("payment-methods-page");

    await user.click(screen.getByTestId("payment-method-scope-selected-BankTransfer"));
    expect(screen.getByTestId("payment-method-branch-list-BankTransfer")).toBeInTheDocument();
    await user.click(screen.getByTestId(`payment-method-branch-BankTransfer-${BRANCH_MAIN}`));
    await user.click(screen.getByTestId("payment-method-save-availability-BankTransfer"));

    await waitFor(() => {
      expect(upsertPaymentMethod).toHaveBeenCalledWith(
        { organizationId: ORG_ID, branchId: null },
        "BankTransfer",
        expect.objectContaining({
          isEnabled: true,
          branchScope: "SelectedBranches",
          selectedBranchIds: [BRANCH_MAIN],
        }),
      );
    });
  });

  it("shows upgrade notice and hides configure controls without Payment Management", async () => {
    workspaceMock.sessionGrant = {
      enabledFeatureCodes: ["store-basic-payments"],
    };
    vi.mocked(listPaymentMethods).mockResolvedValue(
      methods.map((m) =>
        m.methodCode === "BankTransfer"
          ? { ...m, entitled: false, canConfigure: false, isEnabled: false }
          : m,
      ) as never,
    );

    renderPage();
    await screen.findByTestId("payment-methods-page");

    expect(screen.getByTestId("payment-methods-upgrade-management")).toBeInTheDocument();
    expect(screen.queryByTestId("payment-method-toggle-BankTransfer")).not.toBeInTheDocument();
    expect(screen.queryByTestId("payment-method-availability-BankTransfer")).not.toBeInTheDocument();
  });

  it("shows Pro Plus online entitlement as coming soon when entitled", async () => {
    workspaceMock.sessionGrant = {
      enabledFeatureCodes: [
        "store-basic-payments",
        "store-payment-management",
        "store-online-payments",
      ],
    };
    vi.mocked(listPaymentMethods).mockResolvedValue(
      methods.map((m) =>
        m.availability === "ComingSoon" ? { ...m, entitled: true } : m,
      ) as never,
    );

    renderPage();
    await screen.findByTestId("payment-methods-page");

    expect(screen.queryByTestId("payment-methods-upgrade-online")).not.toBeInTheDocument();
    expect(screen.getByTestId("payment-methods-online")).toHaveTextContent("paymentMethods.comingSoon");
  });
});

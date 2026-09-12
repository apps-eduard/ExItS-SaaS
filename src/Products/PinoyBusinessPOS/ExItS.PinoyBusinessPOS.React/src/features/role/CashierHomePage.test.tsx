import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import { CashierHomePage } from "@/features/role/CashierHomePage";

const workspaceState = vi.hoisted(() => ({
  grant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    organizationManagementAuthority: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
  } as PosSessionGrantFacts,
  hasOpenShift: false,
  currentShift: null as null | {
    shiftId: string;
    shiftNumber: string;
    registerId: string;
    registerCode: string;
    registerName: string;
  },
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/selling/SellingModeProvider", () => ({
  useSellingMode: () => ({ enter: vi.fn() }),
}));

vi.mock("@/features/shifts/ShiftContextProvider", () => ({
  useShiftContext: () => ({
    currentShift: workspaceState.currentShift,
    hasOpenShift: workspaceState.hasOpenShift,
    loading: false,
    errorMessage: null,
    denied: false,
    readiness: { ready: false },
    refresh: vi.fn(),
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Test Org",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Main Branch",
      branchType: "Retail",
      experience: "start_selling",
    },
    sessionGrant: workspaceState.grant,
  }),
}));

vi.mock("@/api/pos/pos-reporting-client", () => ({
  getDashboard: vi.fn(async () => ({
    completedSalesTotal: 0,
    completedSaleCount: 0,
  })),
}));

function renderHome() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <CashierHomePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("POS-CASHIER-WORKSPACE-FOCUSED-SHELL-17 Cashier Home", () => {
  beforeEach(() => {
    workspaceState.hasOpenShift = false;
    workspaceState.currentShift = null;
    workspaceState.grant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
    };
  });

  it("renders cashier session + front-counter actions without admin launchers", () => {
    renderHome();
    expect(screen.getByTestId("cashier-home")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-home-session")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-quick-actions")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-primary-open-shift")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-action-orders")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-action-returns")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-action-shift")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-action-register")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-action-switch-workspace")).toBeInTheDocument();
    expect(screen.queryByTestId("manager-quick-actions")).not.toBeInTheDocument();
    expect(screen.queryByTestId("open-inventory")).not.toBeInTheDocument();
    expect(screen.queryByTestId("open-purchasing")).not.toBeInTheDocument();
    expect(screen.queryByTestId("open-dashboard")).not.toBeInTheDocument();
    expect(screen.queryByTestId("open-org-devices")).not.toBeInTheDocument();
  });

  it("primary action becomes Start selling when shift is open", () => {
    workspaceState.hasOpenShift = true;
    workspaceState.currentShift = {
      shiftId: "shift-1",
      shiftNumber: "S-1",
      registerId: "reg-1",
      registerCode: "R1",
      registerName: "Front",
    };
    renderHome();
    expect(screen.getByTestId("cashier-primary-sell")).toBeInTheDocument();
    expect(screen.queryByTestId("cashier-primary-open-shift")).not.toBeInTheDocument();
  });

  it("hides Returns when permission denies it", () => {
    workspaceState.grant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationMember",
      organizationManagementAuthority: false,
      mappedPosRoleCode: "InventoryStaff",
      productLocalRoleCode: "InventoryStaff",
    };
    renderHome();
    expect(screen.queryByTestId("cashier-action-returns")).not.toBeInTheDocument();
  });
});

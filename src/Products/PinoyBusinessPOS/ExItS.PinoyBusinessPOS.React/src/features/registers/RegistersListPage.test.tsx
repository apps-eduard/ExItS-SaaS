import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import { RegistersListPage } from "@/features/registers/RegistersListPage";

const state = vi.hoisted(() => ({
  role: "Cashier" as string,
  currentShift: null as null | {
    shiftId: string;
    shiftNumber: string;
    status: string;
    actorId: string;
    registerId: string;
    registerCode: string;
    registerName: string;
    openingCashAmount: number;
    openedAtUtc: string;
    organizationId: string;
    businessDate: string;
    openingCashCounted: boolean;
    effectiveCashCountMode: string;
    openedBy: string;
    createdAtUtc: string;
    updatedAtUtc: string;
  },
  registers: [] as Array<{
    registerId: string;
    organizationId: string;
    registerCode: string;
    name: string;
    status: string;
    createdAtUtc: string;
    createdBy: string;
    updatedAtUtc: string;
    updatedBy: string;
    hasOpenShift: boolean;
    openShiftActorId?: string | null;
    openShiftId?: string | null;
    openShiftOpenedAtUtc?: string | null;
  }>,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: {
      userId: "paul-actor",
      displayName: "Paul Uy",
    },
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "org-1",
      branchId: "branch-1",
      branchName: "Main Branch",
    },
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: state.role,
      productLocalRoleCode: state.role,
    } as PosSessionGrantFacts,
    deviceEnforcementEnabled: false,
  }),
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (id: string | null | undefined) =>
      id === "mica-actor" ? { displayName: "Mica Uy" } : null,
  }),
}));

vi.mock("@/api/pos/pos-shifts-client", () => ({
  getCurrentCashierShift: vi.fn(async () => state.currentShift),
  getCashierShiftSummary: vi.fn(async () =>
    state.currentShift
      ? {
          shiftId: state.currentShift.shiftId,
          shiftNumber: state.currentShift.shiftNumber,
          status: "Open",
          openingCashAmount: 1000,
          openingCashCounted: true,
          effectiveCashCountMode: "Optional",
          netCashSales: 4850,
          cashSalesTotal: 4850,
          gCashSalesTotal: 0,
          utangSalesTotal: 0,
          cashRefundsTotal: 0,
          totalCashIn: 0,
          totalCashOut: 0,
          expectedCashAmount: 5850,
          completedCashCount: 12,
          voidedCashCount: 0,
          completedGCashCount: 0,
          completedUtangCount: 0,
        }
      : null,
  ),
  isOpenCashierShift: (shift: { status?: string } | null | undefined) =>
    Boolean(shift && shift.status?.toLowerCase() === "open"),
}));

vi.mock("@/api/pos/pos-registers-client", () => ({
  listRegisters: vi.fn(async () => ({
    items: state.registers,
    totalCount: state.registers.length,
    page: 1,
    pageSize: 50,
  })),
}));

vi.mock("@/features/shifts/ensure-pwa-default-register", () => ({
  ensurePwaDefaultCashRegister: vi.fn(async () => ({
    registerId: "reg-free",
    organizationId: "org-1",
    registerCode: "REG-000002",
    name: "PWA-0002",
    status: "Active",
    createdAtUtc: "",
    createdBy: "",
    updatedAtUtc: "",
    updatedBy: "",
    hasOpenShift: false,
  })),
}));

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <RegistersListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("RegistersListPage role UX", () => {
  beforeEach(() => {
    state.role = "Cashier";
    state.currentShift = null;
    state.registers = [];
  });

  it("cashier sees My cash register title and Open shift when no open shift", async () => {
    renderPage();
    expect(screen.getByText("register.myTitle")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("cashier-open-shift")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("registers-list")).not.toBeInTheDocument();
    expect(screen.getByTestId("registers-shifts-nav-link")).toHaveTextContent("shift.myHubTitle");
  });

  it("cashier with open shift sees Continue selling and not Open shift", async () => {
    state.currentShift = {
      shiftId: "shift-paul",
      shiftNumber: "S-1",
      status: "Open",
      actorId: "paul-actor",
      registerId: "reg-2",
      registerCode: "REG-000002",
      registerName: "PWA-0002",
      openingCashAmount: 1000,
      openedAtUtc: "2026-09-08T16:24:00Z",
      organizationId: "org-1",
      businessDate: "2026-09-08",
      openingCashCounted: true,
      effectiveCashCountMode: "Optional",
      openedBy: "paul-actor",
      createdAtUtc: "",
      updatedAtUtc: "",
    };
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("cashier-continue-selling")).toBeInTheDocument();
    });
    expect(screen.getByTestId("cashier-view-my-shift")).toBeInTheDocument();
    expect(screen.getByTestId("cashier-close-shift")).toBeInTheDocument();
    expect(screen.queryByTestId("cashier-open-shift")).not.toBeInTheDocument();
    expect(screen.getByText("REG-000002 — PWA-0002")).toBeInTheDocument();
    expect(screen.queryByText("PWA-0001")).not.toBeInTheDocument();
  });

  it("manager sees all registers with cashier names on open stations", async () => {
    state.role = "StoreManager";
    state.registers = [
      {
        registerId: "reg-1",
        organizationId: "org-1",
        registerCode: "REG-000001",
        name: "PWA-0001",
        status: "Active",
        createdAtUtc: "",
        createdBy: "",
        updatedAtUtc: "",
        updatedBy: "",
        hasOpenShift: true,
        openShiftActorId: "mica-actor",
        openShiftId: "shift-mica",
        openShiftOpenedAtUtc: "2026-09-08T16:10:00Z",
      },
      {
        registerId: "reg-3",
        organizationId: "org-1",
        registerCode: "REG-000003",
        name: "PWA-0003",
        status: "Active",
        createdAtUtc: "",
        createdBy: "",
        updatedAtUtc: "",
        updatedBy: "",
        hasOpenShift: false,
      },
    ];
    renderPage();
    expect(screen.getByText("register.listTitle")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("register-row-reg-1")).toBeInTheDocument();
    });
    expect(screen.getByText("REG-000001 — PWA-0001")).toBeInTheDocument();
    expect(screen.getByText("Mica Uy")).toBeInTheDocument();
    expect(screen.getByText("register.availableStatus")).toBeInTheDocument();
    expect(screen.getByTestId("registers-shifts-nav-link")).toHaveTextContent("shift.hubTitle");
  });
});

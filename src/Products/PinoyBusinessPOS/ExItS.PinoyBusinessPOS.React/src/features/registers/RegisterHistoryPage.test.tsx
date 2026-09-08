import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import { RegisterHistoryPage } from "@/features/registers/RegisterHistoryPage";

const state = vi.hoisted(() => ({
  role: "StoreManager" as string,
  register: {
    registerId: "reg-1",
    organizationId: "org-1",
    registerCode: "REG-000001",
    name: "Front",
    status: "Active",
    createdAtUtc: "",
    createdBy: "",
    updatedAtUtc: "",
    updatedBy: "",
    hasOpenShift: false,
  },
  activity: {
    registerId: "reg-1",
    registerCode: "REG-000001",
    name: "Front",
    status: "Active",
    openShiftCount: 0,
    closedShiftCount: 2,
    completedSaleCount: 5,
    grossSalesTotal: 1250,
  },
  shifts: [
    {
      shiftId: "shift-1",
      organizationId: "org-1",
      shiftNumber: "S-100",
      status: "Closed",
      actorId: "actor-1",
      registerId: "reg-1",
      registerCode: "REG-000001",
      registerName: "Front",
      businessDate: "2026-09-07",
      openingCashAmount: 500,
      openingCashCounted: true,
      effectiveCashCountMode: "Optional",
      openedAtUtc: "2026-09-07T01:00:00Z",
      openedBy: "actor-1",
      createdAtUtc: "",
      updatedAtUtc: "",
      completedTransactionCount: 3,
      completedSalesTotal: 750,
    },
  ],
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "org-1",
      branchId: "branch-1",
      branchName: "Main",
    },
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: state.role,
      productLocalRoleCode: state.role,
    } as PosSessionGrantFacts,
  }),
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: () => ({ displayName: "Mica Uy" }),
    isResolving: false,
  }),
}));

vi.mock("@/api/pos/pos-registers-client", () => ({
  getRegister: vi.fn(async () => state.register),
  getRegisterActivity: vi.fn(async () => state.activity),
}));

vi.mock("@/api/pos/pos-shifts-client", () => ({
  listCashierShifts: vi.fn(async () => ({
    items: state.shifts,
    totalCount: state.shifts.length,
    page: 1,
    pageSize: 50,
  })),
  isOpenCashierShift: (shift: { status?: string } | null | undefined) =>
    Boolean(shift && shift.status?.toLowerCase() === "open"),
}));

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/registers/reg-1/history"]}>
        <Routes>
          <Route path="/registers/:registerId/history" element={<RegisterHistoryPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("RegisterHistoryPage", () => {
  beforeEach(() => {
    state.role = "StoreManager";
  });

  it("shows activity totals and shift history with transaction links", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("register-history-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("register-history-activity")).toBeInTheDocument();
    expect(screen.getByText("5")).toBeInTheDocument();
    expect(screen.getByTestId("register-history-shift-shift-1")).toBeInTheDocument();
    expect(screen.getByTestId("register-history-all-transactions")).toHaveAttribute(
      "href",
      "/registers/reg-1/transactions",
    );
    expect(screen.getByTestId("register-history-shift-txns-shift-1")).toHaveAttribute(
      "href",
      "/shifts/shift-1/transactions",
    );
  });
});

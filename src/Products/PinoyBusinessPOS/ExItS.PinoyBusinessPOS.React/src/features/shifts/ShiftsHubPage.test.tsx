import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import { ShiftsHubPage } from "@/features/shifts/ShiftsHubPage";

const mocks = vi.hoisted(() => ({
  role: "Cashier" as string,
  userId: "paul-actor",
  currentShift: null as null | {
    shiftId: string;
    shiftNumber: string;
    status: string;
    registerCode: string;
    registerName: string;
  },
  listCashierShifts: vi.fn(async () => ({
    items: [
      {
        shiftId: "shift-paul-1",
        organizationId: "org-1",
        shiftNumber: "S-9",
        status: "Closed",
        actorId: "paul-actor",
        registerId: "reg-2",
        registerCode: "REG-000002",
        registerName: "PWA-0002",
        businessDate: "2026-09-07",
        openingCashAmount: 100,
        openingCashCounted: true,
        effectiveCashCountMode: "Optional",
        openedAtUtc: "2026-09-07T01:00:00Z",
        openedBy: "paul-actor",
        createdAtUtc: "",
        updatedAtUtc: "",
        completedTransactionCount: 4,
        completedSalesTotal: 400,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 30,
  })),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: { userId: mocks.userId, displayName: "Paul Uy" },
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
      mappedPosRoleCode: mocks.role,
      productLocalRoleCode: mocks.role,
    } as PosSessionGrantFacts,
  }),
}));

vi.mock("@/features/shifts/ShiftContextProvider", () => ({
  useShiftContext: () => ({
    currentShift: mocks.currentShift,
    loading: false,
    hasOpenShift: Boolean(mocks.currentShift),
    errorMessage: null,
    refresh: vi.fn(),
    readiness: { status: "blocked_no_shift" },
  }),
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (id: string | null | undefined) =>
      id ? { displayName: id === "paul-actor" ? "Paul Uy" : "Other" } : null,
    isResolving: false,
  }),
}));

vi.mock("@/api/pos/pos-registers-client", () => ({
  listRegisters: vi.fn(async () => ({ items: [], totalCount: 0, page: 1, pageSize: 50 })),
}));

vi.mock("@/api/pos/pos-shifts-client", () => ({
  listCashierShifts: mocks.listCashierShifts,
  isOpenCashierShift: (shift: { status?: string } | null | undefined) =>
    Boolean(shift && shift.status?.toLowerCase() === "open"),
}));

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <ShiftsHubPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("ShiftsHubPage my-shifts scoping", () => {
  beforeEach(() => {
    mocks.role = "Cashier";
    mocks.userId = "paul-actor";
    mocks.currentShift = null;
    mocks.listCashierShifts.mockClear();
  });

  it("cashier history requests listCashierShifts with own actorId", async () => {
    renderPage();
    await waitFor(() => {
      expect(mocks.listCashierShifts).toHaveBeenCalled();
    });
    const options = mocks.listCashierShifts.mock.calls[0][1];
    expect(options?.actorId).toBe("paul-actor");
    expect(await screen.findByTestId("shift-history-row-shift-paul-1")).toBeInTheDocument();
    expect(screen.queryByTestId("manager-shifts-history")).not.toBeInTheDocument();
  });

  it("manager history shows filters and does not force actorId", async () => {
    mocks.role = "StoreManager";
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("manager-shifts-history")).toBeInTheDocument();
    });
    expect(screen.getByTestId("manager-shifts-filters")).toBeInTheDocument();
    expect(screen.queryByTestId("cashier-shifts-history")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(mocks.listCashierShifts).toHaveBeenCalled();
    });
    const historyCall = mocks.listCashierShifts.mock.calls.find(
      (call) => !(call[1] && call[1].status === "Open"),
    );
    expect(historyCall?.[1]?.actorId).toBeUndefined();
  });
});

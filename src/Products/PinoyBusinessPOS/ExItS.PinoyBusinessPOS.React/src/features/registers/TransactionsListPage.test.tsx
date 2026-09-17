import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import { TransactionsListPage } from "@/features/registers/TransactionsListPage";
import { listSales } from "@/api/pos/pos-sales-client";

const state = vi.hoisted(() => ({
  role: "StoreManager" as string,
  sales: [
    {
      saleId: "sale-1",
      organizationId: "org-1",
      saleNumber: "SALE-20260908-000004",
      status: "Completed",
      paymentMethod: "Cash",
      subtotal: 702,
      total: 702,
      taxAmount: 0,
      recordedAtUtc: "2026-09-08T20:55:00Z",
      recordedBy: "actor-1",
      updatedAtUtc: "2026-09-08T20:55:00Z",
      lines: [],
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
      branchName: "Main Branch",
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
  getRegister: vi.fn(async () => ({
    registerId: "reg-1",
    organizationId: "org-1",
    registerCode: "REG-000001",
    name: "PWA-0001",
    status: "Active",
    createdAtUtc: "",
    createdBy: "",
    updatedAtUtc: "",
    updatedBy: "",
    hasOpenShift: false,
  })),
}));

vi.mock("@/api/pos/pos-shifts-client", () => ({
  getCashierShift: vi.fn(async () => ({
    shiftId: "shift-1",
    organizationId: "org-1",
    shiftNumber: "S-100",
    status: "Closed",
    actorId: "actor-1",
    registerId: "reg-1",
    registerCode: "REG-000001",
    registerName: "PWA-0001",
    businessDate: "2026-09-08",
    openingCashAmount: 0,
    openingCashCounted: true,
    effectiveCashCountMode: "Optional",
    openedAtUtc: "2026-09-08T01:00:00Z",
    openedBy: "actor-1",
    createdAtUtc: "",
    updatedAtUtc: "",
  })),
}));

vi.mock("@/api/pos/pos-sales-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-sales-client")>(
    "@/api/pos/pos-sales-client",
  );
  return {
    ...actual,
    listSales: vi.fn(async () => ({
      items: state.sales,
      totalCount: state.sales.length,
      page: 1,
      pageSize: 25,
    })),
  };
});

function renderAt(path: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/registers/:registerId/transactions" element={<TransactionsListPage />} />
          <Route path="/shifts/:shiftId/transactions" element={<TransactionsListPage />} />
          <Route path="/sell/sales/:saleId/summary" element={<div data-testid="summary-dest" />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("TransactionsListPage", () => {
  beforeEach(() => {
    state.role = "StoreManager";
    vi.mocked(listSales).mockClear();
  });

  it("lists register transactions with mobile cards and desktop table from one query", async () => {
    renderAt("/registers/reg-1/transactions");
    await waitFor(() => {
      expect(screen.getByTestId("transactions-list-page")).toHaveAttribute("data-scope", "register");
    });

    expect(await screen.findByTestId("transactions-list-cards")).toBeInTheDocument();
    expect(screen.getByTestId("transactions-list-cards").className).toMatch(/\blg:hidden\b/);
    expect(screen.getByTestId("transactions-list-table").className).toMatch(/\bhidden\b/);
    expect(screen.getByTestId("transactions-list-table").className).toMatch(/\blg:block\b/);

    expect(await screen.findByTestId("transaction-card-row-sale-1")).toHaveAttribute(
      "href",
      "/sell/sales/sale-1/summary",
    );
    expect(screen.getByTestId("transaction-table-row-sale-1")).toBeInTheDocument();
    expect(screen.getByTestId("transaction-row-sale-1")).toHaveTextContent("SALE-20260908-000004");
    expect(screen.getAllByText("Mica Uy").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Completed").length).toBeGreaterThan(0);

    expect(vi.mocked(listSales)).toHaveBeenCalledTimes(1);
    const options = vi.mocked(listSales).mock.calls[0][1];
    expect(options?.registerId).toBe("reg-1");
    expect(options?.pageSize).toBe(25);
  });

  it("desktop table row opens transaction summary", async () => {
    const user = userEvent.setup();
    renderAt("/registers/reg-1/transactions");
    await screen.findByTestId("transaction-table-row-sale-1");
    await user.click(screen.getByTestId("transaction-table-row-sale-1"));
    expect(await screen.findByTestId("summary-dest")).toBeInTheDocument();
  });

  it("lists shift transactions scoped by shift id without cashier column", async () => {
    renderAt("/shifts/shift-1/transactions");
    await waitFor(() => {
      expect(screen.getByTestId("transactions-list-page")).toHaveAttribute("data-scope", "shift");
    });
    expect(await screen.findByTestId("transaction-card-row-sale-1")).toHaveAttribute(
      "href",
      "/sell/sales/sale-1/summary",
    );
    expect(screen.queryByText("transactions.col.cashier")).not.toBeInTheDocument();
    const options = vi.mocked(listSales).mock.calls.at(-1)?.[1];
    expect(options?.cashierShiftId).toBe("shift-1");
    expect(options?.shiftId).toBe("shift-1");
  });
});

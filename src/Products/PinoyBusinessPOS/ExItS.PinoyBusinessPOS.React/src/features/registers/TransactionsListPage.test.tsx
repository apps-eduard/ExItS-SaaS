import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
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
      saleNumber: "SALE-100",
      status: "Completed",
      paymentMethod: "Cash",
      subtotal: 100,
      total: 100,
      taxAmount: 0,
      recordedAtUtc: "2026-09-08T10:00:00Z",
      recordedBy: "actor-1",
      updatedAtUtc: "2026-09-08T10:00:00Z",
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
    resolve: () => ({ displayName: "Paul Uy" }),
    isResolving: false,
  }),
}));

vi.mock("@/api/pos/pos-registers-client", () => ({
  getRegister: vi.fn(async () => ({
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
    registerName: "Front",
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
      pageSize: 20,
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

  it("lists register transactions and links to sale summary", async () => {
    renderAt("/registers/reg-1/transactions");
    await waitFor(() => {
      expect(screen.getByTestId("transactions-list-page")).toHaveAttribute("data-scope", "register");
    });
    const row = await screen.findByTestId("transaction-row-sale-1");
    expect(row).toHaveAttribute("href", "/sell/sales/sale-1/summary");
    expect(vi.mocked(listSales)).toHaveBeenCalled();
    const options = vi.mocked(listSales).mock.calls[0][1];
    expect(options?.registerId).toBe("reg-1");
  });

  it("lists shift transactions scoped by shift id", async () => {
    renderAt("/shifts/shift-1/transactions");
    await waitFor(() => {
      expect(screen.getByTestId("transactions-list-page")).toHaveAttribute("data-scope", "shift");
    });
    expect(await screen.findByTestId("transaction-row-sale-1")).toHaveAttribute(
      "href",
      "/sell/sales/sale-1/summary",
    );
    const options = vi.mocked(listSales).mock.calls.at(-1)?.[1];
    expect(options?.cashierShiftId).toBe("shift-1");
    expect(options?.shiftId).toBe("shift-1");
  });
});

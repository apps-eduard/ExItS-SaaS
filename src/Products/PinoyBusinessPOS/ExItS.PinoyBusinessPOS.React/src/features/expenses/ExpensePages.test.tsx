import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as expenseClient from "@/api/pos/pos-expense-client";
import { ExpenseCategoriesPage } from "@/features/expenses/ExpenseCategoriesPage";
import { ExpenseCreatePage } from "@/features/expenses/ExpenseCreatePage";
import { ExpenseDetailPage } from "@/features/expenses/ExpenseDetailPage";
import { ExpenseListPage } from "@/features/expenses/ExpenseListPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const categoryId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const expenseId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const actorId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

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

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (id: string | null | undefined) =>
      id
        ? {
            actorId: id,
            displayName: "Org Cashier",
            actorStatus: "Active",
          }
        : null,
    isLoading: false,
    data: [],
  }),
}));

vi.mock("@/lib/secure-mutation-id", () => ({
  createSecureMutationId: () => ({ ok: true as const, id: expenseId }),
}));

function recordedExpense(overrides: Partial<expenseClient.PosExpenseDto> = {}): expenseClient.PosExpenseDto {
  return {
    expenseId,
    organizationId: orgId,
    expenseNumber: "EXP-20260829-0001",
    categoryId,
    categoryName: "Rent",
    status: "Recorded",
    paymentMethod: "Cash",
    amount: 5000,
    description: "August rent",
    payee: "Landlord",
    gCashReference: null,
    expenseDate: "2026-08-29",
    recordedAtUtc: "2026-08-29T10:00:00Z",
    recordedBy: actorId,
    voidedAtUtc: null,
    voidedBy: null,
    voidReason: null,
    updatedAtUtc: "2026-08-29T10:00:00Z",
    ...overrides,
  };
}

function activeCategory(
  overrides: Partial<expenseClient.PosExpenseCategoryDto> = {},
): expenseClient.PosExpenseCategoryDto {
  return {
    categoryId,
    organizationId: orgId,
    name: "Rent",
    status: "Active",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    ...overrides,
  };
}

function emptySummary(): expenseClient.PosExpenseSummaryDto {
  return {
    fromDate: null,
    toDate: null,
    grossTotal: 0,
    voidedTotal: 0,
    netTotal: 0,
    recordedCount: 0,
    voidedCount: 0,
    byCategory: [],
    byPaymentMethod: [],
  };
}

describe("Expense React CRUD", () => {
  beforeEach(() => {
    workspaceMock.boundWorkspace.branchName = "Main Branch";
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
      mappedPosRoleCode: "Owner",
    };
    vi.spyOn(expenseClient, "listExpenses").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.spyOn(expenseClient, "getExpenseSummary").mockResolvedValue(emptySummary());
    vi.spyOn(expenseClient, "listExpenseCategories").mockResolvedValue({
      items: [activeCategory()],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows organization-wide scope and does not claim Main Branch expenses", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("expense-list-page")).toBeInTheDocument();
    const banner = screen.getByTestId("expense-org-scope-banner");
    expect(banner).toHaveTextContent(/Organization-wide/i);
    expect(banner).not.toHaveTextContent(/Main Branch/i);
    expect(screen.queryByText(/Main Branch Expenses/i)).not.toBeInTheDocument();
  });

  it("shows empty state with record CTA for manage users", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByText("No expenses recorded yet.")).toBeInTheDocument();
    expect(screen.getByTestId("expense-new")).toBeInTheDocument();
  });

  it("hides record CTA for view-only users", async () => {
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationMember",
      productLocalRoleCode: "ReportingUser",
      mappedPosRoleCode: "ReportingUser",
    };

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("expense-list-page")).toBeInTheDocument();
    expect(screen.queryByTestId("expense-new")).not.toBeInTheDocument();
    expect(screen.getByTestId("expense-open-categories")).toBeInTheDocument();
  });

  it("renders summary from server aggregates", async () => {
    vi.spyOn(expenseClient, "getExpenseSummary").mockResolvedValue({
      fromDate: null,
      toDate: null,
      grossTotal: 7000,
      voidedTotal: 2000,
      netTotal: 5000,
      recordedCount: 2,
      voidedCount: 1,
      byCategory: [{ categoryId, categoryName: "Rent", totalAmount: 5000, count: 1 }],
      byPaymentMethod: [{ paymentMethod: "Cash", totalAmount: 5000, count: 1 }],
    });
    vi.spyOn(expenseClient, "listExpenses").mockResolvedValue({
      items: [recordedExpense()],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("expense-summary-net")).toBeInTheDocument();
    expect(screen.getByTestId("expense-summary-by-category")).toHaveTextContent("Rent");
    expect(screen.getByTestId("expense-summary-by-category")).toHaveTextContent("71.4%");
    expect(screen.getByTestId("expense-summary-by-payment")).toHaveTextContent("Cash");
    expect(screen.getByTestId("expense-summary-by-payment")).toHaveTextContent("71.4%");
    expect(screen.getByTestId(`expense-row-${expenseId}`)).toHaveTextContent("EXP-20260829-0001");
    expect(screen.getByTestId("expense-org-scope-banner")).toHaveTextContent(
      "Organization-wide — not limited to the current branch.",
    );
  });

  it("passes list filters to the server client", async () => {
    const listSpy = vi.spyOn(expenseClient, "listExpenses").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-filters");
    await user.selectOptions(screen.getByTestId("expense-filter-status"), "Voided");
    await user.selectOptions(screen.getByTestId("expense-filter-payment"), "ManualGCash");
    await user.type(screen.getByTestId("expense-filter-number"), "EXP-9");
    await user.type(screen.getByTestId("expense-filter-from"), "2026-08-01");
    await user.type(screen.getByTestId("expense-filter-to"), "2026-08-31");

    await waitFor(() => {
      expect(listSpy).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: orgId }),
        expect.objectContaining({
          status: "Voided",
          paymentMethod: "ManualGCash",
          expenseNumber: "EXP-9",
          fromDate: "2026-08-01",
          toDate: "2026-08-31",
        }),
        expect.anything(),
      );
    });
    expect(listSpy.mock.calls.some((c) => c[0] && "branchId" in (c[0] as object) && (c[0] as { branchId?: string }).branchId)).toBe(
      false,
    );
  });

  it("shows filtered empty copy when filters return none", async () => {
    vi.spyOn(expenseClient, "listExpenses").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    await screen.findByTestId("expense-filters");
    await user.selectOptions(screen.getByTestId("expense-filter-status"), "Voided");
    expect(await screen.findByText("No expenses match these filters.")).toBeInTheDocument();
  });

  it("blocks create when no active categories", async () => {
    vi.spyOn(expenseClient, "listExpenseCategories").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 100,
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses/new"]}>
          <Routes>
            <Route path="/expenses/new" element={<ExpenseCreatePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("expense-no-categories")).toBeInTheDocument();
    expect(screen.getByTestId("expense-create-category-cta")).toBeInTheDocument();
    expect(screen.queryByTestId("expense-submit")).not.toBeInTheDocument();
  });

  it("validates amount and description before submit", async () => {
    const recordSpy = vi.spyOn(expenseClient, "recordExpense");
    const user = userEvent.setup();

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses/new"]}>
          <Routes>
            <Route path="/expenses/new" element={<ExpenseCreatePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const category = await screen.findByTestId("expense-category");
    await user.selectOptions(category, categoryId);
    await user.clear(screen.getByTestId("expense-amount"));
    await user.type(screen.getByTestId("expense-amount"), "0");
    await user.clear(screen.getByTestId("expense-description"));
    await user.click(screen.getByTestId("expense-submit"));
    expect(await screen.findByText("Enter an amount greater than zero.")).toBeInTheDocument();
    expect(recordSpy).not.toHaveBeenCalled();

    await user.clear(screen.getByTestId("expense-amount"));
    await user.type(screen.getByTestId("expense-amount"), "5000");
    await user.click(screen.getByTestId("expense-submit"));
    expect(await screen.findByText("Description is required.")).toBeInTheDocument();
    expect(recordSpy).not.toHaveBeenCalled();
  });

  it("shows GCash reference only for ManualGCash and records Cash expense", async () => {
    const recordSpy = vi.spyOn(expenseClient, "recordExpense").mockResolvedValue(recordedExpense());
    const user = userEvent.setup();

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses/new"]}>
          <Routes>
            <Route path="/expenses/new" element={<ExpenseCreatePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-category");
    expect(screen.queryByTestId("expense-gcash-reference")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("expense-payment-ManualGCash"));
    expect(screen.getByTestId("expense-gcash-reference")).toBeInTheDocument();

    await user.click(screen.getByTestId("expense-payment-Cash"));
    expect(screen.queryByTestId("expense-gcash-reference")).not.toBeInTheDocument();

    await user.selectOptions(screen.getByTestId("expense-category"), categoryId);
    await user.clear(screen.getByTestId("expense-amount"));
    await user.type(screen.getByTestId("expense-amount"), "5000");
    await user.clear(screen.getByTestId("expense-description"));
    await user.type(screen.getByTestId("expense-description"), "August rent");
    await user.click(screen.getByTestId("expense-submit"));

    await waitFor(() => {
      expect(recordSpy).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: orgId }),
        expect.objectContaining({
          categoryId,
          paymentMethod: "Cash",
          amount: 5000,
          description: "August rent",
          gCashReference: null,
          expenseId,
        }),
      );
    });
    expect(await screen.findByTestId("expense-record-success")).toBeInTheDocument();
    expect(screen.getByTestId("expense-recorded-number")).toHaveTextContent("EXP-20260829-0001");
  });

  it("records ManualGCash with optional blank reference", async () => {
    const recordSpy = vi
      .spyOn(expenseClient, "recordExpense")
      .mockResolvedValue(
        recordedExpense({ paymentMethod: "ManualGCash", amount: 2000, gCashReference: null }),
      );
    const user = userEvent.setup();

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses/new"]}>
          <Routes>
            <Route path="/expenses/new" element={<ExpenseCreatePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-category");
    await user.selectOptions(screen.getByTestId("expense-category"), categoryId);
    await user.click(screen.getByTestId("expense-payment-ManualGCash"));
    await user.clear(screen.getByTestId("expense-amount"));
    await user.type(screen.getByTestId("expense-amount"), "2000");
    await user.type(screen.getByTestId("expense-description"), "Power");
    await user.click(screen.getByTestId("expense-submit"));

    await waitFor(() => {
      expect(recordSpy).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({
          paymentMethod: "ManualGCash",
          gCashReference: null,
        }),
      );
    });
  });

  it("shows immutable detail without edit and voids with reason", async () => {
    vi.spyOn(expenseClient, "getExpense").mockResolvedValue(recordedExpense());
    const voidSpy = vi.spyOn(expenseClient, "voidExpense").mockResolvedValue(
      recordedExpense({
        status: "Voided",
        voidReason: "Duplicate",
        voidedAtUtc: "2026-08-29T12:00:00Z",
        voidedBy: actorId,
      }),
    );
    const user = userEvent.setup();

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/expenses/${expenseId}`]}>
          <Routes>
            <Route path="/expenses/:expenseId" element={<ExpenseDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("expense-detail-page")).toBeInTheDocument();
    expect(screen.getByTestId("expense-no-edit")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /edit/i })).not.toBeInTheDocument();

    await user.click(screen.getByTestId("expense-void-open"));
    const dialog = screen.getByTestId("expense-void-dialog");
    await user.type(within(dialog).getByTestId("expense-void-reason-input"), "Duplicate");
    await user.click(within(dialog).getByTestId("expense-void-confirm"));

    await waitFor(() => {
      expect(voidSpy).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: orgId }),
        expenseId,
        "Duplicate",
      );
    });
  });

  it("requires void reason before confirm is enabled", async () => {
    vi.spyOn(expenseClient, "getExpense").mockResolvedValue(recordedExpense());
    const voidSpy = vi.spyOn(expenseClient, "voidExpense");
    const user = userEvent.setup();

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/expenses/${expenseId}`]}>
          <Routes>
            <Route path="/expenses/:expenseId" element={<ExpenseDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-detail-page");
    await user.click(screen.getByTestId("expense-void-open"));
    expect(screen.getByTestId("expense-void-confirm")).toBeDisabled();
    expect(voidSpy).not.toHaveBeenCalled();
  });

  it("manages categories: create, rename, deactivate, reactivate", async () => {
    let categories = [activeCategory()];
    vi.spyOn(expenseClient, "listExpenseCategories").mockImplementation(async () => ({
      items: categories,
      totalCount: categories.length,
      page: 1,
      pageSize: 50,
    }));
    const createSpy = vi.spyOn(expenseClient, "createExpenseCategory").mockImplementation(async (_w, body) => {
      const created = activeCategory({
        categoryId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        name: body.name,
      });
      categories = [...categories, created];
      return created;
    });
    const updateSpy = vi.spyOn(expenseClient, "updateExpenseCategory").mockImplementation(async (_w, id, body) => {
      categories = categories.map((c) =>
        c.categoryId === id
          ? { ...c, name: body.name, updatedAtUtc: "2026-08-29T11:00:00Z" }
          : c,
      );
      return categories.find((c) => c.categoryId === id)!;
    });
    const deactivateSpy = vi
      .spyOn(expenseClient, "deactivateExpenseCategory")
      .mockImplementation(async (_w, id) => {
        categories = categories.map((c) =>
          c.categoryId === id ? { ...c, status: "Inactive" } : c,
        );
        return categories.find((c) => c.categoryId === id)!;
      });
    const reactivateSpy = vi
      .spyOn(expenseClient, "reactivateExpenseCategory")
      .mockImplementation(async (_w, id) => {
        categories = categories.map((c) =>
          c.categoryId === id ? { ...c, status: "Active" } : c,
        );
        return categories.find((c) => c.categoryId === id)!;
      });

    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses/categories"]}>
          <Routes>
            <Route path="/expenses/categories" element={<ExpenseCategoriesPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-categories-page");
    await user.type(screen.getByTestId("expense-category-name"), "Electricity");
    await user.click(screen.getByTestId("expense-category-create-submit"));
    await waitFor(() => expect(createSpy).toHaveBeenCalled());

    await user.click(screen.getByTestId(`expense-category-edit-${categoryId}`));
    const nameInput = screen.getByTestId("expense-category-edit-name");
    await user.clear(nameInput);
    await user.type(nameInput, "Utilities");
    await user.click(screen.getByTestId("expense-category-save"));
    await waitFor(() => {
      expect(updateSpy).toHaveBeenCalledWith(
        expect.anything(),
        categoryId,
        expect.objectContaining({
          name: "Utilities",
          expectedUpdatedAtUtc: "2026-08-01T00:00:00Z",
        }),
      );
    });

    vi.spyOn(window, "confirm").mockReturnValue(true);
    await user.click(screen.getByTestId(`expense-category-deactivate-${categoryId}`));
    await waitFor(() => expect(deactivateSpy).toHaveBeenCalledWith(expect.anything(), categoryId));

    await user.click(screen.getByTestId(`expense-category-reactivate-${categoryId}`));
    await waitFor(() => expect(reactivateSpy).toHaveBeenCalledWith(expect.anything(), categoryId));
  });

  it("excludes inactive categories from new expense selector", async () => {
    vi.spyOn(expenseClient, "listExpenseCategories").mockResolvedValue({
      items: [activeCategory({ status: "Active", name: "Rent" })],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses/new"]}>
          <Routes>
            <Route path="/expenses/new" element={<ExpenseCreatePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-create-page");
    expect(expenseClient.listExpenseCategories).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ status: "Active" }),
      expect.anything(),
    );
  });

  it("resets filters when organization changes", async () => {
    const listSpy = vi.spyOn(expenseClient, "listExpenses").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    const user = userEvent.setup();
    const { rerender } = render(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("expense-filters");
    await user.selectOptions(screen.getByTestId("expense-filter-status"), "Voided");
    await waitFor(() => {
      expect(listSpy).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ status: "Voided" }),
        expect.anything(),
      );
    });

    workspaceMock.boundWorkspace.organizationId = "22222222-2222-2222-2222-222222222222";
    rerender(
      <AppProviders>
        <MemoryRouter initialEntries={["/expenses"]}>
          <Routes>
            <Route path="/expenses" element={<ExpenseListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      const last = listSpy.mock.calls.at(-1);
      expect(last?.[0]).toEqual(
        expect.objectContaining({ organizationId: "22222222-2222-2222-2222-222222222222" }),
      );
      expect(last?.[1]).toEqual(expect.objectContaining({ status: undefined }));
    });

    workspaceMock.boundWorkspace.organizationId = orgId;
  });
});

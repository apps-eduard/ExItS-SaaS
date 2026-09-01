import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as stockCountClient from "@/api/pos/pos-stock-count-client";
import { StockCountDetailPage } from "@/features/inventory/StockCountDetailPage";
import { StockCountListPage } from "@/features/inventory/StockCountListPage";

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

const cokeId = "11111111-1111-1111-1111-111111111111";
const riceId = "22222222-2222-2222-2222-222222222222";
const countId = "33333333-3333-3333-3333-333333333333";

function draftCount() {
  return {
    stockCountId: countId,
    organizationId: workspace.organizationId,
    countNumber: null,
    title: "Monthly Count",
    status: "Draft",
    countDate: "2026-08-29",
    notes: null,
    startedAtUtc: null,
    startedBy: null,
    completedAtUtc: null,
    completedBy: null,
    cancelledAtUtc: null,
    cancelledBy: null,
    createdAtUtc: "2026-08-29T08:00:00Z",
    updatedAtUtc: "2026-08-29T08:00:00Z",
    lines: [
      {
        lineId: "44444444-4444-4444-4444-444444444441",
        productId: cokeId,
        productName: "Coke 330ml",
        unitOfMeasure: "pcs",
        lineNumber: 1,
        systemOnHandSnapshot: null,
        countedQuantity: null,
        variance: null,
      },
      {
        lineId: "44444444-4444-4444-4444-444444444442",
        productId: riceId,
        productName: "Rice 5kg",
        unitOfMeasure: "bag",
        lineNumber: 2,
        systemOnHandSnapshot: null,
        countedQuantity: null,
        variance: null,
      },
    ],
  };
}

function inProgressCount() {
  const base = draftCount();
  return {
    ...base,
    countNumber: "SC-20260829-0001",
    status: "InProgress",
    startedAtUtc: "2026-08-29T09:00:00Z",
    startedBy: "99999999-9999-9999-9999-999999999999",
    lines: base.lines.map((line) =>
      line.productId === cokeId
        ? { ...line, systemOnHandSnapshot: 24, countedQuantity: null, variance: null }
        : { ...line, systemOnHandSnapshot: 10, countedQuantity: null, variance: null },
    ),
  };
}

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    sessionGrant: {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
    },
  }),
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

function renderList() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/inventory/stock-counts"]}>
        <Routes>
          <Route path="/inventory/stock-counts" element={<StockCountListPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

function renderDetail(id = countId) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/inventory/stock-counts/${id}`]}>
        <Routes>
          <Route path="/inventory/stock-counts/:stockCountId" element={<StockCountDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("Stock Count React flow", () => {
  beforeEach(() => {
    vi.spyOn(stockCountClient, "listStockCounts").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows empty list state and new CTA", async () => {
    renderList();
    expect(await screen.findByTestId("stock-count-list-page")).toBeInTheDocument();
    expect(await screen.findByText("No stock counts yet")).toBeInTheDocument();
    expect(screen.getByTestId("stock-count-empty-cta")).toBeInTheDocument();
    expect(screen.getByTestId("stock-count-scope-note")).toHaveTextContent(/organization total/i);
  });

  it("lists stock counts with status filters and pagination hooks", async () => {
    const item = { ...draftCount(), title: "Weekly count" };
    vi.spyOn(stockCountClient, "listStockCounts").mockResolvedValue({
      items: [item],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    renderList();
    expect(await screen.findByTestId(`stock-count-row-${countId}`)).toBeInTheDocument();
    expect(screen.getByText("Weekly count")).toBeInTheDocument();
    expect(screen.getByTestId(`stock-count-row-${countId}`)).toHaveTextContent("Draft");
    await userEvent.click(screen.getByTestId("stock-count-filter-InProgress"));
    await waitFor(() => {
      expect(stockCountClient.listStockCounts).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ status: "InProgress" }),
        expect.anything(),
      );
    });
  });

  it("draft detail supports start confirmation and cancel", async () => {
    const getSpy = vi.spyOn(stockCountClient, "getStockCount").mockResolvedValue(draftCount() as never);
    const startSpy = vi
      .spyOn(stockCountClient, "startStockCount")
      .mockResolvedValue(inProgressCount() as never);
    vi.spyOn(window, "confirm").mockReturnValue(true);

    renderDetail();
    expect(await screen.findByTestId("stock-count-detail-page")).toHaveAttribute("data-status", "Draft");
    expect(screen.getByText("Coke 330ml")).toBeInTheDocument();

    await userEvent.click(screen.getByTestId("stock-count-start"));
    await waitFor(() => expect(startSpy).toHaveBeenCalled());
    expect(window.confirm).toHaveBeenCalled();
    expect(getSpy).toHaveBeenCalled();
  });

  it("in-progress screen uses system snapshot and preview variance including explicit zero", async () => {
    vi.spyOn(stockCountClient, "getStockCount").mockResolvedValue(inProgressCount() as never);
    const saveSpy = vi.spyOn(stockCountClient, "updateStockCount").mockResolvedValue({
      ...inProgressCount(),
      lines: inProgressCount().lines.map((line) =>
        line.productId === cokeId
          ? { ...line, countedQuantity: 0, variance: -24 }
          : { ...line, countedQuantity: 10, variance: 0 },
      ),
    } as never);

    renderDetail();
    expect(await screen.findByTestId("stock-count-detail-page")).toHaveAttribute(
      "data-status",
      "InProgress",
    );
    expect(screen.getByTestId(`stock-count-system-${cokeId}`)).toHaveTextContent("24");

    const qty = screen.getByTestId(`stock-count-qty-${cokeId}`);
    await userEvent.clear(qty);
    await userEvent.type(qty, "0");
    expect(screen.getByTestId(`stock-count-variance-${cokeId}`)).toHaveTextContent("-24");

    const riceQty = screen.getByTestId(`stock-count-qty-${riceId}`);
    await userEvent.clear(riceQty);
    await userEvent.type(riceQty, "10");
    expect(screen.getByTestId(`stock-count-variance-${riceId}`)).toHaveTextContent("0");

    await userEvent.click(screen.getByTestId("stock-count-save-progress"));
    await waitFor(() => expect(saveSpy).toHaveBeenCalled());
    const payload = saveSpy.mock.calls[0]?.[2];
    expect(payload?.lines).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ productId: cokeId, countedQuantity: 0 }),
        expect.objectContaining({ productId: riceId, countedQuantity: 10 }),
      ]),
    );
  });

  it("review blocks complete when lines remain uncounted", async () => {
    vi.spyOn(stockCountClient, "getStockCount").mockResolvedValue(inProgressCount() as never);
    renderDetail();
    await screen.findByTestId("stock-count-detail-page");
    await userEvent.click(screen.getByTestId("stock-count-review"));
    const review = await screen.findByTestId("stock-count-review-page");
    expect(within(review).getByTestId("stock-count-remaining")).toBeInTheDocument();
    expect(screen.getByTestId("stock-count-complete")).toBeDisabled();
  });

  it("completed count is read-only and shows reconciled", async () => {
    const completed = {
      ...inProgressCount(),
      status: "Completed",
      completedAtUtc: "2026-08-29T10:00:00Z",
      lines: inProgressCount().lines.map((line) =>
        line.productId === cokeId
          ? { ...line, countedQuantity: 22, variance: -2 }
          : { ...line, countedQuantity: 10, variance: 0 },
      ),
    };
    vi.spyOn(stockCountClient, "getStockCount").mockResolvedValue(completed as never);
    renderDetail();
    expect(await screen.findByTestId("stock-count-detail-page")).toHaveAttribute(
      "data-status",
      "Completed",
    );
    expect(screen.getByTestId("stock-count-reconciled")).toBeInTheDocument();
    expect(screen.queryByTestId("stock-count-start")).not.toBeInTheDocument();
    expect(screen.queryByTestId("stock-count-qty-mobile-" + cokeId)).not.toBeInTheDocument();
  });
});

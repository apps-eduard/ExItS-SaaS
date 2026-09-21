import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { IncomingOrderReceivingIssuesPanel } from "@/features/purchasing/IncomingOrderReceivingIssuesPanel";
import type { ConnectedPoReceivingIssue } from "@/api/pos/pos-connected-suppliers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

const resolveIncomingOrderReceivingIssue = vi.fn();
const toastSuccess = vi.fn();
const toastError = vi.fn();

vi.mock("@/api/pos/pos-connected-suppliers-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-connected-suppliers-client")>(
    "@/api/pos/pos-connected-suppliers-client",
  );
  return {
    ...actual,
    resolveIncomingOrderReceivingIssue: (...args: unknown[]) => resolveIncomingOrderReceivingIssue(...args),
  };
});

vi.mock("@/components/exits/ToastProvider", () => ({
  useExitsToast: () => ({
    success: toastSuccess,
    error: toastError,
    info: vi.fn(),
    warning: vi.fn(),
  }),
}));

vi.mock("@/access/pos-commercial-errors", () => ({
  describePosApiError: (err: unknown, fallback: string) =>
    err instanceof Error ? err.message : fallback,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

const workspace: PosWorkspaceScope = {
  organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
};

const orderId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function pendingIssue(overrides?: Partial<ConnectedPoReceivingIssue>): ConnectedPoReceivingIssue {
  return {
    receivingIssueId: "11111111-1111-1111-1111-111111111111",
    connectedPurchaseOrderId: orderId,
    purchaseOrderId: "22222222-2222-2222-2222-222222222222",
    goodsReceiptId: "33333333-3333-3333-3333-333333333333",
    buyerOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    sellerOrganizationId: workspace.organizationId,
    fulfillmentSourceId: "44444444-4444-4444-4444-444444444444",
    status: "PendingSellerReview",
    createdAtUtc: "2026-09-21T00:00:00Z",
    createdByUserId: "55555555-5555-5555-5555-555555555555",
    resolvedAtUtc: null,
    resolvedByUserId: null,
    sellerNotes: null,
    unresolvedLineCount: 1,
    lines: [
      {
        receivingIssueLineId: "66666666-6666-6666-6666-666666666666",
        goodsReceiptLineId: "77777777-7777-7777-7777-777777777777",
        purchaseOrderLineId: "88888888-8888-8888-8888-888888888888",
        supplierProductId: "99999999-9999-9999-9999-999999999999",
        buyerProductId: "99999999-9999-9999-9999-999999999999",
        nameSnapshot: "Coke 1.5L",
        uomSnapshot: "Piece",
        fulfillmentSourceId: "44444444-4444-4444-4444-444444444444",
        shippedQty: 10,
        goodQty: 9,
        damagedQty: 0,
        missingQty: 1,
        lineKind: "Missing",
        buyerDiscrepancyKind: "Short",
        buyerDiscrepancyNote: null,
        missingResolution: null,
        damagedResolution: null,
        resolutionQty: 0,
        sellerNote: null,
        inventoryMovementId: null,
        returnBatchId: null,
        resolvedAtUtc: null,
        resolvedByUserId: null,
        isResolved: false,
        inventoryEffectPreview: "Select a resolution to preview inventory effect.",
      },
    ],
    ...overrides,
  };
}

function damagedIssue(): ConnectedPoReceivingIssue {
  const base = pendingIssue({ unresolvedLineCount: 1 });
  return {
    ...base,
    lines: [
      {
        ...base.lines[0]!,
        lineKind: "Damaged",
        damagedQty: 1,
        missingQty: 0,
        goodQty: 9,
        buyerDiscrepancyKind: "Damaged",
      },
    ],
  };
}

function renderPanel(
  issues: ConnectedPoReceivingIssue[],
  canManage = true,
  client?: QueryClient,
) {
  const queryClient =
    client ??
    new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
  return {
    queryClient,
    ...render(
      <QueryClientProvider client={queryClient}>
        <IncomingOrderReceivingIssuesPanel
          workspace={workspace}
          orderId={orderId}
          issues={issues}
          canManage={canManage}
        />
      </QueryClientProvider>,
    ),
  };
}

describe("IncomingOrderReceivingIssuesPanel", () => {
  beforeEach(() => {
    resolveIncomingOrderReceivingIssue.mockReset();
    toastSuccess.mockReset();
    toastError.mockReset();
  });

  it("shows pending seller review state", () => {
    renderPanel([pendingIssue()]);
    expect(screen.getByText("incomingOrders.receivingIssues.pendingReview")).toBeInTheDocument();
    expect(screen.getAllByText("incomingOrders.receivingIssues.statusPending").length).toBeGreaterThanOrEqual(1);
  });

  it("shows shipped / good / damaged / missing quantities", () => {
    renderPanel([pendingIssue()]);
    expect(screen.getByText(/incomingOrders.receivingIssues.shipped/)).toBeInTheDocument();
    expect(screen.getByText(/incomingOrders.receivingIssues.good/)).toBeInTheDocument();
    expect(screen.getByText(/incomingOrders.receivingIssues.damaged/)).toBeInTheDocument();
    expect(screen.getByText(/incomingOrders.receivingIssues.missing/)).toBeInTheDocument();
    expect(screen.getByText("Coke 1.5L")).toBeInTheDocument();
  });

  it("offers missing resolution choices when reviewing", async () => {
    const user = userEvent.setup();
    renderPanel([pendingIssue()]);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    const select = screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution");
    expect(within(select).getByText("incomingOrders.receivingIssues.resolution.FoundAtSeller")).toBeInTheDocument();
    expect(within(select).getByText("incomingOrders.receivingIssues.resolution.LostInTransit")).toBeInTheDocument();
    expect(within(select).getByText("incomingOrders.receivingIssues.resolution.Other")).toBeInTheDocument();
  });

  it("previews FoundAtSeller inventory restore", async () => {
    const user = userEvent.setup();
    renderPanel([pendingIssue()]);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    await user.selectOptions(
      screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution"),
      "FoundAtSeller",
    );
    expect(screen.getByText(/incomingOrders.receivingIssues.previewRestore/)).toBeInTheDocument();
  });

  it("previews LostInTransit as no stock change", async () => {
    const user = userEvent.setup();
    renderPanel([pendingIssue()]);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    await user.selectOptions(
      screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution"),
      "LostInTransit",
    );
    expect(screen.getByText("incomingOrders.receivingIssues.previewNone")).toBeInTheDocument();
  });

  it("offers damaged resolution choices including ReturnRequested", async () => {
    const user = userEvent.setup();
    renderPanel([damagedIssue()]);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    const select = screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution");
    expect(within(select).getByText("incomingOrders.receivingIssues.resolution.AcceptedNoReturn")).toBeInTheDocument();
    expect(within(select).getByText("incomingOrders.receivingIssues.resolution.ReturnRequested")).toBeInTheDocument();
    await user.selectOptions(select, "ReturnRequested");
    expect(screen.getByText("incomingOrders.receivingIssues.previewReturn")).toBeInTheDocument();
  });

  it("requires a note for Other resolution", async () => {
    const user = userEvent.setup();
    renderPanel([pendingIssue()]);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    await user.selectOptions(
      screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution"),
      "Other",
    );
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.confirm" }));
    await waitFor(() => {
      expect(toastError).toHaveBeenCalled();
    });
    expect(resolveIncomingOrderReceivingIssue).not.toHaveBeenCalled();
  });

  it("invalidates incoming-order queries on successful resolve", async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");
    resolveIncomingOrderReceivingIssue.mockResolvedValue(pendingIssue({ unresolvedLineCount: 0, status: "Resolved" }));

    renderPanel([pendingIssue()], true, queryClient);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    await user.selectOptions(
      screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution"),
      "FoundAtSeller",
    );
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.confirm" }));

    await waitFor(() => {
      expect(resolveIncomingOrderReceivingIssue).toHaveBeenCalled();
    });
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ["connected-suppliers", "incoming-order", orderId],
    });
    expect(toastSuccess).toHaveBeenCalled();
  });

  it("renders resolved lines as read-only", () => {
    const issue = pendingIssue({
      status: "Resolved",
      unresolvedLineCount: 0,
      lines: [
        {
          ...pendingIssue().lines[0]!,
          isResolved: true,
          missingResolution: "FoundAtSeller",
          resolvedAtUtc: "2026-09-21T01:00:00Z",
          inventoryMovementId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        },
      ],
    });
    renderPanel([issue]);
    expect(screen.queryByRole("button", { name: "incomingOrders.receivingIssues.review" })).not.toBeInTheDocument();
    expect(screen.getByText(/FoundAtSeller/)).toBeInTheDocument();
    expect(screen.getByText(/incomingOrders.receivingIssues.stockRestored/)).toBeInTheDocument();
  });

  it("surfaces API errors via toast", async () => {
    const user = userEvent.setup();
    resolveIncomingOrderReceivingIssue.mockRejectedValue(new Error("server boom"));
    renderPanel([pendingIssue()]);
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.review" }));
    await user.selectOptions(
      screen.getByLabelText("incomingOrders.receivingIssues.chooseResolution"),
      "LostInTransit",
    );
    await user.click(screen.getByRole("button", { name: "incomingOrders.receivingIssues.confirm" }));
    await waitFor(() => {
      expect(toastError).toHaveBeenCalledWith("server boom");
    });
  });

  it("hides review actions when canManage is false", () => {
    renderPanel([pendingIssue()], false);
    expect(screen.queryByRole("button", { name: "incomingOrders.receivingIssues.review" })).not.toBeInTheDocument();
  });
});

import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { catalogs } from "@/i18n/messages";
import { IncomingOrderDetailPage } from "@/features/purchasing/IncomingOrderDetailPage";
import { IncomingOrdersListPage } from "@/features/purchasing/IncomingOrdersListPage";

const orgId = "22222222-2222-4222-8222-222222222222";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const cpoId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const productId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const productIdBanana = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Mica Store",
    branchId,
    branchName: "Iloilo",
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
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}));

const listIncomingOrders = vi.fn();
const getIncomingOrder = vi.fn();
const acceptIncomingOrder = vi.fn();
const declineIncomingOrder = vi.fn();
const prepareIncomingOrder = vi.fn();
const fulfillIncomingOrder = vi.fn();
const proposeIncomingOrderChanges = vi.fn();

vi.mock("@/api/pos/pos-connected-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-connected-suppliers-client")>();
  return {
    ...actual,
    listIncomingOrders: (...args: unknown[]) => listIncomingOrders(...args),
    getIncomingOrder: (...args: unknown[]) => getIncomingOrder(...args),
    acceptIncomingOrder: (...args: unknown[]) => acceptIncomingOrder(...args),
    declineIncomingOrder: (...args: unknown[]) => declineIncomingOrder(...args),
    prepareIncomingOrder: (...args: unknown[]) => prepareIncomingOrder(...args),
    fulfillIncomingOrder: (...args: unknown[]) => fulfillIncomingOrder(...args),
    proposeIncomingOrderChanges: (...args: unknown[]) => proposeIncomingOrderChanges(...args),
  };
});

function pendingOrder(status = "New", lines?: Array<Record<string, unknown>>) {
  const defaultLines = [
    {
      productId,
      nameSnapshot: "Bottled Water 500ml",
      skuSnapshot: "PH-BEV-WATER-500",
      qty: 20,
      unitPriceSnapshot: 12,
      lineTotal: 240,
      unitOfMeasureCode: "Piece",
      onHandQuantity: 40,
      reservedQuantity: 0,
      availableToPromise: 40,
      shortageWarning: false,
    },
  ];
  const resolvedLines = lines ?? defaultLines;
  const totalAmount = resolvedLines.reduce(
    (sum, line) => sum + Number(line.lineTotal ?? 0),
    0,
  );
  return {
    connectedPurchaseOrderId: cpoId,
    relationshipId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    buyerOrganizationId: "11111111-1111-4111-8111-111111111111",
    supplierOrganizationId: orgId,
    buyerPurchaseOrderId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
    buyerPoNumber: "PO-000123",
    orderDate: "2026-09-04",
    notes: null,
    status,
    totalAmount,
    createdAtUtc: "2026-09-04T00:00:00Z",
    updatedAtUtc: "2026-09-04T00:00:00Z",
    lines: resolvedLines,
    displayStatus: status,
    buyerDisplayName: "Paul Store",
    supplierBranchName: "Iloilo",
    paymentTerm: "Cash",
    paymentTermLabel: "Cash",
  };
}

function twoLineOrder(status = "New") {
  return pendingOrder(status, [
    {
      productId,
      nameSnapshot: "Apple",
      skuSnapshot: "PH-FRU-APPLE",
      qty: 5,
      unitPriceSnapshot: 180,
      lineTotal: 900,
      unitOfMeasureCode: "Kilogram",
      onHandQuantity: 10,
      reservedQuantity: 0,
      availableToPromise: 10,
      shortageWarning: false,
    },
    {
      productId: productIdBanana,
      nameSnapshot: "Banana Lakatan",
      skuSnapshot: "PH-FRU-BANANA",
      qty: 5,
      unitPriceSnapshot: 76,
      lineTotal: 380,
      unitOfMeasureCode: "Kilogram",
      onHandQuantity: 10,
      reservedQuantity: 0,
      availableToPromise: 10,
      shortageWarning: false,
    },
  ]);
}

function shortageOrder() {
  return pendingOrder("New", [
    {
      productId,
      nameSnapshot: "Apple",
      skuSnapshot: "PH-FRU-APPLE",
      qty: 4,
      unitPriceSnapshot: 180,
      lineTotal: 720,
      unitOfMeasureCode: "Kilogram",
      onHandQuantity: 5,
      reservedQuantity: 2,
      availableToPromise: 3,
      shortageWarning: true,
    },
    {
      productId: productIdBanana,
      nameSnapshot: "Banana Lakatan",
      skuSnapshot: "PH-FRU-BANANA",
      qty: 2,
      unitPriceSnapshot: 76,
      lineTotal: 152,
      unitOfMeasureCode: "Kilogram",
      onHandQuantity: 8,
      reservedQuantity: 0,
      availableToPromise: 8,
      shortageWarning: false,
    },
  ]);
}

function renderList() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, networkMode: "always" } },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/purchasing/incoming-orders"]}>
        <Routes>
          <Route path="/purchasing/incoming-orders" element={<IncomingOrdersListPage />} />
          <Route
            path="/purchasing/incoming-orders/:connectedPurchaseOrderId"
            element={<IncomingOrderDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderDetail() {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, networkMode: "always" },
      mutations: { networkMode: "always" },
    },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[`/purchasing/incoming-orders/${cpoId}`]}>
        <Routes>
          <Route
            path="/purchasing/incoming-orders/:connectedPurchaseOrderId"
            element={<IncomingOrderDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("IncomingOrders React flow", () => {
  beforeEach(() => {
    listIncomingOrders.mockReset();
    getIncomingOrder.mockReset();
    acceptIncomingOrder.mockReset();
    declineIncomingOrder.mockReset();
    prepareIncomingOrder.mockReset();
    fulfillIncomingOrder.mockReset();
    proposeIncomingOrderChanges.mockReset();
    listIncomingOrders.mockResolvedValue([pendingOrder()]);
    getIncomingOrder.mockResolvedValue(pendingOrder());
    acceptIncomingOrder.mockResolvedValue(pendingOrder("Accepted"));
    declineIncomingOrder.mockResolvedValue(pendingOrder("Declined"));
    prepareIncomingOrder.mockResolvedValue(pendingOrder("Preparing"));
    fulfillIncomingOrder.mockResolvedValue(pendingOrder("Fulfilled"));
    proposeIncomingOrderChanges.mockResolvedValue(pendingOrder("ChangesProposed"));
  });

  it("lists pending incoming POs separately from connection requests", async () => {
    renderList();
    await waitFor(() => expect(screen.getByTestId(`incoming-order-row-${cpoId}`)).toBeInTheDocument());
    expect(listIncomingOrders).toHaveBeenCalledWith(
      expect.objectContaining({ organizationId: orgId }),
      {},
      expect.anything(),
    );
    expect(screen.getAllByText("PO-000123").length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/Paul Store/).length).toBeGreaterThanOrEqual(1);
    expect(screen.queryByText("Incoming connection requests")).not.toBeInTheDocument();
    expect(screen.getByTestId("incoming-orders-filter-pending")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("incoming-orders-filter-pending")).toHaveAccessibleName(/Pending.*1/i);
    expect(screen.getByTestId("incoming-orders-filter-all")).not.toHaveAccessibleName(/All,\s*\d/i);
  });

  it("accepts a pending order and hides accept/decline", async () => {
    const user = userEvent.setup();
    getIncomingOrder
      .mockResolvedValueOnce(pendingOrder("New"))
      .mockResolvedValueOnce(pendingOrder("Accepted"));
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-accept"));
    expect(screen.getByTestId("incoming-order-stock-lines")).toBeInTheDocument();
    expect(screen.getByTestId(`incoming-order-stock-line-${productId}`)).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-stock-summary")).toHaveAttribute("data-tone", "available");
    expect(screen.getByRole("columnheader", { name: "Available stock" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Unit cost" })).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-total-amount")).toHaveTextContent("₱240.00");
    expect(screen.getByTestId("page-header-back-incoming-order-detail")).toHaveAttribute(
      "aria-label",
      "Back",
    );
    expect(screen.getByTestId("incoming-order-propose")).toBeDisabled();
    expect(screen.getByTestId("incoming-order-accept")).toBeEnabled();

    await user.click(screen.getByTestId("incoming-order-accept"));
    await waitFor(() => expect(acceptIncomingOrder).toHaveBeenCalled());
    await waitFor(() => expect(screen.queryByTestId("incoming-order-accept")).not.toBeInTheDocument());
    expect(screen.getByTestId("incoming-order-prepare")).toBeInTheDocument();
    expect(acceptIncomingOrder.mock.calls[0]![1]).toBe(cpoId);
  });

  it("declines with optional reason", async () => {
    const user = userEvent.setup();
    getIncomingOrder
      .mockResolvedValueOnce(pendingOrder("New"))
      .mockResolvedValueOnce(pendingOrder("Declined"));
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-decline"));
    await user.click(screen.getByTestId("incoming-order-decline"));
    await user.selectOptions(screen.getByTestId("incoming-order-decline-reason"), "OutOfStock");
    await user.click(screen.getByTestId("incoming-order-decline-confirm"));
    await waitFor(() => expect(declineIncomingOrder).toHaveBeenCalled());
    expect(declineIncomingOrder.mock.calls[0]![2]).toEqual({
      declineReason: "OutOfStock",
      declineNote: null,
    });
    await waitFor(() => expect(screen.queryByTestId("incoming-order-decline")).not.toBeInTheDocument());
  });

  it("renders buyer and commercial meta as summary cards", async () => {
    getIncomingOrder.mockResolvedValue({
      ...pendingOrder("Accepted"),
      buyerDisplayName: "Paul Store",
      supplierBranchName: "Main warehouse",
      paymentTermLabel: "Cash on delivery",
      paymentTiming: "PayOnDeliveryOrReceipt",
      orderDate: "2026-09-20",
      displayStatus: "PartiallyReceived",
      buyerOutstandingQty: 1,
      fulfilledAtUtc: "2026-09-17T01:00:00Z",
    });
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-summary"));
    expect(screen.getByTestId("incoming-order-summary-title")).toHaveTextContent("Order info");
    expect(screen.queryByTestId("incoming-order-summary-status")).not.toBeInTheDocument();
    expect(screen.queryByTestId("incoming-order-receiving")).not.toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-summary-buyer")).toHaveTextContent("Paul Store");
    expect(screen.getByTestId("incoming-order-summary-fulfill")).toHaveTextContent("Main warehouse");
    expect(screen.getByTestId("incoming-order-summary-payment")).toHaveTextContent("Cash on delivery");
    expect(screen.getByTestId("incoming-order-summary-paymentTiming")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-summary-orderDate")).toHaveTextContent("2026-09-20");
  });

  it("supports prepare then fulfill", async () => {
    const user = userEvent.setup();
    getIncomingOrder
      .mockResolvedValueOnce(pendingOrder("Accepted"))
      .mockResolvedValueOnce(pendingOrder("Preparing"))
      .mockResolvedValueOnce(pendingOrder("Fulfilled"));
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-prepare"));
    await user.click(screen.getByTestId("incoming-order-prepare"));
    await waitFor(() => screen.getByTestId("incoming-order-fulfill"));
    await user.click(screen.getByTestId("incoming-order-fulfill"));
    await waitFor(() => expect(fulfillIncomingOrder).toHaveBeenCalled());
    await waitFor(() => expect(screen.queryByTestId("incoming-order-fulfill")).not.toBeInTheDocument());
  });

  it("hides good/damaged columns and latest receipt on first prepare", async () => {
    getIncomingOrder.mockResolvedValue({
      ...pendingOrder("Accepted", [
        {
          productId,
          nameSnapshot: "Apple",
          skuSnapshot: "PH-FRU-APPLE",
          qty: 4,
          unitPriceSnapshot: 10,
          lineTotal: 40,
          unitOfMeasureCode: "Piece",
          orderedQty: 4,
          goodReceivedQty: 0,
          damagedQty: 0,
          missingQty: 0,
          outstandingQty: 4,
          remainingValue: 40,
        },
      ]),
      buyerOutstandingQty: 4,
      buyerReceipts: [],
    });
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-prepare"));
    expect(screen.getByTestId("incoming-order-lines")).toBeInTheDocument();
    expect(screen.queryByTestId("incoming-order-fulfillment-progress")).not.toBeInTheDocument();
    expect(screen.queryByTestId("incoming-order-latest-receipt")).not.toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Good received" })).not.toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Damaged" })).not.toBeInTheDocument();
  });

  it("renders stock review for pending orders and document lines after accept", async () => {
    getIncomingOrder.mockResolvedValue(twoLineOrder("New"));
    renderDetail();

    await waitFor(() => screen.getByTestId("incoming-order-stock-lines"));
    expect(screen.getByTestId("incoming-order-total-amount")).toHaveTextContent("₱1,280.00");
    expect(screen.getAllByText("Order total").length).toBeGreaterThanOrEqual(1);
    expect(screen.getByTestId(`incoming-order-stock-line-${productId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`incoming-order-stock-line-${productIdBanana}`)).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Unit cost" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Available stock" })).toBeInTheDocument();
    expect(screen.queryByText(/Available to promise/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("incoming-order-lines-search")).not.toBeInTheDocument();
    expect(screen.getByTestId("po-document-print")).toBeInTheDocument();
    expect(screen.getByTestId("po-document-export-menu")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-print-root")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-print-root").querySelector("input")).toBeNull();
    expect(screen.getByTestId("incoming-order-print-root")).not.toHaveTextContent("Accept order");
    expect(screen.getByTestId("incoming-order-accept-notice")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-stock-summary")).toHaveTextContent("Stock available");
  });

  it("highlights shortage rows, keeps available rows read-only, and blocks accept", async () => {
    const user = userEvent.setup();
    getIncomingOrder.mockResolvedValue(shortageOrder());
    proposeIncomingOrderChanges.mockResolvedValue(
      pendingOrder("ChangesProposed", shortageOrder().lines),
    );
    renderDetail();

    await waitFor(() => screen.getByTestId("incoming-order-stock-summary"));
    expect(screen.getByTestId("incoming-order-stock-summary")).toHaveAttribute("data-tone", "adjustment");
    expect(screen.getByTestId("incoming-order-stock-summary")).toHaveTextContent(
      "Stock adjustment required",
    );
    expect(screen.getByTestId("incoming-order-stock-summary")).toHaveTextContent(
      "1 of 2 items does not have enough available stock",
    );
    expect(screen.queryByText(/Requested 4 \/ Available/i)).not.toBeInTheDocument();

    const shortageRow = screen.getByTestId(`incoming-order-stock-line-${productId}`);
    expect(shortageRow).toHaveAttribute("data-shortage", "true");
    expect(screen.getByTestId(`incoming-order-requested-qty-${productId}`).className).toMatch(
      /warning/,
    );
    expect(screen.getByTestId(`incoming-order-available-stock-${productId}`)).toHaveTextContent(/3/);

    const confirmInput = screen.getByTestId(`incoming-order-confirm-qty-${productId}`);
    expect(confirmInput).toHaveValue("3");
    expect(screen.getByTestId(`incoming-order-stock-line-total-${productId}`)).toHaveTextContent(
      "₱540.00",
    );
    expect(screen.getByTestId("incoming-order-total-amount")).toHaveTextContent("₱692.00");
    expect(screen.getByTestId(`incoming-order-confirm-qty-${productIdBanana}`).tagName).not.toBe(
      "INPUT",
    );
    expect(screen.getByTestId(`incoming-order-confirm-qty-${productIdBanana}`)).toHaveTextContent(
      /2/,
    );

    expect(screen.getByTestId("incoming-order-accept")).toBeDisabled();
    expect(screen.getByTestId("incoming-order-propose")).toBeEnabled();
    expect(screen.queryByTestId("incoming-order-propose-validation")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("incoming-order-propose"));
    await waitFor(() => expect(proposeIncomingOrderChanges).toHaveBeenCalled());
    expect(proposeIncomingOrderChanges.mock.calls[0]![2]).toEqual({
      lines: [
        { productId, proposedQty: 3, unavailable: false },
        { productId: productIdBanana, proposedQty: 2, unavailable: false },
      ],
    });
  });

  it("shows propose validation only when proposing without material changes", async () => {
    const user = userEvent.setup();
    getIncomingOrder.mockResolvedValue(shortageOrder());
    renderDetail();

    await waitFor(() => screen.getByTestId(`incoming-order-confirm-qty-${productId}`));
    const confirmInput = screen.getByTestId(`incoming-order-confirm-qty-${productId}`);
    await user.clear(confirmInput);
    await user.type(confirmInput, "4");

    expect(screen.queryByText(/No material changes/i)).not.toBeInTheDocument();
    await user.click(screen.getByTestId("incoming-order-propose"));
    await waitFor(() =>
      expect(screen.getByTestId("incoming-order-propose-validation")).toHaveTextContent(
        /Adjust at least one confirmed quantity/i,
      ),
    );
    expect(proposeIncomingOrderChanges).not.toHaveBeenCalled();
  });

  it("shows authoritative proposal revision after ChangesProposed", async () => {
    const appleId = productId;
    const bananaId = productIdBanana;
    getIncomingOrder.mockResolvedValue({
      ...pendingOrder("ChangesProposed", [
        {
          productId: appleId,
          nameSnapshot: "Apple",
          skuSnapshot: "PH-FRU-APPLE",
          qty: 4,
          unitPriceSnapshot: 200,
          lineTotal: 800,
          unitOfMeasureCode: "Kilogram",
          proposedQty: 4,
          proposedLineTotal: 800,
          availability: "Available",
        },
        {
          productId: bananaId,
          nameSnapshot: "Banana Lakatan",
          skuSnapshot: "PH-FRU-BANANA",
          qty: 4,
          unitPriceSnapshot: 80,
          lineTotal: 320,
          unitOfMeasureCode: "Kilogram",
          proposedQty: 2,
          proposedLineTotal: 160,
          availability: "Available",
        },
      ]),
      totalAmount: 1120,
      proposedTotalAmount: 960,
      inventoryReservationState: "TemporaryProposal",
      inventoryReservationExpiresAtUtc: "2026-09-18T01:00:00Z",
      changesProposedAtUtc: "2026-09-17T01:00:00Z",
    });

    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-proposal"));
    expect(screen.getByTestId("incoming-order-proposal-banner")).toHaveTextContent(
      "Changes proposed",
    );
    expect(screen.getByTestId("incoming-order-proposal-banner")).toHaveTextContent(
      "Awaiting buyer review",
    );
    expect(screen.getByTestId("incoming-order-proposal-reserved-until")).toBeInTheDocument();
    expect(screen.getByTestId(`incoming-order-proposal-proposed-qty-${bananaId}`)).toHaveTextContent(
      /2.*Kg/i,
    );
    expect(screen.getByTestId(`incoming-order-proposal-line-total-${bananaId}`)).toHaveTextContent(
      "₱160.00",
    );
    expect(screen.getByTestId(`incoming-order-proposal-line-total-${appleId}`)).toHaveTextContent(
      "₱800.00",
    );
    expect(screen.getByTestId("incoming-order-proposal-original-total")).toHaveTextContent(
      "₱1,120.00",
    );
    expect(screen.getByTestId("incoming-order-proposal-proposed-total")).toHaveTextContent(
      "₱960.00",
    );
    expect(screen.queryByText("ChangesProposed")).not.toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-withdraw-proposal")).toBeInTheDocument();
    expect(screen.queryByTestId("incoming-order-accept")).not.toBeInTheDocument();
  });

  it("shows fulfillment progress, latest receipt, and hides raw PartiallyReceived status", async () => {
    const receiptId = "99999999-9999-4999-8999-999999999999";
    getIncomingOrder.mockResolvedValue({
      ...pendingOrder("Accepted", [
        {
          productId,
          nameSnapshot: "Apple",
          skuSnapshot: "PH-FRU-APPLE",
          qty: 4,
          unitPriceSnapshot: 10,
          lineTotal: 40,
          unitOfMeasureCode: "Piece",
          orderedQty: 4,
          goodReceivedQty: 3,
          damagedQty: 1,
          missingQty: 0,
          outstandingQty: 1,
          remainingValue: 10,
        },
        {
          productId: productIdBanana,
          nameSnapshot: "Banana",
          skuSnapshot: "PH-FRU-BANANA",
          qty: 4,
          unitPriceSnapshot: 8,
          lineTotal: 32,
          unitOfMeasureCode: "Piece",
          orderedQty: 4,
          goodReceivedQty: 2,
          damagedQty: 0,
          missingQty: 0,
          outstandingQty: 2,
          remainingValue: 16,
        },
      ]),
      displayStatus: "PartiallyReceived",
      buyerReceivingStatus: "PartiallyReceived",
      buyerOutstandingQty: 3,
      fulfilledAtUtc: "2026-09-17T01:00:00Z",
      buyerReceipts: [
        {
          goodsReceiptId: receiptId,
          grnNumber: "260917-001",
          receivedDate: "2026-09-17",
          receivedAtUtc: "2026-09-17T12:00:00Z",
          deliveryReference: "DRV-1",
          notes: "Partial drop",
          status: "Posted",
          goodQtyTotal: 5,
          damagedQtyTotal: 1,
          missingQtyTotal: 0,
          cancelledRemainingTotal: 0,
          lines: [],
        },
      ],
    });

    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-fulfillment-progress"));
    expect(screen.getAllByText("Partially received").length).toBeGreaterThan(0);
    expect(screen.queryByText("PartiallyReceived")).not.toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Good received" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Damaged" })).toBeInTheDocument();
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-good-${productId}`)).toHaveTextContent("3");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-damaged-${productId}`)).toHaveTextContent("1");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-outstanding-${productId}`)).toHaveTextContent(
      "1",
    );
    expect(
      screen.getByTestId(`incoming-order-fulfillment-progress-outstanding-${productIdBanana}`),
    ).toHaveTextContent("2");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-remaining-value-${productId}`)).toHaveTextContent(
      "₱10.00",
    );
    expect(screen.getByTestId("incoming-order-latest-receipt")).toBeInTheDocument();
    expect(screen.getByText(/260917-001/)).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-view-latest-receipt")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-prepare-remaining")).toBeInTheDocument();
  });

  it("keeps fulfillment progress columns while preparing remaining", async () => {
    getIncomingOrder.mockResolvedValue({
      ...pendingOrder("Preparing", [
        {
          productId,
          nameSnapshot: "Apple",
          skuSnapshot: "PH-FRU-APPLE",
          qty: 4,
          unitPriceSnapshot: 10,
          lineTotal: 40,
          unitOfMeasureCode: "Piece",
          orderedQty: 4,
          goodReceivedQty: 3,
          damagedQty: 1,
          missingQty: 0,
          outstandingQty: 1,
          remainingValue: 10,
        },
      ]),
      displayStatus: "Preparing",
      buyerReceivingStatus: "PartiallyReceived",
      buyerOutstandingQty: 1,
      fulfilledAtUtc: "2026-09-17T01:00:00Z",
      buyerReceipts: [
        {
          goodsReceiptId: "99999999-9999-4999-8999-999999999999",
          grnNumber: "260917-001",
          receivedDate: "2026-09-17",
          receivedAtUtc: "2026-09-17T12:00:00Z",
          deliveryReference: null,
          notes: null,
          status: "Posted",
          goodQtyTotal: 3,
          damagedQtyTotal: 1,
          missingQtyTotal: 0,
          cancelledRemainingTotal: 0,
          lines: [],
        },
      ],
    });

    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-fulfillment-progress"));
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-good-${productId}`)).toHaveTextContent("3");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-damaged-${productId}`)).toHaveTextContent("1");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-outstanding-${productId}`)).toHaveTextContent(
      "1",
    );
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-remaining-value-${productId}`)).toHaveTextContent(
      "₱10.00",
    );
    expect(screen.getByTestId("incoming-order-latest-receipt")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-fulfill")).toHaveTextContent("Mark remaining ready");
  });

  it("confirms mark remaining ready with outstanding quantities only", async () => {
    const user = userEvent.setup();
    const preparingRemaining = {
      ...pendingOrder("Preparing", [
        {
          productId,
          nameSnapshot: "Apple",
          skuSnapshot: "PH-FRU-APPLE",
          qty: 4,
          unitPriceSnapshot: 10,
          lineTotal: 40,
          unitOfMeasureCode: "Piece",
          orderedQty: 4,
          goodReceivedQty: 3,
          damagedQty: 1,
          outstandingQty: 1,
          remainingValue: 10,
        },
        {
          productId: productIdBanana,
          nameSnapshot: "Banana",
          skuSnapshot: "PH-FRU-BANANA",
          qty: 4,
          unitPriceSnapshot: 8,
          lineTotal: 32,
          unitOfMeasureCode: "Piece",
          orderedQty: 4,
          goodReceivedQty: 2,
          damagedQty: 0,
          outstandingQty: 2,
          remainingValue: 16,
        },
      ]),
      displayStatus: "PartiallyReceived",
      buyerOutstandingQty: 3,
      fulfilledAtUtc: "2026-09-17T01:00:00Z",
    };
    getIncomingOrder
      .mockResolvedValueOnce(preparingRemaining)
      .mockResolvedValueOnce({
        ...preparingRemaining,
        status: "Fulfilled",
        displayStatus: "AwaitingBuyerReceipt",
      });
    fulfillIncomingOrder.mockResolvedValue({
      ...preparingRemaining,
      status: "Fulfilled",
      displayStatus: "AwaitingBuyerReceipt",
    });

    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-fulfill"));
    expect(screen.getByTestId("incoming-order-fulfill")).toHaveTextContent("Mark remaining ready");
    await user.click(screen.getByTestId("incoming-order-fulfill"));
    await waitFor(() => screen.getByTestId("incoming-order-mark-remaining-confirm"));
    expect(screen.getByTestId(`incoming-order-remaining-line-${productId}`)).toHaveTextContent("1");
    expect(screen.getByTestId(`incoming-order-remaining-line-${productIdBanana}`)).toHaveTextContent("2");
    expect(screen.getByTestId("incoming-order-remaining-total")).toHaveTextContent("3");
    await user.click(screen.getByTestId("incoming-order-mark-remaining-confirm-btn"));
    await waitFor(() => expect(fulfillIncomingOrder).toHaveBeenCalled());
  });
});

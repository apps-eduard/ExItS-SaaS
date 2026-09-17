import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { RecordPaymentModal } from "@/features/customers/RecordPaymentModal";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: vi.fn() }),
}));

vi.mock("@/workspace/use-pos-workspace-scope", () => ({
  usePosWorkspaceScope: () => ({
    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  }),
}));

vi.mock("@/api/pos/pos-customers-client", () => ({
  createCustomerRepayment: vi.fn(),
  UTANG_REPAYMENT_PAYMENT_METHODS: ["Cash", "ManualGCash", "Check"],
}));

const listBusinessCustomerReceivables = vi.fn();
const createBusinessCustomerRepayment = vi.fn();

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  createBusinessCustomerRepayment: (...args: unknown[]) =>
    createBusinessCustomerRepayment(...args),
  listBusinessCustomerReceivables: (...args: unknown[]) =>
    listBusinessCustomerReceivables(...args),
}));

function wrap(ui: ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

const openReceivables = [
  {
    creditEntryId: "11111111-1111-4111-8111-111111111111",
    sourceType: "PO",
    sourceReference: "PO-9",
    dueDate: "2026-09-01",
    outstandingBalance: 300,
    originalAmount: 300,
    createdAtUtc: "2026-08-01T00:00:00Z",
    remarks: null,
  },
  {
    creditEntryId: "22222222-2222-4222-8222-222222222222",
    sourceType: "DirectPurchase",
    sourceReference: "DR-1",
    dueDate: "2026-09-20",
    outstandingBalance: 447,
    originalAmount: 447,
    createdAtUtc: "2026-08-15T00:00:00Z",
    remarks: null,
  },
];

describe("RecordPaymentModal", () => {
  beforeEach(() => {
    listBusinessCustomerReceivables.mockReset();
    createBusinessCustomerRepayment.mockReset();
    listBusinessCustomerReceivables.mockResolvedValue(openReceivables);
  });

  it("keeps over-outstanding amount, shows inline max error, Exact fixes it", async () => {
    const user = userEvent.setup();
    wrap(
      <RecordPaymentModal
        open
        onOpenChange={vi.fn()}
        customerKind="business"
        connectionId="cccccccc-cccc-4ccc-8ccc-cccccccccccc"
        displayName="Kizy Bakery"
        outstandingBalance={747}
        onSuccess={vi.fn()}
      />,
    );

    await screen.findByTestId("record-payment-allocation");

    const amount = screen.getByTestId("record-payment-amount");
    await user.type(amount, "1000");
    await user.tab();

    expect(amount).toHaveValue("1,000.00");
    expect(screen.getByTestId("record-payment-amount-owed")).toHaveTextContent("customers.amountOwed");
    expect(screen.getByTestId("record-payment-exceeds-warning")).toHaveTextContent(
      "customers.paymentExceedsOutstanding",
    );
    expect(screen.getByTestId("record-payment-submit")).toBeDisabled();

    await user.click(screen.getByTestId("record-payment-exact"));
    expect(amount).toHaveValue("747.00");
    expect(screen.queryByTestId("record-payment-exceeds-warning")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("record-payment-submit")).not.toBeDisabled();
    });
  });

  it("shows business allocation preview for automatic FIFO lines", async () => {
    const user = userEvent.setup();
    wrap(
      <RecordPaymentModal
        open
        onOpenChange={vi.fn()}
        customerKind="business"
        connectionId="cccccccc-cccc-4ccc-8ccc-cccccccccccc"
        displayName="Kizy Bakery"
        outstandingBalance={747}
        onSuccess={vi.fn()}
      />,
    );

    await screen.findByTestId("record-payment-allocate-manually");

    await user.click(screen.getByTestId("record-payment-exact"));

    expect(await screen.findByTestId("record-payment-allocation-preview")).toBeInTheDocument();
    expect(
      screen.getByTestId(`record-payment-allocation-line-${openReceivables[0]!.creditEntryId}`),
    ).toHaveTextContent("customers.receivables.amountApplied");
    expect(screen.getByTestId("record-payment-allocate-manually")).toBeInTheDocument();
  });
});

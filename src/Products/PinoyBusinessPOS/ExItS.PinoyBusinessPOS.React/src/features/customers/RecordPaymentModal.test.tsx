import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
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

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  createBusinessCustomerRepayment: vi.fn(),
}));

function wrap(ui: ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe("RecordPaymentModal", () => {
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
    expect(screen.getByTestId("record-payment-submit")).not.toBeDisabled();
  });
});

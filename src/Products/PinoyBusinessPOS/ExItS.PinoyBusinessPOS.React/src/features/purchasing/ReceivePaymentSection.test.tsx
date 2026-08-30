import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { AppProviders } from "@/app/providers";
import { ReceivePaymentSection } from "@/features/purchasing/ReceivePaymentSection";
import type {
  ReceivePaymentMethodCode,
  ReceivePaymentMode,
} from "@/features/purchasing/receive-payment";

function Harness({
  total = 10000,
  allowSupplierCredit = true,
  initialMode = "paidInFull" as ReceivePaymentMode,
}: {
  total?: number;
  allowSupplierCredit?: boolean;
  initialMode?: ReceivePaymentMode;
}) {
  const [mode, setMode] = useState<ReceivePaymentMode>(initialMode);
  const [paidNowText, setPaidNowText] = useState(String(total));
  const [dueDate, setDueDate] = useState("");
  const [paymentMethod, setPaymentMethod] = useState<ReceivePaymentMethodCode>("Cash");
  const paidNowValue =
    mode === "paidInFull" ? total : Number(paidNowText.replace(/,/g, "")) || 0;

  return (
    <AppProviders>
      <ReceivePaymentSection
        estimatedTotal={total}
        mode={mode}
        onModeChange={(next) => {
          setMode(next);
          if (next === "paidInFull") {
            setPaidNowText(String(total));
            setDueDate("");
          }
        }}
        paidNowText={paidNowText}
        onPaidNowChange={setPaidNowText}
        paymentMethod={paymentMethod}
        onPaymentMethodChange={setPaymentMethod}
        dueDate={dueDate}
        onDueDateChange={setDueDate}
        paidNowValue={paidNowValue}
        allowSupplierCredit={allowSupplierCredit}
      />
    </AppProviders>
  );
}

describe("ReceivePaymentSection", () => {
  it("defaults paid-in-full PaidNow to receipt total and hides due date", () => {
    render(<Harness total={10000} />);
    expect(screen.getByTestId("receive-payment-paid-preview")).toHaveTextContent("10,000.00");
    expect(screen.getByTestId("receive-payment-remaining")).toHaveTextContent("0.00");
    expect(screen.queryByTestId("receive-payment-due-date")).not.toBeInTheDocument();
    expect(screen.queryByTestId("receive-payment-paid-now")).not.toBeInTheDocument();
    expect(screen.getByTestId("receive-payment-method")).toBeInTheDocument();
  });

  it("allows supplier credit partial PaidNow with live balance preview", async () => {
    const user = userEvent.setup();
    render(<Harness total={10000} />);
    await user.click(screen.getByTestId("receive-payment-mode-credit"));
    const paidInput = screen.getByTestId("receive-payment-paid-now");
    await user.clear(paidInput);
    await user.type(paidInput, "4000");
    expect(screen.getByTestId("receive-payment-remaining")).toHaveTextContent("6,000.00");
    expect(screen.getByTestId("receive-payment-due-date")).toBeInTheDocument();
  });

  it("keeps due date optional in credit mode", async () => {
    const user = userEvent.setup();
    render(<Harness total={500} initialMode="supplierCredit" />);
    const paidInput = screen.getByTestId("receive-payment-paid-now");
    await user.clear(paidInput);
    await user.type(paidInput, "200");
    expect(screen.getByTestId("receive-payment-due-date")).toBeInTheDocument();
    expect(screen.getByTestId("receive-payment-due-date")).toHaveValue("");
    await user.type(screen.getByTestId("receive-payment-due-date"), "2026-09-30");
    expect(screen.getByTestId("receive-payment-due-date")).toHaveValue("2026-09-30");
  });

  it("hides supplier credit mode when allowSupplierCredit is false", () => {
    render(<Harness allowSupplierCredit={false} />);
    expect(screen.queryByTestId("receive-payment-mode-credit")).not.toBeInTheDocument();
  });

  it("does not mention SupplierPayablePayment in the receive UI", () => {
    render(<Harness />);
    expect(screen.getByTestId("receive-payment-section").textContent).not.toMatch(
      /SupplierPayablePayment/i,
    );
  });

  it("exposes payment method options matching backend codes", async () => {
    const user = userEvent.setup();
    render(<Harness total={100} />);
    const select = screen.getByTestId("receive-payment-method");
    await user.selectOptions(select, "BankTransfer");
    expect(select).toHaveValue("BankTransfer");
    await user.selectOptions(select, "GCash");
    expect(select).toHaveValue("GCash");
    await user.selectOptions(select, "Other");
    expect(select).toHaveValue("Other");
  });
});

describe("ReceivePaymentSection callbacks", () => {
  it("notifies parent of mode and method changes", async () => {
    const user = userEvent.setup();
    const onModeChange = vi.fn();
    const onPaymentMethodChange = vi.fn();
    render(
      <AppProviders>
        <ReceivePaymentSection
          estimatedTotal={200}
          mode="paidInFull"
          onModeChange={onModeChange}
          paidNowText="200"
          onPaidNowChange={vi.fn()}
          paymentMethod="Cash"
          onPaymentMethodChange={onPaymentMethodChange}
          dueDate=""
          onDueDateChange={vi.fn()}
          paidNowValue={200}
        />
      </AppProviders>,
    );
    await user.click(screen.getByTestId("receive-payment-mode-credit"));
    expect(onModeChange).toHaveBeenCalledWith("supplierCredit");
    await user.selectOptions(screen.getByTestId("receive-payment-method"), "GCash");
    expect(onPaymentMethodChange).toHaveBeenCalledWith("GCash");
  });
});

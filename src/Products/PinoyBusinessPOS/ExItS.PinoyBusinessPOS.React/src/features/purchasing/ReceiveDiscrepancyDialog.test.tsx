import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ReceiveDiscrepancyDialog } from "@/features/purchasing/ReceiveDiscrepancyDialog";
import { useState } from "react";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

function Harness() {
  const [lines, setLines] = useState([
    {
      productId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      name: "Rice",
      uom: "Kg",
      outstandingQty: 5,
      goodQty: 3,
      damagedText: "0",
      notDeliveredText: "0",
      otherText: "0",
      otherReasonCode: "",
      otherReasonText: "",
      remarksText: "",
    },
  ]);

  return (
    <ReceiveDiscrepancyDialog
      open
      lines={lines}
      onChangeLine={(productId, patch) => {
        setLines((prev) =>
          prev.map((line) => (line.productId === productId ? { ...line, ...patch } : line)),
        );
      }}
      onCancel={() => undefined}
      onConfirm={() => undefined}
      title="Classify discrepancy"
      classifyHint="Classify the remaining quantity."
      allDamagedLabel="All damaged"
      allNotDeliveredLabel="All not delivered"
      allOtherLabel="Other"
      damagedLabel="Damaged"
      notDeliveredLabel="Not delivered"
      otherLabel="Other qty"
      otherReasonLabel="Reason"
      otherReasons={[
        { value: "WrongItem", label: "Wrong item" },
        { value: "Other", label: "Other" },
      ]}
      otherDescriptionLabel="Short description"
      remarksLabel="Note"
      remarksRequiredLabel="Required"
      remainingToClassifyLabel="Remaining to classify: {qty}"
      cancelLabel="Cancel"
      confirmLabel="Confirm"
      notAcceptedTemplate="{qty} was not accepted"
    />
  );
}

describe("ReceiveDiscrepancyDialog", () => {
  it("blocks confirm until fully classified and supports quick actions", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const productId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    expect(screen.getByTestId("receive-discrepancy-not-accepted")).toHaveTextContent(/was not accepted/i);

    await user.click(screen.getByTestId("receive-discrepancy-quick-fill-option-damaged"));
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    await user.type(screen.getByTestId(`receive-discrepancy-remarks-${productId}`), "Broken bag");
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeEnabled();

    await user.click(screen.getByTestId("receive-discrepancy-quick-fill-option-notDelivered"));
    expect(screen.getByTestId(`receive-discrepancy-not-delivered-${productId}`)).toHaveValue("2");
    expect(screen.queryByTestId(`receive-discrepancy-other-panel-${productId}`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId("receive-discrepancy-quick-fill-option-other"));
    expect(screen.getByTestId(`receive-discrepancy-other-qty-${productId}`)).toHaveValue("2");
    expect(screen.getByTestId(`receive-discrepancy-other-panel-${productId}`)).toBeInTheDocument();
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    await user.selectOptions(screen.getByTestId(`receive-discrepancy-other-reason-${productId}`), "WrongItem");
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeEnabled();
    expect(screen.getByTestId("receive-discrepancy-qty-row")).toBeInTheDocument();
    expect(screen.getByText("*")).toBeInTheDocument();
  });
});
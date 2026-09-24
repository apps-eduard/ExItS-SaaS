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
        { value: "Expired", label: "Expired" },
        { value: "Other", label: "Other" },
      ]}
      otherDescriptionLabel="Short description"
      remarksLabel="Note"
      remarksRequiredLabel="Required"
      remainingToClassifyLabel="Remaining to classify: {qty}"
      cancelLabel="Cancel"
      confirmLabel="Confirm"
      notAcceptedTemplate="{qty} was not accepted"
      actualProductLabel="Actual item"
      renderActualProductPicker={(line) => (
        <button
          type="button"
          data-testid="pick-actual-product"
          onClick={() =>
            setLines((prev) =>
              prev.map((entry) =>
                entry.productId === line.productId
                  ? {
                      ...entry,
                      actualReceivedProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
                      actualReceivedProductName: "Apple Green",
                    }
                  : entry,
              ),
            )
          }
        >
          Pick actual
        </button>
      )}
    />
  );
}

describe("ReceiveDiscrepancyDialog", () => {
  it("opens as a left drawer with Other stepper always visible but disabled until Other is chosen", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const productId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

    const drawer = screen.getByTestId("receive-discrepancy-dialog");
    expect(drawer).toHaveAttribute("data-side", "left");
    expect(screen.getByTestId("receive-discrepancy-cancel")).toBeInTheDocument();
    expect(screen.getByTestId("receive-discrepancy-product-card")).toHaveTextContent("Rice");
    expect(screen.getByText("Classify as")).toHaveClass("font-bold");

    const otherQty = screen.getByTestId(`receive-discrepancy-other-qty-${productId}`);
    expect(otherQty).toBeInTheDocument();
    expect(otherQty).toBeDisabled();
    expect(screen.queryByTestId(`receive-discrepancy-other-panel-${productId}`)).not.toBeInTheDocument();

    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    expect(screen.getByTestId("receive-discrepancy-not-accepted")).toHaveTextContent(/was not accepted/i);

    await user.click(screen.getByTestId("receive-discrepancy-quick-fill-option-damaged"));
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    expect(otherQty).toBeDisabled();
    await user.type(screen.getByTestId(`receive-discrepancy-remarks-${productId}`), "Broken bag");
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeEnabled();

    await user.click(screen.getByTestId("receive-discrepancy-quick-fill-option-notDelivered"));
    expect(screen.getByTestId(`receive-discrepancy-not-delivered-${productId}`)).toHaveValue("2");
    expect(otherQty).toBeDisabled();
    expect(screen.queryByTestId(`receive-discrepancy-other-panel-${productId}`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId("receive-discrepancy-quick-fill-option-other"));
    expect(otherQty).toBeEnabled();
    expect(otherQty).toHaveValue("2");
    expect(screen.getByTestId(`receive-discrepancy-other-panel-${productId}`)).toBeInTheDocument();

    // When Other is selected, Damaged + Not delivered + Other cannot exceed discrepancy (2).
    const damagedQty = screen.getByTestId(`receive-discrepancy-damaged-${productId}`);
    const damagedStepper = damagedQty.closest("[data-testid='quantity-stepper']")!;
    const damagedPlus = damagedStepper.querySelector(".quantity-stepper__btn--plus");
    expect(damagedPlus).toBeDisabled();

    const otherStepper = otherQty.closest("[data-testid='quantity-stepper']")!;
    await user.click(otherStepper.querySelector(".quantity-stepper__btn--minus")!);
    expect(otherQty).toHaveValue("1");
    expect(damagedPlus).toBeEnabled();
    await user.click(damagedPlus!);
    expect(damagedQty).toHaveValue("1");

    const notDeliveredQty = screen.getByTestId(`receive-discrepancy-not-delivered-${productId}`);
    const notDeliveredPlus = notDeliveredQty
      .closest("[data-testid='quantity-stepper']")!
      .querySelector(".quantity-stepper__btn--plus");
    expect(notDeliveredPlus).toBeDisabled();

    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    await user.selectOptions(screen.getByTestId(`receive-discrepancy-other-reason-${productId}`), "WrongItem");
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeDisabled();
    expect(
      screen.queryByTestId(`receive-discrepancy-product-card-actual-${productId}`),
    ).not.toBeInTheDocument();

    await user.click(screen.getByTestId("pick-actual-product"));
    expect(screen.getByTestId(`receive-discrepancy-product-card-actual-${productId}`)).toHaveTextContent(
      /Apple Green/,
    );

    await user.selectOptions(screen.getByTestId(`receive-discrepancy-other-reason-${productId}`), "Expired");
    expect(screen.getByTestId("receive-discrepancy-confirm")).toBeEnabled();
    expect(screen.getByTestId("receive-discrepancy-qty-row")).toBeInTheDocument();
    expect(screen.getByText("*")).toBeInTheDocument();
  });
});
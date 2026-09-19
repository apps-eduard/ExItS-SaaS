import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  QuantityStepper,
  QUANTITY_STEPPER_INPUT_MAX_CH,
  QUANTITY_STEPPER_INPUT_MIN_CH,
  quantityStepperInputWidthCh,
} from "@/components/exits/MoneyQuantity";

describe("QuantityStepper editable mode", () => {
  it("increments and decrements with plus/minus", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );

    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(3);
    rerender(
      <QuantityStepper
        value={3}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(2);
  });

  it("disables minus at minimum and never emits zero", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByLabelText("Decrease")).toBeDisabled();
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).not.toHaveBeenCalled();
  });

  it("normalizes zero/negative manual input to minimum on blur", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "0");
    expect(onChange).not.toHaveBeenCalledWith(0);
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1);
  });

  it("steps 1.5 Kg down to 0.5 (fraction below one allowed)", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1.5");
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(0.5);
  });

  it("allows direct typing for whole units", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "4");
    expect(onChange).not.toHaveBeenCalled();
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(4);
  });

  it("allows typing 1.5 for Kg and commits on blur", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.5");
    expect(input).toHaveValue("1.5");
    expect(onChange).not.toHaveBeenCalled();
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1.5);
  });

  it("keeps transient decimal draft while typing", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.");
    expect(input).toHaveValue("1.");
    expect(onChange).not.toHaveBeenCalled();
    await user.type(input, "2");
    expect(input).toHaveValue("1.2");
  });

  it("blocks a third decimal digit while typing and on paste", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.255");
    expect(input).toHaveValue("1.25");
    expect(onChange).not.toHaveBeenCalled();
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1.25);

    await user.clear(input);
    await user.click(input);
    await user.paste("2.999");
    expect(input).not.toHaveValue("2.999");
    expect(onChange).not.toHaveBeenCalledWith(2.999);
  });

  it("preserves decimal remainder when stepping by whole units", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(2.5);
    rerender(
      <QuantityStepper
        value={2.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(1.5);
  });

  it("rejects decimal typing for whole Pack units", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        unit="Pack"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.");
    expect(input).toHaveValue("1");
    expect(onChange).not.toHaveBeenCalled();

    await user.clear(input);
    await user.click(input);
    await user.paste("1.5");
    // Invalid paste is ignored; draft stays whatever was focused/cleared (never 1.5).
    expect(input).not.toHaveValue("1.5");
    expect(onChange).not.toHaveBeenCalledWith(1.5);
    await user.tab();
    expect(onChange.mock.calls.every((call) => call[0] !== 1.5)).toBe(true);
  });

  it("steps +1/-1 from typed decimal without losing fraction", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.5");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1.5);

    rerender(
      <QuantityStepper
        value={1.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(2.5);

    rerender(
      <QuantityStepper
        value={2.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(1.5);
  });

  it("rejects zero on blur and restores measured minimum 0.01", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    expect(input).toHaveValue("1");
    await user.clear(input);
    await user.type(input, "0");
    expect(onChange).not.toHaveBeenCalled();
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(0.01);
  });

  it("commits measured fractions below one without snapping to 1", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "0.5");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(0.5);

    rerender(
      <QuantityStepper
        value={0.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("0.5");

    await user.clear(screen.getByTestId("qty"));
    await user.type(screen.getByTestId("qty"), "0.25");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(0.25);

    rerender(
      <QuantityStepper
        value={0.25}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("0.25");
  });

  it("accepts typed measured decimals and strips trailing zeros on commit display", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1");
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.25");
    expect(input).toHaveValue("1.25");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1.25);

    rerender(
      <QuantityStepper
        value={1.25}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1.25");

    await user.clear(screen.getByTestId("qty"));
    await user.type(screen.getByTestId("qty"), "1.50");
    expect(screen.getByTestId("qty")).toHaveValue("1.50");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1.5);

    rerender(
      <QuantityStepper
        value={1.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1.5");
    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(2.5);

    rerender(
      <QuantityStepper
        value={2.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("2.5");
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(1.5);
  });

  it("commits 1250.5 as 1,250.5 for measured display", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1250.5");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1250.5);
    rerender(
      <QuantityStepper
        value={1250.5}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1,250.5");
  });

  it("supports ArrowUp/ArrowDown and disabled state", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.click(input);
    await user.keyboard("{ArrowUp}");
    expect(onChange).toHaveBeenLastCalledWith(3);
    await user.keyboard("{ArrowDown}");
    expect(onChange).toHaveBeenLastCalledWith(1);

    rerender(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        disabled
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByLabelText("Increase")).toBeDisabled();
    expect(screen.getByLabelText("Decrease")).toBeDisabled();
    expect(screen.getByTestId("qty")).toBeDisabled();
  });

  it("keeps legacy callback mode for display-only value", async () => {
    const user = userEvent.setup();
    const onIncrement = vi.fn();
    const onDecrement = vi.fn();
    render(
      <QuantityStepper
        value={2}
        increaseLabel="Increase"
        decreaseLabel="Decrease"
        onIncrement={onIncrement}
        onDecrement={onDecrement}
      />,
    );
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
    await user.click(screen.getByLabelText("Increase"));
    await user.click(screen.getByLabelText("Decrease"));
    expect(onIncrement).toHaveBeenCalledOnce();
    expect(onDecrement).toHaveBeenCalledOnce();
  });

  it("auto-widens input from typed text and clamps long values", async () => {
    expect(quantityStepperInputWidthCh("1")).toBe(QUANTITY_STEPPER_INPUT_MIN_CH);
    expect(quantityStepperInputWidthCh("1.5")).toBe(QUANTITY_STEPPER_INPUT_MIN_CH);
    expect(quantityStepperInputWidthCh("11.50")).toBe(QUANTITY_STEPPER_INPUT_MIN_CH);
    expect(quantityStepperInputWidthCh("123456789012345")).toBeGreaterThan(
      QUANTITY_STEPPER_INPUT_MIN_CH,
    );
    expect(quantityStepperInputWidthCh("123456789012345678901234567890")).toBe(
      QUANTITY_STEPPER_INPUT_MAX_CH,
    );

    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    expect(input).toHaveStyle({ width: `${QUANTITY_STEPPER_INPUT_MIN_CH}ch` });

    await user.clear(input);
    await user.type(input, "1.");
    expect(input).toHaveValue("1.");
    expect(input).toHaveStyle({
      width: `${quantityStepperInputWidthCh("1.")}ch`,
    });

    await user.type(input, "5");
    expect(input).toHaveValue("1.5");
    expect(input).toHaveStyle({
      width: `${quantityStepperInputWidthCh("1.5")}ch`,
    });
  });

  it("formats thousands with commas and normalizes empty/0/1 via minus", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1000}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        unit="Pack"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1,000");

    await user.clear(screen.getByTestId("qty"));
    expect(screen.getByTestId("qty")).toHaveValue("");
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(1);

    rerender(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        unit="Pack"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "0");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1);

    expect(screen.getByLabelText("Decrease")).toBeDisabled();
  });

  it("resolves whole vs divisible precision from catalog unitOfMeasure/sellingMode", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        unitOfMeasure="Kilogram"
        sellingMode="ByWeight"
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.25");
    await user.tab();
    expect(onChange).toHaveBeenLastCalledWith(1.25);

    rerender(
      <QuantityStepper
        value={1}
        onChange={onChange}
        unitOfMeasure="Pack"
        sellingMode="PerItem"
        unit="Pack"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const packInput = screen.getByTestId("qty");
    await user.clear(packInput);
    await user.type(packInput, "1.5");
    expect(packInput).not.toHaveValue("1.5");
    await user.tab();
    expect(onChange.mock.calls.every((call) => call[0] !== 1.5)).toBe(true);
  });

  it("displays committed large decimals with thousands separators", () => {
    render(
      <QuantityStepper
        value={1250.25}
        onChange={() => undefined}
        min={0.01}
        step={1}
        precision={2}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByTestId("qty")).toHaveValue("1,250.25");
  });

  it("editOnClick idle mode still steps via onChange without parent ± handlers", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        editOnClick
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );

    // Idle: center is a button, not an input.
    expect(screen.getByTestId("qty")).toHaveRole("button");
    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(3);

    rerender(
      <QuantityStepper
        value={3}
        onChange={onChange}
        editOnClick
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(2);

    rerender(
      <QuantityStepper
        value={2}
        onChange={onChange}
        editOnClick
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );

    // Middle click still opens the input.
    await user.click(screen.getByTestId("qty"));
    expect(screen.getByTestId("qty")).toHaveValue("2");
  });

  it("uses adaptive kg line steps for Kilogram onChange (1→0.9, floor at 0.01)", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        editOnClick
        unitOfMeasure="Kilogram"
        sellingMode="PerItem"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );

    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(0.9);

    rerender(
      <QuantityStepper
        value={0.9}
        onChange={onChange}
        editOnClick
        unitOfMeasure="Kilogram"
        sellingMode="PerItem"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(0.8);

    rerender(
      <QuantityStepper
        value={0.9}
        onChange={onChange}
        editOnClick
        unitOfMeasure="Kilogram"
        sellingMode="PerItem"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(1.9);

    rerender(
      <QuantityStepper
        value={0.01}
        onChange={onChange}
        editOnClick
        unitOfMeasure="Kilogram"
        sellingMode="PerItem"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueClickLabel="Edit qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByLabelText("Decrease")).toBeDisabled();
  });
});

